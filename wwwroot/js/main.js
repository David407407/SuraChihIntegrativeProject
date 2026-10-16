/**
 * @file Punto de entrada de la landing.
 *
 * 1. Registra los componentes (cada import define sus etiquetas <sc-...>).
 * 2. Pide los datos a C# (o a los datos demo en un navegador) y se los pasa a cada sección.
 * 3. Atiende las acciones de toda la página con delegación: cualquier elemento con
 *    `data-accion` (botones, enlaces del pie...; ver nucleo/navegacion.js) y los eventos
 *    `sc:*` de los componentes.
 */

import './componentes/sc-imagen.js';
import './componentes/sc-boton-favorito.js';
import './componentes/sc-chip-categoria.js';
import './componentes/secciones/sc-navbar.js';
import './componentes/secciones/sc-hero.js';
import './componentes/secciones/sc-secciones-fijas.js';
import './componentes/secciones/sc-secciones-datos.js';

import { invocar, enApp } from './nucleo/puente.js';
import { atenderAcciones, cargarSesion } from './nucleo/navegacion.js';
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

// ---------------------------------------------------------------------
// Acciones: todas son comunes a las pantallas (nucleo/navegacion.js), incluido el corazón de
// favoritos y "Ver detalles" / "Voy a ir", que abren el detalle de evento encima de la landing.
// ---------------------------------------------------------------------

atenderAcciones();

// ---------------------------------------------------------------------
// Inicio
// ---------------------------------------------------------------------

document.documentElement.dataset.entorno = enApp ? 'app' : 'navegador';
observarRevelado();
cargarSesion(secciones.navbar);
cargarLanding();
