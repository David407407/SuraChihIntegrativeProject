/**
 * @file Secciones del Home (frames "Home (visitante)" y "Home (logueado)" de Figma):
 *
 *   <sc-saludo>            "Inicia sesión · ..." o "Hola, Andy · ..."      datos = { usuario }
 *   <sc-filtros-rapidos>   Todo / Eventos / Lugares · Hoy · Este fin de semana · Gratis · Fecha · Filtros
 *                          Emite `sc:filtros` → detail { tipo, cuando, gratis }
 *   <sc-recomendados>      "Recomendados para ti" (Card recomendada)    datos = ElementoCollage[]
 *   <sc-categorias>        "Explora por categoría" (Tile categoría)     datos = Etiqueta[]
 *   <sc-fila-tarjetas>     Fila con "Ver todo" (Card evento / Card lugar) datos = { items, favoritos }
 *
 * El carrusel está en sc-carrusel.js y el pie es el mismo <sc-pie> de la landing.
 */

import { Componente, definir, html } from '../../nucleo/componente.js';
import { icono, pildora } from '../atomos.js';
import { tarjetaEvento, tarjetaLugar, tarjetaRecomendada, esqueleto } from '../tarjetas.js';
import { observarRevelado } from '../../utilidades/animaciones.js';
import { mostrarToast } from '../sc-toast.js';

// ---------------------------------------------------------------------
// Saludo
// ---------------------------------------------------------------------

class Saludo extends Componente {
  datosIniciales() { return { usuario: null }; }

  plantilla({ usuario }) {
    return html`
      <div class="saludo contenedor">
        ${usuario
          ? html`<span class="t-small-fuerte">Hola, ${usuario.nombreUsuario}</span>`
          : html`<button type="button" class="saludo__iniciar t-small-fuerte" data-accion="iniciar-sesion">${icono('usuario-cielo-14', 14)}Inicia sesión</button>`}
        <span class="t-small texto-2">· Lo más destacado de esta semana en Chihuahua</span>
      </div>`;
  }
}

// ---------------------------------------------------------------------
// Filtros rápidos
// ---------------------------------------------------------------------

const TIPOS = [
  { valor: 'todo', texto: 'Todo' },
  { valor: 'eventos', texto: 'Eventos' },
  { valor: 'lugares', texto: 'Lugares' },
];

class FiltrosRapidos extends Componente {
  #filtro = { tipo: 'todo', cuando: null, gratis: false };

  plantilla() {
    const { tipo, cuando, gratis } = this.#filtro;
    return html`
      <div class="filtros-rapidos contenedor" role="toolbar" aria-label="Filtros rápidos">
        <div class="segmentos" role="group" aria-label="Mostrar">
          ${TIPOS.map((t) => html`
            <button type="button" class="segmentos__opcion t-small-fuerte" data-tipo="${t.valor}" aria-pressed="${String(t.valor === tipo)}">${t.texto}</button>`)}
        </div>
        <span class="filtros-rapidos__divisor" aria-hidden="true"></span>
        ${pildora({ texto: 'Hoy', valor: 'hoy', activa: cuando === 'hoy' })}
        ${pildora({ texto: 'Este fin de semana', valor: 'fin-de-semana', activa: cuando === 'fin-de-semana' })}
        ${pildora({ texto: 'Gratis', valor: 'gratis', activa: gratis })}
        <span class="filtros-rapidos__espacio"></span>
        <button type="button" class="boton boton--secundario boton--s" data-accion="filtro-fecha">${icono('calendario-16-tinta', 16)}<span>Fecha</span></button>
        <button type="button" class="boton boton--secundario boton--s" data-accion="filtros">${icono('filtros', 16)}<span>Filtros</span></button>
      </div>`;
  }

