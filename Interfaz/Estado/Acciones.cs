using BackendLogica.Modelos;
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
            /// <summary>"Mis planes" (WBS 4.4).</summary>
            public const string MisPlanes = "mis-planes";
            /// <summary>Pantalla "Reseñas" de un evento o lugar.</summary>
            public static string Resenas(TipoResena tipo, int id) => $"resenas/{(tipo == TipoResena.Evento ? "evento" : "lugar")}/{id}";
            public const string Moderacion = "moderacion";
            /// <summary>"Resultados de búsqueda"; los filtros van en la consulta (ver <see cref="FiltroBusqueda.Ruta"/>).</summary>
            public const string Buscar = "buscar";
            /// <summary>"Moderación / Revisar evento" de un evento pendiente.</summary>
            public static string RevisarEvento(int id) => $"moderacion/evento/{id}";
            /// <summary>"Publicar evento" (WBS 4.6).</summary>
            public const string PublicarEvento = "organizador/evento/nuevo";
            /// <summary>La misma pantalla de "Publicar evento", con los datos de un evento propio.</summary>
            public static string EditarEvento(int id) => $"organizador/evento/{id}/editar";
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
        /// <summary>"Ver todo" de las filas de eventos del Home: todos los eventos próximos.</summary>
        public void VerTodosEventos() => Buscar(new FiltroBusqueda { Tipos = [TipoBusqueda.Eventos] });
        /// <summary>Buscador del navbar.</summary>
        public void Buscar(string texto) => Buscar(new FiltroBusqueda { Texto = texto });
        /// <summary>Resultados con esos filtros (Filtros, Calendario, píldoras de los resultados).</summary>
        public void Buscar(FiltroBusqueda filtro) => nav.NavigateTo(filtro.Ruta());

        // ---------------------------------------------------------------- Cuenta

        public void IniciarSesion() => modales.IniciarSesion();

        public void CrearCuenta()
        {
            if (sesion.Iniciada) avisos.Mostrar("Ya tienes una sesión iniciada.", TipoAviso.Info);
            else modales.CrearCuenta();
        }

        public void MisPlanes() => SoloConSesion(() => IrA(Rutas.MisPlanes));
        public void Favoritos() => SoloConSesion(() => IrA(Rutas.Favoritos));
        public void MiPerfil() => SoloConSesion(() => IrA(Rutas.Perfil));
        public void PanelOrganizador() => Proximamente("El panel de organizador");
        /// <summary>Menú de cuenta (solo moderadores): "Moderación / Publicaciones".</summary>
        public void Moderacion() => SoloConSesion(() => IrA(Rutas.Moderacion));
        public void RevisarEvento(int id) => IrA(Rutas.RevisarEvento(id));

        /// <summary>
        /// "Publicar evento" / "Publicar mi evento": sin sesión pide iniciarla; solo los organizadores aprobados
        /// entran a la pantalla (la solicitud para ser organizador aún no tiene pantalla).
        /// </summary>
        public void PublicarEvento()
        {
            if (!sesion.Iniciada) modales.RequiereSesion("Inicia sesión con tu cuenta de organizador para publicar eventos.");
            else if (!sesion.EsOrganizador) avisos.Mostrar("Solo los organizadores aprobados pueden publicar eventos.", TipoAviso.Info);
            else IrA(Rutas.PublicarEvento);
        }

        /// <summary>Editar un evento propio (la misma pantalla de "Publicar evento").</summary>
        public void EditarEvento(int id) => SoloConSesion(() => IrA(Rutas.EditarEvento(id)));

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

        /// <summary>"Ver reseñas (N)" / "Ver todas las reseñas": pantalla Reseñas (la hoja de detalle se cierra sola).</summary>
        public void VerResenas(TipoResena tipo, int id) => IrA(Rutas.Resenas(tipo, id));

        /// <summary>
        /// "Escribir reseña": sin sesión pide iniciarla; con sesión abre la modal.
        /// </summary>
        /// <returns>true si se publicó (o editó) la reseña.</returns>
        public async Task<bool> EscribirResenaAsync(TipoResena tipo, int id, string titulo)
        {
            if (!sesion.Iniciada)
            {
                modales.RequiereSesion("Inicia sesión para escribir reseñas.");
                return false;
            }
            bool publicada = await modales.EscribirResenaAsync(tipo, id, titulo);
            if (publicada) avisos.Mostrar("¡Gracias! Publicamos tu reseña");
            return publicada;
        }

        /// <summary>Compartir de la portada del detalle: el enlace del evento (/e/N) o lugar (/l/N).</summary>
        public void Compartir(TipoResena tipo, int id, string titulo) =>
            modales.Compartir(titulo, $"{SitioPublico}/{(tipo == TipoResena.Evento ? "e" : "l")}/{id}");

        /// <summary>Dirección pública de SuraChih que se comparte (la del diseño: "surachih.com/e/evento").</summary>
        public const string SitioPublico = "https://surachih.com";
        public void VerLugar(int id) => hojas.AbrirLugar(id);

        /// <summary>"Voy a ir" de las tarjetas: abre el detalle con "¿Vas a ir?" encima (o pide iniciar sesión).</summary>
        public void Inscribirse(int id)
        {
            if (sesion.Iniciada) hojas.AbrirEvento(id, inscribir: true);
            else modales.RequiereSesion();
        }
    }
}
