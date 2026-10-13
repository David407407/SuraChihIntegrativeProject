using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using BackendLogica.Seguridad;
using SuraChihIntegrativeProject.Puente;
using SuraChihIntegrativeProject.Servicios;

namespace SuraChihIntegrativeProject.Controladores
{
    /// <summary>
    /// Acciones de los modales de cuenta y del menú de cuenta (WBS 4.2, wwwroot/js/componentes/modales).
    /// Los datos van a las tablas <c>user</c> y <c>password_reset</c> a través de BackendLogica
    /// (<see cref="IUserRepository"/> e <see cref="IPasswordResetRepository"/>).
    /// </summary>
    /// <remarks>
    /// Los errores salen como <see cref="SuraChihException"/> con un mensaje listo para mostrar. Para que la
    /// interfaz sepa en qué campo pintarlo, los de formato empiezan con el mismo texto que usa
    /// <c>Validar</c> de BackendLogica (ver <c>campoDeError</c> en modales/cuenta.js).
    /// </remarks>
    public sealed class ControladorCuenta
    {
        private readonly ISuraChihBD _bd;
        private readonly Sesion _sesion;
        private readonly CorreoSimulado _correo;

        public ControladorCuenta(ISuraChihBD bd, Sesion sesion, CorreoSimulado correo)
        {
            _bd = bd;
            _sesion = sesion;
            _correo = correo;
        }

        public void Registrar(PuenteWeb puente)
        {
            puente.Registrar("sesion.obtener", () => Task.FromResult<object?>(_sesion.ParaInterfaz()));
            puente.Registrar("sesion.cerrar", () =>
            {
                _sesion.Cerrar();
                return Task.FromResult<object?>(null);
            });
            puente.Registrar<DatosInicioSesion>("cuenta.iniciarSesion", async d => await IniciarSesionAsync(d));
            puente.Registrar<DatosRegistro>("cuenta.registrar", async d => await RegistrarAsync(d));
            puente.Registrar<DatosRecuperacion>("cuenta.recuperar", async d => await SolicitarRecuperacionAsync(d));
            puente.Registrar("cuenta.abrirEnlace", () => Task.FromResult<object?>(AbrirEnlaceDelCorreo()));
            puente.Registrar<DatosContrasenaNueva>("cuenta.restablecer", async d => await RestablecerAsync(d));
        }

        // ---------------------------------------------------------------- Iniciar sesión

        private async Task<UsuarioSesion> IniciarSesionAsync(DatosInicioSesion d)
        {
            if (string.IsNullOrWhiteSpace(d.Identificador) || string.IsNullOrEmpty(d.Contrasena))
                throw new DatosInvalidosException("Escribe tu correo y tu contraseña.");

            Usuario usuario = await _bd.Usuarios.IniciarSesionAsync(d.Identificador, d.Contrasena)
                ?? throw new DatosInvalidosException("Correo o contraseña incorrectos.");
            return await AbrirSesionAsync(usuario);
        }

        // ---------------------------------------------------------------- Crear cuenta

        private async Task<UsuarioSesion> RegistrarAsync(DatosRegistro d)
        {
            // BackendLogica valida usuario (3-30, letras, números, punto o guion bajo, igual que el CHECK
            // ck_user_username), correo y contraseña; aquí solo lo que es de la pantalla.
            if (d.Contrasena != d.Confirmacion)
                throw new DatosInvalidosException("Las contraseñas no coinciden.");

            Usuario usuario = await _bd.Usuarios.RegistrarAsync(d.NombreUsuario ?? "", d.Correo ?? "", d.Contrasena ?? "");
            return await AbrirSesionAsync(usuario);
        }

        private async Task<UsuarioSesion> AbrirSesionAsync(Usuario usuario)
        {
            bool esOrganizador = await _bd.Organizadores.EsAprobadoAsync(usuario.Id);
            _sesion.Iniciar(usuario, esOrganizador);
            return _sesion.ParaInterfaz()!;
        }

        // ---------------------------------------------------------------- Recuperar contraseña

        /// <summary>
        /// Siempre responde igual, exista o no la cuenta, para no revelar qué correos están registrados.
        /// </summary>
        private async Task<object?> SolicitarRecuperacionAsync(DatosRecuperacion d)
        {
            string correo = (d.Correo ?? "").Trim().ToLowerInvariant();
            Validar.Correo(correo);

            string? token = await _bd.RecuperacionContrasena.SolicitarAsync(correo);
            if (token != null) _correo.Enviar(correo, token);
            return null;
        }

        /// <summary>"Abrir enlace del correo": mientras no haya SMTP, entrega el token del último enlace.</summary>
        private object AbrirEnlaceDelCorreo()
        {
            string token = _correo.TomarUltimoToken()
                ?? throw new DatosInvalidosException("No encontramos un enlace reciente. Revisa el correo que escribiste y pide uno nuevo.");
            return new { token };
        }

        private async Task<object?> RestablecerAsync(DatosContrasenaNueva d)
        {
            if (string.IsNullOrWhiteSpace(d.Token))
                throw new DatosInvalidosException("El enlace no es válido. Pide uno nuevo.");
            if (d.Contrasena != d.Confirmacion)
                throw new DatosInvalidosException("Las contraseñas no coinciden.");

            bool cambiada = await _bd.RecuperacionContrasena.RestablecerAsync(d.Token, d.Contrasena ?? "");
            if (!cambiada)
                throw new ReglaNegocioException("El enlace ya expiró o ya se usó. Pide uno nuevo.");
            return null;
        }

        private sealed record DatosInicioSesion(string? Identificador, string? Contrasena);
        private sealed record DatosRegistro(string? NombreUsuario, string? Correo, string? Contrasena, string? Confirmacion);
        private sealed record DatosRecuperacion(string? Correo);
        private sealed record DatosContrasenaNueva(string? Token, string? Contrasena, string? Confirmacion);
    }
}
