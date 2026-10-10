using BackendLogica.Contratos;
using BackendLogica.Modelos;

namespace BackendLogica.Falso
{
    /// <summary>
    /// Origen de datos en memoria con los DATOS DE PRUEBA de BaseDeDatos/surachih_v1.sql, para construir y
    /// probar pantallas sin MySQL. Se elige con <c>"UseFakeData": true</c> (ver <see cref="FabricaBD"/>) o
    /// creándolo directamente: <c>ISuraChihBD bd = new SuraChihFalso();</c>
    /// </summary>
    /// <remarks>
    /// Usuarios: mod_ana y mod_luis (moderadores), cafe_dande y misiones_cuu (organizadores aprobados),
    /// mariana_r, diego_m y sofia_c (sofia_c con solicitud de organizador pendiente). Contraseña: Test1234!
    /// Cada instancia empieza con los datos originales; los cambios se pierden al cerrar la app.
    /// </remarks>
    public sealed class SuraChihFalso : ISuraChihBD
    {
        public IUserRepository Usuarios { get; }
        public IPasswordResetRepository RecuperacionContrasena { get; }
        public IOrganizerRepository Organizadores { get; }
        public ITagRepository Etiquetas { get; }
        public IEventRepository Eventos { get; }
        public IPlaceRepository Lugares { get; }
        public IFavoriteRepository Favoritos { get; }
        public IInscriptionRepository Inscripciones { get; }
        public IReviewRepository Resenas { get; }
        public IReportRepository Reportes { get; }
        public IModerationRepository Moderacion { get; }

        public SuraChihFalso()
        {
            var almacen = new AlmacenFalso();
            Usuarios = new FalsoUserRepository(almacen);
            RecuperacionContrasena = new FalsoPasswordResetRepository(almacen);
            Organizadores = new FalsoOrganizerRepository(almacen);
            Etiquetas = new FalsoTagRepository(almacen);
            Eventos = new FalsoEventRepository(almacen);
            Lugares = new FalsoPlaceRepository(almacen);
            Favoritos = new FalsoFavoriteRepository(almacen);
            Inscripciones = new FalsoInscriptionRepository(almacen);
            Resenas = new FalsoReviewRepository(almacen);
            Reportes = new FalsoReportRepository(almacen);
            Moderacion = new FalsoModerationRepository(almacen);
        }

        public Task<bool> ProbarConexionAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task<ResumenPlataforma> ObtenerResumenAsync(CancellationToken ct = default) => SuraChihBD.Resumir(this, ct);
    }
}
