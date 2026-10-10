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

/** Navbar Visitante o Logueado según la sesión de C#. */
async function cargarSesion() {
  try {
    secciones.navbar.datos = { usuario: await invocar('sesion.obtener') };
  } catch (error) {
    console.error(error);   // sin sesión se queda como visitante
  }
}

// ---------------------------------------------------------------------
// Acciones (las pantallas de destino se construirán en las siguientes entregas)
// ---------------------------------------------------------------------

/** Qué hace cada `data-accion`. Las que aún no tienen pantalla muestran un aviso. */
const acciones = {
  'explorar-eventos': () => proximamente('Explorar eventos'),
  'explorar-lugares': () => proximamente('Explorar lugares'),
  'mapa': () => proximamente('El mapa'),
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

function proximamente(que) {
  mostrarToast(`${que} llega en la siguiente versión.`, { tipo: 'info' });
}

function requiereSesion(para) {
  mostrarToast(`Inicia sesión para ${para}.`, { tipo: 'info' });
}

document.addEventListener('click', (e) => {
  const origen = e.target.closest('[data-accion]');
  const accion = origen?.dataset.accion;
  if (!accion || !acciones[accion]) return;
  e.preventDefault();
  acciones[accion](origen.dataset.id ? Number(origen.dataset.id) : undefined);
});

document.addEventListener('sc:buscar', (e) => proximamente(`La búsqueda de “${e.detail.texto}”`));

document.addEventListener('sc:favorito', async ({ detail }) => {
  try {
    const activo = await invocar('favoritos.alternar', { tipo: detail.tipo, id: detail.id });
    detail.boton.activo = activo;
    mostrarToast(activo ? 'Guardado en favoritos' : 'Quitado de favoritos');
  } catch (error) {
    detail.boton.revertir();
    const tipo = error instanceof ErrorPuente && error.codigo === 'requiere-sesion' ? 'info' : 'error';
    mostrarToast(error.message, { tipo });
  }
});

// ---------------------------------------------------------------------
// Inicio
// ---------------------------------------------------------------------

document.documentElement.dataset.entorno = enApp ? 'app' : 'navegador';
observarRevelado();
cargarSesion();
cargarLanding();
