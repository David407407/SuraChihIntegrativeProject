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
  // Con el foco fuera del diálogo (en <body>) Esc no pasa por ese keydown: el navegador lo cierra por su cuenta y
  // la app lo seguiría creyendo abierto. PedirCierre se puede repetir sin problema: si ya está cerrando, no hace nada.
  dialogo.shadowRoot?.querySelector('dialog')?.addEventListener('cancel', (e) => {
    e.preventDefault();
    dialogo.puente?.invokeMethodAsync('PedirCierre');
  });
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
// Después de cerrar una fila: el foco pasa a la que ocupó su lugar (o a la última, o a respaldo si ya no hay filas).
export function enfocarFila(selector, lugar, respaldo) {
  const filas = document.querySelectorAll(selector);
  (filas[Math.min(lugar, filas.length - 1)] ?? document.querySelector(respaldo))?.focus({ preventScroll: true });
}

// Pestaña activa de moderación: a la vista dentro de su fila y, si el foco se perdió al navegar, con el foco.
export function mostrarPestana(selector) {
  const pestana = document.querySelector(selector);
  if (!pestana) return;
  pestana.parentElement.scrollLeft = pestana.offsetLeft - pestana.parentElement.offsetLeft - 16;
  if (document.activeElement === document.body) pestana.focus({ preventScroll: true });
}

// El <dialog> real de FluentDialog está en su shadow DOM: aria-labelledby no alcanza el título, que está afuera.
export function nombrarDialogo(id, contenedor) {
  const titulo = contenedor?.querySelector('#modal-titulo')?.textContent?.trim();
  const dialogo = document.getElementById(id)?.shadowRoot?.querySelector('dialog');
  if (titulo && dialogo) dialogo.setAttribute('aria-label', titulo);
}

