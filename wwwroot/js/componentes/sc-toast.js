/**
 * @file Avisos flotantes ("Toast / Cambios guardados" de Figma).
 *
 * @example
 * import { mostrarToast } from './componentes/sc-toast.js';
 * mostrarToast('Cambios guardados');
 * mostrarToast('No se pudo conectar', { tipo: 'error' });
 */

import { html } from '../nucleo/componente.js';
import { icono } from './atomos.js';

const DURACION_MS = 3200;
let region;

function obtenerRegion() {
  if (!region) {
    region = document.createElement('div');
    region.className = 'toasts';
    region.setAttribute('role', 'status');
    region.setAttribute('aria-live', 'polite');
    document.body.append(region);
  }
  return region;
}

/**
 * @param {string} mensaje
 * @param {{tipo?: 'exito'|'info'|'error'}} [opciones]
 */
export function mostrarToast(mensaje, { tipo = 'exito' } = {}) {
  const toast = document.createElement('div');
  toast.className = `toast toast--${tipo}`;
  toast.innerHTML = String(html`${tipo === 'exito' && icono('check-sol', 16)}<span>${mensaje}</span>`);
  obtenerRegion().append(toast);

  setTimeout(() => {
    toast.classList.add('toast--saliendo');
    toast.addEventListener('animationend', () => toast.remove(), { once: true });
  }, DURACION_MS);
}
