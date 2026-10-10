using BackendLogica.Modelos;

namespace BackendLogica.Contratos
{
    /// <summary>
    /// Lugares: tarjetas, mapa, detalle y registro por organizadores. La calificación sale de la vista
    /// <c>v_place_rating</c>. Implementaciones: <c>LugarDB</c> y <c>FalsoPlaceRepository</c>.
    /// </summary>
    public interface IPlaceRepository
    {
        /// <summary>"Lugares para descubrir": aprobados, mejor calificados primero.</summary>
        Task<List<LugarTarjeta>> ListarRecomendadosAsync(int limite = 4, CancellationToken ct = default);

        /// <summary>Pines y lista del mapa (chips Cafés / Museos / Parques = <paramref name="etiquetaId"/>).</summary>
        Task<List<LugarTarjeta>> ListarAprobadosAsync(int? etiquetaId = null, CancellationToken ct = default);

        /// <summary>Buscador: nombre, dirección o etiqueta.</summary>
        Task<List<LugarTarjeta>> BuscarAsync(string texto, int limite = 30, CancellationToken ct = default);

        Task<int> ContarAprobadosAsync(CancellationToken ct = default);

        /// <summary>Detalle con galería, en cualquier estado.</summary>
        Task<LugarDetalle?> ObtenerAsync(int lugarId, CancellationToken ct = default);

        /// <summary>NUEVO: una fila de <c>v_place_rating</c> ("4.9 ★"). null si el lugar no existe.</summary>
        Task<CalificacionLugar?> ObtenerCalificacionAsync(int lugarId, CancellationToken ct = default);

        Task<List<LugarTarjeta>> ListarDeDuenoAsync(int duenoId, CancellationToken ct = default);

        /// <summary>Lugares en un estado (Pendiente para "Moderación / Publicaciones").</summary>
        Task<List<LugarTarjeta>> ListarPorEstadoAsync(EstadoLugar estado, CancellationToken ct = default);

        /// <summary>Registra un lugar con etiquetas y horario. Queda pendiente de moderación.</summary>
        /// <returns>Id del lugar nuevo.</returns>
        Task<int> CrearAsync(int duenoId, DatosLugar datos, CancellationToken ct = default);

        /// <returns>false si el lugar no existe o no es de este dueño.</returns>
        Task<bool> ActualizarAsync(int lugarId, int duenoId, DatosLugar datos, CancellationToken ct = default);

        /// <summary>Agrega una foto a la galería. Si el lugar estaba aprobado, vuelve a moderación.</summary>
        Task<int> AgregarImagenAsync(int lugarId, string url, CancellationToken ct = default);
    }
}
