/**
 * @file Secciones de la landing cuyo contenido es fijo (textos en contenido/landing.js):
 * <sc-cinta-categorias>, <sc-como-funciona>, <sc-encuesta>, <sc-banda-organizadores>,
 * <sc-llamado-final> y <sc-pie>.
 */

import { Componente, definir, html } from '../../nucleo/componente.js';
import { boton, chip, icono, encabezadoSeccion } from '../atomos.js';
import * as textos from '../../contenido/landing.js';

/** Cinta azul con categorías que se desplaza sin fin ("Cinta categorías", posiciones A → B). */
class CintaCategorias extends Componente {
  plantilla() {
    // Dos copias idénticas: al recorrer exactamente una, el ciclo vuelve a empezar sin saltos.
    const fila = (copia) => html`
      <ul class="cinta__fila" aria-hidden="${copia ? 'true' : 'false'}">
        ${textos.cintaCategorias.map((nombre) => html`
          <li class="t-h3">${nombre}</li>
          <li class="cinta__punto" aria-hidden="true"><img src="assets/iconos/punto-cinta.svg" alt="" width="8" height="8"></li>`)}
      </ul>`;
    return html`
      <div class="cinta" aria-label="Categorías">
        <div class="cinta__pista">${fila(false)}${fila(true)}</div>
      </div>`;
  }
}

/** "Tu plan, en tres pasos". */
class ComoFunciona extends Componente {
  plantilla() {
    const { titulo, bajada, pasos } = textos.comoFunciona;
    return html`
      <section class="seccion" aria-label="${titulo}">
        <div class="contenedor">
          ${encabezadoSeccion({ titulo, bajada })}
          <ol class="pasos">
            ${pasos.map((paso, i) => html`
              <li class="paso" data-revelar style="--orden:${i}">
                <div class="paso__marcas">
                  <span class="paso__numero t-h3">${i + 1}</span>
                  <span class="paso__icono">${icono(paso.icono, 16)}</span>
                </div>
                <h3 class="t-h2">${paso.titulo}</h3>
                <p class="t-body texto-2">${paso.texto}</p>
              </li>`)}
          </ol>
        </div>
      </section>`;
  }
}

/** "Chihuahua tiene mucho que hacer..." con los datos de la encuesta. */
class Encuesta extends Componente {
  plantilla() {
    const { titulo, datos, fuente } = textos.encuesta;
    return html`
      <section class="seccion encuesta" aria-label="Por qué SuraChih">
        <div class="contenedor">
          <h2 class="t-h1 encuesta__titulo" data-revelar>${titulo}</h2>
          <dl class="encuesta__datos">
            ${datos.map((d, i) => html`
              <div class="encuesta__dato" data-revelar style="--orden:${i}">
                <dt class="t-display">${d.cifra}</dt>
                <dd class="t-body">${d.texto}</dd>
              </div>`)}
          </dl>
          <p class="t-small encuesta__fuente" data-revelar>${fuente}</p>
        </div>
      </section>`;
  }
}

/** Banda oscura "¿Organizas eventos en Chihuahua?". */
class BandaOrganizadores extends Componente {
  plantilla() {
    const { etiqueta, titulo, texto } = textos.organizadores;
    return html`
      <section class="contenedor banda-organizadores-envoltura" aria-label="${etiqueta}">
        <div class="banda-organizadores" data-revelar>
          <img class="banda-organizadores__sol" src="assets/iconos/banda-sol.svg" alt="" aria-hidden="true">
          <div class="banda-organizadores__mensaje">
            ${chip(etiqueta, 'sol', 'tienda')}
            <h2 class="t-h2">${titulo}</h2>
            <p class="t-body">${texto}</p>
          </div>
          ${boton({ texto: 'Publicar mi evento', icono: 'subir', accion: 'publicar-evento' })}
        </div>
      </section>`;
  }
}

/** Bloque amarillo final "Tu próximo plan está a un clic". */
class LlamadoFinal extends Componente {
  plantilla() {
    const { titulo, texto } = textos.llamadoFinal;
    return html`
      <section class="contenedor llamado-final-envoltura" aria-label="${titulo}">
        <div class="llamado-final" data-revelar>
          <h2 class="t-display">${titulo}</h2>
          <p class="t-body">${texto}</p>
          <div class="llamado-final__acciones">
            ${boton({ texto: 'Explorar eventos', variante: 'oscuro', accion: 'explorar-eventos' })}
            ${boton({ texto: 'Crear cuenta', variante: 'secundario', accion: 'crear-cuenta' })}
          </div>
        </div>
      </section>`;
  }
}

/** Pie de página. */
class Pie extends Componente {
  plantilla() {
    const { lema, columnas } = textos.pie;
    return html`
      <footer class="pie">
        <div class="pie__contenido contenedor">
          <div class="pie__marca">
            <span class="pie__logo t-h2"><img src="assets/iconos/punto-sol-24.svg" alt="" width="24" height="24">SuraChih</span>
            <p class="t-small">${lema}</p>
          </div>
          ${columnas.map((col) => html`
            <nav class="pie__columna t-small" aria-label="${col.titulo}">
              <p class="t-small-fuerte">${col.titulo}</p>
              ${col.enlaces.map((e) => e.accion
                ? html`<button type="button" class="pie__enlace" data-accion="${e.accion}">${e.texto}</button>`
                : html`<p>${e.texto}</p>`)}
            </nav>`)}
        </div>
      </footer>`;
  }
}

definir('sc-cinta-categorias', CintaCategorias);
definir('sc-como-funciona', ComoFunciona);
definir('sc-encuesta', Encuesta);
definir('sc-banda-organizadores', BandaOrganizadores);
definir('sc-llamado-final', LlamadoFinal);
definir('sc-pie', Pie);
