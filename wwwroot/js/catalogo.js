/**
 * @file Página de referencia (componentes.html): pinta todas las variantes de los componentes base
 * con datos de ejemplo. Sirve para ver que coinciden con Figma y como ejemplo de uso.
 */

import './componentes/sc-imagen.js';
import './componentes/sc-boton-favorito.js';
import './componentes/sc-chip-categoria.js';
import './componentes/sc-mapa.js';
import './componentes/secciones/sc-navbar.js';

import { html } from './nucleo/componente.js';
import { boton, campo, chipCategoria, marcarError, pildora } from './componentes/atomos.js';
import { tarjetaEvento, tarjetaLugar, tarjetaRecomendada, itemLugar } from './componentes/tarjetas.js';
import { abrirDetalleEvento } from './componentes/sc-detalle-evento.js';
import { mostrarToast } from './componentes/sc-toast.js';
import { puntoDeLugar } from './utilidades/lugares.js';
import * as modalesCuenta from './componentes/modales/cuenta.js';
import { alternarMenuCuenta } from './componentes/menu-cuenta.js';

const VARIANTES = ['primario', 'oscuro', 'secundario', 'peligro', 'fantasma'];

// Mismas etiquetas que BaseDeDatos/surachih_v1.sql (tag.icon decide color e icono del chip).
const ETIQUETAS = [
  { id: 1, nombre: 'Familiar', icono: 'family' },
  { id: 2, nombre: 'Música', icono: 'music' },
  { id: 3, nombre: 'Deportes y aire libre', icono: 'run' },
  { id: 4, nombre: 'Tecnología y conferencias', icono: 'robot' },
  { id: 5, nombre: 'Gastronomía', icono: 'food' },
];

const EVENTOS = [
  { id: 2, titulo: 'Minecraft Experience: Villager Rescue', inicio: '2026-10-23T14:00:00', fin: '2026-10-25T21:00:00',
    lugarNombre: 'Forum Buenavista', precioMin: 600, precioMax: 850, imagenUrl: 'assets/demo/minecraft.jpg' },
  { id: 4, titulo: 'Mac DeMarco', inicio: '2026-11-06T21:00:00', fin: '2026-11-06T23:59:00',
    lugarNombre: 'Quarry Studios', precioMin: 1450, precioMax: 2600, imagenUrl: 'assets/demo/mac-demarco.jpg' },
  { id: 7, titulo: 'Harry Potter™: La Experiencia del Bosque Prohibido', fechaPorConfirmar: true, precioPorConfirmar: true,
    inicio: '2026-12-01T00:00:00', fin: '2026-12-01T00:00:00', direccion: 'Chihuahua', imagenUrl: 'assets/demo/harry-potter.jpg' },
  { id: 8, titulo: 'Nombre del evento con dos líneas como máximo aunque el título sea mucho más largo', inicio: '2026-11-26T20:00:00',
    fin: '2026-11-26T23:00:00', lugarNombre: 'Foro Indie Rocks!', precioMin: 0, precioMax: 0, imagenUrl: null },
];

// `latitud`, `longitud` y el `icono` de la etiqueta (tag.icon) son lo que usa el mapa.
const LUGARES = [
  { id: 2, nombre: 'Café de la Tercera', direccion: 'C. Tercera 805, Zona Centro', calificacion: 4.5, precioMin: 200, precioMax: 300,
    latitud: 28.6355, longitud: -106.077,
    imagenUrl: 'assets/demo/cafe-tercera.jpg', etiquetas: [{ id: 6, nombre: 'Café', icono: 'coffee' }], horarioAhora: { abierto: true, hasta: '21:00:00' } },
  { id: 10, nombre: 'Centro Cultural Quinta Gameros', direccion: 'Av. Paseo Bolívar 401', calificacion: 4.8, precioMin: 37, precioMax: 37,
    latitud: 28.6361, longitud: -106.0722,
    imagenUrl: 'assets/demo/quinta-gameros.jpg', etiquetas: [{ id: 10, nombre: 'Museo', icono: 'museum' }], horarioAhora: { abierto: false, abreA: '10:00:00' } },
  { id: 11, nombre: 'Catedral Metropolitana de Chihuahua', direccion: 'Calle Guadalupe Victoria', calificacion: 4.8, precioMin: 0, precioMax: 0,
    latitud: 28.6359, longitud: -106.0762,
    imagenUrl: 'assets/demo/catedral.jpg', etiquetas: [{ id: 12, nombre: 'Templo' }], horarioAhora: { abierto: true, hasta: '20:00:00' } },
  { id: 1, nombre: 'Dandelion Coffee', direccion: 'C. Monte Bello 4334', calificacion: null, precioMin: 100, precioMax: 200,
    latitud: 28.652, longitud: -106.119,
    imagenUrl: null, etiquetas: [{ id: 6, nombre: 'Café', icono: 'coffee' }], horarioAhora: null },
];

