/**
 * @file <sc-imagen>: foto de evento o lugar que viene de la nube (Cloudinary).
 *
 * POR QUÉ EXISTE: los eventos todavía no tienen su foto real en la BD. Este componente es el
 * único lugar que sabe cómo mostrar una foto remota, así que cuando suban las imágenes a
 * Cloudinary y llenen `main_image_url` en la BD, todas las tarjetas se actualizan solas.
 *
 * Qué hace:
 *  - Pide a Cloudinary la versión del tamaño exacto (ver utilidades/cloudinary.js).
 *  - Muestra un brillo de "cargando" y aparece con un fundido al terminar.
 *  - Si no hay URL o la imagen falla, pinta un relleno con el sol de SuraChih (nunca un ícono roto).
 *  - Carga diferida (`loading="lazy"`): solo descarga cuando está por verse.
 *
 * Atributos:
 *   src    URL de la imagen (Cloudinary o local). Vacío = relleno.
 *   alt    Texto alternativo (título del evento/lugar).
 *   ancho  Ancho en px CSS con el que se muestra (para pedir el tamaño a Cloudinary).
 *   alto   Alto en px CSS.
 *   prioridad  Si está presente, carga de inmediato (imágenes visibles al abrir, como el hero).
 *
 * El elemento ocupa todo su contenedor; el redondeo y el tamaño los pone el padre.
 *
 * @example
 * html`<sc-imagen src="${evento.imagenUrl}" alt="${evento.titulo}" ancho="588" alto="330"></sc-imagen>`
 */

import { definir } from '../nucleo/componente.js';
import { urlOptimizada } from '../utilidades/cloudinary.js';

class ImagenNube extends HTMLElement {
  static observedAttributes = ['src', 'alt', 'ancho', 'alto'];

  connectedCallback() { this.#pintar(); }
  attributeChangedCallback() { if (this.isConnected) this.#pintar(); }

  #pintar() {
    const src = this.getAttribute('src')?.trim();
    const alt = this.getAttribute('alt') ?? '';
    this.replaceChildren();
    this.dataset.estado = src ? 'cargando' : 'vacia';
    if (!src) {
      this.setAttribute('role', 'img');
      this.setAttribute('aria-label', alt || 'Imagen no disponible');
      return;
    }
    this.removeAttribute('role');
    this.removeAttribute('aria-label');

    const img = document.createElement('img');
    img.alt = alt;
    img.decoding = 'async';
    img.loading = this.hasAttribute('prioridad') ? 'eager' : 'lazy';
    img.addEventListener('load', () => { this.dataset.estado = 'cargada'; }, { once: true });
    img.addEventListener('error', () => {
      this.dataset.estado = 'vacia';
      img.remove();
    }, { once: true });
    img.src = urlOptimizada(src, {
      ancho: Number(this.getAttribute('ancho')) || undefined,
      alto: Number(this.getAttribute('alto')) || undefined,
    });
    this.append(img);
  }
}

definir('sc-imagen', ImagenNube);
