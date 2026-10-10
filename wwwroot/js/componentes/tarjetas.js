/**
 * @file Tarjetas de evento y de lugar (moléculas). Reciben los records de BackendLogica tal cual
 * llegan por el puente y se encargan de darles formato. Estilos en css/componentes/tarjetas.css.
 */

import { html } from '../nucleo/componente.js';
import { icono, chip, boton } from './atomos.js';
import { diaYMes, rangoFechas, precio, horarioLugar, calificacion, fechaEvento, precioEvento } from '../utilidades/formato.js';

/** Nombre del lugar o, si el evento es en la calle, su dirección. */
const ubicacionEvento = (e) => e.lugarNombre ?? e.direccion ?? '';
/** Primera etiqueta: se muestra como chip de categoría. */
const categoria = (item) => item.etiquetas?.[0]?.nombre;

/**
 * Evento grande de "No te los pierdas" ("Evento principal" en Figma).
 * @param {object} e EventoTarjeta
 */
export function tarjetaEventoPrincipal(e) {
  const { dia, mes } = diaYMes(e.inicio);
  return html`
    <article class="evento-principal" data-revelar>
      <button type="button" class="evento-principal__imagen" data-accion="ver-evento" data-id="${e.id}" aria-label="Ver ${e.titulo}">
        <sc-imagen src="${e.imagenUrl ?? ''}" alt="${e.titulo}" ancho="588" alto="330"></sc-imagen>
        <span class="evento-principal__fecha">
          <span class="t-h1">${dia}</span>
          <span class="t-small-fuerte">${mes}</span>
        </span>
      </button>
      <div class="evento-principal__info">
        ${categoria(e) && chip(categoria(e))}
        <h3 class="evento-principal__titulo t-h1">${e.titulo}</h3>
        ${e.descripcion && html`<p class="evento-principal__descripcion t-body texto-2">${e.descripcion}</p>`}
        <div class="evento-principal__meta t-small-fuerte texto-2">
          ${ubicacionEvento(e) && html`<span>${icono('ubicacion', 14)}${ubicacionEvento(e)}</span>`}
          <span>${icono('etiqueta-precio', 14)}${precio(e.precioMin, e.precioMax, 'desde')}</span>
        </div>
        <div class="evento-principal__acciones">
          ${boton({ texto: 'Voy a ir', icono: 'calendario-check', accion: 'inscribirse', objetivo: e.id })}
          ${boton({ texto: 'Ver detalles', variante: 'secundario', accion: 'ver-evento', objetivo: e.id })}
        </div>
      </div>
    </article>`;
}

/**
 * Fila de la lista de "No te los pierdas".
 * @param {object} e EventoTarjeta
 * @param {number} orden Para escalonar la animación de aparición.
 */
export function tarjetaEventoFila(e, orden = 0) {
  return html`
    <li data-revelar style="--orden:${orden}">
      <button type="button" class="evento-fila" data-accion="ver-evento" data-id="${e.id}">
        <span class="evento-fila__imagen">
          <sc-imagen src="${e.imagenUrl ?? ''}" alt="" ancho="112" alto="112"></sc-imagen>
        </span>
        <span class="evento-fila__textos">
          <span class="t-small-fuerte evento-fila__fecha">${rangoFechas(e.inicio, e.fin)}</span>
          <span class="t-h3 evento-fila__titulo">${e.titulo}</span>
          <span class="t-small texto-2">${ubicacionEvento(e)}</span>
        </span>
        <span class="t-body-fuerte evento-fila__precio">${precio(e.precioMin, e.precioMax)}</span>
        ${icono('chevron-gris-12', 12)}
      </button>
    </li>`;
}

/**
 * Imagen de una card con el favorito y "Ver detalles" (parte común de Card evento y Card lugar).
 * @param {object} p
 * @param {'evento'|'lugar'} p.tipo
 * @param {object} p.item EventoTarjeta o LugarTarjeta
 * @param {string} p.nombre
 * @param {boolean} p.favorito
 * @param {import('../nucleo/componente.js').HtmlSeguro} [p.extra] Lo que va encima de la foto (chip de tipo).
 */
