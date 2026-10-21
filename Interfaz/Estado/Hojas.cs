namespace Interfaz.Estado
{
    /// <summary>
    /// Hoja de detalle: la ventana grande de los frames "Evento / ..." y "Lugar / ..." de Figma. Se abre
    /// encima de la pantalla en la que estés; la pinta Componentes/Detalle/HojaDetalle.razor.
    /// </summary>
    /// <example>
    /// <code>
    /// Hojas.AbrirEvento(3);                    // "Ver detalles" de una tarjeta
    /// Hojas.AbrirEvento(3, inscribir: true);   // y además abre "¿Vas a ir?" ("Voy a ir" de las tarjetas)
    /// Hojas.AbrirLugar(2);                     // pin del mapa o Card lugar
    /// </code>
    /// </example>
    public sealed class Hojas
    {
        public TipoHoja? Tipo { get; private set; }
        public int Id { get; private set; }
        /// <summary>Al terminar de cargar, abrir "¿Vas a ir?" (se consume una vez).</summary>
        public bool Inscribir { get; private set; }
        /// <summary>false cuando la abre Atrás / Adelante: la dirección ya es la correcta.</summary>
        public bool ConHistorial { get; private set; }

        /// <summary>Se pidió abrir, cambiar o cerrar la hoja.</summary>
        public event Action? Cambio;

        public void AbrirEvento(int id, bool inscribir = false) => Abrir(TipoHoja.Evento, id, inscribir);

        public void AbrirLugar(int id) => Abrir(TipoHoja.Lugar, id, false);

        public void Abrir(TipoHoja tipo, int id, bool inscribir, bool historial = true)
        {
            Tipo = tipo;
            Id = id;
            Inscribir = inscribir;
            ConHistorial = historial;
            Cambio?.Invoke();
        }

        public void Cerrar()
        {
            if (Tipo == null) return;
            Tipo = null;
            Inscribir = false;
            Cambio?.Invoke();
        }

        /// <summary>La hoja ya se cerró sola (con Atrás): se olvida sin avisar.</summary>
        internal void Olvidar()
        {
            Tipo = null;
            Inscribir = false;
        }

        /// <summary>Lo llama la hoja cuando ya atendió <see cref="Inscribir"/>.</summary>
        public bool TomarInscribir()
        {
            bool inscribir = Inscribir;
            Inscribir = false;
            return inscribir;
        }
    }

    public enum TipoHoja { Evento, Lugar }
}
