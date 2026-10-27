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
// Ventanas (FluentDialog): la modal y la hoja de detalle
// ---------------------------------------------------------------------

/**
 * Esc dentro de un FluentDialog (con PreventDismissOnEscape): avisa a C# para cerrar con animación.
 * @param {string} id Id del <fluent-dialog>.
 * @param {any} puente DotNetObjectReference con el método `PedirCierre`.
 */
export function escucharEsc(id, puente) {
  const dialogo = document.getElementById(id);
  if (!dialogo) return;
  dialogo.puente = puente;
  if (dialogo.dataset.enlazado) return;
  dialogo.dataset.enlazado = 'si';
  // En captura: FluentDialog atiende Esc dentro de su shadow DOM y no deja que el evento suba.
  dialogo.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') dialogo.puente?.invokeMethodAsync('PedirCierre');
  }, true);
}

/** Regresa al principio un FluentDialog que se desplaza por dentro (la hoja de detalle). */
export function subirDialogo(id) {
  document.getElementById(id)?.shadowRoot?.querySelector('[part="dialog"]')?.scrollTo({ top: 0 });
}

/** Pone o quita una clase de <html> (p. ej. 'con-modal' bloquea el scroll de atrás mientras hay un FluentDialog). */
export function claseHtml(clase, activa) {
  document.documentElement.classList.toggle(clase, activa);
}

/** Foco al primer campo (o al primer botón si no hay campos), como un formulario normal. */
export function enfocarPrimero(contenedor) {
  // Un cuadro después: FluentDialog pone el foco en sí mismo al abrirse, y esto debe ir después.
  requestAnimationFrame(() => {
    if (!contenedor || contenedor.contains(document.activeElement)) return;   // un campo ya pidió el foco
    // Los campos y botones de Fluent (fluent-text-input, fluent-button) pasan el foco a su control interno.
    const primero = contenedor.querySelector('[autofocus], input:not([type=hidden]):not([disabled]), fluent-text-input:not([disabled])')
      ?? contenedor.querySelector('button:not([data-cerrar-modal]), fluent-button:not([data-cerrar-modal])');
    primero?.focus({ preventScroll: true });
  });
}

/** Hoja compartida por todos los campos de contraseña (ver quitarOjoDelNavegador). */
let sinOjo;

/**
 * Quita el ojo que Edge (y WebView2, la app de escritorio) pone en los campos de contraseña: ya está
 * nuestro botón "Mostrar", y el de Edge aparece y desaparece según se escriba o se pierda el foco.
 * El <input> vive dentro del shadow DOM de fluent-text-input, así que el CSS global no lo alcanza.
 */
export async function quitarOjoDelNavegador(campo) {
  if (!campo) return;
  await customElements.whenDefined(campo.localName);
  const raiz = campo.shadowRoot;
  if (!raiz) return;
  if (!sinOjo) {
    sinOjo = new CSSStyleSheet();
    sinOjo.replaceSync('input::-ms-reveal, input::-ms-clear { display: none !important; }');
  }
  if (!raiz.adoptedStyleSheets.includes(sinOjo)) raiz.adoptedStyleSheets = [...raiz.adoptedStyleSheets, sinOjo];
}

// ---------------------------------------------------------------------
// Dirección de la hoja de detalle. Se usa history directo (y no NavigationManager) porque una
// navegación de Blazor sube la página hasta arriba, y la pantalla de atrás debe quedarse donde estaba.
// Atrás / Adelante sí pasan por Blazor: la hoja escucha LocationChanged.
// ---------------------------------------------------------------------

/** Pone #evento-3 en la dirección. `reemplazar`: de una recomendación a otra, un solo Atrás. */
export function ponerDireccion(hash, reemplazar) {
  if (location.hash === hash) return;
  // Con la ruta completa: con <base href="/"> un "#evento-3" suelto apuntaría a la raíz y no a esta pantalla.
  const destino = location.pathname + location.search + hash;
  if (reemplazar) history.replaceState(history.state, '', destino);
  else history.pushState(history.state, '', destino);
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
