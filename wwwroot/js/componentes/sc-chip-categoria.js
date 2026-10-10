/**
 * @file <sc-chip-categoria>: chip de filtro por categoría ("Chip categoría" de Figma, estados
 * Normal y Activo). Normalmente se crea con `chipCategoria()` de atomos.js.
 *
 * Atributos:
 *   etiqueta Id de la etiqueta (tag.id).
 *   nombre   Texto visible ("Familiar").
 *   icono    Valor de tag.icon (family, music, run, robot, food): decide color e icono.
 *   activo   Presente si el filtro está aplicado.
 *
 * Evento: `sc:categoria` → detail { id, nombre, activo, chip }
 * Para leer varios a la vez: `[...contenedor.querySelectorAll('sc-chip-categoria[activo]')].map((c) => c.etiquetaId)`
 */

import { definir, html } from '../nucleo/componente.js';
import { claseCategoria, iconoCategoria } from './atomos.js';

class ChipCategoria extends HTMLElement {
  #boton;

  connectedCallback() {
    if (this.#boton) return;
    const iconoBD = this.getAttribute('icono');
    this.innerHTML = String(html`
      <button type="button" class="${claseCategoria(iconoBD)}" aria-pressed="${String(this.activo)}">
        ${iconoCategoria(iconoBD)}${this.getAttribute('nombre') ?? ''}
      </button>`);
    this.#boton = this.querySelector('button');
    this.#boton.addEventListener('click', () => {
      this.activo = !this.activo;
      this.dispatchEvent(new CustomEvent('sc:categoria', {
        bubbles: true,
        detail: { id: this.etiquetaId, nombre: this.getAttribute('nombre'), activo: this.activo, chip: this },
      }));
    });
  }

  /** Id numérico de la etiqueta (no se usa `id` para no pisar el atributo id del elemento). */
  get etiquetaId() { return Number(this.getAttribute('etiqueta')); }

  get activo() { return this.hasAttribute('activo'); }
  set activo(valor) {
    this.toggleAttribute('activo', Boolean(valor));
    this.#boton?.setAttribute('aria-pressed', String(this.activo));
  }
}

definir('sc-chip-categoria', ChipCategoria);
