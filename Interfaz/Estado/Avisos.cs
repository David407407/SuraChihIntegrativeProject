namespace Interfaz.Estado
{
    /// <summary>
    /// Avisos flotantes ("Toast / Cambios guardados" de Figma). Los pinta Componentes/Avisos/Toasts.razor.
    /// </summary>
    /// <example>
    /// <code>
    /// Avisos.Mostrar("Cambios guardados");
    /// Avisos.Mostrar("No se pudo conectar", TipoAviso.Error);
    /// </code>
    /// </example>
    /// <remarks>
    /// La ventana modal (&lt;dialog&gt;) se pinta por encima de todo, incluidos los avisos. Para avisar algo
    /// con una modal abierta se usa el aviso de la propia modal (<c>modal__aviso</c>).
    /// </remarks>
    public sealed class Avisos
    {
        private const int DuracionMs = 3200;
        /// <summary>Lo que dura la animación de salida (toast-sale en Toasts.razor.css).</summary>
        private const int SalidaMs = 320;

        private readonly List<Aviso> _lista = [];
        private int _siguienteId;

        public IReadOnlyList<Aviso> Lista => _lista;

        public event Action? Cambio;

        public void Mostrar(string mensaje, TipoAviso tipo = TipoAviso.Exito)
        {
            var aviso = new Aviso(++_siguienteId, mensaje, tipo);
            _lista.Add(aviso);
            Cambio?.Invoke();
            _ = QuitarDespuesAsync(aviso);
        }

        /// <summary>"Compartir eventos llega en la siguiente versión."</summary>
        public void Proximamente(string que) => Mostrar($"{que} llega en la siguiente versión.", TipoAviso.Info);

        private async Task QuitarDespuesAsync(Aviso aviso)
        {
            await Task.Delay(DuracionMs);
            aviso.Saliendo = true;
            Cambio?.Invoke();
            await Task.Delay(SalidaMs);
            _lista.Remove(aviso);
            Cambio?.Invoke();
        }
    }

    public enum TipoAviso { Exito, Info, Error }

    public sealed class Aviso(int id, string mensaje, TipoAviso tipo)
    {
        public int Id { get; } = id;
        public string Mensaje { get; } = mensaje;
        public TipoAviso Tipo { get; } = tipo;
        public bool Saliendo { get; set; }
    }
}
