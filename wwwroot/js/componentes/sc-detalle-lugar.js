/**
 * @file Detalle de lugar: la hoja "Lugar / ..." de Figma (página Rediseño). Los 17 frames
 * "Lugar / Café de la Tercera", "Lugar / Museo del Mamut"... son ESTA misma plantilla con otros datos.
 * Misma ventana que el detalle de evento (hoja.js).
 *
 * Se abre al elegir un pin del mapa (mapa.js) y con data-accion="ver-lugar" data-id="N" (Card lugar).
 *
 * Partes (de arriba a abajo, igual que las capas de Figma):
 *   Portada   foto + Compartir, favorito y Cerrar.
 *   Info      chip de tipo, nombre, "Acerca del lugar", "Ubicación" (mini mapa <sc-mapa>) y "Opiniones".
 *   Boleto    Horario (abierto ahora), Precio promedio, Calificación, Dirección + "Cómo llegar",
 *             "Escribir reseña" y "Reportar lugar".
 *   También te puede gustar   hasta 3 "Card lugar" (tarjetaLugar) que manda C#.
 *
 * Datos: invocar('lugar.obtener', { id }) → Controladores/ControladorLugar.cs (forma en contenido/datos-demo.js).
 */

import './sc-imagen.js';
import './sc-mapa.js';
import './sc-boton-favorito.js';

import { html } from '../nucleo/componente.js';
import { invocar } from '../nucleo/puente.js';
import { boton, chip, icono } from './atomos.js';
import { tarjetaLugar } from './tarjetas.js';
import { crearHoja, portadaHoja, datoHoja, direccionConCiudad } from './hoja.js';
import { calificacion, horarioLugar, precio } from '../utilidades/formato.js';
import { proximamente, requiereSesion } from '../nucleo/navegacion.js';

const hoja = crearHoja({
  prefijo: 'lugar',
  obtener: (id) => invocar('lugar.obtener', { id }),
  plantilla,
  textoError: 'No se pudo abrir el lugar.',
  acciones: {
    'hoja-compartir': () => proximamente('Compartir lugares'),
    'lugar-escribir-resena': () => requiereSesion('Escribir reseñas'),
    'lugar-reportar': () => requiereSesion('Reportar lugares'),
  },
});

/** Abre la hoja del lugar (o la cambia por otro si ya está abierta). */
export const abrirDetalleLugar = (id, opciones) => hoja.abrir(id, opciones);
export const cerrarDetalleLugar = () => hoja.cerrar();
/** Si la página se abrió con #lugar-N, abre ese lugar. */
export const abrirLugarDesdeDireccion = () => hoja.abrirDesdeDireccion();

// ---------------------------------------------------------------------
// Plantilla
// ---------------------------------------------------------------------

/** Liga de Google Maps con las coordenadas del lugar (se abre en el navegador del sistema). */
const comoLlegar = (l) => `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(`${l.latitud},${l.longitud}`)}`;

/** Cinco estrellas de 12px: llenas hasta la calificación de la reseña. */
function estrellas(n) {
  return html`
    <span class="opinion__estrellas" role="img" aria-label="${n} de 5 estrellas">
      ${[1, 2, 3, 4, 5].map((i) => icono(i <= n ? 'estrella-12' : 'estrella-12-vacia', 12))}
    </span>`;
}

function opinion(r) {
  return html`
    <article class="opinion">
      <p class="opinion__cabeza"><span class="t-small-fuerte">${r.nombreUsuario}</span>${estrellas(r.calificacion)}</p>
      ${r.comentario && html`<p class="t-small texto-2">${r.comentario}</p>`}
    </article>`;
}

function plantilla(datos) {
  const l = datos.lugar;
  const tipo = l.etiquetas?.[0]?.nombre;
  const hayMapa = l.latitud != null && l.longitud != null;
  const opiniones = datos.opiniones ?? [];

  return html`
    ${portadaHoja({ tipo: 'lugar', id: l.id, titulo: l.nombre, imagenUrl: l.imagenUrl, favorito: datos.favorito })}

    <div class="hoja__cuerpo">
      <div class="hoja__info">
        ${tipo && html`<div class="hoja__categorias">${chip(tipo, 'fondo')}</div>`}
        <h1 class="t-h1 hoja__titulo">${l.nombre}</h1>
        <h2 class="t-h3">Acerca del lugar</h2>
        <p class="t-body texto-2 hoja__descripcion">${l.descripcion || 'Este lugar todavía no tiene descripción.'}</p>
        <h2 class="t-h3">Ubicación</h2>
        ${hayMapa && html`
          <sc-mapa class="hoja__mapa" lat="${l.latitud}" lng="${l.longitud}" titulo="${l.nombre}" aria-label="Mapa: ${l.nombre}"></sc-mapa>`}
        <p class="t-small texto-2">${direccionConCiudad(l.direccion)}</p>
        <h2 class="t-h3">Opiniones</h2>
        ${opiniones.length > 0
          ? html`<div class="hoja__opiniones">${opiniones.map(opinion)}</div>`
          : html`<p class="t-small texto-2">Todavía no hay opiniones. ¡Sé la primera persona en escribir una!</p>`}
      </div>

      <aside class="boleto" aria-label="Horario, precio y calificación">
        ${datoHoja('reloj', 'Horario', horarioLugar(l.horarioAhora).texto)}
        ${datoHoja('etiqueta-16', 'Precio promedio', precio(l.precioMin, l.precioMax) || 'Precio por confirmar')}
        ${datoHoja('estrella-16', 'Calificación', l.calificacion != null ? calificacion(l.calificacion) : 'Sin reseñas todavía')}
        ${datoHoja('ubicacion-16', 'Dirección', l.direccion)}
        <hr class="boleto__divisor">
        ${hayMapa && boton({ texto: 'Cómo llegar', icono: 'como-llegar', href: comoLlegar(l), externo: true })}
        ${boton({ texto: 'Escribir reseña', variante: 'secundario', icono: 'estrella-18', accion: 'lugar-escribir-resena' })}
        <button type="button" class="boleto__enlace t-small texto-2" data-accion="lugar-reportar">
          ${icono('bandera', 12)}<span>Reportar lugar</span>
        </button>
      </aside>
    </div>

    ${datos.recomendados?.length > 0 && html`
      <section class="hoja__recomendados">
        <h2 class="t-h2">También te puede gustar</h2>
        <div class="hoja__fila">${datos.recomendados.map((r, i) => tarjetaLugar(r, { orden: i }))}</div>
      </section>`}`;
}
