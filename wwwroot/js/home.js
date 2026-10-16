/**
 * @file Punto de entrada del Home (home.html).
 *
 * 1. Pide todas las secciones a C# en una sola llamada ('home.obtener') o a los datos demo.
 * 2. Reparte los datos a cada componente <sc-...> y los corazones de favoritos.
 * 3. Los filtros rápidos (Todo / Eventos / Lugares, Hoy, Este fin de semana, Gratis) filtran aquí,
 *    sobre lo que ya llegó, sin volver a consultar: ocultan secciones o quitan tarjetas.
 * 4. Al iniciar o cerrar sesión se vuelve a pedir: con sesión, "Te podrían interesar" pone primero
 *    los favoritos del usuario.
 */

import './componentes/sc-imagen.js';
import './componentes/sc-boton-favorito.js';
import './componentes/secciones/sc-navbar.js';
import './componentes/secciones/sc-carrusel.js';
import './componentes/secciones/sc-secciones-home.js';
import './componentes/secciones/sc-secciones-fijas.js';   // <sc-pie>

import { invocar, enApp } from './nucleo/puente.js';
import { atenderAcciones, cargarSesion, proximamente } from './nucleo/navegacion.js';
import { mostrarToast } from './componentes/sc-toast.js';
import { aFecha } from './utilidades/formato.js';

const $ = (selector) => document.querySelector(selector);
const secciones = {
  navbar: $('sc-navbar'),
  saludo: $('sc-saludo'),
  carrusel: $('sc-carrusel'),
  recomendados: $('sc-recomendados'),
  categorias: $('sc-categorias'),
};

/** Cada fila del Home: de qué propiedad de la respuesta sale y si es de eventos o de lugares. */
const FILAS = [
  { elemento: $('#fila-te-podrian'), datos: 'tePodrianInteresar', tipo: 'evento' },
  { elemento: $('#fila-musica'), datos: 'musica', tipo: 'evento' },
  { elemento: $('#fila-actividades'), datos: 'actividades', tipo: 'evento' },
  { elemento: $('#fila-cafes'), datos: 'cafes', tipo: 'lugar' },
  { elemento: $('#fila-tarde'), datos: 'paraLaTarde', tipo: 'lugar' },
];

/** Última respuesta de 'home.obtener' (null mientras carga). */
let home = null;
let filtro = { tipo: 'todo', cuando: null, gratis: false };

// ---------------------------------------------------------------------
// Filtros rápidos (en el navegador, sobre los datos ya cargados)
// ---------------------------------------------------------------------

/** [desde, hasta) del filtro "cuándo", igual que FiltroEventos.RangoFechas de C#. */
function rangoCuando(cuando, ahora = new Date()) {
  const hoy = new Date(ahora.getFullYear(), ahora.getMonth(), ahora.getDate());
  const dias = (n) => new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate() + n);
  if (cuando === 'hoy') return [hoy, dias(1)];
  if (cuando === 'fin-de-semana') {
    const dia = hoy.getDay();   // 0 = domingo, 6 = sábado
    if (dia === 6) return [hoy, dias(2)];
    if (dia === 0) return [hoy, dias(1)];
    return [dias(6 - dia), dias(8 - dia)];
  }
  return null;
}

const esGratis = (item, tipo) => (tipo === 'evento'
  ? !item.precioPorConfirmar && Number(item.precioMax) === 0
  : item.precioMax != null && Number(item.precioMax) === 0);

/** Un evento entra si se cruza con el rango (los de varios días cuentan aunque hayan empezado antes). */
function enRango(evento, [desde, hasta]) {
  if (evento.fechaPorConfirmar) return false;
  return aFecha(evento.inicio) < hasta && aFecha(evento.fin ?? evento.inicio) >= desde;
}

function pasaFiltro(item, tipo) {
  if (filtro.gratis && !esGratis(item, tipo)) return false;
  const rango = rangoCuando(filtro.cuando);
  // "Hoy" y "Este fin de semana" son de eventos: los lugares no se filtran por fecha.
  return !(rango && tipo === 'evento' && !enRango(item, rango));
}

/** Qué tipo de tarjeta deja ver cada opción de "Todo / Eventos / Lugares". */
const TIPO_DE_SEGMENTO = { eventos: 'evento', lugares: 'lugar' };
const tipoVisible = (tipo) => filtro.tipo === 'todo' || TIPO_DE_SEGMENTO[filtro.tipo] === tipo;

function pintarConFiltro() {
  if (!home) return;
  secciones.recomendados.datos = home.recomendados
    .filter((el) => tipoVisible(el.tipo) && pasaFiltro(el.tipo === 'lugar' ? el.lugar : el.evento, el.tipo));

  for (const fila of FILAS) {
    const todos = home[fila.datos] ?? [];
    // Sin datos en la BD la sección no aparece; con filtros que la vacían, avisa.
    fila.elemento.hidden = todos.length === 0 || !tipoVisible(fila.tipo);
    fila.elemento.datos = {
      items: todos.filter((item) => pasaFiltro(item, fila.tipo)),
      favoritos: fila.tipo === 'evento' ? home.favoritosEventos : home.favoritosLugares,
    };
  }
}

document.addEventListener('sc:filtros', ({ detail }) => {
  filtro = detail;
  pintarConFiltro();
});

// ---------------------------------------------------------------------
// Datos
// ---------------------------------------------------------------------

async function cargarHome() {
  try {
    const datos = await invocar('home.obtener');
    home = {
      ...datos,
      favoritosEventos: new Set(datos.favoritosEventos ?? []),
      favoritosLugares: new Set(datos.favoritosLugares ?? []),
    };
  } catch (error) {
    console.error(error);
    mostrarToast(error.message || 'No se pudo cargar el inicio.', { tipo: 'error' });
    home = {
      carrusel: [], recomendados: [], categorias: [], tePodrianInteresar: [], musica: [],
      actividades: [], cafes: [], paraLaTarde: [], favoritosEventos: new Set(), favoritosLugares: new Set(),
    };
  }
  secciones.carrusel.datos = home.carrusel;
  secciones.categorias.datos = home.categorias;
  pintarConFiltro();
}

// Al entrar o salir de la cuenta: saludo nuevo y, si los datos se armaron para otro usuario,
// se vuelven a pedir (con sesión, favoritos primero y corazones llenos).
document.addEventListener('sc:usuario', ({ detail }) => {
  secciones.saludo.datos = { usuario: detail.usuario };
  if (home && (detail.usuario?.id ?? null) !== (home.usuarioId ?? null)) cargarHome();
});

// ---------------------------------------------------------------------
// Inicio
// ---------------------------------------------------------------------

document.documentElement.dataset.entorno = enApp ? 'app' : 'navegador';
atenderAcciones({
  'filtro-fecha': () => proximamente('El calendario'),
  'filtros': () => proximamente('Los filtros avanzados'),
});
cargarSesion(secciones.navbar);
cargarHome();
