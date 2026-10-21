using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Interfaz.Estado
{
    /// <summary>
    /// Llamadas a <c>wwwroot/js/interop.js</c>: lo poco que Blazor no hace solo (ventanas &lt;dialog&gt;,
    /// desplazar la página, la dirección de la hoja de detalle y el mapa de Leaflet).
    /// </summary>
    public sealed class InteropJs(IJSRuntime js) : IAsyncDisposable
    {
        private Task<IJSObjectReference>? _modulo;
        private bool? _reducirMovimiento;

        private Task<IJSObjectReference> Modulo() =>
            _modulo ??= js.InvokeAsync<IJSObjectReference>("import", "./_content/Interfaz/js/interop.js").AsTask();

        public async ValueTask LlamarAsync(string funcion, params object?[] argumentos)
        {
            var modulo = await Modulo();
            await modulo.InvokeVoidAsync(funcion, argumentos);
        }

        public async ValueTask<T> LlamarAsync<T>(string funcion, params object?[] argumentos)
        {
            var modulo = await Modulo();
            return await modulo.InvokeAsync<T>(funcion, argumentos);
        }

        /// <summary>"Reducir movimiento" del sistema: sin carruseles automáticos ni conteos animados.</summary>
        public async ValueTask<bool> ReducirMovimientoAsync() =>
            _reducirMovimiento ??= await LlamarAsync<bool>("reducirMovimiento");

        public ValueTask SubirAsync() => LlamarAsync("subir");
        public ValueTask SubirDeInmediatoAsync() => LlamarAsync("subirDeInmediato");
        public ValueTask<bool> DesplazarAAsync(string id) => LlamarAsync<bool>("desplazarA", id);
        public ValueTask MostrarElementoAsync(ElementReference elemento) => LlamarAsync("mostrarElemento", elemento);

        public async ValueTask DisposeAsync()
        {
            if (_modulo == null) return;
            try
            {
                var modulo = await _modulo;
                await modulo.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // La ventana ya se cerró.
            }
        }
    }
}
