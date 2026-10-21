using BackendLogica.Modelos;
using Interfaz.Componentes.Modales;
using Microsoft.AspNetCore.Components;

namespace Interfaz.Estado
{
    /// <summary>
    /// La ventana modal (los "Modal / ..." de Figma). La pinta Componentes/Modales/VentanaModal.razor
    /// con un &lt;dialog&gt; nativo, así que el foco queda atrapado dentro, Esc cierra y lo de atrás no se
    /// puede tocar.
    /// </summary>
    /// <remarks>
    /// Solo hay una modal a la vez: si ya hay una abierta, abrir otra cambia su contenido (p. ej. de
    /// "Inicia sesión" a "Crea tu cuenta") sin cerrar y volver a abrir. La que se reemplaza termina
    /// con resultado <c>null</c>.
    /// </remarks>
    public sealed class Modales
    {
        private TaskCompletionSource<object?>? _pendiente;
        private object? _resultado;

        /// <summary>Componente que se muestra dentro de la modal, o null si está cerrada.</summary>
        public Type? Componente { get; private set; }
        public IDictionary<string, object?> Parametros { get; private set; } = new Dictionary<string, object?>();
        /// <summary>Cambia con cada contenido nuevo (para reiniciar el formulario y la animación de cambio).</summary>
        public int Version { get; private set; }
        public bool Cerrando { get; private set; }
        public bool Abierta => Componente != null;

        public event Action? Cambio;

        /// <summary>Abre la modal con <typeparamref name="T"/> (o cambia su contenido si ya estaba abierta).</summary>
        /// <returns>Lo que pase a <see cref="Cerrar"/> el componente; null si se cerró de otra forma.</returns>
        public Task<object?> Abrir<T>(IDictionary<string, object?>? parametros = null) where T : IComponent
        {
            _pendiente?.TrySetResult(null);
            _pendiente = new TaskCompletionSource<object?>();
            _resultado = null;
            Componente = typeof(T);
            Parametros = parametros ?? new Dictionary<string, object?>();
            Cerrando = false;
            Version++;
            Cambio?.Invoke();
            return _pendiente.Task;
        }

        /// <summary>Cierra con animación. La tarea de <see cref="Abrir{T}"/> termina ya cerrada, con <paramref name="resultado"/>.</summary>
        public void Cerrar(object? resultado = null)
        {
            if (!Abierta || Cerrando) return;
            _resultado = resultado;
            Cerrando = true;
            Cambio?.Invoke();
        }

        /// <summary>Lo llama VentanaModal cuando terminó la animación de salida.</summary>
        internal void TerminarCierre()
        {
            if (!Cerrando) return;   // mientras salía se abrió otra
            Componente = null;
            Parametros = new Dictionary<string, object?>();
            Cerrando = false;
            var pendiente = _pendiente;
            _pendiente = null;
            pendiente?.TrySetResult(_resultado);
            _resultado = null;
            Cambio?.Invoke();
        }

        // ---------------------------------------------------------------- Modales de cuenta (WBS 4.2)

        /// <param name="correo">Para rellenar el campo (el foco va directo a la contraseña).</param>
        /// <param name="aviso">Aviso informativo arriba del formulario.</param>
        public void IniciarSesion(string? correo = null, string? aviso = null) =>
            _ = Abrir<ModalIniciarSesion>(new Dictionary<string, object?> { ["Correo"] = correo, ["Aviso"] = aviso });

        public void CrearCuenta(string? correo = null) =>
            _ = Abrir<ModalCrearCuenta>(new Dictionary<string, object?> { ["Correo"] = correo });

        public void RecuperarContrasena(string? correo = null) =>
            _ = Abrir<ModalRecuperarContrasena>(new Dictionary<string, object?> { ["Correo"] = correo });

        public void RevisaTuCorreo(string correo) =>
            _ = Abrir<ModalRevisaTuCorreo>(new Dictionary<string, object?> { ["Correo"] = correo });

        /// <param name="token">Token de un solo uso del enlace (C# solo guarda su SHA-256 en password_reset).</param>
        public void ContrasenaNueva(string token, string? correo = null) =>
            _ = Abrir<ModalContrasenaNueva>(new Dictionary<string, object?> { ["Token"] = token, ["Correo"] = correo });

        /// <summary>Un visitante intentó algo que necesita cuenta (inscribirse, favoritos, reseñas...).</summary>
        public void RequiereSesion(string? texto = null) =>
            _ = Abrir<ModalRequiereSesion>(texto == null ? null : new Dictionary<string, object?> { ["Texto"] = texto });

        // ---------------------------------------------------------------- Inscripción y cuenta

        /// <summary>"Modal / Confirmar inscripción" (¿Vas a ir?).</summary>
        /// <returns>true si quedó inscrito.</returns>
        public async Task<bool> ConfirmarInscripcionAsync(EventoTarjeta evento) =>
            await Abrir<ModalInscripcion>(new Dictionary<string, object?> { ["Evento"] = evento, ["Cancelar"] = false }) as bool? ?? false;

        /// <summary>"Modal / Cancelar inscripción" (¿Cancelar tu inscripción?).</summary>
        /// <returns>true si sigue inscrito ("Mantener mi lugar" o cerrar).</returns>
        public async Task<bool> ConfirmarCancelacionAsync(EventoTarjeta evento) =>
            await Abrir<ModalInscripcion>(new Dictionary<string, object?> { ["Evento"] = evento, ["Cancelar"] = true }) as bool? ?? true;

        /// <summary>"Modal / Eliminar cuenta".</summary>
        /// <returns>true si la cuenta quedó desactivada; false si se cerró o canceló.</returns>
        public async Task<bool> ConfirmarEliminarCuentaAsync() =>
            await Abrir<ModalEliminarCuenta>() as bool? ?? false;
    }
}
