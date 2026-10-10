/**
 * @file <sc-boton-favorito>: corazón para guardar un evento o lugar ("Botón favorito" de Figma).
 *
 * Estados del diseño: Vacío → Hover (borde y corazón rojos) → Lleno (fondo rosa, corazón relleno).
 * Al hacer clic cambia de inmediato (respuesta optimista) y emite `sc:favorito`; quien lo escucha
 * (main.js) guarda el cambio y, si falla, llama a `revertir()`.
 *
 * Atributos:
 *   tipo     'evento' | 'lugar'
 *   objetivo Id del evento o lugar.
 *   tamano   's' (36px, por defecto) | 'm' (40px)
 *   activo   Presente si ya es favorito.
 *   etiqueta Nombre de lo que se guarda, para lectores de pantalla.
 *
 * Evento: `sc:favorito` → detail { tipo, id, activo, boton }
 */

import { definir } from '../nucleo/componente.js';

class BotonFavorito extends HTMLElement {
  #boton;

  connectedCallback() {
    if (this.#boton) return;
    this.#boton = document.createElement('button');
    this.#boton.type = 'button';
    this.#boton.className = 'boton-favorito__boton';
    this.#boton.innerHTML = `
      <img class="boton-favorito__vacio" src="assets/iconos/corazon-vacio.svg" alt="">
      <img class="boton-favorito__hover" src="assets/iconos/corazon-hover.svg" alt="">
      <img class="boton-favorito__lleno" src="assets/iconos/corazon-lleno.svg" alt="">`;
    this.#boton.addEventListener('click', (e) => {
      e.stopPropagation();   // no abrir la tarjeta que lo contiene
      this.#alternar();
    });
    this.append(this.#boton);
    this.#actualizarAria();
  }

  get activo() { return this.hasAttribute('activo'); }
  set activo(valor) {
    this.toggleAttribute('activo', Boolean(valor));
    this.#actualizarAria();
  }

  /** Regresa al estado anterior (cuando el guardado falla o requiere iniciar sesión). */
  revertir() { this.activo = !this.activo; }

  #alternar() {
    this.activo = !this.activo;
    if (this.activo) {
      this.classList.remove('latido');
      void this.offsetWidth;          // reinicia la animación
      this.classList.add('latido');
    }
    this.dispatchEvent(new CustomEvent('sc:favorito', {
      bubbles: true,
      detail: { tipo: this.getAttribute('tipo'), id: Number(this.getAttribute('objetivo')), activo: this.activo, boton: this },
    }));
  }

  #actualizarAria() {
    if (!this.#boton) return;
    const nombre = this.getAttribute('etiqueta') ?? '';
    this.#boton.setAttribute('aria-pressed', String(this.activo));
    this.#boton.setAttribute('aria-label', `${this.activo ? 'Quitar de' : 'Guardar en'} favoritos ${nombre}`.trim());
  }
}

definir('sc-boton-favorito', BotonFavorito);
