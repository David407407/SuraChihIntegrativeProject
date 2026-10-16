/**
 * @file Hoja de detalle: la ventana grande de los frames "Evento / ..." y "Lugar / ..." de Figma
 * (portada de 1040 × 440, cuerpo con Info + Boleto y una fila de recomendaciones). Aquí vive solo lo que
 * comparten las dos: el `<dialog>`, el esqueleto de carga, cerrar (Esc, "Fondo cerrar", botón), el foco
 * y la dirección `#evento-N` / `#lugar-N`. Lo que va dentro lo pintan sc-detalle-evento.js y
 * sc-detalle-lugar.js. Estilos en css/componentes/detalle-evento.css.
 *
 * @example
 * const hoja = crearHoja({
 *   prefijo: 'lugar',
 *   obtener: (id) => invocar('lugar.obtener', { id }),
 *   plantilla: (datos) => html`...`,              // usa `portadaHoja(...)` y la clase .hoja__titulo
 *   acciones: { 'lugar-reportar': (datos) => ... },  // data-accion="lugar-reportar" dentro de la hoja
 * });
 * hoja.abrir(2);
 */

import { html } from '../nucleo/componente.js';
import { icono } from './atomos.js';
import { esqueleto } from './tarjetas.js';
import { mostrarToast } from './sc-toast.js';
import { observarRevelado, reducirMovimiento } from '../utilidades/animaciones.js';

/** Hojas creadas, para cerrar la otra si se abre una distinta (solo hay una a la vez). */
const hojas = new Set();

/**
 * Portada común: foto + Compartir, favorito y Cerrar ("Acciones" en Figma).
 * @param {object} p
 * @param {'evento'|'lugar'} p.tipo
 * @param {number} p.id
 * @param {string} p.titulo
 * @param {string|null} p.imagenUrl
 * @param {boolean} p.favorito
 */
export function portadaHoja({ tipo, id, titulo, imagenUrl, favorito }) {
  return html`
    <header class="hoja__portada">
      <sc-imagen src="${imagenUrl ?? ''}" alt="${titulo}" ancho="1040" alto="440" prioridad></sc-imagen>
      <div class="hoja__acciones">
        <button type="button" class="hoja__accion" data-accion="hoja-compartir" aria-label="Compartir">${icono('compartir', 16)}</button>
        <sc-boton-favorito tipo="${tipo}" objetivo="${id}" tamano="m" etiqueta="${titulo}"${favorito && html` activo`}></sc-boton-favorito>
        <button type="button" class="hoja__accion" data-accion="hoja-cerrar" aria-label="Cerrar">${icono('cerrar-16', 16)}</button>
      </div>
    </header>`;
}

/** Renglón del boleto: icono de 16px en caja gris + etiqueta y valor ("Dato Fecha", "Dato Horario"...). */
export function datoHoja(nombreIcono, etiqueta, valor) {
  return html`
    <div class="boleto__dato">
      <span class="boleto__icono">${icono(nombreIcono, 16)}</span>
      <p class="boleto__textos"><span class="t-caption texto-2">${etiqueta}</span><span class="t-body-fuerte">${valor}</span></p>
    </div>`;
}

/** Texto bajo el mini mapa, como en Figma: "Forum Buenavista, Chihuahua". */
export function direccionConCiudad(...partes) {
  const texto = partes.filter(Boolean).join(', ');
  if (!texto) return 'Ubicación por confirmar';
  return /chihuahua/i.test(texto) ? texto : `${texto}, Chihuahua`;
}

/**
 * @param {object} config
 * @param {string} config.prefijo 'evento' o 'lugar': la dirección queda en #<prefijo>-<id>.
 * @param {(id:number) => Promise<object>} config.obtener Pide los datos a C# (o a los demo).
 * @param {(datos:object) => any} config.plantilla HTML de la hoja (html``).
 * @param {Record<string, (datos:object, origen:HTMLElement) => void>} [config.acciones] data-accion propias.
 * @param {(datos:object, opciones:object) => void} [config.alMostrar] Después de pintar (p. ej. abrir "¿Vas a ir?").
 * @param {string} [config.textoError] Toast si no se pudo cargar.
 */
