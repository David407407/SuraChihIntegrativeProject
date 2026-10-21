/**
 * @file Lo poco que Blazor no puede hacer solo. Lo carga Estado/InteropJs.cs con `import()`.
 *
 * - Ventanas <dialog> (modales y hoja de detalle): abrir con showModal(), cerrar con animación,
 *   Esc y clic en el velo, y regresar el foco a donde estaba.
 * - Dirección #evento-N / #lugar-N mientras la hoja de detalle está abierta (Atrás la cierra).
 * - Desplazar la página (volver arriba, saltar a una fila del Home).
 * - Aparición de elementos al hacer scroll ([data-revelar]).
 * - El mapa: <sc-mapa> es un Web Component sobre Leaflet (sc-mapa.js); aquí se conecta con C#.
 */

import './sc-mapa.js';

const reducido = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches;

/** true si el usuario pidió reducir el movimiento en su sistema (C# detiene carruseles y conteos). */
export function reducirMovimiento() {
  return reducido();
}

// ---------------------------------------------------------------------
// Desplazamiento
// ---------------------------------------------------------------------

export function subir() {
  window.scrollTo({ top: 0, behavior: reducido() ? 'auto' : 'smooth' });
}

/** Sube sin animación (al cambiar de pantalla). */
export function subirDeInmediato() {
  window.scrollTo({ top: 0, behavior: 'instant' });
}

/** @returns {boolean} false si no hay un elemento visible con ese id. */
export function desplazarA(id) {
  const destino = document.getElementById(id);
  if (!destino || destino.offsetParent === null) return false;
  destino.scrollIntoView({ behavior: reducido() ? 'auto' : 'smooth', block: 'start' });
  return true;
}

/** Desplaza su contenedor lo justo para que se vea el elemento (fila elegida en la lista del mapa). */
export function mostrarElemento(elemento) {
  elemento?.scrollIntoView({ block: 'nearest', behavior: reducido() ? 'auto' : 'smooth' });
}

// ---------------------------------------------------------------------
// Aparición al hacer scroll: [data-revelar] aparece (fade + subida) al entrar en pantalla.
// Con style="--orden: 2" se escalona. Estilos en css/base.css. Se marca con un atributo (no con una
// clase) porque Blazor reescribe el atributo class cuando cambia, y la marca se perdería.
// ---------------------------------------------------------------------

const observador = 'IntersectionObserver' in window
  ? new IntersectionObserver((entradas) => {
      for (const entrada of entradas) {
        if (!entrada.isIntersecting) continue;
        entrada.target.setAttribute('data-revelado', '');
        observador.unobserve(entrada.target);
      }
    }, { rootMargin: '0px 0px -10% 0px', threshold: 0.12 })
  : null;

function observar(raiz) {
  const nuevos = raiz.matches?.('[data-revelar]') ? [raiz] : [];
  nuevos.push(...(raiz.querySelectorAll?.('[data-revelar]:not([data-revelado])') ?? []));
  for (const el of nuevos) {
    if (observador && !reducido()) observador.observe(el);
    else el.setAttribute('data-revelado', '');
  }
}

document.documentElement.classList.add('con-revelado');

// Blazor agrega y quita elementos todo el tiempo: se observan en cuanto aparecen.
new MutationObserver((cambios) => {
  for (const cambio of cambios) for (const nodo of cambio.addedNodes) if (nodo.nodeType === 1) observar(nodo);
}).observe(document.documentElement, { childList: true, subtree: true });
observar(document.documentElement);

// ---------------------------------------------------------------------
// Ventanas <dialog>
// ---------------------------------------------------------------------

/**
 * Abre el <dialog> (si no está abierto ya) y avisa a C# cuando el usuario pide cerrarlo.
 * @param {HTMLDialogElement} dialogo
 * @param {any} puente DotNetObjectReference con el método `PedirCierre`.
 * @param {string} claseHtml Clase de <html> que bloquea el scroll de atrás ('con-modal' o 'con-hoja').
 */
