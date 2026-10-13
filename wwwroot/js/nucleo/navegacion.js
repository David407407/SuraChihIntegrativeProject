/**
 * @file Lo que comparten todas las pantallas: cambiar de página, las acciones `data-accion`
 * (botones del navbar, tarjetas, pie...) y el estado Visitante / Logueado del navbar.
 *
 * Cada pantalla es un .html de wwwroot con su script de entrada (main.js, mapa.js...) que llama
 * a `atenderAcciones()` y `cargarSesion()`.
 */

import { invocar } from './puente.js';
import { mostrarToast } from '../componentes/sc-toast.js';

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

export function requiereSesion(para) {
  mostrarToast(`Inicia sesión para ${para}.`, { tipo: 'info' });
}

/** Qué hace cada `data-accion`. Las que aún no tienen pantalla muestran un aviso. */
const acciones = {
  'inicio': () => irA('inicio'),
  'mapa': () => irA('mapa'),
  'explorar-eventos': () => proximamente('Explorar eventos'),
  'explorar-lugares': () => proximamente('Explorar lugares'),
  'iniciar-sesion': () => proximamente('Iniciar sesión'),
  'crear-cuenta': () => proximamente('Crear cuenta'),
  'mis-planes': () => proximamente('Mis planes'),
  'favoritos': () => proximamente('Mis favoritos'),
  'menu-cuenta': () => proximamente('El menú de tu cuenta'),
  'publicar-evento': () => requiereSesion('publicar tu evento'),
  'inscribirse': () => requiereSesion('inscribirte'),
  'ver-evento': (id) => proximamente(`El detalle del evento #${id}`),
  'ver-lugar': (id) => proximamente(`El detalle del lugar #${id}`),
};

/**
 * Atiende con delegación los `data-accion` de toda la página y el buscador del navbar.
 * @param {Record<string, (id?: number) => void>} [propias] Acciones de esta pantalla (reemplazan a las comunes).
 */
export function atenderAcciones(propias = {}) {
  const todas = { ...acciones, ...propias };

  document.addEventListener('click', (e) => {
    const origen = e.target.closest('[data-accion]');
    const accion = origen?.dataset.accion;
    if (!accion || !todas[accion]) return;
    e.preventDefault();
    todas[accion](origen.dataset.id ? Number(origen.dataset.id) : undefined);
  });

  document.addEventListener('sc:buscar', (e) => proximamente(`La búsqueda de “${e.detail.texto}”`));
}

/**
 * Navbar Visitante o Logueado según la sesión de C#.
 * @param {HTMLElement} navbar El <sc-navbar> de la página.
 */
export async function cargarSesion(navbar) {
  try {
    navbar.datos = { usuario: await invocar('sesion.obtener') };
  } catch (error) {
    console.error(error);   // sin sesión se queda como visitante
  }
}
