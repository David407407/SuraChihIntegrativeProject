using BackendLogica.Datos;

namespace BackendLogica.Falso
{
    /// <summary>
    /// Clase padre de los repositorios falsos. Ejecuta cada operación con el almacén bloqueado y devuelve
    /// una <see cref="Task"/> ya terminada; los errores viajan dentro de la tarea, como en MySQL, así que
    /// <c>await</c> y <c>try/catch</c> se escriben igual con los dos orígenes.
    /// </summary>
    public abstract class RepositorioFalso
    {
        private protected readonly AlmacenFalso Almacen;

        private protected RepositorioFalso(AlmacenFalso almacen) => Almacen = almacen;

        protected static DateTime Ahora => DateTime.Now;

        private protected Task<T> Hacer<T>(Func<AlmacenFalso, T> trabajo, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return Task.FromCanceled<T>(ct);
            try
            {
                lock (Almacen.Candado) return Task.FromResult(trabajo(Almacen));
            }
            catch (Exception ex)
            {
                return Task.FromException<T>(ex);
            }
        }

        private protected Task Hacer(Action<AlmacenFalso> trabajo, CancellationToken ct) =>
            Hacer(a => { trabajo(a); return true; }, ct);

        /// <summary>Lo que hace un trigger con <c>SIGNAL SQLSTATE '45000'</c> (error 1644).</summary>
        protected static ReglaNegocioException Trigger(string mensaje) => new(mensaje);
    }
}
