/**
 * @file <sc-mapa>: mapa reutilizable (Leaflet + teselas de OpenStreetMap).
 *
 * Leaflet viene empaquetado en /vendor/leaflet (no se descarga de internet); solo las teselas
 * (las imágenes del mapa) se piden a OpenStreetMap, por eso siempre se muestra su atribución.
 * Estilos en css/mapa.css.
 *
 * Desde Blazor NO se usa directamente: se usa el componente Componentes/Mapa/Mapa.razor, que pinta
 * esta etiqueta y la conecta con C# a través de interop.js (mapaConectar, mapaMostrar, mapaSeleccionar).
 *
 * TRES FORMAS DE USARLO
 *
 * 1. Explorar (pantalla Mapa): muchos pines que se pueden elegir.
 *      <Mapa Puntos="puntos" AlElegirPin="..." AlCambiarZona="..." />
 *
 * 2. Detalle (detalle de evento o de lugar): un solo pin fijo.
 *      <Mapa Lat="evento.Latitud" Lng="evento.Longitud" Titulo="@evento.LugarNombre" />
 *
 * 3. Elegir ubicación ("Publicar evento"): un pin que se arrastra o se coloca con un clic.
 *      <Mapa Arrastrable Lat="datos.Latitud" Lng="datos.Longitud" AlMoverUbicacion="..." />
 *
 * Atributos:
 *   lat, lng     Coordenadas del pin único (formas 2 y 3). Vacíos = sin pin.
 *   titulo       Texto del pin único (aparece al pasar el mouse).
 *   icono        Icono del pin único: archivo de assets/iconos sin extensión (por defecto 'ubicacion').
 *   zoom         Zoom con un solo pin (por defecto 16).
 *   arrastrable  El pin se puede mover; un clic en el mapa lo coloca ahí.
 *
 * Propiedades y métodos:
 *   puntos                       Arreglo de { id, tipo?, lat, lng, titulo?, icono? }. `tipo` distingue
 *                                un evento de un lugar con el mismo id. Sin lat/lng se ignoran.
 *   mostrar(puntos, { ajustar }) Igual que `puntos`, y con `ajustar` encuadra el mapa para verlos todos.
 *   seleccionar(punto, opciones) Marca un pin como activo (null = ninguno). Con `mover` desplaza el mapa
 *                                lo justo para que se vea; con `acercar` vuela hasta él con zoom.
 *   ajustar()                    Encuadra todos los pines.
 *   visibles()                   Puntos dentro de lo que se ve ahora.
 *   ubicacion                    { lat, lng } del pin arrastrable, o null si aún no se coloca.
 *
 * Eventos:
 *   sc:mapa-pin        detail { punto }       clic (o Enter) en un pin.
 *   sc:mapa-deseleccion                      clic en el mapa fuera de los pines con un pin activo
 *                                            (el pin ya se desmarcó solo).
 *   sc:mapa-zona       detail { visibles }    cambió lo que se ve (al mover, acercar o cambiar los puntos).
 *   sc:mapa-ubicacion  detail { lat, lng }    se movió el pin arrastrable. Ya viene con 6 decimales y
 *                                             dentro del estado de Chihuahua, como lo pide la BD.
 */

import * as L from '../vendor/leaflet/leaflet-src.esm.js';
import { CARTO_KEY } from './claves.js';

