/**
 * @file Punto de entrada de la pantalla Mapa (mapa.html): lista de lugares a la izquierda y
 * <sc-mapa> a la derecha, sincronizados.
 *
 * 1. Pide los lugares a C# ('mapa.obtener') o a los datos demo en un navegador.
 * 2. El buscador y las píldoras de tipo deciden qué lugares van al mapa.
 * 3. La lista muestra solo los que se ven en el mapa ("N lugares en esta zona"): al moverlo o
 *    acercarlo, <sc-mapa> avisa con `sc:mapa-zona` y la lista se vuelve a pintar.
 * 4. Elegir una fila resalta su pin; elegir un pin resalta su fila.
 */

import './componentes/sc-imagen.js';
import './componentes/sc-mapa.js';
import './componentes/secciones/sc-navbar.js';

import { html } from './nucleo/componente.js';
import { invocar, enApp } from './nucleo/puente.js';
import { atenderAcciones, cargarSesion } from './nucleo/navegacion.js';
import { campo, pildora } from './componentes/atomos.js';
import { itemLugar, esqueleto } from './componentes/tarjetas.js';
import { mostrarToast } from './componentes/sc-toast.js';
import { puntoDeLugar, tipoLugar, tiposPresentes } from './utilidades/lugares.js';

const ESPERA_BUSCADOR_MS = 200;

const mapa = document.getElementById('mapa');
const panel = document.getElementById('panel');

panel.innerHTML = String(html`
  <form class="panel-mapa__buscador" role="search">
    ${campo({ nombre: 'buscar', etiqueta: 'Buscar en el mapa', tipo: 'search', icono: 'buscar-gris', autocompletar: 'off' })}
  </form>
  <div class="panel-mapa__segmentos" role="group" aria-label="Tipo de lugar"></div>
  <p class="panel-mapa__conteo t-small-fuerte texto-2" aria-live="polite">Buscando lugares…</p>
  <ul class="panel-mapa__lista">${[0, 1, 2, 3].map(() => html`<li>${esqueleto('item-lugar-esqueleto')}</li>`)}</ul>`);

const buscador = panel.querySelector('form');
const segmentos = panel.querySelector('.panel-mapa__segmentos');
const conteo = panel.querySelector('.panel-mapa__conteo');
const lista = panel.querySelector('.panel-mapa__lista');

/** Todos los lugares aprobados (LugarTarjeta[]), tal como llegan de C#. */
let lugares = [];
const filtro = { texto: '', tipo: '' };
let seleccionado = null;      // id del lugar elegido
let pintados = null;          // lo que ya muestra la lista, para no repintar (y recargar fotos) sin necesidad

// ---------------------------------------------------------------------
// Filtros: buscador + píldoras de tipo
// ---------------------------------------------------------------------

/** Minúsculas y sin acentos, para que "cafe" encuentre "Café". */
const normalizar = (texto) => String(texto ?? '').normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();

function coincide(lugar) {
  if (filtro.tipo && tipoLugar(lugar) !== filtro.tipo) return false;
  if (!filtro.texto) return true;
  const donde = [lugar.nombre, lugar.direccion, ...(lugar.etiquetas ?? []).map((t) => t.nombre)];
  return donde.some((texto) => normalizar(texto).includes(filtro.texto));
}

const filtrados = () => lugares.filter(coincide);

/** Manda al mapa los lugares que pasan el filtro y lo encuadra para verlos todos. */
function aplicarFiltro() {
  const visibles = filtrados();
  if (!visibles.some((lugar) => lugar.id === seleccionado)) seleccionado = null;
  mapa.mostrar(visibles.map(puntoDeLugar), { ajustar: true });
}

function pintarSegmentos() {
  const opciones = [{ valor: '', texto: 'Todo' }, ...tiposPresentes(lugares)];
  segmentos.innerHTML = String(html`${opciones.map((o) => pildora({ ...o, activa: o.valor === filtro.tipo }))}`);
}

segmentos.addEventListener('click', (e) => {
  const elegida = e.target.closest('.pildora');
  if (!elegida || elegida.dataset.valor === filtro.tipo) return;
  filtro.tipo = elegida.dataset.valor;
  pintarSegmentos();
  aplicarFiltro();
});

let espera;
buscador.addEventListener('input', () => {
  clearTimeout(espera);
  espera = setTimeout(() => {
    filtro.texto = normalizar(buscador.elements.buscar.value.trim());
    aplicarFiltro();
  }, ESPERA_BUSCADOR_MS);
});
buscador.addEventListener('submit', (e) => e.preventDefault());

// ---------------------------------------------------------------------
// Lista: los lugares que se ven en el mapa
// ---------------------------------------------------------------------

function pintarLista(visibles) {
  const ids = new Set(visibles.map((punto) => punto.id));
  const enZona = filtrados().filter((lugar) => ids.has(lugar.id));

  const firma = enZona.map((lugar) => lugar.id).join(',') || mensajeVacio();
  if (firma === pintados) return;
  pintados = firma;

  conteo.textContent = enZona.length === 0 ? 'Sin lugares en esta zona'
    : `${enZona.length} ${enZona.length === 1 ? 'lugar' : 'lugares'} en esta zona`;
  lista.innerHTML = String(enZona.length > 0
    ? html`${enZona.map((lugar) => itemLugar(lugar, { activo: lugar.id === seleccionado }))}`
    : html`<li class="panel-mapa__vacio t-small texto-2">${mensajeVacio()}</li>`);
}

function mensajeVacio() {
  if (lugares.length === 0) return 'Todavía no hay lugares publicados.';
  if (filtrados().length === 0) return 'Ningún lugar coincide con tu búsqueda.';
  return 'No hay lugares en esta parte del mapa. Aléjalo o muévelo para ver más.';
}

/**
 * @param {number|null} id Id del lugar.
 * @param {{mover?:boolean, mostrarFila?:boolean}} opciones `mover`: el mapa se desplaza lo justo para
 *   que el pin se vea bien. `mostrarFila`: la lista se desplaza hasta su fila.
 */
function seleccionar(id, { mover = false, mostrarFila = false } = {}) {
  seleccionado = id;
  mapa.seleccionar(id == null ? null : { id, tipo: 'lugar' }, { mover });
  for (const fila of lista.querySelectorAll('.item-lugar')) {
    const activa = Number(fila.dataset.lugar) === id;
    fila.setAttribute('aria-current', String(activa));
    if (activa && mostrarFila) fila.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
  }
}

lista.addEventListener('click', (e) => {
  const fila = e.target.closest('.item-lugar');
  if (fila) seleccionar(Number(fila.dataset.lugar), { mover: true });
});

mapa.addEventListener('sc:mapa-pin', ({ detail }) => seleccionar(detail.punto.id, { mostrarFila: true }));
mapa.addEventListener('sc:mapa-zona', ({ detail }) => pintarLista(detail.visibles));

// ---------------------------------------------------------------------
// Inicio
// ---------------------------------------------------------------------

async function cargarMapa() {
  try {
    lugares = (await invocar('mapa.obtener')).lugares ?? [];
  } catch (error) {
    console.error(error);
    mostrarToast(error.message || 'No se pudieron cargar los lugares.', { tipo: 'error' });
  }
  pintarSegmentos();
  aplicarFiltro();
  if (lugares.length === 0) pintarLista([]);   // sin pines el mapa no se mueve y no avisaría
}

document.documentElement.dataset.entorno = enApp ? 'app' : 'navegador';
atenderAcciones();
cargarSesion(document.querySelector('sc-navbar'));
cargarMapa();
