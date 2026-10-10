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
 * Botón del sistema de diseño (componente "Botón" de Figma: 5 tipos × 2 tamaños).
 * Con `href` se pinta como enlace; con `accion` se pinta como botón que main.js atiende.
 *
 * @example
 * boton({ texto: 'Voy a ir', icono: 'calendario-check', accion: 'inscribirse', objetivo: evento.id })
 * boton({ texto: 'Eliminar cuenta', variante: 'peligro', tamano: 's', accion: 'eliminar-cuenta' })
 * boton({ texto: 'Entrar', tipo: 'submit' })   // dentro de un <form>
 *
 * @param {object} p
 * @param {string} p.texto
 * @param {'primario'|'oscuro'|'secundario'|'peligro'|'fantasma'|'contorno-claro'} [p.variante]
 *   Los cinco primeros son los de Figma; 'contorno-claro' es el del hero azul.
 * @param {'m'|'s'} [p.tamano] m = 48px de alto, s = 36px.
 * @param {string} [p.icono] Nombre del icono (ver `icono`). En oscuro y peligro se pinta blanco.
 * @param {string} [p.accion] Valor de data-accion (navegación que resuelve main.js).
 * @param {number} [p.objetivo] Id del evento/lugar al que se refiere la acción (data-id).
 * @param {string} [p.href]
 * @param {'button'|'submit'} [p.tipo] Tipo del <button> (submit para enviar formularios).
 * @param {boolean} [p.deshabilitado]
 * @param {string} [p.clase] Clases extra.
 */
export function boton({
  texto, variante = 'primario', tamano = 'm', icono: nombreIcono, accion, objetivo, href,
  tipo = 'button', deshabilitado = false, clase = '',
}) {
  const clases = `boton boton--${variante} boton--${tamano} ${clase}`.trim();
  const contenido = html`${nombreIcono && icono(nombreIcono, tamano === 'm' ? 18 : 16)}<span>${texto}</span>`;
  return href
    ? html`<a class="${clases}" href="${href}" data-accion="${accion ?? ''}" data-id="${objetivo ?? ''}"${deshabilitado && html` aria-disabled="true"`}>${contenido}</a>`
    : html`<button type="${tipo}" class="${clases}" data-accion="${accion ?? ''}" data-id="${objetivo ?? ''}"${deshabilitado && html` disabled`}>${contenido}</button>`;
}

let siguienteCampo = 1;

/**
 * Campo de texto (componente "Campo" de Figma): icono a la izquierda, 48px de alto.
 * Es un `<input>` normal, así que se lee con `FormData` o `form.elements` como cualquier formulario.
 * Las contraseñas traen un botón "Mostrar" / "Ocultar".
 *
 * @example
 * campo({ nombre: 'correo', etiqueta: 'Correo electrónico', tipo: 'email', icono: 'correo', requerido: true })
 * campo({ nombre: 'contrasena', etiqueta: 'Contraseña', tipo: 'password', ayuda: 'Mínimo 8 caracteres' })
 *
 * @param {object} p
 * @param {string} p.nombre Atributo name (clave en FormData).
 * @param {string} p.etiqueta Texto de la etiqueta (y del placeholder si no se da otro).
 * @param {boolean} [p.etiquetaVisible] false = la etiqueta solo la leen los lectores de pantalla (como en Figma).
 * @param {'text'|'email'|'password'|'search'|'tel'|'url'|'number'|'date'|'time'} [p.tipo]
 * @param {string} [p.icono] Icono de 16px de assets/iconos (p. ej. 'correo', 'buscar-gris').
 * @param {string} [p.placeholder]
 * @param {string} [p.valor]
 * @param {string} [p.ayuda] Texto pequeño debajo.
 * @param {string} [p.error] Mensaje de error (pinta el borde rojo). Ver también `marcarError`.
 * @param {boolean} [p.requerido]
 * @param {boolean} [p.deshabilitado]
 * @param {string} [p.autocompletar] Atributo autocomplete ('email', 'current-password'...).
 * @param {number} [p.maximo] maxlength.
 */