const ENTIDADES = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };
/** Escapa texto para insertarlo en HTML (los títulos de los pines vienen de la BD). */
const escapar = (valor) => String(valor).replace(/[&<>"']/g, (c) => ENTIDADES[c]);
/** Iconos de assets/iconos: la ruta sale de este archivo (en la app se sirve bajo _content/Interfaz/). */
const rutaIcono = (nombre) => new URL(`../assets/iconos/${nombre}.svg`, import.meta.url).href;

/** Caja del estado de Chihuahua: los mismos límites que los CHECK de place y event en la BD. */
export const LIMITES_CHIHUAHUA = Object.freeze({ sur: 25.5, norte: 31.8, oeste: -109.2, este: -103.3 });

const CENTRO_CHIHUAHUA = [28.6353, -106.0889];   // ciudad de Chihuahua
const ZOOM = { inicial: 13, unPin: 16, alVolar: 15, minimo: 6, maximo: 19 };
const OSM = '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a> contributors';

// Con llave de CARTO (contenido/claves.js, compartida por el equipo) se usa su estilo Voyager,
// hecho con datos de OpenStreetMap y con teselas @2x para pantallas con escala (se ve nítido).
// Sin llave se usan las teselas estándar de OpenStreetMap.
const NITIDAS = window.devicePixelRatio > 1 ? '@2x' : '';   // p. ej. Windows con escala al 125 % o 150 %
const TESELAS = CARTO_KEY
  ? `https://basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}${NITIDAS}.png?key=${encodeURIComponent(CARTO_KEY)}`
  : 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
const ATRIBUCION = CARTO_KEY
  ? `${OSM} &copy; <a href="https://carto.com/attributions" target="_blank" rel="noopener">CARTO</a>`
  : OSM;
const ID_UNICO = 'unico';

const clave = (punto) => `${punto.tipo ?? 'punto'}:${punto.id}`;
const limitar = (valor, minimo, maximo) => Math.min(Math.max(valor, minimo), maximo);
/** La BD guarda DECIMAL(9,6): seis decimales. */
const redondear = (valor) => Math.round(valor * 1e6) / 1e6;
const esCoordenada = (valor) => valor !== null && valor !== '' && Number.isFinite(Number(valor));
/** Sin animaciones si el usuario las redujo o si la ventana no se ve (ahí no avanzan y el mapa no avisaría). */
const sinMovimiento = () => document.hidden || window.matchMedia('(prefers-reduced-motion: reduce)').matches;

class Mapa extends HTMLElement {
  static observedAttributes = ['lat', 'lng', 'titulo', 'icono', 'arrastrable'];

  #mapa = null;
  #capa = null;
  #marcadores = new Map();
  #puntos = [];
  #activo = null;
  #porPropiedad = false;   // true cuando los puntos se dieron con `puntos` y no con atributos
  #encuadrado = false;     // el primer grupo de puntos encuadra el mapa solo
  #observador = null;

  // ------------------------------------------------------------------
  // API pública
  // ------------------------------------------------------------------

  get puntos() { return this.#puntos; }
  set puntos(lista) { this.mostrar(lista); }

  get arrastrable() { return this.hasAttribute('arrastrable'); }

  get ubicacion() {
    const punto = this.arrastrable ? this.#puntos[0] : null;
    return punto ? { lat: punto.lat, lng: punto.lng } : null;
  }

  /**
   * @param {Array<{id:number|string, tipo?:string, lat:number, lng:number, titulo?:string, icono?:string}>} lista
   * @param {{ajustar?:boolean}} [opciones]
   */
  mostrar(lista, { ajustar = false } = {}) {
    this.#porPropiedad = true;
    this.#puntos = (lista ?? [])
      .filter((p) => esCoordenada(p.lat) && esCoordenada(p.lng))
      .map((p) => ({ ...p, lat: Number(p.lat), lng: Number(p.lng) }));
    if (this.#mapa) this.#pintarPuntos(ajustar);
  }

  /**
   * @param {{id:number|string, tipo?:string}|null} punto
   * @param {{mover?:boolean, acercar?:boolean}} [opciones]
   */
  seleccionar(punto, { mover = false, acercar = false } = {}) {
    const nueva = punto ? clave(punto) : null;
    if (nueva !== this.#activo) {
      this.#marcarActivo(this.#activo, false);
      this.#activo = nueva;
      this.#marcarActivo(nueva, true);
    }
    const marcador = this.#marcadores.get(nueva);
    if (!marcador) return;
    const animate = !sinMovimiento();
    if (acercar) {
      const zoom = Math.max(this.#mapa.getZoom(), ZOOM.alVolar);
      if (animate) this.#mapa.flyTo(marcador.getLatLng(), zoom, { duration: 0.6 });
      else this.#mapa.setView(marcador.getLatLng(), zoom, { animate });
    } else if (mover) {
      this.#mapa.panInside(marcador.getLatLng(), { padding: [80, 80], animate });
    }
  }

  /** @param {{animar?:boolean}} [opciones] */
  ajustar({ animar = true } = {}) {
    if (!this.#mapa || !this.#tieneTamano() || this.#puntos.length === 0) return;
    const animate = animar && !sinMovimiento();
    if (this.#puntos.length === 1) {
      const [{ lat, lng }] = this.#puntos;
      this.#mapa.setView([lat, lng], Number(this.getAttribute('zoom')) || ZOOM.unPin, { animate });
    } else {
      const caja = L.latLngBounds(this.#puntos.map((p) => [p.lat, p.lng]));
      this.#mapa.fitBounds(caja, { padding: [56, 56], maxZoom: ZOOM.alVolar, animate });
    }
  }

  visibles() {
    if (!this.#mapa || !this.#tieneTamano()) return [];
    const caja = this.#mapa.getBounds();
    return this.#puntos.filter((p) => caja.contains([p.lat, p.lng]));
  }

  // ------------------------------------------------------------------
  // Ciclo de vida
  // ------------------------------------------------------------------

  connectedCallback() {
    if (!this.#mapa) this.#crear();
    this.#observador = new ResizeObserver(() => this.#alCambiarTamano());
    this.#observador.observe(this);
  }

  disconnectedCallback() {
    this.#observador?.disconnect();
  }

  attributeChangedCallback() {
    if (!this.#mapa || this.#porPropiedad) return;
    this.#puntos = this.#puntosDeAtributos();
    this.#pintarPuntos(true);
  }

  #crear() {
    this.innerHTML = `
      <div class="mapa__lienzo"></div>
      <div class="mapa__zoom" role="group" aria-label="Zoom del mapa">
        <button type="button" class="mapa__zoom-boton" data-zoom="1" aria-label="Acercar">+</button>
        <button type="button" class="mapa__zoom-boton" data-zoom="-1" aria-label="Alejar">–</button>
      </div>`;

    const { sur, norte, oeste, este } = LIMITES_CHIHUAHUA;
    this.#mapa = L.map(this.querySelector('.mapa__lienzo'), {
      center: CENTRO_CHIHUAHUA,
      zoom: ZOOM.inicial,
      minZoom: ZOOM.minimo,
      maxZoom: ZOOM.maximo,
      zoomControl: false,   // se usan los botones del diseño
      maxBounds: L.latLngBounds([sur, oeste], [norte, este]).pad(0.1),
      maxBoundsViscosity: 0.8,
    });
    this.#mapa.attributionControl.setPrefix(false);
    L.tileLayer(TESELAS, { maxZoom: ZOOM.maximo, attribution: ATRIBUCION }).addTo(this.#mapa);
    this.#capa = L.layerGroup().addTo(this.#mapa);

    this.#mapa.on('moveend', () => this.#avisarZona());
    this.#mapa.on('zoomend', () => this.#actualizarZoom());
    // Clic en el mapa (fuera de los pines; arrastrar el mapa no cuenta como clic): con pin
    // arrastrable lo coloca ahí; si no, quita la selección.
    this.#mapa.on('click', (e) => {
      if (this.arrastrable) {
        this.#fijarUbicacion(e.latlng);
      } else if (this.#activo) {
        this.seleccionar(null);
        this.#emitir('sc:mapa-deseleccion', {});
      }
    });
    this.querySelector('.mapa__zoom').addEventListener('click', (e) => {
      const paso = Number(e.target.closest('[data-zoom]')?.dataset.zoom);
      if (paso) this.#mapa.setZoom(this.#mapa.getZoom() + paso);
    });

    if (!this.#porPropiedad) this.#puntos = this.#puntosDeAtributos();
    this.#actualizarZoom();
    this.#pintarPuntos(false);
  }

  /** Leaflet necesita saber cuándo cambia el tamaño de su caja (la ventana, un panel que se abre...). */
  #alCambiarTamano() {
    if (!this.#tieneTamano()) return;
    this.#mapa.invalidateSize();
    // Si los puntos llegaron mientras el mapa estaba oculto (sin tamaño), se encuadran ahora.
    if (!this.#encuadrado && this.#puntos.length > 0) {
      this.#encuadrado = true;
      this.ajustar({ animar: false });
    }
  }

  // ------------------------------------------------------------------
  // Pines
  // ------------------------------------------------------------------

  #puntosDeAtributos() {
    const lat = this.getAttribute('lat');
    const lng = this.getAttribute('lng');
    if (!esCoordenada(lat) || !esCoordenada(lng)) return [];
    return [{
      id: ID_UNICO,
      lat: Number(lat),
      lng: Number(lng),
      titulo: this.getAttribute('titulo') ?? '',
      icono: this.getAttribute('icono') ?? undefined,
    }];
  }

  #pintarPuntos(ajustar) {
    this.#capa.clearLayers();
    this.#marcadores.clear();
    if (!this.#puntos.some((p) => clave(p) === this.#activo)) this.#activo = null;

    for (const punto of this.#puntos) {
      const marcador = L.marker([punto.lat, punto.lng], {
        icon: this.#icono(punto),
        draggable: this.arrastrable,
        autoPan: this.arrastrable,
        riseOnHover: true,
        zIndexOffset: clave(punto) === this.#activo ? 1000 : 0,
      });
      marcador.on('click', () => {
        if (!this.arrastrable) this.seleccionar(punto);
        this.#emitir('sc:mapa-pin', { punto });
      });
      marcador.on('dragend', () => this.#fijarUbicacion(marcador.getLatLng()));
      // Leaflet pinta el texto del tooltip como HTML: se escapa porque el título viene de la BD.
      if (punto.titulo) marcador.bindTooltip(escapar(punto.titulo), { direction: 'top', offset: [0, -22], className: 'mapa__etiqueta' });
      this.#capa.addLayer(marcador);
      marcador.getElement()?.setAttribute('aria-label', punto.titulo || 'Ubicación');
      this.#marcadores.set(clave(punto), marcador);
    }

    const encuadrar = this.#puntos.length > 0 && this.#tieneTamano() && (ajustar || !this.#encuadrado);
    if (encuadrar) {
      const primeraVez = !this.#encuadrado;
      this.#encuadrado = true;
      this.ajustar({ animar: ajustar && !primeraVez });   // al terminar de moverse avisa la zona
    } else {
      this.#avisarZona();
    }
  }

  #icono(punto) {
    const clases = `pin${clave(punto) === this.#activo ? ' pin--activo' : ''}${this.arrastrable ? ' pin--arrastrable' : ''}`;
    return L.divIcon({
      className: 'mapa__pin',
      iconSize: [40, 40],
      iconAnchor: [20, 20],
      html: `<span class="${clases}"><img src="${escapar(rutaIcono(punto.icono ?? 'ubicacion'))}" alt=""></span>`,
    });
  }

  #marcarActivo(claveDelPunto, activo) {
    const marcador = this.#marcadores.get(claveDelPunto);
    if (!marcador) return;
    marcador.getElement()?.querySelector('.pin')?.classList.toggle('pin--activo', activo);
    marcador.setZIndexOffset(activo ? 1000 : 0);
  }

  // ------------------------------------------------------------------
  // Pin arrastrable
  // ------------------------------------------------------------------

  #fijarUbicacion(posicion) {
    const lat = redondear(limitar(posicion.lat, LIMITES_CHIHUAHUA.sur, LIMITES_CHIHUAHUA.norte));
    const lng = redondear(limitar(posicion.lng, LIMITES_CHIHUAHUA.oeste, LIMITES_CHIHUAHUA.este));
    const anterior = this.#puntos[0];
    const punto = { id: ID_UNICO, titulo: this.getAttribute('titulo') ?? '', ...anterior, lat, lng };
    this.#puntos = [punto];

    const marcador = anterior && this.#marcadores.get(clave(anterior));
    if (marcador) {
      marcador.setLatLng([lat, lng]);
    } else {
      this.#encuadrado = true;   // el primer clic coloca el pin sin mover el mapa
      this.#pintarPuntos(false);
    }
    this.#emitir('sc:mapa-ubicacion', { lat, lng });
  }

  // ------------------------------------------------------------------
  // Apoyo
  // ------------------------------------------------------------------

  /** false mientras el mapa está oculto (display: none): Leaflet no puede encuadrar sin tamaño. */
  #tieneTamano() {
    return this.clientWidth > 0 && this.clientHeight > 0;
  }

  #actualizarZoom() {
    const zoom = this.#mapa.getZoom();
    this.querySelector('[data-zoom="1"]').disabled = zoom >= this.#mapa.getMaxZoom();
    this.querySelector('[data-zoom="-1"]').disabled = zoom <= this.#mapa.getMinZoom();
  }

  #avisarZona() {
    if (!this.#tieneTamano()) return;   // oculto: no hay "zona" que avisar
    this.#emitir('sc:mapa-zona', { visibles: this.visibles() });
  }

  #emitir(nombre, detalle) {
    this.dispatchEvent(new CustomEvent(nombre, { detail: detalle, bubbles: true, composed: true }));
  }
}

if (!customElements.get('sc-mapa')) customElements.define('sc-mapa', Mapa);
