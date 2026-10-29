using BackendLogica.Contratos;
using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Pantalla "Mis planes" (Paginas/MisPlanes.razor, WBS 4.4): los eventos en los que el usuario tocó
    /// "Voy a ir" (tabla <c>inscription</c>, estados active / cancelled), en tres pestañas.
    /// </summary>
    /// <remarks>
    /// Próximos: inscripción activa en un evento aprobado que no ha terminado (los más cercanos primero).
    /// Pasados: inscripción activa en un evento que ya terminó. Cancelados: el usuario canceló su
    /// inscripción o el organizador canceló el evento. Lo decide <see cref="IInscriptionRepository.ListarPlanesAsync"/>.
    /// </remarks>
    public sealed class ServicioPlanes(ISuraChihBD bd, Sesion sesion)
    {
        public Task<List<PlanUsuario>> ListarAsync(PestanaPlanes pestana)
        {
            Usuario usuario = sesion.Requerir("ver tus planes");
            return bd.Inscripciones.ListarPlanesAsync(usuario.Id, pestana);
        }
    }
}