export function campo({
  nombre, etiqueta, etiquetaVisible = false, tipo = 'text', icono: nombreIcono, placeholder, valor = '',
  ayuda, error, requerido = false, deshabilitado = false, autocompletar, maximo,
}) {
  const id = `campo-${nombre}-${siguienteCampo++}`;
  const clases = `campo${error ? ' campo--error' : ''}${deshabilitado ? ' campo--deshabilitado' : ''}`;
  return html`
    <div class="${clases}">
      <label class="campo__etiqueta t-small-fuerte${etiquetaVisible ? '' : ' solo-lectores'}" for="${id}">${etiqueta}</label>
      <div class="campo__caja">
        ${nombreIcono && icono(nombreIcono, 16)}
        <input class="campo__entrada" id="${id}" name="${nombre}" type="${tipo}" value="${valor}"
          placeholder="${placeholder ?? etiqueta}"
          aria-describedby="${id}-error${ayuda ? ` ${id}-ayuda` : ''}"
          ${requerido && html`required`} ${deshabilitado && html`disabled`} ${error && html`aria-invalid="true"`}
          ${autocompletar && html`autocomplete="${autocompletar}"`} ${maximo && html`maxlength="${maximo}"`}>
        ${tipo === 'password' && html`<button type="button" class="campo__ver" aria-controls="${id}" aria-pressed="false">Mostrar</button>`}
      </div>
      ${ayuda && html`<p class="campo__ayuda t-caption" id="${id}-ayuda">${ayuda}</p>`}
      <p class="campo__error t-caption" id="${id}-error" role="alert">${error ?? ''}</p>
    </div>`;
}

/**
 * Pone o quita el estado de error de un campo ya pintado (p. ej. con el mensaje de C#).
 * @param {HTMLElement} elemento El `.campo` o cualquier elemento dentro de él (el input).
 * @param {string|null} mensaje null o '' quita el error.
 */
export function marcarError(elemento, mensaje) {
  const contenedor = elemento.closest('.campo');
  if (!contenedor) return;
  contenedor.classList.toggle('campo--error', Boolean(mensaje));
  contenedor.querySelector('.campo__error').textContent = mensaje ?? '';
  const entrada = contenedor.querySelector('.campo__entrada');
  if (mensaje) entrada.setAttribute('aria-invalid', 'true');
  else entrada.removeAttribute('aria-invalid');
}

// Mostrar / ocultar contraseña y quitar el error al volver a escribir (una sola vez para toda la página).
document.addEventListener('click', (e) => {
  const ver = e.target.closest('.campo__ver');
  if (!ver) return;
  const entrada = document.getElementById(ver.getAttribute('aria-controls'));
  const mostrar = entrada.type === 'password';
  entrada.type = mostrar ? 'text' : 'password';
  ver.textContent = mostrar ? 'Ocultar' : 'Mostrar';
  ver.setAttribute('aria-pressed', String(mostrar));
});
document.addEventListener('input', (e) => {
  if (e.target.matches?.('.campo--error .campo__entrada')) marcarError(e.target, null);
});

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
 * Estilo de cada categoría, según el campo `icono` de la etiqueta en la BD (tag.icon).
 * Las etiquetas que no están aquí (las de lugares: coffee, museum...) se pintan con fondo neutro y sin icono.
 */
const CATEGORIAS = {
  family: 'familiar',
  music: 'musica',
  run: 'deportes',
  robot: 'tecnologia',
  food: 'gastronomia',
};

/**
 * "Chip categoría" de Figma (Familiar, Música, Deportes...).
 * - Interactivo (por defecto): `<sc-chip-categoria>`, se activa / desactiva y emite `sc:categoria`.
 * - Estático (`interactivo: false`): solo muestra la categoría, p. ej. en el detalle de un evento.
 *
 * @example
 * etiquetas.map((t) => chipCategoria(t, { activo: filtro.has(t.id) }))
 * evento.etiquetas.map((t) => chipCategoria(t, { interactivo: false }))
 *
 * @param {{id:number, nombre:string, icono?:string}} etiqueta Etiqueta de BackendLogica.
 * @param {{activo?:boolean, interactivo?:boolean}} [opciones]
 */
export function chipCategoria(etiqueta, { activo = false, interactivo = true } = {}) {
  if (interactivo) {
    return html`<sc-chip-categoria etiqueta="${etiqueta.id}" nombre="${etiqueta.nombre}" icono="${etiqueta.icono ?? ''}"${activo && html` activo`}></sc-chip-categoria>`;
  }
  return html`<span class="${claseCategoria(etiqueta.icono)}">${iconoCategoria(etiqueta.icono)}${etiqueta.nombre}</span>`;
}

/** Clases del chip según el icono de la etiqueta (las usa también sc-chip-categoria.js). */
export function claseCategoria(iconoBD) {
  const tipo = CATEGORIAS[iconoBD];
  return `chip-categoria${tipo ? ` chip-categoria--${tipo}` : ''}`;
}

/** Icono de 16px de la categoría, o nada si la etiqueta no tiene uno en Figma. */
export function iconoCategoria(iconoBD) {
  const tipo = CATEGORIAS[iconoBD];
  return tipo ? icono(`cat-${tipo}`, 16) : '';
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
