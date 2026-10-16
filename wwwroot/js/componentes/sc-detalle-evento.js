/**
 * @file Detalle de evento: la hoja "Evento / ..." de Figma (página Rediseño). Los 24 frames
 * "Evento / Minecraft...", "Evento / Las Ligas Menores"... son ESTA misma plantilla con otros datos.
 * La ventana, el historial y la carga son los de hoja.js (los comparte con el detalle de lugar).
 *
 * @example
 * abrirDetalleEvento(3);                       // lo hace sola la acción data-accion="ver-evento" data-id="3"
 * abrirDetalleEvento(3, { inscribir: true });  // y además abre "¿Vas a ir?" (botón "Voy a ir" de las tarjetas)
 *
 * Partes (de arriba a abajo, igual que las capas de Figma):
 *   Portada   foto (sc-imagen) + Compartir, favorito (sc-boton-favorito tamaño m) y Cerrar.
 *   Info      categorías, título, "Acerca del evento", "Ubicación" con el mini mapa (<sc-mapa> de Carlos).
 *   Boleto    Fecha, Horario, Lugar, Precio y las acciones según el estado (ver `estadoDe`).
 *   Te podría interesar   hasta 3 "Card evento" (tarjetaEvento) que manda C#.
 *
 * Estados (columnas de v_event_card: status, is_finished, cancel_reason):
 *   normal      "Voy a ir" + "Comprar boletos".
 *   inscrito    "Ya vas a ir" (verde) + "Cancelar inscripción" + "Comprar boletos".
 *   finalizado  aviso "Este evento ya terminó" + "Escribir reseña" + "Ver reseñas (N)".
 *   cancelado   aviso "Este evento fue cancelado" + "Motivo".
 * "Fecha por confirmar" / "Precio por confirmar" salen de utilidades/formato.js.
 *
 * Datos: invocar('evento.obtener', { id }) → Controladores/ControladorEvento.cs (forma en contenido/datos-demo.js).
 */

import './sc-imagen.js';
import './sc-mapa.js';
import './sc-boton-favorito.js';

import { html } from '../nucleo/componente.js';
import { invocar } from '../nucleo/puente.js';
import { boton, chipCategoria, icono } from './atomos.js';
import { tarjetaEvento } from './tarjetas.js';
import { mostrarToast } from './sc-toast.js';
import { abrirRequiereSesion } from './modales/cuenta.js';
import { confirmarInscripcion, confirmarCancelacion } from './modales/inscripcion.js';
import { crearHoja, portadaHoja, datoHoja, direccionConCiudad } from './hoja.js';
import { fechaEvento, horarioEvento, precioEvento } from '../utilidades/formato.js';
import { haySesion, proximamente, requiereSesion } from '../nucleo/navegacion.js';

const hoja = crearHoja({
  prefijo: 'evento',
  obtener: (id) => invocar('evento.obtener', { id }),
  plantilla,
  textoError: 'No se pudo abrir el evento.',
  acciones: {
    'hoja-compartir': () => proximamente('Compartir eventos'),
    'evento-voy': (datos) => alPulsarVoyAIr(datos),
    'evento-cancelar-inscripcion': (datos) => alPulsarCancelar(datos),
    'evento-escribir-resena': () => requiereSesion('Escribir reseñas'),
    'evento-ver-resenas': () => proximamente('Las reseñas'),
    'evento-reportar': () => requiereSesion('Reportar eventos'),
  },
  alMostrar: (datos, { inscribir }) => {
    if (inscribir && estadoDe(datos.evento, datos.inscrito) === 'normal') alPulsarVoyAIr(datos);
  },
});

/**
 * Abre la hoja del evento (o la cambia por otro si ya está abierta, p. ej. desde "Te podría interesar").
 * @param {number} id
 * @param {{inscribir?:boolean, historial?:boolean}} [opciones] `inscribir`: al cargar abre "¿Vas a ir?"
 *   (o "Inicia sesión para continuar").
 */
export const abrirDetalleEvento = (id, opciones) => hoja.abrir(id, opciones);
export const cerrarDetalleEvento = () => hoja.cerrar();
/** Si la página se abrió con #evento-N (o se regresó a él con Adelante), abre ese evento. */
export const abrirEventoDesdeDireccion = () => hoja.abrirDesdeDireccion();

// ---------------------------------------------------------------------
// "Voy a ir" / "Cancelar inscripción"
// ---------------------------------------------------------------------

async function alPulsarVoyAIr(datos) {
  if (!haySesion()) { abrirRequiereSesion(); return; }
  if (await confirmarInscripcion(datos.evento)) cambiarInscripcion(datos, true, '¡Listo! Lo agregamos a Mis planes');
}

async function alPulsarCancelar(datos) {
  if (!(await confirmarCancelacion(datos.evento))) cambiarInscripcion(datos, false, 'Cancelaste tu inscripción');
}

/** Pinta de nuevo solo el boleto (el resto de la hoja y el mapa se quedan como están). */
function cambiarInscripcion(datos, inscrito, aviso) {
  if (hoja.datos !== datos) return;   // mientras tanto se abrió otro evento
  datos.inscrito = inscrito;
  hoja.repintar('.boleto', contenidoBoleto(datos));
  mostrarToast(aviso);
}

