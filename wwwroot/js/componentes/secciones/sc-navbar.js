/**
 * @file <sc-navbar>: barra superior fija (componente "Navbar" de Figma, estados Visitante y Logueado).
 *
 * Datos: `navbar.datos = { usuario }`, donde `usuario` es el Usuario de BackendLogica
 * (lo que responde 'sesion.obtener') o null para el visitante.
 *
 * Acciones (data-accion, las atiende main.js):
 *   Visitante: 'mapa', 'iniciar-sesion', 'crear-cuenta'
 *   Logueado:  'mapa', 'mis-planes', 'favoritos', 'menu-cuenta'
 * Evento: `sc:buscar` → detail { texto } al enviar el buscador.
 */

import { Componente, definir, html } from '../../nucleo/componente.js';
import { icono, boton } from '../atomos.js';

class Navbar extends Componente {
  datosIniciales() { return { usuario: null }; }

  plantilla({ usuario }) {
    return html`
      <nav class="navbar${usuario ? ' navbar--logueado' : ''}" aria-label="Principal">
        <div class="navbar__contenido contenedor">
          <a class="logo" href="#inicio" aria-label="SuraChih, inicio">
            <img src="assets/iconos/logo-sol.svg" alt="" width="26" height="26">
            <span>SuraChih</span>
          </a>
          <form class="navbar__buscador" role="search">
            ${icono('buscar-gris', 16)}
            <label class="solo-lectores" for="navbar-buscar">Buscar</label>
            <input id="navbar-buscar" type="search" class="t-small" placeholder="Busca eventos, cafés o museos" autocomplete="off">
          </form>
          <div class="navbar__acciones">
            <button type="button" class="navbar__enlace t-small-fuerte" data-accion="mapa">${icono('mapa', 16)}Mapa</button>
            ${usuario ? this.#accionesUsuario(usuario) : this.#accionesVisitante()}
          </div>
        </div>
      </nav>`;
  }

  #accionesVisitante() {
    return html`
      ${boton({ texto: 'Iniciar sesión', variante: 'fantasma', tamano: 's', accion: 'iniciar-sesion' })}
      ${boton({ texto: 'Crear cuenta', variante: 'oscuro', tamano: 's', accion: 'crear-cuenta' })}`;
  }

  #accionesUsuario(usuario) {
    const nombre = usuario.nombreUsuario ?? '';
    return html`
      <button type="button" class="navbar__enlace t-small-fuerte" data-accion="mis-planes">${icono('mis-planes', 16)}Mis planes</button>
      <button type="button" class="navbar__circulo navbar__favoritos" data-accion="favoritos" aria-label="Mis favoritos">
        ${icono('corazon-vacio', 16)}
      </button>
      <button type="button" class="navbar__circulo navbar__avatar" data-accion="menu-cuenta" aria-label="Cuenta de ${nombre}" aria-haspopup="menu">
        ${usuario.avatarUrl
          ? html`<img src="${usuario.avatarUrl}" alt="" width="40" height="40">`
          : html`<span aria-hidden="true">${nombre.charAt(0).toUpperCase()}</span>`}
      </button>`;
  }

  alPintar() {
    this.querySelector('form').addEventListener('submit', (e) => {
      e.preventDefault();
      const texto = this.querySelector('input').value.trim();
      if (texto) this.emitir('sc:buscar', { texto });
    });
  }
}

definir('sc-navbar', Navbar);
