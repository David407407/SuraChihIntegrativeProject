using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using BackendLogica.Seguridad;

namespace BackendLogica.Falso
{
    /// <summary><see cref="IOrganizerRepository"/> en memoria.</summary>
    public sealed class FalsoOrganizerRepository : RepositorioFalso, IOrganizerRepository
    {
        internal FalsoOrganizerRepository(AlmacenFalso almacen) : base(almacen) { }

        public Task SolicitarAsync(SolicitudOrganizador s, CancellationToken ct = default) =>
            Hacer(a =>
            {
                Validar.Requerido(s.NombreNegocio, "El nombre del negocio");
                Validar.Requerido(s.RedSocialUrl, "El enlace a tu red social");
                Validar.WhatsApp(s.WhatsApp);
                if (a.Usuario(s.UsuarioId) == null) throw AlmacenFalso.NoExiste();

                var fila = a.Organizador(s.UsuarioId);
                if (fila == null)
                {
                    fila = new FilaOrganizador { UsuarioId = s.UsuarioId };
                    a.Organizadores.Add(fila);
                }
                else if (fila.Estado != EstadoOrganizador.Rechazado)
                    throw new DuplicadoException("Ya enviaste una solicitud de organizador.");

                fila.NombreNegocio = s.NombreNegocio.Trim();
                fila.Descripcion = s.Descripcion;
                fila.RedSocialUrl = s.RedSocialUrl.Trim();
                fila.WhatsApp = s.WhatsApp;
                fila.Estado = EstadoOrganizador.Pendiente;
                fila.SolicitadoEn = Ahora;
            }, ct);

        public Task<PerfilOrganizador?> ObtenerAsync(int usuarioId, CancellationToken ct = default) =>
            Hacer(a => a.Organizador(usuarioId)?.ADatos(), ct);

        public Task<bool> EsAprobadoAsync(int usuarioId, CancellationToken ct = default) =>
            Hacer(a => a.EsOrganizadorAprobado(usuarioId), ct);

        public Task<List<PerfilOrganizador>> ListarPorEstadoAsync(EstadoOrganizador estado, CancellationToken ct = default) =>
            Hacer(a => a.Organizadores.Where(o => o.Estado == estado).OrderBy(o => o.SolicitadoEn).Select(o => o.ADatos()).ToList(), ct);

        public Task<bool> ActualizarPerfilAsync(int usuarioId, string nombreNegocio, string? descripcion, string whatsApp, CancellationToken ct = default) =>
            Hacer(a =>
            {
                Validar.Requerido(nombreNegocio, "El nombre del negocio");
                Validar.WhatsApp(whatsApp);
                var fila = a.Organizador(usuarioId);
                if (fila == null) return false;
                fila.NombreNegocio = nombreNegocio.Trim();
                fila.Descripcion = descripcion;
                fila.WhatsApp = whatsApp;
                return true;
            }, ct);

        public Task<bool> MarcarVerificadoAsync(int usuarioId, bool verificado, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var fila = a.Organizador(usuarioId);
                if (fila == null) return false;
                fila.Verificado = verificado;
                return true;
            }, ct);
    }
}
