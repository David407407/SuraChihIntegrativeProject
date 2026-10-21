using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Pantalla "Mi perfil" con la modal "Eliminar cuenta" (Paginas/Perfil.razor y
    /// Componentes/Modales/ModalEliminarCuenta.razor). Los datos van a la tabla <c>user</c> a través de BackendLogica.
    /// </summary>
    /// <remarks>
    /// Los mensajes de error se pintan en su campo según el texto (ver <c>Utilidades/ReglasCuenta.cs</c>),
    /// así que los de contraseña mencionan "contraseña" y los de duplicados son los de BackendLogica.
    /// </remarks>
    public sealed class ServicioPerfil(ISuraChihBD bd, Sesion sesion)
    {
        // ---------------------------------------------------------------- Datos de la cuenta

        /// <summary>"Guardar cambios": nombre de usuario y correo. La foto se conserva.</summary>
        /// <remarks>La sesión se actualiza, así que el navbar y el menú de cuenta cambian sin recargar.</remarks>
        public async Task ActualizarAsync(string? nombreUsuario, string? correo)
        {
            Usuario usuario = sesion.Requerir("editar tu perfil");

            // BackendLogica valida el formato y la BD (UNIQUE) los duplicados: "Ese correo ya está registrado."
            bool guardado = await bd.Usuarios.ActualizarPerfilAsync(usuario.Id, nombreUsuario ?? "", correo ?? "", usuario.AvatarUrl);
            if (!guardado) throw new ReglaNegocioException("Tu cuenta ya no está activa.");

            Usuario actualizado = await bd.Usuarios.ObtenerAsync(usuario.Id)
                ?? throw new ReglaNegocioException("Tu cuenta ya no está activa.");
            sesion.Iniciar(actualizado, sesion.EsOrganizador);
        }

        // ---------------------------------------------------------------- Contraseña

        /// <summary>"Actualizar contraseña": revisa la actual con el mismo BCrypt del inicio de sesión.</summary>
        public async Task CambiarContrasenaAsync(string? actual, string? nueva)
        {
            Usuario usuario = sesion.Requerir("cambiar tu contraseña");
            if (string.IsNullOrEmpty(actual))
                throw new DatosInvalidosException("Escribe tu contraseña actual.");

            // La nueva se valida antes (8+ caracteres, letras y números) y lanza DatosInvalidosException.
            bool cambiada = await bd.Usuarios.CambiarContrasenaAsync(usuario.Id, actual, nueva ?? "");
            if (!cambiada) throw new DatosInvalidosException("La contraseña actual no es correcta.");
        }

        // ---------------------------------------------------------------- Eliminar cuenta

        /// <summary>
        /// "Eliminar mi cuenta": pide la contraseña, desactiva la cuenta (<c>is_active = FALSE</c>, nunca DELETE,
        /// para conservar el historial). La sesión la cierra quien llama, cuando ya mostró el aviso.
        /// Una cuenta desactivada ya no puede iniciar sesión.
        /// </summary>
        public async Task EliminarAsync(string? contrasena)
        {
            Usuario usuario = sesion.Requerir("eliminar tu cuenta");
            if (string.IsNullOrEmpty(contrasena))
                throw new DatosInvalidosException("Escribe tu contraseña para confirmar.");

            bool correcta = await bd.Usuarios.IniciarSesionAsync(usuario.Correo, contrasena) is { } u && u.Id == usuario.Id;
            if (!correcta) throw new DatosInvalidosException("La contraseña no es correcta.");

            await bd.Usuarios.DesactivarAsync(usuario.Id);
        }
    }
}