export function crearHoja({ prefijo, obtener, plantilla, acciones = {}, alMostrar, textoError = 'No se pudo abrir.' }) {
  const marca = `#${prefijo}-`;
  /** @type {HTMLDialogElement|null} */
  let dialogo = null;
  let datos = null;
  let idActual = null;
  let solicitud = 0;        // una respuesta lenta de un id anterior no pisa al nuevo
  let conHistorial = false; // esta hoja agregó #prefijo-N al historial (cerrar hace history.back())
  let focoAnterior = null;
  let siguienteTitulo = 1;

  const caja = () => dialogo.querySelector('.hoja__caja');
  const abierta = () => Boolean(dialogo?.open) && !dialogo.classList.contains('hoja--cerrando');

  const idEnDireccion = () => {
    const id = location.hash.startsWith(marca) ? Number(location.hash.slice(marca.length)) : NaN;
    return Number.isInteger(id) && id > 0 ? id : null;
  };

  function crearDialogo() {
    const d = document.createElement('dialog');
    d.className = 'hoja';
    d.innerHTML = '<article class="hoja__caja"></article>';
    d.addEventListener('cancel', (e) => {   // Esc
      e.preventDefault();
      cerrar();
    });
    d.addEventListener('click', (e) => {
      if (e.target === d) { cerrar(); return; }   // "Fondo cerrar": clic fuera de la hoja
      const origen = e.target.closest('[data-accion]');
      const accion = origen?.dataset.accion;
      const comunes = { 'hoja-cerrar': () => cerrar() };
      const manejador = comunes[accion] ?? acciones[accion];
      if (!manejador) return;   // las demás (ver-evento, ver-lugar...) las atiende nucleo/navegacion.js
      e.preventDefault();
      e.stopPropagation();
      if (datos) manejador(datos, origen);
    });
    document.body.append(d);
    return d;
  }

  function ponerDireccion(id) {
    const destino = `${marca}${id}`;
    if (location.hash === destino) return;
    if (conHistorial) {
      history.replaceState({ [prefijo]: id }, '', destino);   // de una recomendación a otra: un solo Atrás
    } else {
      history.pushState({ [prefijo]: id }, '', destino);
      conHistorial = true;
    }
  }

  function pintarCargando() {
    dialogo.setAttribute('aria-busy', 'true');
    caja().innerHTML = String(html`
      ${esqueleto('hoja__portada hoja__esqueleto-portada')}
      <div class="hoja__cuerpo" aria-hidden="true">
        <div class="hoja__info">
          ${esqueleto('hoja__esqueleto-linea hoja__esqueleto-linea--chips')}
          ${esqueleto('hoja__esqueleto-linea hoja__esqueleto-linea--titulo')}
          ${esqueleto('hoja__esqueleto-linea')}
          ${esqueleto('hoja__esqueleto-linea')}
          ${esqueleto('hoja__mapa')}
        </div>
        ${esqueleto('boleto hoja__esqueleto-boleto')}
      </div>
      <p class="solo-lectores" role="status">Cargando…</p>`);
  }

  function pintar() {
    const contenedor = caja();
    contenedor.innerHTML = String(plantilla(datos));
    const titulo = contenedor.querySelector('.hoja__titulo');
    if (titulo) {
      titulo.id = `hoja-${prefijo}-titulo-${siguienteTitulo++}`;
      titulo.tabIndex = -1;
      dialogo.setAttribute('aria-labelledby', titulo.id);
      // El foco va al título: los lectores de pantalla lo anuncian y Tab sigue con la acción principal.
      titulo.focus({ preventScroll: true });
    }
    dialogo.removeAttribute('aria-busy');
    observarRevelado(contenedor);
  }

  /**
   * Abre la hoja (o la cambia por otro id si ya está abierta).
   * @param {number} id
   * @param {object} [opciones] Se pasan a `alMostrar`. `historial: false` cuando la abre Atrás / Adelante.
   */
  async function abrir(id, opciones = {}) {
    if (!Number.isInteger(id)) return;
    for (const otra of hojas) if (otra !== hoja) otra.cerrar({ sinAnimacion: true });
    if (!dialogo) dialogo = crearDialogo();
    const numero = ++solicitud;
    idActual = id;

    if (!dialogo.open || dialogo.classList.contains('hoja--cerrando')) {
      dialogo.classList.remove('hoja--cerrando');
      if (!dialogo.open) {
        focoAnterior = document.activeElement;
        dialogo.showModal();
      }
      document.documentElement.classList.add('con-hoja');
    }
    if (opciones.historial !== false) ponerDireccion(id);
    pintarCargando();
    dialogo.scrollTo({ top: 0 });

    try {
      const respuesta = await obtener(id);
      if (numero !== solicitud || !dialogo.open) return;
      datos = respuesta;
      pintar();
      alMostrar?.(datos, opciones);
    } catch (error) {
      if (numero !== solicitud) return;
      console.error(error);
      cerrar();
      mostrarToast(error.message || textoError, { tipo: 'error' });
    }
  }

  /** Cierra con animación y regresa el foco a donde estaba. */
  function cerrar({ desdeHistorial = false, sinAnimacion = false } = {}) {
    if (!dialogo?.open || (dialogo.classList.contains('hoja--cerrando') && !sinAnimacion)) return;
    solicitud++;
    const d = dialogo;

    if (!desdeHistorial) {
      if (conHistorial && !sinAnimacion) history.back();   // el popstate que llega ya la encuentra cerrando
      else if (location.hash.startsWith(marca)) history.replaceState(history.state, '', location.pathname + location.search);
    }
    conHistorial = false;

    const terminar = () => {
      if (!d.classList.contains('hoja--cerrando')) return;   // se volvió a abrir mientras salía
      d.classList.remove('hoja--cerrando');
      d.close();
      d.querySelector('.hoja__caja').replaceChildren();
      if (![...hojas].some((h) => h.abierta())) document.documentElement.classList.remove('con-hoja');
      datos = null;
      idActual = null;
      if (!sinAnimacion) focoAnterior?.focus?.({ preventScroll: true });
      focoAnterior = null;
    };
    d.classList.add('hoja--cerrando');
    if (sinAnimacion || reducirMovimiento()) { terminar(); return; }
    d.querySelector('.hoja__caja').addEventListener('animationend', terminar, { once: true });
    setTimeout(terminar, 400);   // por si la animación no llega a correr
  }

  /** Vuelve a pedir los datos del mismo id (p. ej. después de iniciar sesión). */
  function refrescar() {
    if (abierta() && idActual) abrir(idActual, { historial: false });
  }

  /** Si la página se abrió con #prefijo-N (o se regresó a él con Adelante), lo abre. */
  function abrirDesdeDireccion() {
    const id = idEnDireccion();
    if (id) abrir(id, { historial: false });
  }

  /** Repinta una parte sin recargar (p. ej. el boleto al inscribirse). */
  function repintar(selector, contenido) {
    const parte = dialogo?.querySelector(selector);
    if (parte) parte.innerHTML = String(contenido);
  }

  window.addEventListener('popstate', () => {
    const id = idEnDireccion();
    if (id) {
      abrir(id, { historial: false });
    } else if (dialogo?.open) {
      conHistorial = false;
      cerrar({ desdeHistorial: true });
    }
  });

  // Al iniciar sesión con la hoja abierta se recarga para mostrar inscripción y corazón de ese usuario.
  document.addEventListener('sc:sesion', refrescar);

  const hoja = {
    abrir, cerrar, refrescar, abrirDesdeDireccion, repintar, abierta,
    get datos() { return datos; },
  };
  hojas.add(hoja);
  return hoja;
}
