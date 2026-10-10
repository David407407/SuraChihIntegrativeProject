using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using BackendLogica.Seguridad;

namespace BackendLogica.Falso
{
    /// <summary><see cref="IUserRepository"/> en memoria. Mismas validaciones y mensajes que <c>UsuarioDB</c>.</summary>
    public sealed class FalsoUserRepository : RepositorioFalso, IUserRepository
    {
        internal FalsoUserRepository(AlmacenFalso almacen) : base(almacen) { }

        public Task<Usuario> RegistrarAsync(string nombreUsuario, string correo, string contrasena, CancellationToken ct = default) =>
            Hacer(a =>
            {
                nombreUsuario = nombreUsuario.Trim();
                correo = correo.Trim().ToLowerInvariant();
                Validar.NombreUsuario(nombreUsuario);
                Validar.Correo(correo);
                Contrasenas.Validar(contrasena);
                RevisarDuplicados(a, nombreUsuario, correo, excepto: null);

                var fila = new FilaUsuario
                {
                    Id = AlmacenFalso.Siguiente(a.Usuarios, u => u.Id),
                    NombreUsuario = nombreUsuario, Correo = correo, Hash = Contrasenas.Hashear(contrasena),
                    Activo = true, CreadoEn = Ahora
                };
                a.Usuarios.Add(fila);
                return fila.ADatos();
            }, ct);

        public Task<Usuario?> IniciarSesionAsync(string identificador, string contrasena, CancellationToken ct = default) =>
            Hacer(a =>
            {
                identificador = identificador.Trim();
                var fila = a.Usuarios.FirstOrDefault(u => u.Activo &&
                    (AlmacenFalso.Igual(u.Correo, identificador) || AlmacenFalso.Igual(u.NombreUsuario, identificador)));
                return fila != null && Contrasenas.Verificar(contrasena, fila.Hash) ? fila.ADatos() : null;
            }, ct);

        public Task<Usuario?> ObtenerAsync(int usuarioId, CancellationToken ct = default) =>
            Hacer(a => a.Usuario(usuarioId)?.ADatos(), ct);

        public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default) =>
            Hacer(a => a.Usuarios.Any(u => AlmacenFalso.Igual(u.NombreUsuario, nombreUsuario.Trim())), ct);

        public Task<bool> ExisteCorreoAsync(string correo, CancellationToken ct = default) =>
            Hacer(a => a.Usuarios.Any(u => AlmacenFalso.Igual(u.Correo, correo.Trim())), ct);

        public Task<bool> ActualizarPerfilAsync(int usuarioId, string nombreUsuario, string correo, string? avatarUrl, CancellationToken ct = default) =>
            Hacer(a =>
            {
                nombreUsuario = nombreUsuario.Trim();
                correo = correo.Trim().ToLowerInvariant();
                Validar.NombreUsuario(nombreUsuario);
                Validar.Correo(correo);

                var fila = a.Usuario(usuarioId);
                if (fila is not { Activo: true }) return false;
                RevisarDuplicados(a, nombreUsuario, correo, excepto: usuarioId);
                fila.NombreUsuario = nombreUsuario;
                fila.Correo = correo;
                fila.AvatarUrl = avatarUrl;
                return true;
            }, ct);

        public Task<bool> CambiarContrasenaAsync(int usuarioId, string actual, string nueva, CancellationToken ct = default) =>
            Hacer(a =>
            {
                Contrasenas.Validar(nueva);
                var fila = a.Usuario(usuarioId);
                if (fila is not { Activo: true } || !Contrasenas.Verificar(actual, fila.Hash)) return false;
                fila.Hash = Contrasenas.Hashear(nueva);
                return true;
            }, ct);

        public Task<bool> DesactivarAsync(int usuarioId, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var fila = a.Usuario(usuarioId);
                if (fila == null) return false;
                fila.Activo = false;
                return true;
            }, ct);

        /// <summary>Llaves UNIQUE uq_user_username y uq_user_email (con los mismos mensajes que TraductorErrores).</summary>
        private static void RevisarDuplicados(AlmacenFalso a, string nombreUsuario, string correo, int? excepto)
        {
            var otros = a.Usuarios.Where(u => u.Id != excepto).ToList();
            if (otros.Any(u => AlmacenFalso.Igual(u.Correo, correo)))
                throw new DuplicadoException("Ese correo ya está registrado.");
            if (otros.Any(u => AlmacenFalso.Igual(u.NombreUsuario, nombreUsuario)))
                throw new DuplicadoException("Ese nombre de usuario ya está ocupado.");
        }
    }
}
