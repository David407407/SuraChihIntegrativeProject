using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using BackendLogica.Seguridad;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Modales de cuenta y menú de cuenta (WBS 4.2, Componentes/Modales). Los datos van a las tablas
    /// <c>user</c> y <c>password_reset</c> a través de BackendLogica
    /// (<see cref="IUserRepository"/> e <see cref="IPasswordResetRepository"/>).
    /// </summary>
    /// <remarks>
    /// Los errores salen como <see cref="SuraChihException"/> con un mensaje listo para mostrar. Para que la
    /// interfaz sepa en qué campo pintarlo, los de formato empiezan con el mismo texto que usa
    /// <c>Validar</c> de BackendLogica (ver <c>Utilidades/ReglasCuenta.cs</c>).
    /// </remarks>
    public sealed class ServicioCuenta(ISuraChihBD bd, Sesion sesion, CorreoSimulado correo)
    {
        // ---------------------------------------------------------------- Iniciar sesión

        public async Task<Usuario> IniciarSesionAsync(string? identificador, string? contrasena)
        {
            if (string.IsNullOrWhiteSpace(identificador) || string.IsNullOrEmpty(contrasena))
                throw new DatosInvalidosException("Escribe tu correo o usuario y tu contraseña.");

            // identificador: correo o nombre de usuario (UsuarioDB busca en los dos)
            Usuario usuario = await bd.Usuarios.IniciarSesionAsync(identificador.Trim(), contrasena)
                ?? throw new DatosInvalidosException("Usuario, correo o contraseña incorrectos.");
            return await AbrirSesionAsync(usuario);
        }

        // ---------------------------------------------------------------- Crear cuenta

        public async Task<Usuario> RegistrarAsync(string? nombreUsuario, string? correoElectronico, string? contrasena, string? confirmacion)
        {
            // BackendLogica valida usuario (3-30, letras, números, punto o guion bajo, igual que el CHECK
            // ck_user_username), correo y contraseña; aquí solo lo que es de la pantalla.
            if (contrasena != confirmacion)
                throw new DatosInvalidosException("Las contraseñas no coinciden.");

            Usuario usuario = await bd.Usuarios.RegistrarAsync(nombreUsuario ?? "", correoElectronico ?? "", contrasena ?? "");
            return await AbrirSesionAsync(usuario);
        }

        private async Task<Usuario> AbrirSesionAsync(Usuario usuario)
        {
            bool esOrganizador = await bd.Organizadores.EsAprobadoAsync(usuario.Id);
            sesion.Iniciar(usuario, esOrganizador);
            return usuario;
        }

        public void CerrarSesion() => sesion.Cerrar();

        // ---------------------------------------------------------------- Recuperar contraseña

        /// <summary>
        /// Siempre responde igual, exista o no la cuenta, para no revelar qué correos están registrados.
        /// </summary>
        public async Task SolicitarRecuperacionAsync(string? correoElectronico)
        {
            string limpio = (correoElectronico ?? "").Trim().ToLowerInvariant();
            Validar.Correo(limpio);

            string? token = await bd.RecuperacionContrasena.SolicitarAsync(limpio);
            if (token != null) correo.Enviar(limpio, token);
        }

        /// <summary>"Abrir enlace del correo": mientras no haya SMTP, entrega el token del último enlace.</summary>
        public string AbrirEnlaceDelCorreo() =>
            correo.TomarUltimoToken()
            ?? throw new DatosInvalidosException("No encontramos un enlace reciente. Revisa el correo que escribiste y pide uno nuevo.");

        public async Task RestablecerAsync(string? token, string? contrasena, string? confirmacion)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new DatosInvalidosException("El enlace no es válido. Pide uno nuevo.");
            if (contrasena != confirmacion)
                throw new DatosInvalidosException("Las contraseñas no coinciden.");

            bool cambiada = await bd.RecuperacionContrasena.RestablecerAsync(token, contrasena ?? "");
            if (!cambiada)
                throw new ReglaNegocioException("El enlace ya expiró o ya se usó. Pide uno nuevo.");
        }
    }
}
