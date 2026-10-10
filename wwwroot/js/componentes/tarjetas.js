/**
 * @file Tarjetas de evento y de lugar (moléculas). Reciben los records de BackendLogica tal cual
 * llegan por el puente y se encargan de darles formato. Estilos en css/componentes/tarjetas.css.
 */

import { html } from '../nucleo/componente.js';
import { icono, chip, boton } from './atomos.js';
import { diaYMes, rangoFechas, precio, horarioLugar, calificacion } from '../utilidades/formato.js';

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
 * Tarjeta de "Lugares para descubrir" ("Card lugar" de Figma, estados Normal y Hover).
 * @param {object} l LugarTarjeta
 * @param {number} orden
 */
export function tarjetaLugar(l, orden = 0) {
  const horario = horarioLugar(l.horarioAhora);
  return html`
    <article class="tarjeta-lugar" data-revelar style="--orden:${orden}">
      <div class="tarjeta-lugar__imagen">
        <sc-imagen src="${l.imagenUrl ?? ''}" alt="${l.nombre}" ancho="282" alto="282"></sc-imagen>
        ${categoria(l) && html`<span class="tarjeta-lugar__tipo">${chip(categoria(l), 'superficie')}</span>`}
        <sc-boton-favorito class="tarjeta-lugar__favorito" tipo="lugar" objetivo="${l.id}" etiqueta="${l.nombre}"></sc-boton-favorito>
        <span class="tarjeta-lugar__ver t-small-fuerte" aria-hidden="true">Ver detalles ${icono('chevron-tinta-10', 10)}</span>
      </div>
      <div class="tarjeta-lugar__info">
        <h3 class="t-card-titulo tarjeta-lugar__titulo">
          <button type="button" class="tarjeta-lugar__enlace" data-accion="ver-lugar" data-id="${l.id}">${l.nombre}</button>
        </h3>
        <p class="t-small texto-2 tarjeta-lugar__direccion">${l.direccion}</p>
        <p class="t-small tarjeta-lugar__meta">
          ${l.calificacion != null && html`<span class="tarjeta-lugar__calificacion t-small-fuerte">${icono('estrella', 13)}${calificacion(l.calificacion)}</span>`}
          ${precio(l.precioMin, l.precioMax) && html`<span class="texto-2">${precio(l.precioMin, l.precioMax)}</span>`}
          <span class="${horario.abierto ? 'tarjeta-lugar__abierto' : 'texto-2'}">${horario.texto}</span>
        </p>
      </div>
    </article>`;
}

/** Esqueleto con brillo mientras cargan los datos. */
export function esqueleto(clase) {
  return html`<div class="esqueleto ${clase}" aria-hidden="true"></div>`;
}
