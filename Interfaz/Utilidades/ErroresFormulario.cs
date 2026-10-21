namespace Interfaz.Utilidades
{
    /// <summary>
    /// Errores de un formulario, por campo. Se pasa a cada <c>&lt;Campo Errores="errores"&gt;</c>: el campo
    /// pinta su mensaje (borde rojo) y lo quita en cuanto el usuario vuelve a escribir.
    /// </summary>
    /// <example>
    /// <code>
    /// errores.Poner("correo", "Escribe un correo válido.");   // y el foco va a ese campo
    /// errores.Aviso = "Algo salió mal.";                       // mensaje que no es de ningún campo
    /// </code>
    /// </example>
    public sealed class ErroresFormulario
    {
        private readonly Dictionary<string, string> _porCampo = new(StringComparer.Ordinal);

        public string? this[string campo] => _porCampo.GetValueOrDefault(campo);

        /// <summary>Mensaje general (arriba del formulario o en un aviso de la modal).</summary>
        public string? Aviso { get; set; }

        /// <summary>El aviso es informativo (gris), no un error (rojo).</summary>
        public bool AvisoInformativo { get; set; }

        /// <summary>Campo que debe recibir el foco en el siguiente pintado (lo atiende el propio campo).</summary>
        public string? Enfocar { get; private set; }

        public bool HayErrores => _porCampo.Count > 0 || Aviso != null;

        public void Poner(string campo, string mensaje, bool enfocar = true)
        {
            _porCampo[campo] = mensaje;
            if (enfocar) Enfocar = campo;
        }

        public void Quitar(string campo) => _porCampo.Remove(campo);

        public void PonerAviso(string mensaje, bool informativo = false)
        {
            Aviso = mensaje;
            AvisoInformativo = informativo;
        }

        public void Limpiar()
        {
            _porCampo.Clear();
            Aviso = null;
            AvisoInformativo = false;
            Enfocar = null;
        }

        /// <summary>Pide el foco para un campo sin marcarlo con error (p. ej. la contraseña si ya viene el correo).</summary>
        public void PedirFoco(string campo) => Enfocar = campo;

        internal bool TomarFoco(string campo)
        {
            if (Enfocar != campo) return false;
            Enfocar = null;
            return true;
        }
    }
}
