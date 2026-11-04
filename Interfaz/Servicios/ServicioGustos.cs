using BackendLogica.Contratos;
using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// "Tus gustos" (Componentes/Modales/ModalGustos.razor): las categorías que el usuario elige al crear su
    /// cuenta o en Mi perfil. Se guardan en <c>user_interest</c> y ordenan "Te podrían interesar" y
    /// "Recomendados para ti" del inicio. Si acepta avisos queda en <c>user.notify_interests</c>, para cuando
    /// haya app de celular.
    /// </summary>
    public sealed class ServicioGustos(ISuraChihBD bd, Sesion sesion)
    {
        /// <summary>Se guardaron gustos nuevos: el Home y Mi perfil se vuelven a cargar.</summary>
        public event Action? Cambio;

        /// <summary>Todas las categorías activas (de eventos y de lugares), en el orden del frontend.</summary>
        public Task<List<Etiqueta>> ListarCategoriasAsync() => bd.Etiquetas.ListarAsync();

        /// <summary>Los gustos del usuario con sesión; sin sesión, ninguno.</summary>
        public Task<Gustos> ObtenerAsync() =>
            sesion.Usuario is { } usuario ? bd.Etiquetas.ObtenerGustosAsync(usuario.Id) : Task.FromResult(Gustos.Ninguno);

        public async Task GuardarAsync(IReadOnlyCollection<int> etiquetaIds, bool avisos)
        {
            Usuario usuario = sesion.Requerir("guardar tus gustos");
            await bd.Etiquetas.GuardarGustosAsync(usuario.Id, etiquetaIds, avisos);
            Cambio?.Invoke();
        }
    }
}
