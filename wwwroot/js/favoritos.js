/**
 * @file Punto de entrada de Mis favoritos (favoritos.html), frames "Mis favoritos" y
 * "Mis favoritos (vacío)" de Figma.
 *
 * - Pide a C# los eventos y lugares guardados ('favoritos.listar') y los pinta con las Card evento
 *   y Card lugar de siempre, con el corazón lleno (primero los eventos, como en el diseño).
 * - Las píldoras Todos / Eventos / Lugares filtran aquí, sin volver a consultar.
 * - Al vaciar un corazón (y que C# lo confirme, evento `sc:favorito-guardado`) la tarjeta se va;
 *   sin nada guardado se muestra el estado vacío con "Explorar eventos".
 */

import './componentes/sc-imagen.js';
import './componentes/sc-boton-favorito.js';
import './componentes/sc-volver-arriba.js';
import './componentes/secciones/sc-navbar.js';

import { html } from './nucleo/componente.js';
import { invocar, enApp } from './nucleo/puente.js';
import { atenderAcciones, cargarSesion, irA } from './nucleo/navegacion.js';
import { boton, icono, pildora } from './componentes/atomos.js';
import { tarjetaEvento, tarjetaLugar, esqueleto } from './componentes/tarjetas.js';
import { mostrarToast } from './componentes/sc-toast.js';
import { abrirRequiereSesion } from './componentes/modales/cuenta.js';
import { observarRevelado } from './utilidades/animaciones.js';

const segmentos = document.querySelector('.favoritos__segmentos');
const contenedor = document.getElementById('favoritos');

const OPCIONES = [
  { texto: 'Todos', valor: 'todos' },
  { texto: 'Eventos', valor: 'eventos' },
  { texto: 'Lugares', valor: 'lugares' },
];

/** Respuesta de 'favoritos.listar' ({ eventos, lugares }) o null mientras carga / sin sesión. */
let favoritos = null;
let filtro = 'todos';
/** Id del usuario cuyos favoritos se muestran, para no volver a pedir lo mismo. */
let usuarioId = null;

// ---------------------------------------------------------------------
// Pintar
// ---------------------------------------------------------------------

function pintarSegmentos() {
  segmentos.innerHTML = String(html`${OPCIONES.map((o) => pildora({ ...o, activa: o.valor === filtro }))}`);
}

function pintarCargando() {
  segmentos.hidden = true;
  contenedor.innerHTML = String(html`
    <div class="rejilla-tarjetas favoritos__rejilla">
      ${[0, 1, 2, 3].map(() => esqueleto('favoritos__esqueleto'))}
    </div>`);
}

/** "Estado vacío" de Figma. Con un filtro activo lo dice para ese tipo. */
function vacio() {
  const titulos = { todos: 'Aún no guardas nada', eventos: 'Aún no guardas eventos', lugares: 'Aún no guardas lugares' };
  return html`
    <div class="favoritos__vacio">
      <span class="favoritos__vacio-icono">${icono('corazon-cielo-32', 32)}</span>
      <h2 class="t-h2">${titulos[filtro]}</h2>
      <p class="t-body texto-2">Toca el corazón en un evento o lugar para encontrarlo aquí después.</p>
      <div class="favoritos__vacio-boton">${boton({ texto: 'Explorar eventos', variante: 'oscuro', accion: 'explorar-eventos' })}</div>
    </div>`;
}

function pintar() {
  if (!favoritos) return;
  const { eventos, lugares } = favoritos;
  const total = eventos.length + lugares.length;
  segmentos.hidden = total === 0;
  if (total > 0) pintarSegmentos();

  const tarjetas = [
    ...(filtro === 'lugares' ? [] : eventos.map((e) => ({ tipo: 'evento', item: e }))),
    ...(filtro === 'eventos' ? [] : lugares.map((l) => ({ tipo: 'lugar', item: l }))),
  ];
  contenedor.innerHTML = String(tarjetas.length === 0 ? vacio() : html`
    <div class="rejilla-tarjetas favoritos__rejilla">
      ${tarjetas.map(({ tipo, item }, i) => (tipo === 'evento'
        ? tarjetaEvento(item, { orden: i % 4, favorito: true })
        : tarjetaLugar(item, { orden: i % 4, favorito: true })))}
    </div>`);
  observarRevelado(contenedor);
}

function pintarSinSesion() {
  segmentos.hidden = true;
  contenedor.innerHTML = String(html`
    <div class="favoritos__vacio">
      <span class="favoritos__vacio-icono">${icono('corazon-cielo-32', 32)}</span>
      <h2 class="t-h2">Inicia sesión para ver tus favoritos</h2>
      <p class="t-body texto-2">Guarda eventos y lugares con el corazón y encuéntralos aquí después.</p>
      <div class="favoritos__vacio-boton">${boton({ texto: 'Iniciar sesión', variante: 'oscuro', accion: 'iniciar-sesion' })}</div>
    </div>`);
}

// ---------------------------------------------------------------------
// Datos y eventos
// ---------------------------------------------------------------------

async function cargarFavoritos() {
  pintarCargando();
  try {
    const datos = await invocar('favoritos.listar');
    favoritos = { eventos: datos.eventos ?? [], lugares: datos.lugares ?? [] };
    pintar();
  } catch (error) {
    if (error.codigo === 'requiere-sesion') { pintarSinSesion(); abrirRequiereSesion(); return; }
    console.error(error);
    mostrarToast(error.message || 'No se pudieron cargar tus favoritos.', { tipo: 'error' });
    favoritos = { eventos: [], lugares: [] };
    pintar();
  }
}

segmentos.addEventListener('click', (e) => {
  const elegida = e.target.closest('.pildora');
  if (!elegida || elegida.dataset.valor === filtro) return;
  filtro = elegida.dataset.valor;
  pintar();
});

// El corazón se vació y C# ya lo guardó: la tarjeta sale de la lista.
document.addEventListener('sc:favorito-guardado', ({ detail }) => {
  if (!favoritos || detail.activo) return;
  const lista = detail.tipo === 'evento' ? 'eventos' : 'lugares';
  favoritos[lista] = favoritos[lista].filter((item) => item.id !== detail.id);
  pintar();
});

document.addEventListener('sc:usuario', ({ detail }) => {
  const id = detail.usuario?.id ?? null;
  if (id === usuarioId && favoritos) return;
  if (usuarioId != null && id == null) { irA('home'); return; }   // cerró sesión aquí
  usuarioId = id;
  if (id == null) {
    pintarSinSesion();
    abrirRequiereSesion({ texto: 'Inicia sesión para ver los eventos y lugares que guardaste.' });
  } else {
    cargarFavoritos();
  }
});

// ---------------------------------------------------------------------
// Inicio
// ---------------------------------------------------------------------

document.documentElement.dataset.entorno = enApp ? 'app' : 'navegador';
atenderAcciones();
cargarSesion(document.querySelector('sc-navbar'));
