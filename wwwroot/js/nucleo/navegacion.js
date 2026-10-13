/**
 * @file Lo que comparten todas las pantallas: cambiar de página, las acciones `data-accion`
 * (botones del navbar, tarjetas, pie...) y la sesión (navbar Visitante / Logueado, modales de
 * cuenta y menú de cuenta).
 *
 * Cada pantalla es un .html de wwwroot con su script de entrada (main.js, mapa.js...) que llama
 * a `atenderAcciones()` y `cargarSesion(navbar)`.
 */

import { invocar } from './puente.js';
import { mostrarToast } from '../componentes/sc-toast.js';
import { abrirIniciarSesion, abrirCrearCuenta, abrirRequiereSesion } from '../componentes/modales/cuenta.js';
import { alternarMenuCuenta, cerrarMenuCuenta } from '../componentes/menu-cuenta.js';

/** Pantallas que ya existen. Al construir una nueva, agrégala aquí y en `acciones`. */
const PAGINAS = {
  inicio: 'index.html',
  mapa: 'mapa.html',
};

/** Cambia de pantalla; si ya se está en ella, sube al inicio de la página. */
export function irA(pagina) {
  const destino = PAGINAS[pagina];
  const actual = location.pathname.split('/').pop() || PAGINAS.inicio;
  if (actual === destino) window.scrollTo({ top: 0 });
  else location.href = destino;
}

export function proximamente(que) {
  mostrarToast(`${que} llega en la siguiente versión.`, { tipo: 'info' });
}

// ---------------------------------------------------------------------
// Sesión
// ---------------------------------------------------------------------

/** Usuario con sesión (UsuarioSesion de C#) o null para el visitante. */
let usuarioActual = null;
/** El <sc-navbar> de la página, para pasarlo de Visitante a Logueado. */
let navbar = null;

function ponerUsuario(usuario) {
  usuarioActual = usuario ?? null;
  cerrarMenuCuenta();
  if (navbar) navbar.datos = { usuario: usuarioActual };
}

/**
 * Navbar Visitante o Logueado según la sesión de C#.
 * @param {HTMLElement} elNavbar El <sc-navbar> de la página.
 */
export async function cargarSesion(elNavbar) {
  navbar = elNavbar;
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

/** Sin sesión: modal "Inicia sesión para continuar". Con sesión, la pantalla aún no existe. */
export function requiereSesion(que) {
  if (usuarioActual) proximamente(que);
  else abrirRequiereSesion();
}

// ---------------------------------------------------------------------
// Acciones
// ---------------------------------------------------------------------

/** Qué hace cada `data-accion`. Las que aún no tienen pantalla muestran un aviso. */
const acciones = {
  'inicio': () => irA('inicio'),
  'mapa': () => irA('mapa'),
  'explorar-eventos': () => proximamente('Explorar eventos'),
  'explorar-lugares': () => proximamente('Explorar lugares'),
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

/**
 * Atiende con delegación los `data-accion` de toda la página, el buscador del navbar y el
 * inicio de sesión desde los modales de cuenta.
 * @param {Record<string, (id?: number, origen?: HTMLElement) => void>} [propias] Acciones de esta
 *   pantalla (reemplazan a las comunes).
 */
export function atenderAcciones(propias = {}) {
  const todas = { ...acciones, ...propias };

  document.addEventListener('click', (e) => {
    const origen = e.target.closest('[data-accion]');
    const accion = origen?.dataset.accion;
    if (!accion || !todas[accion]) return;
    e.preventDefault();
    todas[accion](origen.dataset.id ? Number(origen.dataset.id) : undefined, origen);
  });

  document.addEventListener('sc:buscar', (e) => proximamente(`La búsqueda de “${e.detail.texto}”`));

  // Lo lanzan los modales de cuenta al iniciar sesión o crear la cuenta.
  document.addEventListener('sc:sesion', ({ detail }) => {
    ponerUsuario(detail.usuario);
    const nombre = detail.usuario.nombreUsuario;
    mostrarToast(detail.nuevo ? `¡Bienvenido a SuraChih, ${nombre}!` : `¡Hola de nuevo, ${nombre}!`);
  });
}
