/**
 * @file Punto de entrada de la landing.
 *
 * 1. Registra los componentes (cada import define sus etiquetas <sc-...>).
 * 2. Pide los datos a C# (o a los datos demo en un navegador) y se los pasa a cada sección.
 * 3. Atiende las acciones de toda la página con delegación: cualquier elemento con
 *    `data-accion` (botones, enlaces del pie...) y los eventos `sc:*` de los componentes.
 */

import './componentes/sc-imagen.js';
import './componentes/sc-boton-favorito.js';
import './componentes/sc-chip-categoria.js';
import './componentes/secciones/sc-navbar.js';
import './componentes/secciones/sc-hero.js';
import './componentes/secciones/sc-secciones-fijas.js';
import './componentes/secciones/sc-secciones-datos.js';

import { invocar, enApp, ErrorPuente } from './nucleo/puente.js';
import { mostrarToast } from './componentes/sc-toast.js';
import { abrirIniciarSesion, abrirCrearCuenta, abrirRequiereSesion } from './componentes/modales/cuenta.js';
import { alternarMenuCuenta, cerrarMenuCuenta } from './componentes/menu-cuenta.js';
import { observarRevelado } from './utilidades/animaciones.js';

const secciones = {
  navbar: document.querySelector('sc-navbar'),
  hero: document.querySelector('sc-hero'),
  eventos: document.querySelector('sc-eventos-destacados'),
  lugares: document.querySelector('sc-lugares'),
};

// ---------------------------------------------------------------------
// Datos
// ---------------------------------------------------------------------

async function cargarLanding() {
  try {
    const datos = await invocar('landing.obtener');
    secciones.hero.datos = { resumen: datos.resumen, collage: datos.collage };
    secciones.eventos.datos = datos.eventos;
    secciones.lugares.datos = datos.lugares;
  } catch (error) {
    console.error(error);
    secciones.hero.datos = { resumen: null, collage: [] };
    secciones.eventos.datos = [];
    secciones.lugares.datos = [];
    mostrarToast(error.message || 'No se pudieron cargar los datos.', { tipo: 'error' });
  }
}

/** Usuario con sesión (UsuarioSesion de C#) o null para el visitante. */
let usuarioActual = null;

function ponerUsuario(usuario) {
  usuarioActual = usuario ?? null;
  cerrarMenuCuenta();
  secciones.navbar.datos = { usuario: usuarioActual };
}

/** Navbar Visitante o Logueado según la sesión de C#. */
async function cargarSesion() {
  try {
    ponerUsuario(await invocar('sesion.obtener'));
  } catch (error) {
    console.error(error);   // sin sesión se queda como visitante
  }
}

async function cerrarSesion() {
  try {
    await invocar('sesion.cerrar');
    ponerUsuario(null);
    mostrarToast('Cerraste sesión');
  } catch (error) {
    mostrarToast(error.message, { tipo: 'error' });
  }
}

// Lo lanzan los modales de cuenta al iniciar sesión o crear la cuenta.
document.addEventListener('sc:sesion', ({ detail }) => {
  ponerUsuario(detail.usuario);
  const nombre = detail.usuario.nombreUsuario;
  mostrarToast(detail.nuevo ? `¡Bienvenido a SuraChih, ${nombre}!` : `¡Hola de nuevo, ${nombre}!`);
});

// ---------------------------------------------------------------------
// Acciones (las pantallas que faltan se construirán en las siguientes entregas)
// ---------------------------------------------------------------------

/** Qué hace cada `data-accion`. Las que aún no tienen pantalla muestran un aviso. */
const acciones = {
  'explorar-eventos': () => proximamente('Explorar eventos'),
  'explorar-lugares': () => proximamente('Explorar lugares'),
  'mapa': () => proximamente('El mapa'),
  'iniciar-sesion': () => abrirIniciarSesion(),
  'crear-cuenta': () => (usuarioActual ? mostrarToast('Ya tienes una sesión iniciada.', { tipo: 'info' }) : abrirCrearCuenta()),
  'mis-planes': () => (usuarioActual ? proximamente('Mis planes') : abrirRequiereSesion()),
  'favoritos': () => (usuarioActual ? proximamente('Mis favoritos') : abrirRequiereSesion()),
  'menu-cuenta': (_id, origen) => alternarMenuCuenta(origen, usuarioActual),
  // Menú de cuenta
  'mi-perfil': () => proximamente('Mi perfil'),
  'panel-organizador': () => proximamente('El panel de organizador'),
  'moderacion': () => proximamente('Moderación'),
  'cerrar-sesion': () => cerrarSesion(),
  'publicar-evento': () => requiereSesion('Publicar eventos'),
  'inscribirse': () => requiereSesion('La inscripción a eventos'),
  'ver-evento': (id) => proximamente(`El detalle del evento #${id}`),
  'ver-lugar': (id) => proximamente(`El detalle del lugar #${id}`),
};

function proximamente(que) {
  mostrarToast(`${que} llega en la siguiente versión.`, { tipo: 'info' });
}

/** Sin sesión: modal "Inicia sesión para continuar". Con sesión, la pantalla aún no existe. */
function requiereSesion(que) {
  if (usuarioActual) proximamente(que);
  else abrirRequiereSesion();
}

document.addEventListener('click', (e) => {
  const origen = e.target.closest('[data-accion]');
  const accion = origen?.dataset.accion;
  if (!accion || !acciones[accion]) return;
  e.preventDefault();
  acciones[accion](origen.dataset.id ? Number(origen.dataset.id) : undefined, origen);
});

document.addEventListener('sc:buscar', (e) => proximamente(`La búsqueda de “${e.detail.texto}”`));

document.addEventListener('sc:favorito', async ({ detail }) => {
  try {
    const activo = await invocar('favoritos.alternar', { tipo: detail.tipo, id: detail.id });
    detail.boton.activo = activo;
    mostrarToast(activo ? 'Guardado en favoritos' : 'Quitado de favoritos');
  } catch (error) {
    detail.boton.revertir();
    if (error instanceof ErrorPuente && error.codigo === 'requiere-sesion') abrirRequiereSesion();
    else mostrarToast(error.message, { tipo: 'error' });
  }
});

// ---------------------------------------------------------------------
// Inicio
// ---------------------------------------------------------------------

document.documentElement.dataset.entorno = enApp ? 'app' : 'navegador';
observarRevelado();
cargarSesion();
cargarLanding();