  alPintar() {
    this.querySelector('.filtros-rapidos').addEventListener('click', (e) => {
      const segmento = e.target.closest('[data-tipo]');
      const opcion = e.target.closest('.pildora')?.dataset.valor;
      if (segmento) this.#filtro.tipo = segmento.dataset.tipo;
      else if (opcion === 'gratis') this.#filtro.gratis = !this.#filtro.gratis;
      // "Hoy" y "Este fin de semana" se excluyen entre sí; tocar el activo lo quita.
      else if (opcion) this.#filtro.cuando = this.#filtro.cuando === opcion ? null : opcion;
      else return;
      this.pintar();
      this.querySelector(segmento ? `[data-tipo="${this.#filtro.tipo}"]` : `.pildora[data-valor="${opcion}"]`)?.focus();
      this.emitir('sc:filtros', { ...this.#filtro });
    });
  }
}

// ---------------------------------------------------------------------
// Recomendados para ti
// ---------------------------------------------------------------------

class Recomendados extends Componente {
  datosIniciales() { return null; }

  plantilla(elementos) {
    return html`
      <section class="home-seccion" aria-labelledby="titulo-recomendados">
        <div class="contenedor">
          <header class="home-seccion__encabezado">
            <h2 class="t-h2" id="titulo-recomendados">Recomendados para ti</h2>
            <p class="t-body texto-2">Lo que más se está guardando esta semana en Chihuahua.</p>
          </header>
          <div class="rejilla-recomendadas">
            ${elementos == null
              ? [0, 1, 2, 3].map(() => esqueleto('home__esqueleto-recomendada'))
              : elementos.map((el, i) => tarjetaRecomendada(el.tipo === 'lugar' ? el.lugar : el.evento, { tipo: el.tipo, orden: i }))}
          </div>
        </div>
      </section>`;
  }

  alPintar() {
    this.hidden = Array.isArray(this.datos) && this.datos.length === 0;
    observarRevelado(this);
  }
}

// ---------------------------------------------------------------------
// Explora por categoría
// ---------------------------------------------------------------------

/**
 * Tile de cada categoría, por tag.icon. `destino` es la fila del Home a la que lleva el tile
 * (la pantalla "Explorar" con filtros aún no existe).
 */
const TILES = {
  music: { texto: 'Música', icono: 'tile-musica', fondo: 'var(--categoria-musica)', destino: 'fila-musica' },
  family: { texto: 'Familiar', icono: 'tile-familiar', fondo: 'var(--categoria-familiar)', destino: 'fila-actividades' },
  run: { texto: 'Aire libre', icono: 'tile-aire-libre', fondo: 'var(--categoria-deportes)', destino: 'fila-actividades' },
  robot: { texto: 'Tecnología', icono: 'tile-tecnologia', fondo: 'var(--categoria-tecnologia)', destino: 'fila-actividades' },
  food: { texto: 'Gastronomía', icono: 'tile-gastronomia', fondo: 'var(--categoria-gastronomia)', destino: 'fila-actividades' },
  coffee: { texto: 'Cafés', icono: 'tile-cafes', fondo: 'var(--categoria-cafes)', destino: 'fila-cafes' },
  museum: { texto: 'Museos', icono: 'tile-museos', fondo: 'var(--categoria-museos)', destino: 'fila-tarde' },
};

/** "Tile categoría" de Figma (Normal / Hover). */
function tileCategoria(etiqueta) {
  const tile = TILES[etiqueta.icono];
  if (!tile) return '';
  return html`
    <button type="button" class="tile-categoria" style="--fondo-tile:${tile.fondo}" data-destino="${tile.destino}">
      <span class="tile-categoria__circulo">${icono(tile.icono, 18)}</span>
      <span class="tile-categoria__nombre t-h3">${tile.texto}</span>
      <span class="tile-categoria__flecha">${icono('chevron-tinta-12', 12)}</span>
    </button>`;
}

class Categorias extends Componente {
  datosIniciales() { return null; }

  plantilla(etiquetas) {
    return html`
      <section class="home-seccion" aria-labelledby="titulo-categorias">
        <div class="contenedor">
          <h2 class="t-h2 home-seccion__titulo" id="titulo-categorias">Explora por categoría</h2>
          <div class="tiles">
            ${etiquetas == null
              ? Object.keys(TILES).map(() => esqueleto('home__esqueleto-tile'))
              : etiquetas.map(tileCategoria)}
          </div>
        </div>
      </section>`;
  }

  alPintar() {
    this.hidden = Array.isArray(this.datos) && this.datos.length === 0;
    this.querySelector('.tiles').addEventListener('click', (e) => {
      const tile = e.target.closest('[data-destino]');
      if (!tile) return;
      const destino = document.getElementById(tile.dataset.destino);
      if (!destino || destino.hidden) {
        mostrarToast(`Todavía no hay nada de ${tile.querySelector('.tile-categoria__nombre').textContent} por aquí.`, { tipo: 'info' });
        return;
      }
      const suave = !window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      destino.scrollIntoView({ behavior: suave ? 'smooth' : 'auto', block: 'start' });
    });
  }
}

// ---------------------------------------------------------------------
// Fila de tarjetas ("Te podrían interesar", "Música en vivo"...)
// ---------------------------------------------------------------------

/**
 * Atributos: `titulo`, `tipo` ('evento' | 'lugar') y `ver-todo` (data-accion del enlace "Ver todo").
 * Datos: { items, favoritos } — `favoritos` es un Set con los ids guardados por el usuario.
 * Si no hay datos en la BD la sección se oculta; si los filtros la dejan vacía, lo dice.
 */
class FilaTarjetas extends Componente {
  datosIniciales() { return null; }

  plantilla(datos) {
    const titulo = this.getAttribute('titulo') ?? '';
    const esEvento = this.getAttribute('tipo') !== 'lugar';
    const idTitulo = `${this.id || 'fila'}-titulo`;
    return html`
      <section class="home-seccion fila" aria-labelledby="${idTitulo}">
        <div class="contenedor">
          <header class="fila__encabezado">
            <h2 class="t-h2" id="${idTitulo}">${titulo}</h2>
            <button type="button" class="fila__ver-todo t-small-fuerte" data-accion="${this.getAttribute('ver-todo') ?? ''}">
              Ver todo ${icono('chevron-tinta-10', 10)}
            </button>
          </header>
          ${datos == null
            ? html`<div class="fila__pista">${[0, 1, 2, 3, 4].map(() => esqueleto('home__esqueleto-tarjeta'))}</div>`
            : datos.items.length === 0
              ? html`<p class="fila__vacio t-body texto-2">Nada por aquí con estos filtros. Prueba quitando alguno.</p>`
              : html`<div class="fila__pista">${datos.items.map((item, i) => (esEvento
                ? tarjetaEvento(item, { orden: Math.min(i, 4), favorito: datos.favoritos?.has(item.id) })
                : tarjetaLugar(item, { orden: Math.min(i, 4), favorito: datos.favoritos?.has(item.id) })))}</div>`}
        </div>
      </section>`;
  }

  alPintar() { observarRevelado(this); }
}

definir('sc-saludo', Saludo);
definir('sc-filtros-rapidos', FiltrosRapidos);
definir('sc-recomendados', Recomendados);
definir('sc-categorias', Categorias);
definir('sc-fila-tarjetas', FilaTarjetas);
