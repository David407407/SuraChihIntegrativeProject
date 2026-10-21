using Interfaz.Servicios;
using Microsoft.AspNetCore.Components;

namespace Interfaz.Estado
{
    /// <summary>
    /// Lo que hacen los botones comunes a todas las pantallas (navbar, menú de cuenta, tarjetas, pie...).
    /// Las que aún no tienen pantalla muestran "llega en la siguiente versión".
    /// </summary>
    /// <remarks>
    /// Al construir una pantalla nueva, agrega su ruta en <see cref="Rutas"/> (y su @page en Paginas/).
    /// </remarks>
    public sealed class Acciones(NavigationManager nav, Sesion sesion, Avisos avisos, Modales modales, Hojas hojas, InteropJs js)
    {
        /// <summary>Rutas de las pantallas (las mismas de los @page).</summary>
        public static class Rutas
        {
            public const string Landing = "";
            public const string Home = "home";
            public const string Mapa = "mapa";
            public const string Perfil = "perfil";
            public const string Favoritos = "favoritos";
        }

        /// <summary>Ruta de la pantalla actual, sin "/" ni query ni #: "", "home", "mapa"...</summary>
        public string RutaActual
        {
            get
            {
                string relativa = nav.ToBaseRelativePath(nav.Uri);
                int corte = relativa.IndexOfAny(['?', '#']);
                return (corte >= 0 ? relativa[..corte] : relativa).Trim('/');
            }
        }

        /// <summary>Cambia de pantalla; si ya se está en ella, sube al inicio de la página.</summary>
        public void IrA(string ruta)
        {
            if (RutaActual == ruta) _ = js.SubirAsync();
            else nav.NavigateTo(ruta);
        }

        public void Proximamente(string que) => avisos.Proximamente(que);

        /// <summary>Sin sesión: modal "Inicia sesión para continuar". Con sesión, la pantalla aún no existe.</summary>
        public void RequiereSesion(string que)
        {
            if (sesion.Iniciada) Proximamente(que);
            else modales.RequiereSesion();
        }

        // ---------------------------------------------------------------- Navegación

        /// <summary>Logo y enlace "Inicio" del navbar: al Home (la landing solo es la primera pantalla de la app).</summary>
        public void Inicio() => IrA(Rutas.Home);
        public void Mapa() => IrA(Rutas.Mapa);
        /// <summary>Botones "Explorar" de la landing: llevan al Home.</summary>
        public void ExplorarEventos() => IrA(Rutas.Home);
        public void ExplorarLugares() => IrA(Rutas.Home);
        /// <summary>"Ver todo" de las filas de eventos del Home: la pantalla de resultados aún no existe.</summary>
        public void VerTodosEventos() => Proximamente("Explorar eventos");
        /// <summary>Buscador del navbar.</summary>
        public void Buscar(string texto) => Proximamente($"La búsqueda de “{texto}”");

        // ---------------------------------------------------------------- Cuenta

        public void IniciarSesion() => modales.IniciarSesion();

        public void CrearCuenta()
        {
            if (sesion.Iniciada) avisos.Mostrar("Ya tienes una sesión iniciada.", TipoAviso.Info);
            else modales.CrearCuenta();
        }

        public void MisPlanes() => SoloConSesion(() => Proximamente("Mis planes"));
        public void Favoritos() => SoloConSesion(() => IrA(Rutas.Favoritos));
        public void MiPerfil() => SoloConSesion(() => IrA(Rutas.Perfil));
        public void PanelOrganizador() => Proximamente("El panel de organizador");
        public void Moderacion() => Proximamente("Moderación");
        public void PublicarEvento() => RequiereSesion("Publicar eventos");

        public void CerrarSesion()
        {
            sesion.Cerrar();
            avisos.Mostrar("Cerraste sesión");
        }

        private void SoloConSesion(Action accion)
        {
            if (sesion.Iniciada) accion();
            else modales.RequiereSesion();
        }

        // ---------------------------------------------------------------- Detalles (se abren encima de cualquier pantalla)

        public void VerEvento(int id) => hojas.AbrirEvento(id);
        public void VerLugar(int id) => hojas.AbrirLugar(id);

        /// <summary>"Voy a ir" de las tarjetas: abre el detalle con "¿Vas a ir?" encima (o pide iniciar sesión).</summary>
        public void Inscribirse(int id)
        {
            if (sesion.Iniciada) hojas.AbrirEvento(id, inscribir: true);
            else modales.RequiereSesion();
        }
    }
}
