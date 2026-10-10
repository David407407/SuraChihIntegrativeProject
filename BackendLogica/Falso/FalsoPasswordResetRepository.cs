using BackendLogica.Contratos;
using BackendLogica.Repositorios;
using BackendLogica.Seguridad;

namespace BackendLogica.Falso
{
    /// <summary>
    /// <see cref="IPasswordResetRepository"/> en memoria. No manda correos: la pantalla recibe el token
    /// igual que con MySQL y decide qué hacer con él.
    /// </summary>
    public sealed class FalsoPasswordResetRepository : RepositorioFalso, IPasswordResetRepository
    {
        internal FalsoPasswordResetRepository(AlmacenFalso almacen) : base(almacen) { }

        public Task<string?> SolicitarAsync(string correo, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var usuario = a.Usuarios.FirstOrDefault(u => u.Activo && AlmacenFalso.Igual(u.Correo, correo.Trim()));
                if (usuario == null) return null;

                foreach (var anterior in a.Recuperaciones.Where(r => r.UsuarioId == usuario.Id && r.UsadoEn == null))
                    anterior.UsadoEn = Ahora;

                string token = Tokens.Generar();
                a.Recuperaciones.Add(new FilaRecuperacion
                {
                    Id = a.SiguienteRecuperacion(), UsuarioId = usuario.Id,
                    TokenHash = Tokens.Hashear(token), ExpiraEn = Ahora.Add(RecuperacionContrasenaDB.Vigencia)
                });
                return (string?)token;
            }, ct);

        public Task<bool> RestablecerAsync(string token, string nuevaContrasena, CancellationToken ct = default) =>
            Hacer(a =>
            {
                Contrasenas.Validar(nuevaContrasena);
                string hash = Tokens.Hashear(token.Trim());
                var fila = a.Recuperaciones.FirstOrDefault(r => r.TokenHash == hash && r.UsadoEn == null && r.ExpiraEn > Ahora);
                if (fila == null) return false;

                a.Usuario(fila.UsuarioId)!.Hash = Contrasenas.Hashear(nuevaContrasena);
                fila.UsadoEn = Ahora;
                return true;
            }, ct);
    }
}
