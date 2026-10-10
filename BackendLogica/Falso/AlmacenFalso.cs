using System.Globalization;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace BackendLogica.Falso
{
    /// <summary>
    /// "Base de datos" en memoria del repositorio falso: las mismas tablas que surachih_v1.sql, sembradas con
    /// sus DATOS DE PRUEBA (mismos ids, textos, fechas y relaciones). Contraseña de todos: Test1234!
    /// </summary>
    /// <remarks>
    /// Todos los repositorios falsos de una <see cref="SuraChihFalso"/> comparten un almacén, así que lo que
    /// escribe uno lo ve el otro (igual que con MySQL). Los cambios se pierden al cerrar la app.
    /// Cada operación toma <see cref="Candado"/>, así que es seguro usarlo desde varios hilos.
    /// </remarks>
    internal sealed class AlmacenFalso
    {
        public readonly object Candado = new();

        public readonly List<FilaUsuario> Usuarios = [];
        public readonly List<FilaOrganizador> Organizadores = [];
        public readonly List<FilaRecuperacion> Recuperaciones = [];
        public readonly List<FilaEtiqueta> Etiquetas = [];
        public readonly List<FilaLugar> Lugares = [];
        public readonly List<FilaEvento> Eventos = [];
        public readonly List<FilaModeracion> Moderaciones = [];
        public readonly List<FilaFavorito> Favoritos = [];
        public readonly List<FilaFavoritoLugar> FavoritosLugares = [];
        public readonly List<FilaInscripcion> Inscripciones = [];
        public readonly List<FilaVista> Vistas = [];
        public readonly List<FilaResena> Resenas = [];
        public readonly List<FilaReporte> Reportes = [];

        // Contadores AUTO_INCREMENT
        private int _imagenEvento, _imagenLugar, _recuperacion, _vista;
        public int SiguienteImagenEvento() => ++_imagenEvento;
        public int SiguienteImagenLugar() => ++_imagenLugar;
        public int SiguienteRecuperacion() => ++_recuperacion;
        public int SiguienteVista() => ++_vista;
        public static int Siguiente<T>(IEnumerable<T> filas, Func<T, int> id) => filas.Select(id).DefaultIfEmpty(0).Max() + 1;

        public AlmacenFalso() => Sembrar(DateTime.Now);

        // ------------------------------------------------------------------
        // Lo que en MySQL resuelven las vistas y el collation utf8mb4_0900_ai_ci
        // ------------------------------------------------------------------

        private static readonly CompareInfo Comparador = CultureInfo.InvariantCulture.CompareInfo;
        private const CompareOptions SinAcentosNiMayusculas = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

        /// <summary>LIKE '%texto%' sin distinguir acentos ni mayúsculas ("cafe" encuentra "Café").</summary>
        public static bool Contiene(string? fuente, string texto) =>
            fuente != null && Comparador.IndexOf(fuente, texto, SinAcentosNiMayusculas) >= 0;

        /// <summary>= de MySQL con el collation de la BD (las llaves UNIQUE comparan así).</summary>
        public static bool Igual(string a, string b) => Comparador.Compare(a, b, SinAcentosNiMayusculas) == 0;

        public FilaUsuario? Usuario(int id) => Usuarios.FirstOrDefault(u => u.Id == id);
        public FilaLugar? Lugar(int? id) => id == null ? null : Lugares.FirstOrDefault(l => l.Id == id);
        public FilaEvento? Evento(int id) => Eventos.FirstOrDefault(e => e.Id == id);
        public FilaOrganizador? Organizador(int usuarioId) => Organizadores.FirstOrDefault(o => o.UsuarioId == usuarioId);

        public bool EsOrganizadorAprobado(int usuarioId) => Organizador(usuarioId)?.Estado == EstadoOrganizador.Aprobado;

        /// <summary>Lo que ve cualquier visitante: aprobado y sin terminar.</summary>
        public static bool Vigente(FilaEvento e, DateTime ahora) =>
            e.Estado == EstadoEvento.Aprobado && (e.FechaPorConfirmar || e.Fin > ahora);

        /// <summary>Orden por start_at; los de fecha por confirmar van al final.</summary>
        public static (bool, DateTime) OrdenFecha(FilaEvento e) => (e.FechaPorConfirmar, e.Inicio);

        public List<Etiqueta> EtiquetasDe(IEnumerable<int> ids) =>
            Etiquetas.Where(t => t.Activa && ids.Contains(t.Datos.Id))
                     .Select(t => t.Datos)
                     .OrderBy(t => t.Orden).ThenBy(t => t.Nombre)
                     .ToList();

        /// <summary>Fila de <c>v_event_card</c> + interesados + etiquetas.</summary>
        public EventoTarjeta Tarjeta(FilaEvento e, DateTime ahora)
        {
            var lugar = Lugar(e.LugarId);
            var org = Organizador(e.OrganizadorId)!;
            return new EventoTarjeta
            {
                Id = e.Id,
                Titulo = e.Titulo,
                Descripcion = e.Descripcion,
                Inicio = e.Inicio,
                Fin = e.Fin,
                FechaPorConfirmar = e.FechaPorConfirmar,
                PrecioMin = e.PrecioMin,
                PrecioMax = e.PrecioMax,
                EsGratis = !e.PrecioPorConfirmar && e.PrecioMax == 0,
                PrecioPorConfirmar = e.PrecioPorConfirmar,
                BoletosUrl = e.BoletosUrl,
                ImagenUrl = e.ImagenUrl,
                Estado = e.Estado,
                Terminado = e.Estado == EstadoEvento.Aprobado && !e.FechaPorConfirmar && e.Fin < ahora,
                Destacado = e.Destacado,
                OrdenDestacado = e.OrdenDestacado,
                LugarId = e.LugarId,
                LugarNombre = lugar?.Nombre,
                Direccion = e.Direccion ?? lugar?.Direccion,
                Latitud = e.Latitud ?? lugar?.Latitud,
                Longitud = e.Longitud ?? lugar?.Longitud,
                OrganizadorId = e.OrganizadorId,
                OrganizadorNombre = org.NombreNegocio,
                OrganizadorWhatsApp = org.WhatsApp,
                OrganizadorVerificado = org.Verificado,
                Interesados = Favoritos.Count(f => f.EventoId == e.Id),
                Etiquetas = EtiquetasDe(e.EtiquetaIds)
            };
        }

        /// <summary>Fila de <c>place</c> + <c>v_place_rating</c> + etiquetas, horario y "abierto ahora".</summary>
        public LugarTarjeta Tarjeta(FilaLugar l, DateTime ahora)
        {
            var calificacion = Calificacion(l.Id);
            return new LugarTarjeta
            {
                Id = l.Id,
                Nombre = l.Nombre,
                Descripcion = l.Descripcion,
                Direccion = l.Direccion,
                Latitud = l.Latitud,
                Longitud = l.Longitud,
                Telefono = l.Telefono,
                SitioWebUrl = l.SitioWebUrl,
                PrecioMin = l.PrecioMin,
                PrecioMax = l.PrecioMax,
                ImagenUrl = l.ImagenUrl,
                Estado = l.Estado,
                DuenoId = l.DuenoId,
                Calificacion = calificacion.Calificacion,
                NumResenas = calificacion.NumResenas,
                Etiquetas = EtiquetasDe(l.EtiquetaIds),
                Horario = l.Horario.OrderBy(h => h.Dia).ToList(),
                HorarioAhora = EstadoHorario.Calcular(l.Horario, ahora)
            };
        }

        /// <summary>Fila de <c>v_place_rating</c>: promedio (1 decimal) y número de reseñas visibles.</summary>
        public CalificacionLugar Calificacion(int lugarId)
        {
            var resumen = Repositorios.ResenaDB.Resumir(
                Resenas.Where(r => r.Tipo == TipoResena.Lugar && r.ObjetivoId == lugarId && !r.Oculta)
                       .GroupBy(r => r.Calificacion)
                       .Select(g => (g.Key, g.Count())));
            return new CalificacionLugar(lugarId, resumen.Promedio, resumen.Total);
        }

        /// <summary>Lo que hace MySQL al violar una llave foránea (error 1452).</summary>
        public static DatosInvalidosException NoExiste() => new("El registro relacionado no existe.");

        // ------------------------------------------------------------------
        // DATOS DE PRUEBA (copiados de BaseDeDatos/surachih_v1.sql)
        // ------------------------------------------------------------------

        private const string HashTest1234 = "$2b$11$C4rR0iUKQKbnm.V0sIsKHeyxIUqVR7g8aqJbWo48UbbeVbh/PeDsC";
        private const string ImagenDemo = "https://res.cloudinary.com/demo/image/upload/sample.jpg";

        private void Sembrar(DateTime ahora)
        {
            void Etiqueta(int id, string nombre, string icono, string color, AlcanceEtiqueta alcance, int orden) =>
                Etiquetas.Add(new FilaEtiqueta(new Etiqueta(id, nombre, icono, color, alcance, orden), true));
            Etiqueta(1, "Familiar", "family", "#9F1239", AlcanceEtiqueta.Ambos, 1);
            Etiqueta(2, "Música", "music", "#5B21B6", AlcanceEtiqueta.Ambos, 2);
            Etiqueta(3, "Deportes y aire libre", "run", "#065F46", AlcanceEtiqueta.Ambos, 3);
            Etiqueta(4, "Tecnología y conferencias", "robot", "#075985", AlcanceEtiqueta.Evento, 4);
            Etiqueta(5, "Gastronomía", "food", "#92400E", AlcanceEtiqueta.Ambos, 5);
            Etiqueta(6, "Cafetería", "coffee", "#92400E", AlcanceEtiqueta.Lugar, 10);
            Etiqueta(7, "Restaurante", "restaurant", "#9A3412", AlcanceEtiqueta.Lugar, 11);
            Etiqueta(8, "Bar", "bar", "#5B21B6", AlcanceEtiqueta.Lugar, 12);
            Etiqueta(9, "Foro y venue", "stage", "#86198F", AlcanceEtiqueta.Lugar, 13);
            Etiqueta(10, "Museo y galería", "museum", "#155E75", AlcanceEtiqueta.Lugar, 14);
            Etiqueta(11, "Parque", "tree", "#065F46", AlcanceEtiqueta.Lugar, 15);

            void Usuario(int id, string nombre, string correo, bool esMod) =>
                Usuarios.Add(new FilaUsuario { Id = id, NombreUsuario = nombre, Correo = correo, Hash = HashTest1234, EsModerador = esMod, Activo = true, CreadoEn = ahora });
            Usuario(1, "mod_ana", "ana.mod@example.com", true);
            Usuario(2, "mod_luis", "luis.mod@example.com", true);
            Usuario(3, "cafe_dande", "dandelion@example.com", false);
            Usuario(4, "misiones_cuu", "misiones@example.com", false);
            Usuario(5, "mariana_r", "mariana.r@example.com", false);
            Usuario(6, "diego_m", "diego.m@example.com", false);
            Usuario(7, "sofia_c", "sofia.c@example.com", false);

            // 3 y 4 se aprueban; 7 queda pendiente para probar el flujo.
            void Organizador(int id, string negocio, string red, string whatsApp, EstadoOrganizador estado) =>
                Organizadores.Add(new FilaOrganizador { UsuarioId = id, NombreNegocio = negocio, RedSocialUrl = red, WhatsApp = whatsApp, Estado = estado, SolicitadoEn = ahora });
            Organizador(3, "Dandelion Coffee", "https://instagram.com/ejemplo_dandelion", "526141110001", EstadoOrganizador.Aprobado);
            Organizador(4, "Misiones Chihuahua", "https://instagram.com/ejemplo_misiones", "526141110002", EstadoOrganizador.Aprobado);
            Organizador(7, "Colectivo Sofía", "https://instagram.com/ejemplo_sofia", "526141110003", EstadoOrganizador.Pendiente);

            Lugares.Add(new FilaLugar
            {
                Id = 1, DuenoId = 3, Nombre = "Dandelion Coffee", Descripcion = "Cafetería de especialidad.",
                Direccion = "C. Monte Bello 4334, Chihuahua", Latitud = 28.652000m, Longitud = -106.119000m,
                PrecioMin = 100, PrecioMax = 200, ImagenUrl = ImagenDemo, Estado = EstadoLugar.Aprobado, CreadoEn = ahora,
                EtiquetaIds = [6],
                Horario =
                [
                    new(DayOfWeek.Monday, new(8, 0, 0), new(21, 0, 0)), new(DayOfWeek.Tuesday, new(8, 0, 0), new(21, 0, 0)),
                    new(DayOfWeek.Wednesday, new(8, 0, 0), new(21, 0, 0)), new(DayOfWeek.Thursday, new(8, 0, 0), new(21, 0, 0)),
                    new(DayOfWeek.Friday, new(8, 0, 0), new(21, 0, 0)), new(DayOfWeek.Saturday, new(9, 0, 0), new(21, 0, 0))
                ]
            });
            Lugares.Add(new FilaLugar
            {
                Id = 2, DuenoId = 3, Nombre = "Café de la Tercera", Descripcion = "Café y terraza en el centro.",
                Direccion = "C. Tercera 805, Centro, Chihuahua", Latitud = 28.635500m, Longitud = -106.077000m,
                PrecioMin = 200, PrecioMax = 300, ImagenUrl = ImagenDemo, Estado = EstadoLugar.Aprobado, CreadoEn = ahora,
                EtiquetaIds = [6, 5],
                Horario =   // cruza medianoche
                [
                    new(DayOfWeek.Thursday, new(18, 0, 0), new(1, 0, 0)),
                    new(DayOfWeek.Friday, new(18, 0, 0), new(2, 0, 0)),
                    new(DayOfWeek.Saturday, new(18, 0, 0), new(2, 0, 0))
                ]
            });

            Eventos.Add(new FilaEvento
            {
                Id = 1, OrganizadorId = 4, Titulo = "Misión 404 - Juego callejero guiado por app",
                Descripcion = "Recorre el centro resolviendo acertijos 🔍🧩",
                Inicio = new DateTime(2026, 10, 1, 10, 0, 0), Fin = new DateTime(2026, 11, 29, 20, 0, 0),
                Direccion = "Plaza de Armas, Centro, Chihuahua", Latitud = 28.635300m, Longitud = -106.075600m,
                PrecioMin = 370, PrecioMax = 370, BoletosUrl = "https://boletos.example.com/mision404", ImagenUrl = ImagenDemo,
                Estado = EstadoEvento.Aprobado, Destacado = true, OrdenDestacado = 1, CreadoEn = ahora, EtiquetaIds = [1]
            });
            Eventos.Add(new FilaEvento
            {
                Id = 2, OrganizadorId = 3, LugarId = 1, Titulo = "Cata de café de especialidad",
                Descripcion = "Tres orígenes, tres métodos de extracción ☕",
                Inicio = new DateTime(2026, 10, 24, 17, 0, 0), Fin = new DateTime(2026, 10, 24, 19, 0, 0),
                PrecioMin = 250, PrecioMax = 250, ImagenUrl = ImagenDemo, Estado = EstadoEvento.Aprobado, CreadoEn = ahora,
                EtiquetaIds = [5], Galeria = [new(SiguienteImagenEvento(), ImagenDemo, 1)]
            });
            Eventos.Add(new FilaEvento
            {
                Id = 3, OrganizadorId = 4, Titulo = "Carrera nocturna 5K", Descripcion = "Carrera recreativa familiar 🏃",
                Inicio = new DateTime(2026, 9, 20, 19, 0, 0), Fin = new DateTime(2026, 9, 20, 22, 0, 0),
                Direccion = "Parque El Palomar, Chihuahua", Latitud = 28.640000m, Longitud = -106.085000m,
                ImagenUrl = ImagenDemo, Estado = EstadoEvento.Aprobado, CreadoEn = ahora, EtiquetaIds = [1, 3]
            });
            // El evento 4 queda pendiente para probar el dashboard de moderación.
            Eventos.Add(new FilaEvento
            {
                Id = 4, OrganizadorId = 4, Titulo = "Taller de fotografía urbana",
                Descripcion = "Salida fotográfica por el centro histórico 📷",
                Inicio = new DateTime(2026, 11, 8, 9, 0, 0), Fin = new DateTime(2026, 11, 8, 13, 0, 0),
                Direccion = "Catedral de Chihuahua", Latitud = 28.635900m, Longitud = -106.076200m,
                PrecioMin = 450, PrecioMax = 600, Estado = EstadoEvento.Pendiente, CreadoEn = ahora
            });

            void Decision(int modId, TipoObjetivoModeracion tipo, int objetivo, string? comentario) =>
                Moderaciones.Add(new FilaModeracion(Moderaciones.Count + 1, modId, tipo, objetivo, DecisionModeracion.Aprobado, comentario, ahora));
            Decision(1, TipoObjetivoModeracion.Organizador, 3, "Perfil de Instagram activo y coherente con el negocio.");
            Decision(2, TipoObjetivoModeracion.Organizador, 4, null);
            Decision(1, TipoObjetivoModeracion.Lugar, 1, null);
            Decision(2, TipoObjetivoModeracion.Lugar, 2, null);
            Decision(1, TipoObjetivoModeracion.Evento, 1, null);
            Decision(2, TipoObjetivoModeracion.Evento, 2, null);
            Decision(1, TipoObjetivoModeracion.Evento, 3, null);

            Favoritos.AddRange([new(5, 1, ahora), new(6, 1, ahora), new(5, 2, ahora)]);
            foreach (var (usuario, evento) in new[] { (5, 1), (6, 2), (7, 2) })
                Inscripciones.Add(new FilaInscripcion { UsuarioId = usuario, EventoId = evento, Estado = EstadoInscripcion.Activa, CreadaEn = ahora });
            foreach (var (evento, usuario) in new (int, int?)[] { (1, 5), (1, 6), (1, 7), (1, null), (2, 5), (2, 6) })
                Vistas.Add(new FilaVista(SiguienteVista(), evento, usuario, ahora));

            Resenas.Add(new FilaResena { Id = 1, Tipo = TipoResena.Evento, ObjetivoId = 3, UsuarioId = 5, Calificacion = 5, Comentario = "Muy bien organizada.", CreadaEn = ahora });
            Resenas.Add(new FilaResena { Id = 1, Tipo = TipoResena.Lugar, ObjetivoId = 1, UsuarioId = 5, Calificacion = 5, Comentario = "El mejor flat white.", CreadaEn = ahora });
            Resenas.Add(new FilaResena { Id = 2, Tipo = TipoResena.Lugar, ObjetivoId = 1, UsuarioId = 6, Calificacion = 4, CreadaEn = ahora });
            Resenas.Add(new FilaResena { Id = 3, Tipo = TipoResena.Lugar, ObjetivoId = 2, UsuarioId = 7, Calificacion = 4, Comentario = "Buen ambiente.", CreadaEn = ahora });
        }
    }

    // ----------------------------------------------------------------------
    // Filas (una clase por tabla; mutables porque UPDATE las cambia)
    // ----------------------------------------------------------------------

    internal sealed class FilaUsuario
    {
        public int Id;
        public string NombreUsuario = "", Correo = "", Hash = "";
        public string? AvatarUrl;
        public bool EsModerador, Activo;
        public DateTime CreadoEn;

        public Usuario ADatos() => new(Id, NombreUsuario, Correo, AvatarUrl, EsModerador, Activo, CreadoEn);
    }

    internal sealed class FilaOrganizador
    {
        public int UsuarioId;
        public string NombreNegocio = "", RedSocialUrl = "", WhatsApp = "";
        public string? Descripcion;
        public EstadoOrganizador Estado;
        public bool Verificado;
        public DateTime SolicitadoEn;

        public PerfilOrganizador ADatos() =>
            new(UsuarioId, NombreNegocio, Descripcion, RedSocialUrl, WhatsApp, Estado, Verificado, SolicitadoEn);
    }

    internal sealed class FilaRecuperacion
    {
        public int Id, UsuarioId;
        public string TokenHash = "";
        public DateTime ExpiraEn;
        public DateTime? UsadoEn;
    }

    internal sealed record FilaEtiqueta(Etiqueta Datos, bool Activa);

    internal sealed record FilaImagen(int Id, string Url, int Orden);

    internal sealed class FilaLugar
    {
        public int Id, DuenoId;
        public string Nombre = "", Direccion = "";
        public string? Descripcion, Telefono, SitioWebUrl, ImagenUrl;
        public decimal Latitud, Longitud;
        public decimal? PrecioMin, PrecioMax;
        public EstadoLugar Estado;
        public DateTime CreadoEn;
        public List<int> EtiquetaIds = [];
        public List<HorarioDia> Horario = [];
        public List<FilaImagen> Galeria = [];
    }

    internal sealed class FilaEvento
    {
        public int Id, OrganizadorId;
        public int? LugarId;
        public string Titulo = "", Descripcion = "";
        public DateTime Inicio, Fin;
        public bool FechaPorConfirmar, PrecioPorConfirmar;
        public string? Direccion, BoletosUrl, ImagenUrl, MotivoCancelacion;
        public decimal? Latitud, Longitud;
        public decimal PrecioMin, PrecioMax;
        public EstadoEvento Estado;
        public bool Destacado;
        public int? OrdenDestacado;
        public DateTime CreadoEn;
        public List<int> EtiquetaIds = [];
        public List<FilaImagen> Galeria = [];
    }

    internal sealed record FilaModeracion(int Id, int ModeradorId, TipoObjetivoModeracion Tipo, int ObjetivoId,
        DecisionModeracion Decision, string? Comentario, DateTime DecididoEn);

    internal sealed record FilaFavorito(int UsuarioId, int EventoId, DateTime CreadoEn);

    internal sealed record FilaFavoritoLugar(int UsuarioId, int LugarId, DateTime CreadoEn);

    internal sealed class FilaInscripcion
    {
        public int UsuarioId, EventoId;
        public EstadoInscripcion Estado;
        public DateTime CreadaEn;
        public DateTime? CanceladaEn;
    }

    internal sealed record FilaVista(int Id, int EventoId, int? UsuarioId, DateTime VistoEn);

    internal sealed class FilaResena
    {
        public int Id, ObjetivoId, UsuarioId, Calificacion;
        public TipoResena Tipo;
        public string? Comentario, Respuesta;
        public DateTime? RespondidaEn;
        public bool Oculta;
        public DateTime CreadaEn;
    }

    internal sealed class FilaReporte
    {
        public int Id, ReportanteId, ObjetivoId;
        public TipoObjetivoReporte Tipo;
        public MotivoReporte Motivo;
        public string? Detalles;
        public EstadoReporte Estado;
        public int? ResueltoPor;
        public DateTime? ResueltoEn;
        public DateTime CreadoEn;

        public Reporte ADatos() => new(Id, ReportanteId, Tipo, ObjetivoId, Motivo, Detalles, Estado, ResueltoPor, ResueltoEn, CreadoEn);
    }
}