const muestra = (titulo, uso, contenido, clase = '') => html`
  <section class="muestra">
    <h2 class="t-h2">${titulo}</h2>
    <code>${uso}</code>
    <div class="muestra__panel ${clase}">${contenido}</div>
  </section>`;

document.getElementById('catalogo').innerHTML = String(html`
  ${muestra('Botón · 5 tipos × 2 tamaños', "boton({ texto, variante: 'primario'|'oscuro'|'secundario'|'peligro'|'fantasma', tamano: 'm'|'s', icono, accion })", html`
    ${['m', 's'].map((tamano) => html`
      <div class="muestra__fila" style="margin-bottom:16px">
        ${VARIANTES.map((variante) => boton({ texto: 'Botón', variante, tamano }))}
      </div>
      <div class="muestra__fila" style="margin-bottom:16px">
        ${VARIANTES.map((variante) => boton({ texto: 'Con icono', variante, tamano, icono: 'subir' }))}
      </div>`)}
    <div class="muestra__fila">${boton({ texto: 'Deshabilitado', deshabilitado: true })}${boton({ texto: 'Deshabilitado', variante: 'secundario', deshabilitado: true })}</div>
  `, 'muestra__panel--blanco')}

  ${muestra('Campo', "campo({ nombre, etiqueta, tipo, icono: 'correo', ayuda, error, requerido })", html`
    <form class="muestra__campos" id="formulario-demo" novalidate>
      ${campo({ nombre: 'correo', etiqueta: 'Correo electrónico', tipo: 'email', icono: 'correo', autocompletar: 'email' })}
      ${campo({ nombre: 'usuario', etiqueta: 'Nombre de usuario', etiquetaVisible: true, ayuda: 'De 3 a 30 letras, números, punto o guion bajo.' })}
      ${campo({ nombre: 'contrasena', etiqueta: 'Contraseña', etiquetaVisible: true, tipo: 'password', autocompletar: 'current-password' })}
      ${campo({ nombre: 'correo2', etiqueta: 'Correo con error', etiquetaVisible: true, tipo: 'email', icono: 'correo', valor: 'ana@', error: 'Escribe un correo válido.' })}
      ${campo({ nombre: 'fijo', etiqueta: 'Deshabilitado', etiquetaVisible: true, valor: 'mariana_r', deshabilitado: true })}
      <div class="muestra__fila">${boton({ texto: 'Probar validación', tipo: 'submit', tamano: 's', variante: 'oscuro' })}</div>
    </form>
  `, 'muestra__panel--blanco')}

  ${muestra('Chip categoría · Normal / Activo', 'chipCategoria(etiqueta, { activo, interactivo })  →  evento sc:categoria', html`
    <div class="muestra__fila" style="margin-bottom:16px">${ETIQUETAS.map((t) => chipCategoria(t))}</div>
    <div class="muestra__fila" style="margin-bottom:16px">${ETIQUETAS.map((t) => chipCategoria(t, { activo: true }))}</div>
    <div class="muestra__fila">${ETIQUETAS.slice(0, 2).map((t) => chipCategoria(t, { interactivo: false }))}${chipCategoria({ id: 6, nombre: 'Cafetería', icono: 'coffee' }, { interactivo: false })}</div>
  `, 'muestra__panel--blanco')}

  ${muestra('Botón favorito · Vacío / Hover / Lleno', '<sc-boton-favorito tipo="evento" objetivo="1" tamano="s|m" activo>  →  evento sc:favorito', html`
    <div class="muestra__fila">
      <sc-boton-favorito tipo="evento" objetivo="1" etiqueta="Demo"></sc-boton-favorito>
      <sc-boton-favorito tipo="evento" objetivo="1" etiqueta="Demo" activo></sc-boton-favorito>
      <sc-boton-favorito tipo="evento" objetivo="1" etiqueta="Demo" tamano="m"></sc-boton-favorito>
      <sc-boton-favorito tipo="evento" objetivo="1" etiqueta="Demo" tamano="m" activo></sc-boton-favorito>
    </div>
  `)}

  ${muestra('Navbar · Visitante / Logueado', "navbar.datos = { usuario }   (usuario = respuesta de 'sesion.obtener' o null)", html`
    <div class="muestra__navbar">
      <sc-navbar id="navbar-visitante"></sc-navbar>
      <sc-navbar id="navbar-logueado"></sc-navbar>
    </div>
  `, 'muestra__panel--blanco')}

  ${muestra('Modales de cuenta (WBS 4.2) · usan los datos demo: mariana_r / Test1234!', 'abrirIniciarSesion() · abrirCrearCuenta() · abrirRecuperarContrasena() · abrirContrasenaNueva(token) · abrirRequiereSesion() · alternarMenuCuenta(boton, usuario)', html`
    <div class="muestra__fila">
      ${[['abrirIniciarSesion', 'Iniciar sesión'], ['abrirCrearCuenta', 'Crear cuenta'], ['abrirRecuperarContrasena', 'Recuperar contraseña'],
        ['abrirContrasenaNueva', 'Contraseña nueva'], ['abrirRequiereSesion', 'Inicia sesión para continuar']]
        .map(([funcion, texto]) => html`<button type="button" class="boton boton--secundario boton--s" data-modal="${funcion}"><span>${texto}</span></button>`)}
      <button type="button" class="boton boton--oscuro boton--s" data-menu-cuenta><span>Menú de cuenta</span></button>
    </div>
  `)}

  ${muestra('Card evento · Normal / Hover (pasa el mouse)', 'tarjetaEvento(evento, { orden, favorito })', html`
    <div class="rejilla-tarjetas">${EVENTOS.map((e, i) => tarjetaEvento(e, { orden: i, favorito: i === 1 }))}</div>
  `, 'muestra__panel--blanco')}

  ${muestra('Card lugar · Normal / Hover (pasa el mouse)', 'tarjetaLugar(lugar, { orden, favorito })', html`
    <div class="rejilla-tarjetas">${LUGARES.map((l, i) => tarjetaLugar(l, { orden: i, favorito: i === 0 }))}</div>
  `, 'muestra__panel--blanco')}

  ${muestra('Card recomendada · Normal / Hover (pasa el mouse)', "tarjetaRecomendada(item, { tipo: 'evento' | 'lugar', orden })   dentro de .rejilla-recomendadas", html`
    <div class="rejilla-recomendadas">
      ${tarjetaRecomendada({ ...EVENTOS[0], descripcion: 'Embárcate en una misión inmersiva e interactiva de Minecraft junto a tus amigos y familia.', etiquetas: [ETIQUETAS[0]] })}
      ${tarjetaRecomendada({ ...LUGARES[0], descripcion: 'Café de especialidad y terraza en el centro.' }, { tipo: 'lugar', orden: 1 })}
    </div>
  `)}

  ${muestra('Detalle de evento (hoja "Evento / ...") · usa los datos demo', 'abrirDetalleEvento(id, { inscribir })   ·   data-accion="ver-evento" data-id="id"', html`
    <div class="muestra__fila">
      ${[[2, 'Normal'], [1, 'Fecha y precio por confirmar'], [8, 'Finalizado'], [9, 'Cancelado']]
        .map(([id, texto]) => html`<button type="button" class="boton boton--secundario boton--s" data-detalle="${id}"><span>${texto}</span></button>`)}
    </div>
    <p class="t-small texto-2" style="margin-top:12px">«Ya vas a ir»: inicia sesión con mariana_r / Test1234! desde la landing y pulsa «Voy a ir».</p>
  `)}

  ${muestra('Píldora · Normal / Activa', 'pildora({ texto, valor, activa })   (una activa por grupo; se lee con data-valor)', html`
    <div class="muestra__fila" role="group" aria-label="Tipo de lugar">
      ${pildora({ texto: 'Todo', activa: true })}${pildora({ texto: 'Cafés', valor: 'coffee' })}${pildora({ texto: 'Museos', valor: 'museum' })}${pildora({ texto: 'Parques', valor: 'tree' })}
    </div>
  `, 'muestra__panel--blanco')}

  ${muestra('Item lugar (lista del mapa) · Normal / Activo', 'itemLugar(lugar, { activo })   →  <button data-lugar="id">', html`
    <ul class="muestra__lista">${LUGARES.map((l, i) => itemLugar(l, { activo: i === 0 }))}</ul>
  `, 'muestra__panel--blanco')}

  ${muestra('Mapa · Explorar (varios pines)', 'mapa.puntos = lugares.map(puntoDeLugar)   →  eventos sc:mapa-pin { punto } y sc:mapa-zona { visibles }', html`
    <sc-mapa id="mapa-explorar" class="muestra__mapa" aria-label="Mapa de ejemplo con varios lugares"></sc-mapa>
  `)}

  ${muestra('Mapa · Detalle de evento o lugar (un pin fijo)', '<sc-mapa lat="28.6353" lng="-106.0756" titulo="Plaza de Armas">', html`
    <sc-mapa class="muestra__mapa" lat="28.6353" lng="-106.0756" titulo="Plaza de Armas" aria-label="Mapa de ejemplo con un lugar"></sc-mapa>
  `)}

  ${muestra('Mapa · Elegir ubicación (pin arrastrable de "Publicar evento")', '<sc-mapa arrastrable lat lng>   →  evento sc:mapa-ubicacion { lat, lng }', html`
    <sc-mapa id="mapa-elegir" class="muestra__mapa" arrastrable lat="28.6353" lng="-106.0756" aria-label="Mapa de ejemplo para elegir una ubicación"></sc-mapa>
    <p id="mapa-coordenadas" class="t-small texto-2" style="margin-top:12px">Arrastra el pin o haz clic en el mapa.</p>
  `)}
`);

