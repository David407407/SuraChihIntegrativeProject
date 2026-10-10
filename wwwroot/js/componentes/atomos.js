/**
 * @file Piezas pequeñas sin estado que se repiten en toda la app.
 * Son funciones que devuelven HTML (no etiquetas propias) porque no necesitan lógica.
 * Estilos en css/componentes/atomos.css.
 */

import { html } from '../nucleo/componente.js';

/**
 * Icono SVG exportado de Figma. Se dibuja centrado en una caja cuadrada de `caja` px,
 * igual que en el diseño (los SVG traen su tamaño y color originales).
 * @param {string} nombre Archivo en assets/iconos sin extensión.
 * @param {number} [caja] Tamaño de la caja en px.
 */
export function icono(nombre, caja = 16) {
  return html`<span class="icono" style="--caja:${caja}px" aria-hidden="true"><img src="assets/iconos/${nombre}.svg" alt=""></span>`;
}

/**
 * Botón del sistema de diseño (componente "Botón" de Figma).
 * Con `href` se pinta como enlace; con `accion` se pinta como botón que main.js atiende.
 *
 * @param {object} p
 * @param {string} p.texto
 * @param {'primario'|'oscuro'|'secundario'|'fantasma'|'contorno-claro'} [p.variante]
 * @param {'m'|'s'} [p.tamano] m = 48px de alto, s = 36px.
 * @param {string} [p.icono] Nombre del icono (ver `icono`).
 * @param {string} [p.accion] Valor de data-accion (navegación que resuelve main.js).
 * @param {number} [p.objetivo] Id del evento/lugar al que se refiere la acción (data-id).
 * @param {string} [p.href]
 * @param {string} [p.clase] Clases extra.
 */
export function boton({ texto, variante = 'primario', tamano = 'm', icono: nombreIcono, accion, objetivo, href, clase = '' }) {
  const clases = `boton boton--${variante} boton--${tamano} ${clase}`.trim();
  const contenido = html`${nombreIcono && icono(nombreIcono, tamano === 'm' ? 18 : 16)}<span>${texto}</span>`;
  return href
    ? html`<a class="${clases}" href="${href}" data-accion="${accion ?? ''}" data-id="${objetivo ?? ''}">${contenido}</a>`
    : html`<button type="button" class="${clases}" data-accion="${accion ?? ''}" data-id="${objetivo ?? ''}">${contenido}</button>`;
}

/**
 * Chip de texto pequeño ("Chip tipo" de Figma): categoría de un evento o lugar.
 * @param {string} texto
 * @param {'sol-suave'|'superficie'|'sol'} [variante]
 * @param {string} [nombreIcono]
 */
export function chip(texto, variante = 'sol-suave', nombreIcono) {
  return html`<span class="chip chip--${variante}">${nombreIcono && icono(nombreIcono, 12)}${texto}</span>`;
}

/**
 * Encabezado de sección (título H1 + bajada) con acción opcional a la derecha.
 * @param {object} p
 * @param {string} p.titulo
 * @param {string} [p.bajada]
 * @param {import('../nucleo/componente.js').HtmlSeguro} [p.accion] Normalmente un `boton(...)`.
 */
export function encabezadoSeccion({ titulo, bajada, accion }) {
  return html`
    <header class="encabezado-seccion" data-revelar>
      <div class="encabezado-seccion__textos">
        <h2 class="t-h1">${titulo}</h2>
        ${bajada && html`<p class="t-lead texto-2">${bajada}</p>`}
      </div>
      ${accion}
    </header>`;
}
