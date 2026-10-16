/**
 * @file <sc-volver-arriba>: botón flotante que regresa al principio de la página.
 * Aparece abajo a la derecha cuando ya se bajó más de una pantalla. Se usa en pantallas largas
 * (el Home): basta con poner `<sc-volver-arriba></sc-volver-arriba>` al final del <body>.
 * Estilos en css/componentes/atomos.css.
 */

import { definir, html } from '../nucleo/componente.js';
import { icono } from './atomos.js';

class VolverArriba extends HTMLElement {
  #boton;
  #pendiente = false;

  connectedCallback() {
    if (this.#boton) return;
    this.innerHTML = String(html`
      <button type="button" class="volver-arriba" aria-label="Volver arriba" title="Volver arriba">
        ${icono('flecha-siguiente-blanca', 14)}
      </button>`);
    this.#boton = this.querySelector('button');
    this.#boton.addEventListener('click', () => {
      const suave = !window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      window.scrollTo({ top: 0, behavior: suave ? 'smooth' : 'auto' });
    });
    window.addEventListener('scroll', () => this.#alDesplazar(), { passive: true });
    this.#actualizar();
  }

  /** Revisa la posición una vez por cuadro, no en cada evento de scroll. */
  #alDesplazar() {
    if (this.#pendiente) return;
    this.#pendiente = true;
    requestAnimationFrame(() => {
      this.#pendiente = false;
      this.#actualizar();
    });
  }

  #actualizar() {
    this.toggleAttribute('visible', window.scrollY > window.innerHeight);
  }
}

definir('sc-volver-arriba', VolverArriba);
