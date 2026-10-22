using Microsoft.FluentUI.AspNetCore.Components;

namespace Interfaz.Estado
{
    /// <summary>
    /// Avisos flotantes ("Toast / Cambios guardados" de Figma). Son FluentToast de Fluent UI (los pinta el
    /// FluentProviders de la plantilla) con la forma de Figma en css/controles.css.
    /// </summary>
    /// <example>
    /// <code>
    /// Avisos.Mostrar("Cambios guardados");
    /// Avisos.Mostrar("No se pudo conectar", TipoAviso.Error);
    /// </code>
    /// </example>
    public sealed class Avisos(INotificationService notificaciones)
    {
        private static readonly TimeSpan Duracion = TimeSpan.FromMilliseconds(3200);

        public void Mostrar(string mensaje, TipoAviso tipo = TipoAviso.Exito) =>
            _ = notificaciones.ShowToastAsync(opciones =>
            {
                opciones.Message = mensaje;
                opciones.Intent = tipo switch
                {
                    TipoAviso.Error => ToastIntent.Error,
                    TipoAviso.Info => ToastIntent.Info,
                    _ => ToastIntent.Success,
                };
                opciones.Class = $"toast toast--{tipo.ToString().ToLowerInvariant()}";
                opciones.Inverted = true;   // fondo tinta, como en Figma
                opciones.Position = ToastPosition.BottomCenter;
                opciones.VerticalOffset = 32;
                opciones.Lifetime = Duracion;
                opciones.AllowDismiss = false;
            });

        /// <summary>"Compartir eventos llega en la siguiente versión."</summary>
        public void Proximamente(string que) => Mostrar($"{que} llega en la siguiente versión.", TipoAviso.Info);
    }

    public enum TipoAviso { Exito, Info, Error }
}
