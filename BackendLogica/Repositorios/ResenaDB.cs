using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using BackendLogica.Seguridad;

namespace BackendLogica.Repositorios
{
    /// <summary>
    /// Reseñas de eventos y de lugares. Las dos tablas tienen la misma forma, así que un solo
    /// repositorio las atiende eligiendo tabla y columnas según <see cref="TipoResena"/>.
    /// </summary>
    /// <remarks>
    /// Reglas en la BD: un evento solo se reseña cuando ya terminó; nadie reseña lo propio;
    /// una reseña por usuario.
    /// </remarks>
    public sealed class ResenaDB : ConexionDB, IReviewRepository
    {
        /// <summary>Nombres de tabla/columnas de cada tipo. Son constantes: nunca vienen del usuario.</summary>
        private sealed record Tabla(string Resenas, string ColObjetivo, string ColRespuesta, string Objetivos, string ColDueno);

        private static Tabla De(TipoResena tipo) => tipo == TipoResena.Evento
            ? new Tabla("event_review", "event_id", "organizer_reply", "event", "organizer_id")
            : new Tabla("place_review", "place_id", "owner_reply", "place", "owner_id");

        public ResenaDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }

        /// <returns>Id de la reseña nueva.</returns>
        public Task<int> CrearAsync(TipoResena tipo, int objetivoId, int usuarioId, int calificacion, string? comentario, CancellationToken ct = default)
        {
            Validar.Calificacion(calificacion);
            var t = De(tipo);
            return InsertarAsync($"""
                INSERT INTO {t.Resenas} ({t.ColObjetivo}, user_id, rating, comment)
                VALUES (@objetivoId, @usuarioId, @calificacion, @comentario)
                """, new { objetivoId, usuarioId, calificacion, comentario = Limpiar(comentario) }, ct);
        }

        /// <summary>El autor edita su reseña.</summary>
        public async Task<bool> EditarAsync(TipoResena tipo, int resenaId, int usuarioId, int calificacion, string? comentario, CancellationToken ct = default)
        {
            Validar.Calificacion(calificacion);
            var t = De(tipo);
            return await EjecutarAsync($"""
                UPDATE {t.Resenas} SET rating = @calificacion, comment = @comentario
                WHERE id = @resenaId AND user_id = @usuarioId
                """, new { resenaId, usuarioId, calificacion, comentario = Limpiar(comentario) }, ct) > 0;
        }

        /// <summary>El organizador / dueño responde públicamente. Solo funciona sobre lo que es suyo.</summary>
        public async Task<bool> ResponderAsync(TipoResena tipo, int resenaId, int duenoId, string respuesta, CancellationToken ct = default)
        {
            Validar.Requerido(respuesta, "La respuesta");
            var t = De(tipo);
            return await EjecutarAsync($"""
                UPDATE {t.Resenas} r JOIN {t.Objetivos} o ON o.id = r.{t.ColObjetivo}
                SET r.{t.ColRespuesta} = @respuesta, r.replied_at = NOW()
                WHERE r.id = @resenaId AND o.{t.ColDueno} = @duenoId
                """, new { resenaId, duenoId, respuesta = respuesta.Trim() }, ct) > 0;
        }

        /// <summary>Reseñas de un evento o lugar, las más recientes primero.</summary>
        public Task<List<Resena>> ListarAsync(TipoResena tipo, int objetivoId, bool incluirOcultas = false, CancellationToken ct = default)
        {
            var t = De(tipo);
            return ConsultarAsync($"""
                SELECT r.*, r.{t.ColObjetivo} AS target_id, r.{t.ColRespuesta} AS reply, u.username, u.avatar_url
                FROM {t.Resenas} r JOIN user u ON u.id = r.user_id
                WHERE r.{t.ColObjetivo} = @objetivoId AND (@incluirOcultas OR NOT r.is_hidden)
                ORDER BY r.created_at DESC
                """, r => new Resena(
                    r.Entero("id"), tipo, r.Entero("target_id"), r.Entero("user_id"), r.Texto("username"),
                    r.TextoONulo("avatar_url"), r.Entero("rating"), r.TextoONulo("comment"), r.TextoONulo("reply"),
                    r.FechaONula("replied_at"), r.Bool("is_hidden"), r.Fecha("created_at")),
                new { objetivoId, incluirOcultas }, ct);
        }

        /// <summary>Promedio, total y barras por estrellas (solo reseñas visibles, como las vistas de la BD).</summary>
        public async Task<ResumenResenas> ObtenerResumenAsync(TipoResena tipo, int objetivoId, CancellationToken ct = default)
        {
            var t = De(tipo);
            var filas = await ConsultarAsync($"""
                SELECT rating, COUNT(*) AS total
                FROM {t.Resenas}
                WHERE {t.ColObjetivo} = @objetivoId AND NOT is_hidden
                GROUP BY rating
                """, r => (Estrellas: r.Entero("rating"), Total: r.Entero("total")), new { objetivoId }, ct);
            return Resumir(filas);
        }

        /// <summary>Arma el resumen a partir de (estrellas, cuántas). La usa también el repositorio falso.</summary>
        internal static ResumenResenas Resumir(IEnumerable<(int Estrellas, int Total)> filas)
        {
            var porEstrellas = Enumerable.Range(1, 5).ToDictionary(e => e, _ => 0);
            foreach (var (estrellas, total) in filas) porEstrellas[estrellas] += total;

            int cuantas = porEstrellas.Values.Sum();
            // ROUND(AVG(rating), 1) de MySQL redondea alejándose del cero.
            decimal? promedio = cuantas == 0 ? null
                : Math.Round((decimal)porEstrellas.Sum(p => p.Key * p.Value) / cuantas, 1, MidpointRounding.AwayFromZero);
            return new ResumenResenas(promedio, cuantas, porEstrellas);
        }

        /// <summary>Un moderador oculta (o vuelve a mostrar) una reseña tras un reporte.</summary>
        public async Task<bool> CambiarVisibilidadAsync(TipoResena tipo, int resenaId, bool oculta, CancellationToken ct = default) =>
            await EjecutarAsync($"UPDATE {De(tipo).Resenas} SET is_hidden = @oculta WHERE id = @resenaId",
                new { resenaId, oculta }, ct) > 0;

        private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }
}
