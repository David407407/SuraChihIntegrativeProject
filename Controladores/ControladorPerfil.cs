using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using SuraChihIntegrativeProject.Puente;
using SuraChihIntegrativeProject.Servicios;

namespace SuraChihIntegrativeProject.Controladores
{
    /// <summary>
    /// Pantallas "Mi perfil" (con la modal "Eliminar cuenta") y "Mis favoritos"
    /// (wwwroot/js/perfil.js y wwwroot/js/favoritos.js). Los datos van a las tablas <c>user</c>,
    /// <c>favorite</c> y <c>place_favorite</c> a través de BackendLogica.
    /// </summary>
    /// <remarks>
    /// Los mensajes de error se pintan en su campo según el texto (ver <c>campoDeError</c> en perfil.js),
    /// así que los de contraseña mencionan "contraseña" y los de duplicados son los de BackendLogica.
    /// </remarks>
    public sealed class ControladorPerfil
    {
        private readonly ISuraChihBD _bd;
        private readonly Sesion _sesion;

        public ControladorPerfil(ISuraChihBD bd, Sesion sesion)
        {
            _bd = bd;
            _sesion = sesion;
        }

        public void Registrar(PuenteWeb puente)
        {
            puente.Registrar<DatosPerfil>("perfil.actualizar", async d => await ActualizarAsync(d));
            puente.Registrar<DatosContrasena>("perfil.contrasena", async d => await CambiarContrasenaAsync(d));
            puente.Registrar<DatosEliminar>("perfil.eliminar", async d => await EliminarAsync(d));
            puente.Registrar("favoritos.listar", async () => await ListarFavoritosAsync());
        }

        // ---------------------------------------------------------------- Datos de la cuenta

        /// <summary>"Guardar cambios": nombre de usuario y correo. La foto se conserva.</summary>
        /// <returns>El usuario actualizado, para que el navbar y el menú de cuenta cambien sin recargar.</returns>
        private async Task<UsuarioSesion> ActualizarAsync(DatosPerfil d)
        {
            Usuario usuario = _sesion.Requerir("editar tu perfil");

            // BackendLogica valida el formato y la BD (UNIQUE) los duplicados: "Ese correo ya está registrado."
            bool guardado = await _bd.Usuarios.ActualizarPerfilAsync(usuario.Id, d.NombreUsuario ?? "", d.Correo ?? "", usuario.AvatarUrl);
            if (!guardado) throw new ReglaNegocioException("Tu cuenta ya no está activa.");

            Usuario actualizado = await _bd.Usuarios.ObtenerAsync(usuario.Id)
                ?? throw new ReglaNegocioException("Tu cuenta ya no está activa.");
            _sesion.Iniciar(actualizado, _sesion.EsOrganizador);
            return _sesion.ParaInterfaz()!;
        }

        // ---------------------------------------------------------------- Contraseña

        /// <summary>"Actualizar contraseña": revisa la actual con el mismo BCrypt del inicio de sesión.</summary>
        private async Task<object?> CambiarContrasenaAsync(DatosContrasena d)
        {
            Usuario usuario = _sesion.Requerir("cambiar tu contraseña");
            if (string.IsNullOrEmpty(d.Actual))
                throw new DatosInvalidosException("Escribe tu contraseña actual.");

            // La nueva se valida antes (8+ caracteres, letras y números) y lanza DatosInvalidosException.
            bool cambiada = await _bd.Usuarios.CambiarContrasenaAsync(usuario.Id, d.Actual, d.Nueva ?? "");
            if (!cambiada) throw new DatosInvalidosException("La contraseña actual no es correcta.");
            return null;
        }

        // ---------------------------------------------------------------- Eliminar cuenta

        /// <summary>
        /// "Eliminar mi cuenta": pide la contraseña, desactiva la cuenta (<c>is_active = FALSE</c>, nunca DELETE,
        /// para conservar el historial) y cierra la sesión. Una cuenta desactivada ya no puede iniciar sesión.
        /// </summary>
        private async Task<object?> EliminarAsync(DatosEliminar d)
        {
            Usuario usuario = _sesion.Requerir("eliminar tu cuenta");
            if (string.IsNullOrEmpty(d.Contrasena))
                throw new DatosInvalidosException("Escribe tu contraseña para confirmar.");

            bool correcta = await _bd.Usuarios.IniciarSesionAsync(usuario.Correo, d.Contrasena) is { } u && u.Id == usuario.Id;
            if (!correcta) throw new DatosInvalidosException("La contraseña no es correcta.");

            await _bd.Usuarios.DesactivarAsync(usuario.Id);
            _sesion.Cerrar();
            return null;
        }

        // ---------------------------------------------------------------- Mis favoritos

        /// <summary>Eventos (incluye los que ya terminaron) y lugares aprobados, los más recientes primero.</summary>
        private async Task<DatosFavoritos> ListarFavoritosAsync()
        {
            Usuario usuario = _sesion.Requerir("ver tus favoritos");
            var eventos = _bd.Favoritos.ListarAsync(usuario.Id);
            var lugares = _bd.Favoritos.ListarLugaresAsync(usuario.Id);
            await Task.WhenAll(eventos, lugares);
            return new DatosFavoritos(eventos.Result, lugares.Result);
        }

        private sealed record DatosPerfil(string? NombreUsuario, string? Correo);
        private sealed record DatosContrasena(string? Actual, string? Nueva);
        private sealed record DatosEliminar(string? Contrasena);
    }

    /// <summary>Respuesta de "favoritos.listar". La forma está documentada en wwwroot/js/contenido/datos-demo.js.</summary>
    public sealed record DatosFavoritos(
        IReadOnlyList<EventoTarjeta> Eventos,
        IReadOnlyList<LugarTarjeta> Lugares);
}
