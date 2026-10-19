/**
 * @file <sc-navbar>: barra superior fija (componente "Navbar" de Figma, estados Visitante y Logueado).
 *
 * Datos: `navbar.datos = { usuario }`, donde `usuario` es el Usuario de BackendLogica
 * (lo que responde 'sesion.obtener') o null para el visitante.
 *
 * Atributo `pagina`: la pantalla actual ('home', 'mapa'...). Su enlace se marca como activo
 * para que siempre se vea dónde estás y cómo volver: `<sc-navbar pagina="home">`.
 *
 * Acciones (data-accion, las atiende nucleo/navegacion.js):
 *   Siempre:   'inicio' (logo y enlace "Inicio": van al Home), 'mapa'
 *   Visitante: 'iniciar-sesion', 'crear-cuenta'
 *   Logueado:  'mis-planes', 'favoritos', 'menu-cuenta'
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
          <a class="logo" href="home.html" data-accion="inicio" aria-label="SuraChih, ir al inicio">
            <img src="assets/iconos/logo-sol.svg" alt="" width="26" height="26">
            <span>SuraChih</span>
          </a>
          ${this.#enlace('home', 'inicio', 'inicio', 'Inicio')}
          <form class="navbar__buscador" role="search">
            ${icono('buscar-gris', 16)}
            <label class="solo-lectores" for="navbar-buscar">Buscar</label>
            <input id="navbar-buscar" type="search" class="t-small" placeholder="Busca eventos, cafés o museos" autocomplete="off">
          </form>
          <div class="navbar__acciones">
            ${this.#enlace('mapa', 'mapa', 'mapa', 'Mapa')}
            ${usuario ? this.#accionesUsuario(usuario) : this.#accionesVisitante()}
          </div>
        </div>
      </nav>`;
  }

  /** Enlace de texto con icono; el de la pantalla actual (atributo `pagina`) sale activo. */
  #enlace(pagina, accion, nombreIcono, texto) {
    const actual = this.getAttribute('pagina') === pagina;
    return html`
      <button type="button" class="navbar__enlace t-small-fuerte" data-accion="${accion}"${actual && html` aria-current="page"`}>
        ${icono(nombreIcono, 16)}${texto}
      </button>`;
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
      <button type="button" class="navbar__circulo navbar__favoritos" data-accion="favoritos" aria-label="Mis favoritos"${this.getAttribute('pagina') === 'favoritos' && html` aria-current="page"`}>
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