document.getElementById('mapa-explorar').puntos = LUGARES.map(puntoDeLugar);
document.addEventListener('sc:mapa-pin', ({ detail }) => { if (detail.punto.titulo) mostrarToast(detail.punto.titulo, { tipo: 'info' }); });
document.addEventListener('sc:mapa-ubicacion', ({ detail }) => {
  document.getElementById('mapa-coordenadas').textContent = `latitud: ${detail.lat} · longitud: ${detail.lng}`;
});

document.getElementById('navbar-logueado').datos = {
  usuario: { id: 5, nombreUsuario: 'andy', correo: 'andy@example.com', avatarUrl: null },
};

// En la app las tarjetas aparecen con scroll (data-revelar); aquí se muestran de una vez.
document.querySelectorAll('[data-revelar]').forEach((el) => el.classList.add('revelado'));

document.getElementById('formulario-demo').addEventListener('submit', (e) => {
  e.preventDefault();
  const correo = e.target.elements.correo;
  marcarError(correo, correo.value.includes('@') ? null : 'Escribe un correo válido.');
});

document.addEventListener('click', (e) => {
  const detalle = e.target.closest('[data-detalle]')?.dataset.detalle;
  if (detalle) abrirDetalleEvento(Number(detalle));
  const modal = e.target.closest('[data-modal]')?.dataset.modal;
  if (modal) modalesCuenta[modal](modal === 'abrirContrasenaNueva' ? 'token-de-ejemplo' : undefined);
  const menu = e.target.closest('[data-menu-cuenta]');
  if (menu) {
    alternarMenuCuenta(menu, { id: 1, nombreUsuario: 'Andy', correo: 'andy@correo.com', avatarUrl: null, esModerador: true, esOrganizador: true });
  }
});
document.addEventListener('sc:sesion', ({ detail }) => mostrarToast(`Sesión iniciada: ${detail.usuario.nombreUsuario}`));

document.addEventListener('sc:categoria', ({ detail }) =>
  mostrarToast(`${detail.nombre}: ${detail.activo ? 'filtro activado' : 'filtro quitado'}`));
document.addEventListener('sc:favorito', ({ detail }) =>
  mostrarToast(detail.activo ? 'Guardado en favoritos' : 'Quitado de favoritos'));
document.addEventListener('click', (e) => {
  const accion = e.target.closest('[data-accion]')?.dataset.accion;
  if (accion) { e.preventDefault(); mostrarToast(`data-accion="${accion}"`, { tipo: 'info' }); }
});
