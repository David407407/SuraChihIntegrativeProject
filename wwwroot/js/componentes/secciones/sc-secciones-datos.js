/**
 * @file Secciones de la landing que se llenan con la BD:
 * <sc-eventos-destacados> ("No te los pierdas") y <sc-lugares> ("Lugares para descubrir").
 *
 * Ambas pintan su encabezado una vez y muestran esqueletos con brillo; cuando llegan los datos
 * (propiedad `datos` = arreglo de records) solo se reemplaza el área de tarjetas.
 */

import { Componente, definir, html } from '../../nucleo/componente.js';
import { boton, encabezadoSeccion } from '../atomos.js';
import { tarjetaEventoPrincipal, tarjetaEventoFila, tarjetaLugar, esqueleto } from '../tarjetas.js';
import { observarRevelado } from '../../utilidades/animaciones.js';
import * as textos from '../../contenido/landing.js';

/** Base de las dos secciones: encabezado fijo + área de contenido que se actualiza. */
class SeccionConDatos extends Componente {
  /** @returns {{titulo:string, bajada:string, accion:any, clase:string}} */
  configuracion() { throw new Error('Implementar configuracion()'); }
  /** HTML mientras cargan los datos. */
  esqueletos() { return ''; }
  /** HTML con datos (arreglo no vacío). */
  contenido(items) { return ''; } // eslint-disable-line no-unused-vars
  /** Mensaje cuando la BD no devuelve nada. */
  mensajeVacio() { return ''; }

  plantilla() {
    const { titulo, bajada, accion, clase } = this.configuracion();
    return html`
      <section class="seccion ${clase}" aria-label="${titulo}">
        <div class="contenedor">
          ${encabezadoSeccion({ titulo, bajada, accion })}
          <div class="seccion__contenido" aria-busy="true">${this.esqueletos()}</div>
        </div>
      </section>`;
  }

  actualizar(items) {
    const area = this.querySelector('.seccion__contenido');
    area.removeAttribute('aria-busy');
    area.innerHTML = String(items?.length
      ? this.contenido(items)
      : html`<p class="seccion__vacio t-body texto-2">${this.mensajeVacio()}</p>`);
    observarRevelado(area);
  }
}

class EventosDestacados extends SeccionConDatos {
  configuracion() {
    return {
      ...textos.eventosDestacados,
      clase: 'seccion--fondo',
      accion: boton({ texto: 'Ver todos los eventos', variante: 'secundario', icono: 'chevron-tinta-18', accion: 'explorar-eventos' }),
    };
  }

  esqueletos() {
    return html`
      <div class="destacados">
        ${esqueleto('esqueleto--principal')}
        <div class="destacados__lista">${[0, 1, 2, 3].map(() => esqueleto('esqueleto--fila'))}</div>
      </div>`;
  }

  contenido([principal, ...resto]) {
    return html`
      <div class="destacados">
        ${tarjetaEventoPrincipal(principal)}
        ${resto.length > 0 && html`<ul class="destacados__lista">${resto.slice(0, 4).map((e, i) => tarjetaEventoFila(e, i))}</ul>`}
      </div>`;
  }

  mensajeVacio() { return textos.eventosDestacados.vacio; }
}

class Lugares extends SeccionConDatos {
  configuracion() {
    return {
      ...textos.lugares,
      clase: '',
      accion: boton({ texto: 'Ver en el mapa', variante: 'secundario', icono: 'mapa-18', accion: 'mapa' }),
    };
  }

  esqueletos() {
    return html`<div class="lugares">${[0, 1, 2, 3].map(() => esqueleto('esqueleto--lugar'))}</div>`;
  }

  contenido(lugares) {
    return html`<div class="lugares">${lugares.slice(0, 4).map((l, i) => tarjetaLugar(l, i))}</div>`;
  }

  mensajeVacio() { return textos.lugares.vacio; }
}

definir('sc-eventos-destacados', EventosDestacados);
definir('sc-lugares', Lugares);
