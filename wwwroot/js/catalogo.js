/**
 * @file Página de referencia (componentes.html): pinta todas las variantes de los componentes base
 * con datos de ejemplo. Sirve para ver que coinciden con Figma y como ejemplo de uso.
 */

import './componentes/sc-imagen.js';
import './componentes/sc-boton-favorito.js';
import './componentes/sc-chip-categoria.js';
import './componentes/secciones/sc-navbar.js';

import { html } from './nucleo/componente.js';
import { boton, campo, chipCategoria, marcarError } from './componentes/atomos.js';
import { tarjetaEvento, tarjetaLugar } from './componentes/tarjetas.js';
import { mostrarToast } from './componentes/sc-toast.js';

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

const LUGARES = [
  { id: 2, nombre: 'Café de la Tercera', direccion: 'C. Tercera 805, Zona Centro', calificacion: 4.5, precioMin: 200, precioMax: 300,
    imagenUrl: 'assets/demo/cafe-tercera.jpg', etiquetas: [{ id: 6, nombre: 'Café' }], horarioAhora: { abierto: true, hasta: '21:00:00' } },
  { id: 10, nombre: 'Centro Cultural Quinta Gameros', direccion: 'Av. Paseo Bolívar 401', calificacion: 4.8, precioMin: 37, precioMax: 37,
    imagenUrl: 'assets/demo/quinta-gameros.jpg', etiquetas: [{ id: 10, nombre: 'Museo' }], horarioAhora: { abierto: false, abreA: '10:00:00' } },
  { id: 11, nombre: 'Catedral Metropolitana de Chihuahua', direccion: 'Calle Guadalupe Victoria', calificacion: 4.8, precioMin: 0, precioMax: 0,
    imagenUrl: 'assets/demo/catedral.jpg', etiquetas: [{ id: 12, nombre: 'Templo' }], horarioAhora: { abierto: true, hasta: '20:00:00' } },
  { id: 1, nombre: 'Dandelion Coffee', direccion: 'C. Monte Bello 4334', calificacion: null, precioMin: 100, precioMax: 200,
    imagenUrl: null, etiquetas: [{ id: 6, nombre: 'Café' }], horarioAhora: null },
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

  ${muestra('Card evento · Normal / Hover (pasa el mouse)', 'tarjetaEvento(evento, { orden, favorito })', html`
    <div class="rejilla-tarjetas">${EVENTOS.map((e, i) => tarjetaEvento(e, { orden: i, favorito: i === 1 }))}</div>
  `, 'muestra__panel--blanco')}

  ${muestra('Card lugar · Normal / Hover (pasa el mouse)', 'tarjetaLugar(lugar, { orden, favorito })', html`
    <div class="rejilla-tarjetas">${LUGARES.map((l, i) => tarjetaLugar(l, { orden: i, favorito: i === 0 }))}</div>
  `, 'muestra__panel--blanco')}
`);

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

document.addEventListener('sc:categoria', ({ detail }) =>
  mostrarToast(`${detail.nombre}: ${detail.activo ? 'filtro activado' : 'filtro quitado'}`));
document.addEventListener('sc:favorito', ({ detail }) =>
  mostrarToast(detail.activo ? 'Guardado en favoritos' : 'Quitado de favoritos'));
document.addEventListener('click', (e) => {
  const accion = e.target.closest('[data-accion]')?.dataset.accion;
  if (accion) { e.preventDefault(); mostrarToast(`data-accion="${accion}"`, { tipo: 'info' }); }
});