export function abrirDialogo(dialogo, puente, claseHtml) {
  dialogo.puente = puente;
  if (!dialogo.dataset.enlazado) {
    dialogo.dataset.enlazado = 'si';
    // Esc: cerrar con animación en vez del cierre instantáneo del navegador.
    dialogo.addEventListener('cancel', (e) => {
      e.preventDefault();
      dialogo.puente?.invokeMethodAsync('PedirCierre');
    });
    // Clic en el velo (fuera de la caja): el <dialog> ocupa toda la ventana y la caja va dentro.
    dialogo.addEventListener('click', (e) => {
      if (e.target === dialogo) dialogo.puente?.invokeMethodAsync('PedirCierre');
    });
  }
  dialogo.classList.remove(`${dialogo.classList[0]}--cerrando`);
  if (!dialogo.open) {
    dialogo.focoAnterior = document.activeElement;
    dialogo.showModal();
  }
  document.documentElement.classList.add(claseHtml);
  dialogo.scrollTo({ top: 0 });
}

/**
 * Cierra con la animación de salida (clase "<bloque>--cerrando") y devuelve el foco.
 * @returns {Promise<void>} Se resuelve ya cerrado, para que C# quite el contenido.
 */
export function cerrarDialogo(dialogo, claseHtml) {
  return new Promise((resolver) => {
    if (!dialogo?.open) { resolver(); return; }
    const cerrando = `${dialogo.classList[0]}--cerrando`;
    let terminado = false;
    const terminar = () => {
      if (terminado) return;
      terminado = true;
      dialogo.classList.remove(cerrando);
      dialogo.close();
      document.documentElement.classList.remove(claseHtml);
      dialogo.focoAnterior?.focus?.({ preventScroll: true });
      dialogo.focoAnterior = null;
      resolver();
    };
    if (reducido()) { terminar(); return; }
    dialogo.classList.add(cerrando);
    dialogo.firstElementChild?.addEventListener('animationend', terminar, { once: true });
    setTimeout(terminar, 400);   // por si la animación no llega a correr
  });
}

/** Foco al primer campo (o al primer botón si no hay campos), como un formulario normal. */
export function enfocarPrimero(contenedor) {
  if (!contenedor || contenedor.contains(document.activeElement)) return;   // un campo ya pidió el foco
  const primero = contenedor.querySelector('[autofocus], input:not([type=hidden]):not([disabled])')
    ?? contenedor.querySelector('button:not([data-cerrar-modal])');
  primero?.focus({ preventScroll: true });
}

// ---------------------------------------------------------------------
// Dirección de la hoja de detalle. Se usa history directo (y no NavigationManager) porque una
// navegación de Blazor sube la página hasta arriba, y la pantalla de atrás debe quedarse donde estaba.
// Atrás / Adelante sí pasan por Blazor: la hoja escucha LocationChanged.
// ---------------------------------------------------------------------

/** Pone #evento-3 en la dirección. `reemplazar`: de una recomendación a otra, un solo Atrás. */
export function ponerDireccion(hash, reemplazar) {
  if (location.hash === hash) return;
  if (reemplazar) history.replaceState(history.state, '', hash);
  else history.pushState(history.state, '', hash);
}

/** Quita el #... al cerrar la hoja: con Atrás si la hoja lo agregó; si no, reemplazándolo. */
export function quitarDireccion(conHistorial) {
  if (conHistorial) history.back();
  else if (location.hash) history.replaceState(history.state, '', location.pathname + location.search);
}

// ---------------------------------------------------------------------
// Mapa (<sc-mapa>, ver sc-mapa.js)
// ---------------------------------------------------------------------

/** Pasa a C# los eventos del mapa: elegir un pin, quitar la selección y cambiar la zona visible. */
export function mapaConectar(mapa, puente) {
  const llamar = (metodo, ...args) => puente.invokeMethodAsync(metodo, ...args).catch(() => {});   // el componente ya no existe
  mapa.addEventListener('sc:mapa-pin', ({ detail }) => {
    if (Number.isInteger(detail.punto.id)) llamar('AlElegirPinJs', detail.punto.id, detail.punto.tipo ?? '');
  });
  mapa.addEventListener('sc:mapa-deseleccion', () => llamar('AlDeseleccionarJs'));
  // El pin único de los detalles tiene id "unico" (no es un lugar): solo se mandan los ids numéricos.
  mapa.addEventListener('sc:mapa-zona', ({ detail }) => llamar('AlCambiarZonaJs', detail.visibles.map((p) => p.id).filter(Number.isInteger)));
  mapa.addEventListener('sc:mapa-ubicacion', ({ detail }) => llamar('AlMoverUbicacionJs', detail.lat, detail.lng));
}

export function mapaMostrar(mapa, puntos, ajustar) {
  mapa.mostrar(puntos, { ajustar });
}

export function mapaSeleccionar(mapa, punto, mover) {
  mapa.seleccionar(punto, { mover });
}
