/**
 * @file Base de todos los componentes de la interfaz.
 *
 * Los componentes son Web Components nativos (sin frameworks): cada uno es una etiqueta
 * `<sc-algo>` que se pinta a sí misma con `plantilla()` y recibe sus datos por la propiedad
 * `datos`. Se usa el DOM normal (sin Shadow DOM) para que todos compartan los estilos de /css.
 *
 * @example
 * class SaludoBonito extends Componente {
 *   plantilla({ nombre }) { return html`<p>Hola, ${nombre}</p>`; }
 * }
 * customElements.define('sc-saludo', SaludoBonito);
 * document.querySelector('sc-saludo').datos = { nombre: 'Ana' };
 */

/** Marca un texto como HTML ya escapado para que `html` no lo vuelva a escapar. */
class HtmlSeguro {
  /** @param {string} texto */
  constructor(texto) { this.texto = texto; }
  toString() { return this.texto; }
}

const ENTIDADES = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };

/** Escapa texto para insertarlo en HTML (evita inyectar etiquetas desde datos de la BD). */
export function escapar(valor) {
  return String(valor).replace(/[&<>"']/g, (c) => ENTIDADES[c]);
}

function convertir(valor) {
  if (valor == null || valor === false) return '';
  if (valor instanceof HtmlSeguro) return valor.texto;
  if (Array.isArray(valor)) return valor.map(convertir).join('');
  return escapar(valor);
}

/**
 * Plantilla HTML segura: todo lo interpolado se escapa, salvo otros `html` (o arreglos de ellos).
 * `null`, `undefined` y `false` no pintan nada, para poder escribir `${cond && html`...`}`.
 * @returns {HtmlSeguro}
 */
export function html(partes, ...valores) {
  let resultado = partes[0];
  valores.forEach((valor, i) => { resultado += convertir(valor) + partes[i + 1]; });
  return new HtmlSeguro(resultado);
}

/** Clase padre de los componentes con datos. */
export class Componente extends HTMLElement {
  #datos = null;
  #pintado = false;

  /** Datos del componente. Asignarlos llama a `actualizar` (por defecto, vuelve a pintarlo todo). */
  get datos() { return this.#datos; }
  set datos(valor) {
    this.#datos = valor;
    if (this.#pintado) this.actualizar(valor);
  }

  connectedCallback() {
    if (!this.#pintado) this.pintar();
  }

  /**
   * Reacciona a datos nuevos. Sobrescribir para actualizar solo una parte
   * (p. ej. la lista de tarjetas) y no reiniciar animaciones del resto.
   */
  actualizar() { this.pintar(); }

  /** Reemplaza el contenido con la plantilla y avisa a `alPintar` para enlazar eventos. */
  pintar() {
    this.#pintado = true;
    this.innerHTML = String(this.plantilla(this.#datos ?? this.datosIniciales()));
    this.alPintar();
  }

  /** Datos a usar mientras no lleguen los reales (sobrescribir si hace falta). */
  datosIniciales() { return {}; }

  /**
   * HTML del componente. Debe sobrescribirse.
   * @param {any} datos
   * @returns {HtmlSeguro|string}
   */
  plantilla(datos) { return ''; } // eslint-disable-line no-unused-vars

  /** Se llama después de cada pintado (enlazar eventos, animaciones...). */
  alPintar() {}

  /**
   * Lanza un evento personalizado que sube por el DOM (lo escucha main.js).
   * @param {string} nombre
   * @param {object} [detalle]
   */
  emitir(nombre, detalle = {}) {
    this.dispatchEvent(new CustomEvent(nombre, { detail: detalle, bubbles: true, composed: true }));
  }
}

/** Registra un componente solo una vez (evita errores si un módulo se importa dos veces). */
export function definir(etiqueta, clase) {
  if (!customElements.get(etiqueta)) customElements.define(etiqueta, clase);
}