export function enfocarPrimero(contenedor) {
  // Un cuadro después: FluentDialog pone el foco en sí mismo al abrirse, y esto debe ir después.
  requestAnimationFrame(() => {
    if (!contenedor || contenedor.contains(document.activeElement)) return;   // un campo ya pidió el foco
    // Los campos y botones de Fluent (fluent-text-input, fluent-button) pasan el foco a su control interno.
    const primero = contenedor.querySelector('[autofocus], input:not([type=hidden]):not([disabled]), fluent-text-input:not([disabled])')
      ?? contenedor.querySelector('button:not([data-cerrar-modal]), fluent-button:not([data-cerrar-modal])')
      ?? contenedor.querySelector('button, fluent-button');   // solo tiene "Listo" o "Entendido"
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
  // Va como "lugar:3" / "evento:3" (PuntoMapa.Clave): un evento y un lugar pueden tener el mismo id.
  mapa.addEventListener('sc:mapa-zona', ({ detail }) => llamar('AlCambiarZonaJs',
    detail.visibles.filter((p) => Number.isInteger(p.id)).map((p) => `${p.tipo}:${p.id}`)));
  mapa.addEventListener('sc:mapa-ubicacion', ({ detail }) => llamar('AlMoverUbicacionJs', detail.lat, detail.lng));
}

export function mapaMostrar(mapa, puntos, ajustar) {
  mapa.mostrar(puntos, { ajustar });
}

export function mapaSeleccionar(mapa, punto, mover) {
  mapa.seleccionar(punto, { mover });
}

// ---------------------------------------------------------------------
// Compartir (ModalCompartir)
// ---------------------------------------------------------------------

/** Copia el enlace al portapapeles. Si el navegador no lo permite, prueba con execCommand. */
export async function copiarTexto(texto) {
  try {
    await navigator.clipboard.writeText(texto);
    return true;
  } catch {
    const caja = document.createElement('textarea');
    caja.value = texto;
    caja.setAttribute('readonly', '');
    caja.style.position = 'fixed';
    caja.style.opacity = '0';
    document.body.appendChild(caja);
    caja.select();
    const copiado = document.execCommand('copy');
    caja.remove();
    return copiado;
  }
}

/** Selecciona todo el texto de una caja (el enlace de Compartir). */
export function seleccionarTexto(caja) {
  caja?.select();
}

// ---------------------------------------------------------------------
// Navegación
// ---------------------------------------------------------------------

/** Regresa a la página anterior ("Volver al evento" de Reseñas). false si no hay historial. */
export function atras() {
  if (history.length <= 1) return false;
  history.back();
  return true;
}

/** true si el sistema tiene su menú Compartir (navigator.share), para mostrar "Más" en Compartir. */
export function puedeCompartir() {
  return typeof navigator.share === 'function';
}

/** Abre el menú Compartir del sistema. false si no se pudo (el usuario lo cerró no cuenta como error). */
export async function compartirSistema(titulo, texto, url) {
  try {
    await navigator.share({ title: titulo, text: texto, url });
    return true;
  } catch (error) {
    return error?.name === 'AbortError';
  }
}

// ---------------------------------------------------------------------
// Filas de tarjetas del Home (FilaTarjetas.razor)
// ---------------------------------------------------------------------

/**
 * Prepara una fila deslizable: marca su sección con data-inicio / data-fin (las flechas se apagan en las
 * orillas) y deja arrastrarla con el mouse. El touchpad y Shift + rueda ya la mueven solos. Si ya estaba
 * conectada no hace nada.
 */
export function conectarPista(pista) {
  if (!pista || pista.dataset.conectada) return;
  pista.dataset.conectada = '1';
  const fila = pista.closest('.fila') ?? pista.parentElement;

  const actualizar = () => {
    const inicio = pista.scrollLeft <= 4;
    const fin = pista.scrollLeft + pista.clientWidth >= pista.scrollWidth - 4;
    fila.toggleAttribute('data-inicio', inicio);
    fila.toggleAttribute('data-fin', fin);
  };
  pista.addEventListener('scroll', actualizar, { passive: true });
  new ResizeObserver(actualizar).observe(pista);
  actualizar();

  // Arrastrar con el mouse (en pantallas táctiles el dedo ya desliza solo).
  let inicioX = 0, inicioScroll = 0, abajo = false, arrastro = false;
  pista.addEventListener('pointerdown', (e) => {
    if (e.pointerType !== 'mouse' || e.button !== 0) return;
    abajo = true;
    arrastro = false;
    inicioX = e.clientX;
    inicioScroll = pista.scrollLeft;
  });
  pista.addEventListener('pointermove', (e) => {
    if (!abajo) return;
    const dx = e.clientX - inicioX;
    if (!arrastro && Math.abs(dx) < 6) return;   // un clic normal no es arrastre
    if (!arrastro) {
      arrastro = true;
      pista.classList.add('fila__pista--arrastrando');
      pista.setPointerCapture(e.pointerId);
    }
    pista.scrollLeft = inicioScroll - dx;
  });
  const soltar = (e) => {
    if (!abajo) return;
    abajo = false;
    if (arrastro) {
      pista.classList.remove('fila__pista--arrastrando');
      if (pista.hasPointerCapture?.(e.pointerId)) pista.releasePointerCapture(e.pointerId);
    }
  };
  pista.addEventListener('pointerup', soltar);
  pista.addEventListener('pointercancel', soltar);
  // Al soltar después de arrastrar, ese clic no abre la tarjeta.
  pista.addEventListener('click', (e) => {
    if (!arrastro) return;
    arrastro = false;
    e.preventDefault();
    e.stopPropagation();
  }, true);
}

/** Flechas ‹ ›: mueve la fila casi una pantalla (deja ver un pedazo de la última tarjeta). */
export function moverPista(pista, direccion) {
  if (!pista) return;
  const paso = Math.max(pista.clientWidth * 0.8, 300);
  pista.scrollBy({ left: direccion * paso, behavior: reducido() ? 'auto' : 'smooth' });
}

// ---------------------------------------------------------------------
// Carrusel del Home (Carrusel.razor): color de fondo según la foto
// ---------------------------------------------------------------------

/**
 * Color que más se ve en la foto, como [r, g, b] (0-255), o null si no se pudo leer (sin CORS, no cargó...).
 * Dibuja la foto en 32 × 32, agrupa los píxeles en cubetas de color y se queda con la que más pesa; los
 * colores vivos pesan más y los casi blancos, casi negros o grises casi no cuentan, para que un fondo
 * blanco o negro no gane al color del cartel. C# lo ajusta después para que el texto blanco se lea.
 */
export function colorDeImagen(url) {
  return new Promise((resolver) => {
    const img = new Image();
    img.crossOrigin = 'anonymous';
    img.onerror = () => resolver(null);
    img.onload = () => {
      try {
        const lado = 32;
        const lienzo = document.createElement('canvas');
        lienzo.width = lienzo.height = lado;
        const ctx = lienzo.getContext('2d', { willReadFrequently: true });
        ctx.drawImage(img, 0, 0, lado, lado);
        const datos = ctx.getImageData(0, 0, lado, lado).data;

        const cubetas = new Map();
        for (let i = 0; i < datos.length; i += 4) {
          if (datos[i + 3] < 128) continue;   // transparente
          const [r, g, b] = [datos[i], datos[i + 1], datos[i + 2]];
          const max = Math.max(r, g, b), min = Math.min(r, g, b);
          const saturacion = max === 0 ? 0 : (max - min) / max;
          const extremo = max < 28 || min > 232;   // casi negro o casi blanco
          // Los colores vivos pesan mucho más que los apagados, y los muy oscuros poco: en un cartel negro con
          // letras rojas gana el rojo.
          const peso = extremo ? 0.02 : (0.1 + 2 * saturacion * saturacion) * (max < 70 ? 0.3 : 1);
          const clave = (r >> 5) << 6 | (g >> 5) << 3 | (b >> 5);   // 8 niveles por canal
          const c = cubetas.get(clave) ?? { peso: 0, r: 0, g: 0, b: 0 };
          c.peso += peso; c.r += r * peso; c.g += g * peso; c.b += b * peso;
          cubetas.set(clave, c);
        }
        let mejor = null;
        for (const c of cubetas.values()) if (!mejor || c.peso > mejor.peso) mejor = c;
        resolver(mejor ? [mejor.r / mejor.peso, mejor.g / mejor.peso, mejor.b / mejor.peso].map(Math.round) : null);
      } catch {
        resolver(null);   // canvas "contaminado": la imagen no permite leer sus píxeles
      }
    };
    img.src = url;
  });
}
