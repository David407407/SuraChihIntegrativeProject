/**
 * @file <sc-hero>: portada azul con mensaje, acciones, cifras y collage.
 *
 * datos: { resumen: ResumenPlataforma, collage: [...] }
 * Al llegar los datos solo se actualizan las cifras (con conteo animado) y el collage;
 * el resto no se repinta para no reiniciar animaciones.
 */

import { Componente, definir, html } from '../../nucleo/componente.js';
import { boton } from '../atomos.js';
import { reducirMovimiento } from '../../utilidades/animaciones.js';
import { hero as textos } from '../../contenido/landing.js';
import './sc-collage-hero.js';

const CIFRAS = [
  { clave: 'eventosEstaSemana', texto: 'eventos esta semana' },
  { clave: 'lugaresParaDescubrir', texto: 'lugares para descubrir' },
  { clave: 'categorias', texto: 'categorías' },
];

class Hero extends Componente {
  plantilla() {
    return html`
      <section class="hero" id="inicio" aria-labelledby="hero-titulo">
        <img class="hero__brillo" src="assets/iconos/hero-brillo.svg" alt="" aria-hidden="true">
        <img class="hero__sol" src="assets/iconos/hero-sol.svg" alt="" aria-hidden="true">
        <div class="hero__contenido contenedor">
          <div class="hero__mensaje">
            <span class="hero__sello t-small-fuerte" data-revelar>
              <img src="assets/iconos/punto-sol-10.svg" alt="" width="10" height="10">${textos.sello}
            </span>
            <h1 id="hero-titulo" class="hero__titulo" data-revelar style="--orden:1">${textos.titulo}</h1>
            <p class="hero__bajada t-lead" data-revelar style="--orden:2">${textos.bajada}</p>
            <div class="hero__acciones" data-revelar style="--orden:3">
              ${boton({ texto: 'Explorar eventos', icono: 'buscar-tinta', accion: 'explorar-eventos' })}
              ${boton({ texto: 'Crear cuenta gratis', variante: 'contorno-claro', accion: 'crear-cuenta' })}
            </div>
            <dl class="hero__cifras" data-revelar style="--orden:4">
              ${CIFRAS.map((c) => html`
                <div class="hero__cifra">
                  <dt class="t-small">${c.texto}</dt>
                  <dd class="t-h2" data-cifra="${c.clave}">–</dd>
                </div>`)}
            </dl>
          </div>
          <sc-collage-hero class="hero__collage"></sc-collage-hero>
        </div>
      </section>`;
  }

  actualizar(datos) {
    if (!datos) return;
    this.querySelector('sc-collage-hero').datos = datos.collage ?? [];
    for (const { clave } of CIFRAS) {
      const destino = this.querySelector(`[data-cifra="${clave}"]`);
      contar(destino, datos.resumen?.[clave] ?? 0);
    }
  }
}

/** Anima un número de 0 a `valor` (≈0.9 s, desacelerando). */
function contar(elemento, valor) {
  if (reducirMovimiento() || valor === 0) {
    elemento.textContent = String(valor);
    return;
  }
  const duracion = 900;
  const inicio = performance.now();
  const paso = (ahora) => {
    const t = Math.min(1, (ahora - inicio) / duracion);
    elemento.textContent = String(Math.round(valor * (1 - Math.pow(1 - t, 3))));
    if (t < 1) requestAnimationFrame(paso);
  };
  requestAnimationFrame(paso);
}

definir('sc-hero', Hero);
