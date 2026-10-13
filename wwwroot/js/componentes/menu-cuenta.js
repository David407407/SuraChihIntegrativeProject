/**
 * @file Menú de cuenta (frame "Menú de cuenta" de Figma): se despliega bajo el avatar del navbar.
 *
 * Encabezado con avatar, nombre y correo; opciones según el usuario y "Cerrar sesión".
 * "Panel de organizador" solo sale a organizadores aprobados y "Moderación" solo a moderadores.
 *
 * Cada opción es un botón con `data-accion` (las atiende main.js como el resto de la página):
 * 'mi-perfil', 'mis-planes', 'favoritos', 'panel-organizador', 'moderacion', 'cerrar-sesion'.
 *
 * @example
 * alternarMenuCuenta(botonAvatar, usuario);   // usuario = lo que responde 'sesion.obtener'
 */

import { html } from '../nucleo/componente.js';
import { icono } from './atomos.js';

/** @type {HTMLElement|null} */
let menu = null;
/** @type {HTMLElement|null} */
let ancla = null;

function opcion(texto, nombreIcono, accion, clase = '') {
  return html`
    <button type="button" role="menuitem" class="menu-cuenta__opcion ${clase}" data-accion="${accion}" tabindex="-1">
      ${icono(nombreIcono, 16)}<span>${texto}</span>
    </button>`;
}

function plantilla(usuario) {
  const nombre = usuario.nombreUsuario ?? '';
  return html`
    <div class="menu-cuenta__encabezado">
      <span class="menu-cuenta__avatar" aria-hidden="true">
        ${usuario.avatarUrl
          ? html`<img src="${usuario.avatarUrl}" alt="" width="40" height="40">`
          : nombre.charAt(0).toUpperCase()}
      </span>
      <span class="menu-cuenta__datos">
        <span class="menu-cuenta__nombre t-body-fuerte">${nombre}</span>
        <span class="menu-cuenta__correo t-small texto-2">${usuario.correo}</span>
      </span>
    </div>
    <hr class="menu-cuenta__linea">
    ${opcion('Mi perfil', 'perfil', 'mi-perfil')}
    ${opcion('Mis planes', 'mis-planes', 'mis-planes')}
    ${opcion('Mis favoritos', 'corazon-vacio', 'favoritos')}
    ${usuario.esOrganizador && opcion('Panel de organizador', 'tienda-16', 'panel-organizador')}
    ${usuario.esModerador && opcion('Moderación', 'escudo', 'moderacion')}
    <hr class="menu-cuenta__linea">
    ${opcion('Cerrar sesión', 'salir', 'cerrar-sesion', 'menu-cuenta__opcion--peligro')}`;
}

function colocar() {
  if (!menu || !ancla) return;
  const r = ancla.getBoundingClientRect();
  menu.style.top = `${Math.round(r.bottom + 12)}px`;
  menu.style.right = `${Math.round(document.documentElement.clientWidth - r.right)}px`;
}

function opciones() {
  return [...menu.querySelectorAll('.menu-cuenta__opcion')];
}

function alTeclado(e) {
  if (!menu) return;
  const lista = opciones();
  const i = lista.indexOf(document.activeElement);
  if (e.key === 'Escape') { e.preventDefault(); cerrarMenuCuenta(true); }
  else if (e.key === 'ArrowDown') { e.preventDefault(); lista[(i + 1) % lista.length].focus(); }
  else if (e.key === 'ArrowUp') { e.preventDefault(); lista[(i - 1 + lista.length) % lista.length].focus(); }
  else if (e.key === 'Home') { e.preventDefault(); lista[0].focus(); }
  else if (e.key === 'End') { e.preventDefault(); lista.at(-1).focus(); }
  else if (e.key === 'Tab') cerrarMenuCuenta(false);
}

function alClicFuera(e) {
  if (!menu) return;
  if (menu.contains(e.target)) {
    // Una opción: main.js atiende su data-accion; aquí solo se cierra el menú.
    if (e.target.closest('.menu-cuenta__opcion')) cerrarMenuCuenta(false);
    return;
  }
  if (!ancla?.contains(e.target)) cerrarMenuCuenta(false);
}

const alMoverVentana = () => colocar();

/**
 * Abre el menú bajo `boton` o lo cierra si ya estaba abierto.
 * @param {HTMLElement} boton El avatar del navbar.
 * @param {object} usuario UsuarioSesion de C# (id, nombreUsuario, correo, avatarUrl, esModerador, esOrganizador).
 */
export function alternarMenuCuenta(boton, usuario) {
  if (menu) { cerrarMenuCuenta(true); return; }
  if (!usuario) return;

  ancla = boton;
  menu = document.createElement('div');
  menu.className = 'menu-cuenta';
  menu.id = 'menu-cuenta';
  menu.setAttribute('role', 'menu');
  menu.setAttribute('aria-label', `Cuenta de ${usuario.nombreUsuario}`);
  menu.innerHTML = String(plantilla(usuario));
  document.body.append(menu);
  colocar();

  boton.setAttribute('aria-expanded', 'true');
  boton.setAttribute('aria-controls', menu.id);
  opciones()[0]?.focus({ preventScroll: true });

  menu.addEventListener('keydown', alTeclado);
  // En el siguiente ciclo, para que el clic que lo abrió no lo cierre.
  setTimeout(() => document.addEventListener('click', alClicFuera, true));
  window.addEventListener('resize', alMoverVentana);
  window.addEventListener('scroll', alMoverVentana, { passive: true });
}

/**
 * Cierra el menú si está abierto.
 * @param {boolean} [devolverFoco] true para regresar el foco al avatar (Esc o volver a hacer clic).
 */
export function cerrarMenuCuenta(devolverFoco = false) {
  if (!menu) return;
  const viejo = menu;
  menu = null;
  document.removeEventListener('click', alClicFuera, true);
  window.removeEventListener('resize', alMoverVentana);
  window.removeEventListener('scroll', alMoverVentana);
  ancla?.setAttribute('aria-expanded', 'false');
  if (devolverFoco) ancla?.focus();
  ancla = null;

  viejo.classList.add('menu-cuenta--saliendo');
  const quitar = () => viejo.remove();
  viejo.addEventListener('animationend', quitar, { once: true });
  setTimeout(quitar, 300);
}
