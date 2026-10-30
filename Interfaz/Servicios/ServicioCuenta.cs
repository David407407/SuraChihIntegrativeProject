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
    public sealed class ServicioCuenta(ISuraChihBD bd, Sesion sesion, CorreoSimulado correo, ServicioGoogle google)
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

        // ---------------------------------------------------------------- Continuar con Google

        /// <summary>
        /// "Continuar con Google" (iniciar sesión y crear cuenta): abre el navegador, y con el correo que Google
        /// verificó inicia sesión en esa cuenta o crea una nueva.
        /// </summary>
        /// <remarks>
        /// Mientras la BD no tenga una tabla de cuentas externas, la cuenta se une por el correo. La cuenta nueva
        /// lleva un nombre de usuario sacado del correo y una contraseña aleatoria que nadie conoce (la BD la
        /// exige); si después quiere entrar con contraseña, usa "¿Olvidaste tu contraseña?".
        /// </remarks>
        /// <returns>El usuario y si la cuenta se acaba de crear.</returns>
        public async Task<(Usuario Usuario, bool Nueva)> ContinuarConGoogleAsync()
        {
            PerfilGoogle perfil = await google.IniciarAsync();
            if (!perfil.CorreoVerificado)
                throw new ReglaNegocioException("Tu cuenta de Google no tiene el correo verificado.");
            string correoGoogle = perfil.Correo.Trim().ToLowerInvariant();

            if (await bd.Usuarios.ObtenerPorCorreoAsync(correoGoogle) is { } existente)
            {
                if (!existente.Activo)
                    throw new ReglaNegocioException("La cuenta con ese correo fue eliminada.");
                return (await AbrirSesionAsync(existente), false);
            }

            string nombre = await NombreDisponibleAsync(correoGoogle);
            Usuario nuevo = await bd.Usuarios.RegistrarAsync(nombre, correoGoogle, ContrasenaAleatoria());
            if (perfil.FotoUrl is { Length: <= 500 } foto && foto.StartsWith("https://"))
            {
                // La foto es un extra: si no se guarda, la cuenta igual queda creada.
                try
                {
                    if (await bd.Usuarios.ActualizarPerfilAsync(nuevo.Id, nuevo.NombreUsuario, nuevo.Correo, foto))
                        nuevo = await bd.Usuarios.ObtenerAsync(nuevo.Id) ?? nuevo;
                }
                catch (SuraChihException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Google] foto no guardada: {ex.Message}");
                }
            }
            return (await AbrirSesionAsync(nuevo), true);
        }

        /// <summary>"betsy.nayeli2310@gmail.com" → "betsy.nayeli2310" (o "betsy.nayeli23102" si ya existe).</summary>
        private async Task<string> NombreDisponibleAsync(string correoGoogle)
        {
            string local = correoGoogle.Split('@')[0];
            string limpio = new(local.Where(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.').ToArray());
            if (limpio.Length < 3) limpio = $"usuario{limpio}";
            if (limpio.Length > 26) limpio = limpio[..26];   // deja lugar para el número

            if (!await bd.Usuarios.ExisteNombreUsuarioAsync(limpio)) return limpio;
            for (int i = 2; i < 10000; i++)
            {
                string candidato = $"{limpio}{i}";
                if (!await bd.Usuarios.ExisteNombreUsuarioAsync(candidato)) return candidato;
            }
            return $"{limpio[..Math.Min(limpio.Length, 20)]}{Random.Shared.Next(100000, 999999)}";
        }

        /// <summary>Contraseña que nadie conoce (letras y números, como pide la BD).</summary>
        private static string ContrasenaAleatoria() =>
            $"G{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))}a9";

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
