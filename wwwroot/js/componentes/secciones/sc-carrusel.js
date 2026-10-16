/**
 * @file <sc-carrusel>: "Carrusel destacado" del Home (Figma, 5 slides).
 *
 * Datos: `carrusel.datos = elementos`, un arreglo de { tipo: 'evento'|'lugar', evento?, lugar? }
 * (ElementoCollage de C#: eventos destacados por los moderadores + un lugar).
 *
 * - Avanza solo cada 6 s; se pausa con el mouse o el foco encima, con la ventana oculta y con
 *   "reducir movimiento" del sistema.
 * - Puntos (el activo es una píldora de 28px) y flechas Anterior / Siguiente; también ← y → del teclado.
 * - "Ver detalles" usa data-accion 'ver-evento' / 'ver-lugar' (lo atiende nucleo/navegacion.js).
 */

import { Componente, definir, html } from '../../nucleo/componente.js';
import { boton, icono } from '../atomos.js';
import { fechaEvento, horarioLugar } from '../../utilidades/formato.js';
import { esqueleto } from '../tarjetas.js';

const INTERVALO_MS = 6000;
/** Fondos de los 5 slides de Figma; si hay más slides se repiten. */
const FONDOS = ['#0a3b8c', '#9d174d', '#065f46', '#7f1d1d', '#2f7bff'];
const sinMovimiento = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches;

/** Lo que muestra un slide, igual para eventos y lugares. */
function contenidoSlide({ tipo, evento, lugar }) {
  if (tipo === 'lugar') {
    const horario = horarioLugar(lugar.horarioAhora);
    return {
      id: lugar.id, accion: 'ver-lugar', titulo: lugar.nombre, descripcion: lugar.descripcion,
      imagen: lugar.imagenUrl, etiqueta: lugar.etiquetas?.[0]?.nombre,
      icono: 'ubicacion-14-blanco',
      meta: [lugar.direccion, horario.abierto ? horario.texto.toLowerCase() : null].filter(Boolean).join(', '),
    };
  }
  return {
    id: evento.id, accion: 'ver-evento', titulo: evento.titulo, descripcion: evento.descripcion,
    imagen: evento.imagenUrl, etiqueta: evento.etiquetas?.[0]?.nombre,
    icono: 'calendario-14-blanco',
    meta: [fechaEvento(evento), evento.lugarNombre ?? evento.direccion].filter(Boolean).join(', '),
  };
}

class Carrusel extends Componente {
  #actual = 0;
  #total = 0;
  #temporizador = null;
  #pausado = false;

  datosIniciales() { return null; }

  plantilla(elementos) {
    if (elementos == null) return html`<div class="contenedor">${esqueleto('carrusel__esqueleto')}</div>`;
    if (elementos.length === 0) return '';
    this.#total = elementos.length;
    this.#actual = Math.min(this.#actual, this.#total - 1);
    return html`
      <div class="contenedor">
      <section class="carrusel" aria-roledescription="carrusel" aria-label="Lo más destacado">
        <div class="carrusel__pista" aria-live="off">
          ${elementos.map((elemento, i) => this.#slide(contenidoSlide(elemento), i))}
        </div>
        ${this.#total > 1 && html`
          <div class="carrusel__puntos" role="group" aria-label="Elegir slide">
            ${elementos.map((_, i) => html`<button type="button" class="carrusel__punto" data-ir="${i}" aria-label="Slide ${i + 1} de ${this.#total}"></button>`)}
          </div>
          <div class="carrusel__flechas">
            <button type="button" class="carrusel__flecha" data-paso="-1" aria-label="Anterior">${icono('flecha-anterior-blanca', 14)}</button>
            <button type="button" class="carrusel__flecha" data-paso="1" aria-label="Siguiente">${icono('flecha-siguiente-blanca', 14)}</button>
          </div>`}
      </section>
      </div>`;
  }

  #slide(s, i) {
    return html`
      <article class="carrusel__slide" style="--fondo:${FONDOS[i % FONDOS.length]}"
        aria-roledescription="slide" aria-label="${i + 1} de ${this.#total}: ${s.titulo}">
        <div class="carrusel__texto">
          ${s.etiqueta && html`<span class="carrusel__etiqueta t-caption">${s.etiqueta}</span>`}
          <h2 class="carrusel__titulo t-h1">${s.titulo}</h2>
          ${s.descripcion && html`<p class="carrusel__descripcion t-body">${s.descripcion}</p>`}
          ${s.meta && html`<p class="carrusel__meta t-small-fuerte">${icono(s.icono, 14)}<span>${s.meta}</span></p>`}
          ${boton({ texto: 'Ver detalles', variante: 'blanco', icono: 'chevron-tinta-12', accion: s.accion, objetivo: s.id, clase: 'carrusel__ver' })}
        </div>
        <button type="button" class="carrusel__imagen" data-accion="${s.accion}" data-id="${s.id}" tabindex="-1" aria-hidden="true">
          <sc-imagen src="${s.imagen ?? ''}" alt="" ancho="540" alto="300"${i === 0 && html` prioridad`}></sc-imagen>
        </button>
      </article>`;
  }

  alPintar() {
    const carrusel = this.querySelector('.carrusel');
    if (!carrusel) return;
    carrusel.addEventListener('click', (e) => {
      const punto = e.target.closest('[data-ir]');
      const flecha = e.target.closest('[data-paso]');
      if (punto) this.ir(Number(punto.dataset.ir));
      else if (flecha) this.ir(this.#actual + Number(flecha.dataset.paso));
    });
    carrusel.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowLeft') this.ir(this.#actual - 1);
      if (e.key === 'ArrowRight') this.ir(this.#actual + 1);
    });
    // Pausa mientras el usuario está leyendo o usando el teclado dentro.
    carrusel.addEventListener('mouseenter', () => { this.#pausado = true; });
    carrusel.addEventListener('mouseleave', () => { this.#pausado = false; });
    carrusel.addEventListener('focusin', () => { this.#pausado = true; });
    carrusel.addEventListener('focusout', () => { this.#pausado = false; });
    this.ir(this.#actual, { animar: false });
    this.#reiniciarTemporizador();
  }

  /** Muestra el slide `indice` (da la vuelta en los extremos). */
  ir(indice, { animar = true } = {}) {
    if (this.#total === 0) return;
    this.#actual = (indice + this.#total) % this.#total;
    const pista = this.querySelector('.carrusel__pista');
    pista.classList.toggle('carrusel__pista--sin-animacion', !animar);
    pista.style.transform = `translateX(${-100 * this.#actual}%)`;
    this.querySelectorAll('.carrusel__slide').forEach((slide, i) => {
      const visible = i === this.#actual;
      slide.inert = !visible;   // los slides ocultos no reciben Tab ni los leen los lectores de pantalla
      slide.setAttribute('aria-hidden', String(!visible));
    });
    this.querySelectorAll('.carrusel__punto').forEach((punto, i) => {
      punto.setAttribute('aria-current', String(i === this.#actual));
    });
    this.#reiniciarTemporizador();
  }

  #reiniciarTemporizador() {
    clearInterval(this.#temporizador);
    if (this.#total < 2 || sinMovimiento()) return;
    this.#temporizador = setInterval(() => {
      if (!this.#pausado && !document.hidden) this.ir(this.#actual + 1);
    }, INTERVALO_MS);
  }

  disconnectedCallback() {
    clearInterval(this.#temporizador);
  }
}

definir('sc-carrusel', Carrusel);
