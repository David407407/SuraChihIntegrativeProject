/**
 * @file <sc-collage-hero>: tres tarjetas inclinadas que rotan solas
 * (componente "Collage hero" de Figma, variantes Paso=1, 2 y 3).
 *
 * En cada paso la tarjeta de la izquierda pasa al centro, la del centro a la derecha y la de la
 * derecha a la izquierda. Las posiciones están en css/componentes/hero.css y el cambio se anima
 * con transiciones CSS. Se pausa con el mouse encima, si la ventana no está visible o si el
 * sistema pide reducir movimiento.
 *
 * datos: arreglo de hasta 3 elementos { tipo: 'evento', evento } | { tipo: 'lugar', lugar }
 * (records de BackendLogica). Sin datos se muestran tarjetas vacías con brillo de carga.
 */

import { Componente, definir, html } from '../../nucleo/componente.js';
import { icono } from '../atomos.js';
import { rangoFechas, horarioLugar } from '../../utilidades/formato.js';
import { reducirMovimiento } from '../../utilidades/animaciones.js';
import { hero as textos } from '../../contenido/landing.js';

const POSICIONES = ['centro', 'izq', 'der'];
const INTERVALO_MS = 4000;
const ANCHO_DISENO = 600;   // ancho del collage en Figma; se escala para pantallas más chicas

/** Título, subtítulo e imagen de cada tarjeta según sea evento o lugar. */
function contenidoTarjeta(item) {
  if (item?.tipo === 'evento') {
    const e = item.evento;
    const lugar = e.lugarNombre ?? e.direccion;
    return { titulo: e.titulo, subtitulo: [rangoFechas(e.inicio, e.fin), lugar].filter(Boolean).join(' · '), imagen: e.imagenUrl };
  }
  if (item?.tipo === 'lugar') {
    const l = item.lugar;
    return { titulo: l.nombre, subtitulo: horarioLugar(l.horarioAhora).texto, imagen: l.imagenUrl };
  }
  return { titulo: '', subtitulo: '', imagen: '' };
}

class CollageHero extends Componente {
  #paso = 0;
  #temporizador = null;
  #redimension = null;

  datosIniciales() { return [null, null, null]; }

  plantilla(items) {
    // Sin nada publicado todavía se dejan los rellenos con el sol para no romper la composición.
    const tarjetas = (items.length ? items : this.datosIniciales()).slice(0, 3);
    return html`
      <div class="collage" aria-roledescription="carrusel" aria-label="Eventos y lugares destacados">
        ${tarjetas.map((item, i) => {
          const t = contenidoTarjeta(item);
          return html`
            <figure class="collage__tarjeta" data-pos="${POSICIONES[i]}" aria-hidden="${i === 0 ? 'false' : 'true'}">
              <sc-imagen src="${t.imagen ?? ''}" alt="${t.titulo}" ancho="300" alto="390" prioridad></sc-imagen>
              <span class="collage__sombra"></span>
              ${t.titulo && html`
                <figcaption class="collage__texto">
                  <span class="t-body-fuerte">${t.titulo}</span>
                  <span class="t-caption">${t.subtitulo}</span>
                </figcaption>`}
            </figure>`;
        })}
        <span class="collage__chip collage__chip--inscrito t-small-fuerte">${icono('check-exito', 14)}${textos.chipInscrito}</span>
        <span class="collage__chip collage__chip--favorito t-small-fuerte">${icono('corazon-relleno-13', 13)}${textos.chipFavorito}</span>
      </div>`;
  }

  alPintar() {
    this.#paso = 0;
    this.#escalar();
    this.#iniciarRotacion();

    if (!this.#redimension) {
      this.#redimension = new ResizeObserver(() => this.#escalar());
      this.#redimension.observe(this);
      this.addEventListener('mouseenter', () => this.#detener());
      this.addEventListener('mouseleave', () => this.#iniciarRotacion());
      document.addEventListener('visibilitychange', () =>
        document.hidden ? this.#detener() : this.#iniciarRotacion());
    }
  }

  disconnectedCallback() {
    this.#detener();
    this.#redimension?.disconnect();
    this.#redimension = null;
  }

  /** Mantiene la composición de 600×560 de Figma escalándola al ancho disponible. */
  #escalar() {
    const escala = Math.min(1, this.clientWidth / ANCHO_DISENO) || 1;
    this.style.setProperty('--escala', escala.toFixed(4));
  }

  #iniciarRotacion() {
    this.#detener();
    const tarjetas = this.querySelectorAll('.collage__tarjeta');
    if (tarjetas.length < 2 || reducirMovimiento()) return;
    this.#temporizador = setInterval(() => this.#avanzar(tarjetas), INTERVALO_MS);
  }

  #detener() {
    clearInterval(this.#temporizador);
    this.#temporizador = null;
  }

  /** Paso siguiente: la tarjeta i va a la posición (i - paso) mod 3. */
  #avanzar(tarjetas) {
    this.#paso = (this.#paso + 1) % tarjetas.length;
    tarjetas.forEach((tarjeta, i) => {
      const pos = POSICIONES[(i - this.#paso + tarjetas.length * 2) % tarjetas.length];
      tarjeta.dataset.pos = pos;
      tarjeta.setAttribute('aria-hidden', String(pos !== 'centro'));
    });
  }
}

definir('sc-collage-hero', CollageHero);