// ---------------------------------------------------------------------
// Plantilla
// ---------------------------------------------------------------------

/** @returns {'normal'|'inscrito'|'finalizado'|'cancelado'|'no-publicado'} */
function estadoDe(e, inscrito) {
  if (e.estado === 'cancelado') return 'cancelado';
  if (e.terminado) return 'finalizado';
  if (e.estado !== 'aprobado') return 'no-publicado';   // borrador o en revisión: solo lo ve su organizador
  return inscrito ? 'inscrito' : 'normal';
}

/** Lugar del evento o, si es en la calle, su dirección. */
const lugarDe = (e) => e.lugarNombre ?? e.direccion;

function plantilla(datos) {
  const e = datos.evento;
  const hayMapa = e.latitud != null && e.longitud != null;
  const favoritos = new Set(datos.favoritos ?? []);

  return html`
    ${portadaHoja({ tipo: 'evento', id: e.id, titulo: e.titulo, imagenUrl: e.imagenUrl, favorito: datos.favorito })}

    <div class="hoja__cuerpo">
      <div class="hoja__info">
        ${e.etiquetas?.length > 0 && html`
          <div class="hoja__categorias">${e.etiquetas.map((t) => chipCategoria(t, { interactivo: false }))}</div>`}
        <h1 class="t-h1 hoja__titulo">${e.titulo}</h1>
        <h2 class="t-h3">Acerca del evento</h2>
        <p class="t-body texto-2 hoja__descripcion">${e.descripcion || 'El organizador todavía no agrega una descripción.'}</p>
        <h2 class="t-h3">Ubicación</h2>
        ${hayMapa && html`
          <sc-mapa class="hoja__mapa" lat="${e.latitud}" lng="${e.longitud}" titulo="${lugarDe(e) ?? e.titulo}"
            aria-label="Mapa: ${lugarDe(e) ?? 'ubicación del evento'}"></sc-mapa>`}
        <p class="t-small texto-2">${direccionConCiudad(e.lugarNombre, e.direccion)}</p>
      </div>

      <aside class="boleto" aria-label="Fecha, lugar y precio">${contenidoBoleto(datos)}</aside>
    </div>

    ${datos.recomendados?.length > 0 && html`
      <section class="hoja__recomendados">
        <h2 class="t-h2">Te podría interesar</h2>
        <div class="hoja__fila">
          ${datos.recomendados.map((r, i) => tarjetaEvento(r, { orden: i, favorito: favoritos.has(r.id) }))}
        </div>
      </section>`}`;
}

const AVISOS = {
  finalizado: { icono: 'evento-terminado', texto: 'Este evento ya terminó' },
  cancelado: { icono: 'evento-cancelado', texto: 'Este evento fue cancelado' },
  'no-publicado': { icono: 'evento-terminado', texto: 'Este evento todavía no está publicado' },
};

function contenidoBoleto(datos) {
  const e = datos.evento;
  const estado = estadoDe(e, datos.inscrito);
  const aviso = AVISOS[estado];
  const activo = estado === 'normal' || estado === 'inscrito';
  const horario = activo && horarioEvento(e);
  const comprar = activo && e.boletosUrl
    && boton({ texto: 'Comprar boletos', variante: 'secundario', icono: 'boleto', href: e.boletosUrl, externo: true });

  return html`
    ${aviso && html`
      <p class="boleto__aviso boleto__aviso--${estado} t-small-fuerte">${icono(aviso.icono, 16)}<span>${aviso.texto}</span></p>`}
    ${datoHoja('calendario', 'Fecha', fechaEvento(e))}
    ${horario && datoHoja('reloj', 'Horario', horario)}
    ${datoHoja('ubicacion-16', 'Lugar', lugarDe(e) ?? 'Por confirmar')}
    ${datoHoja('etiqueta-16', 'Precio', precioEvento(e) || 'Precio por confirmar')}
    <hr class="boleto__divisor">

    ${estado === 'normal' && html`
      ${boton({ texto: 'Voy a ir', icono: 'calendario-check', accion: 'evento-voy' })}
      ${comprar}`}

    ${estado === 'inscrito' && html`
      <button type="button" class="boleto__inscrito" data-accion="evento-cancelar-inscripcion">
        ${icono('check-circulo-blanco', 18)}<span>Ya vas a ir</span>
      </button>
      <button type="button" class="boleto__enlace boleto__enlace--peligro t-small-fuerte" data-accion="evento-cancelar-inscripcion">
        ${icono('x-error-12', 12)}<span>Cancelar inscripción</span>
      </button>
      ${comprar}`}

    ${estado === 'finalizado' && html`
      ${boton({ texto: 'Escribir reseña', icono: 'estrella-18', accion: 'evento-escribir-resena' })}
      ${boton({ texto: `Ver reseñas (${datos.numResenas ?? 0})`, variante: 'secundario', accion: 'evento-ver-resenas' })}`}

    ${estado === 'cancelado' && e.motivoCancelacion && html`
      <div class="boleto__motivo t-small">
        <p class="t-small-fuerte">Motivo</p>
        <p class="texto-2">${e.motivoCancelacion}</p>
      </div>`}

    <button type="button" class="boleto__enlace t-small texto-2" data-accion="evento-reportar">
      ${icono('bandera', 12)}<span>Reportar evento</span>
    </button>`;
}
