/**
 * @file Ventana modal base (los "Modal / ..." de Figma): caja blanca centrada sobre un velo,
 * con botón de cerrar. Usa `<dialog>` nativo, así que el foco queda atrapado dentro, Esc cierra
 * y lo de atrás no se puede tocar.
 *
 * Solo hay una modal a la vez: si ya hay una abierta, `mostrarModal` cambia su contenido
 * (p. ej. de "Inicia sesión" a "Crea tu cuenta") sin cerrar y volver a abrir.
 *
 * @example
 * import { mostrarModal, cerrarModal } from './sc-modal.js';
 * const contenido = mostrarModal(html`<h2 class="modal__titulo t-h2" id="titulo-x">Hola</h2>`, { titulo: 'titulo-x' });
 * contenido.querySelector('button').addEventListener('click', cerrarModal);
 *
 * Ojo: el `<dialog>` se pinta por encima de todo (capa superior), incluidos los toasts. Para avisar
 * algo con la modal abierta usa `.modal__aviso` dentro de ella, o cierra la modal y luego el toast.
 */

import { html } from '../nucleo/componente.js';
import { icono } from './atomos.js';

/** @type {HTMLDialogElement|null} */
let dialogo = null;
/** Elemento que tenía el foco antes de abrir, para regresárselo al cerrar. */
let focoAnterior = null;
let alCerrarActual = null;

const reducirMovimiento = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches;

function crearDialogo() {
  const d = document.createElement('dialog');
  d.className = 'modal';
  d.innerHTML = String(html`
    <div class="modal__caja">
      <button type="button" class="modal__cerrar" data-cerrar-modal aria-label="Cerrar">${icono('cerrar', 14)}</button>
      <div class="modal__contenido"></div>
    </div>`);

  // Esc: cerrar con animación en vez del cierre instantáneo del navegador.
  d.addEventListener('cancel', (e) => {
    e.preventDefault();
    cerrarModal();
  });
  // Clic en el velo (fuera de la caja) o en cualquier [data-cerrar-modal].
  d.addEventListener('click', (e) => {
    if (e.target === d || e.target.closest('[data-cerrar-modal]')) cerrarModal();
  });
  document.body.append(d);
  return d;
}

/**
 * Abre la modal (o reemplaza su contenido si ya está abierta).
 * @param {import('../nucleo/componente.js').HtmlSeguro|string} contenido
 * @param {object} [opciones]
 * @param {string} [opciones.titulo] id del título, para que los lectores de pantalla lo anuncien.
 * @param {() => void} [opciones.alCerrar] Se llama una vez cuando la modal se cierra.
 * @returns {HTMLElement} El contenedor del contenido, para enlazar eventos.
 */
export function mostrarModal(contenido, { titulo, alCerrar } = {}) {
  const yaAbierta = Boolean(dialogo?.open);
  if (!dialogo) dialogo = crearDialogo();

  const caja = dialogo.querySelector('.modal__contenido');
  caja.innerHTML = String(contenido);
  if (titulo) dialogo.setAttribute('aria-labelledby', titulo);
  else dialogo.removeAttribute('aria-labelledby');
  alCerrarActual = alCerrar ?? null;

  if (yaAbierta) {
    // Cambio de pantalla dentro de la misma modal: solo un pequeño desvanecido del contenido.
    caja.classList.remove('modal__contenido--cambio');
    void caja.offsetWidth;   // reinicia la animación
    caja.classList.add('modal__contenido--cambio');
  } else {
    focoAnterior = document.activeElement;
    dialogo.classList.remove('modal--cerrando');
    dialogo.showModal();
    document.documentElement.classList.add('con-modal');
  }

  // Foco al primer campo (o al primer botón si no hay campos), como un formulario normal.
  const primero = caja.querySelector('[autofocus], input:not([type=hidden]), button:not([data-cerrar-modal])');
  primero?.focus({ preventScroll: true });
  return caja;
}

/** Cierra la modal abierta (con animación) y devuelve el foco a donde estaba. */
export function cerrarModal() {
  if (!dialogo?.open || dialogo.classList.contains('modal--cerrando')) return;
  const d = dialogo;
  let terminado = false;
  const terminar = () => {
    if (terminado) return;
    terminado = true;
    d.close();
    d.classList.remove('modal--cerrando');
    d.querySelector('.modal__contenido').innerHTML = '';
    document.documentElement.classList.remove('con-modal');
    focoAnterior?.focus?.({ preventScroll: true });
    focoAnterior = null;
    const alCerrar = alCerrarActual;
    alCerrarActual = null;
    alCerrar?.();
  };

  if (reducirMovimiento()) { terminar(); return; }
  d.classList.add('modal--cerrando');
  d.querySelector('.modal__caja').addEventListener('animationend', terminar, { once: true });
  setTimeout(terminar, 400);   // por si la animación no llega a correr
}

/** true si hay una modal abierta. */
export function hayModalAbierta() {
  return Boolean(dialogo?.open);
}