function imagenTarjeta({ tipo, item, nombre, favorito, extra }) {
  return html`
    <div class="tarjeta__imagen">
      <sc-imagen src="${item.imagenUrl ?? ''}" alt="${nombre}" ancho="282" alto="282"></sc-imagen>
      ${extra}
      <sc-boton-favorito class="tarjeta__favorito" tipo="${tipo}" objetivo="${item.id}" etiqueta="${nombre}"${favorito && html` activo`}></sc-boton-favorito>
      <span class="tarjeta__ver t-small-fuerte" aria-hidden="true">Ver detalles ${icono('chevron-tinta-10', 10)}</span>
    </div>`;
}

/**
 * "Card evento" de Figma (Normal / Hover): foto cuadrada, fecha, título (2 líneas), lugar y precio.
 * Se usa en el inicio, resultados de búsqueda, Mis favoritos y "Te podría interesar".
 *
 * @example
 * html`<div class="rejilla-tarjetas">${eventos.map((e, i) => tarjetaEvento(e, { orden: i, favorito: ids.has(e.id) }))}</div>`
 *
 * @param {object} e EventoTarjeta
 * @param {{orden?:number, favorito?:boolean}} [opciones] `favorito`: pinta el corazón lleno
 *   (usa `Favoritos.ListarIdsAsync` para saber cuáles).
 */
export function tarjetaEvento(e, { orden = 0, favorito = false } = {}) {
  return html`
    <article class="tarjeta" data-revelar style="--orden:${orden}">
      ${imagenTarjeta({ tipo: 'evento', item: e, nombre: e.titulo, favorito })}
      <div class="tarjeta__info">
        <p class="t-small-fuerte tarjeta__fecha tarjeta__linea">${fechaEvento(e)}</p>
        <h3 class="t-card-titulo tarjeta__titulo">
          <button type="button" class="tarjeta__enlace" data-accion="ver-evento" data-id="${e.id}"><span class="tarjeta__titulo-texto">${e.titulo}</span></button>
        </h3>
        ${ubicacionEvento(e) && html`<p class="t-small texto-2 tarjeta__linea">${ubicacionEvento(e)}</p>`}
        ${precioEvento(e) && html`<p class="t-small-fuerte tarjeta__linea">${precioEvento(e)}</p>`}
      </div>
    </article>`;
}

/**
 * "Card lugar" de Figma (Normal / Hover): foto con chip de tipo, nombre, dirección,
 * calificación, precio y horario de hoy.
 * @param {object} l LugarTarjeta
 * @param {number|{orden?:number, favorito?:boolean}} [opciones] Un número se toma como `orden`
 *   (así lo llamaba la landing antes).
 */
export function tarjetaLugar(l, opciones = {}) {
  const { orden = 0, favorito = false } = typeof opciones === 'number' ? { orden: opciones } : opciones;
  const horario = horarioLugar(l.horarioAhora);
  return html`
    <article class="tarjeta" data-revelar style="--orden:${orden}">
      ${imagenTarjeta({
        tipo: 'lugar', item: l, nombre: l.nombre, favorito,
        extra: categoria(l) && html`<span class="tarjeta__tipo">${chip(categoria(l), 'superficie')}</span>`,
      })}
      <div class="tarjeta__info">
        <h3 class="t-card-titulo tarjeta__titulo">
          <button type="button" class="tarjeta__enlace" data-accion="ver-lugar" data-id="${l.id}"><span class="tarjeta__titulo-texto">${l.nombre}</span></button>
        </h3>
        <p class="t-small texto-2 tarjeta__linea">${l.direccion}</p>
        <p class="t-small tarjeta__meta">
          ${l.calificacion != null && html`<span class="tarjeta__calificacion t-small-fuerte">${icono('estrella', 13)}${calificacion(l.calificacion)}</span>`}
          ${precio(l.precioMin, l.precioMax) && html`<span class="texto-2">${precio(l.precioMin, l.precioMax)}</span>`}
          <span class="${horario.abierto ? 'tarjeta__abierto' : 'texto-2'}">${horario.texto}</span>
        </p>
      </div>
    </article>`;
}

/** Esqueleto con brillo mientras cargan los datos. */
export function esqueleto(clase) {
  return html`<div class="esqueleto ${clase}" aria-hidden="true"></div>`;
}
