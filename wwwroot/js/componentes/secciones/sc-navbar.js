/**
 * @file <sc-navbar>: barra superior fija (componente "Navbar", estado Visitante).
 * Evento: `sc:buscar` → detail { texto } al enviar el buscador.
 */

import { Componente, definir, html } from '../../nucleo/componente.js';
import { icono, boton } from '../atomos.js';

class Navbar extends Componente {
  plantilla() {
    return html`
      <nav class="navbar" aria-label="Principal">
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
            ${boton({ texto: 'Iniciar sesión', variante: 'fantasma', tamano: 's', accion: 'iniciar-sesion' })}
            ${boton({ texto: 'Crear cuenta', variante: 'oscuro', tamano: 's', accion: 'crear-cuenta' })}
          </div>
        </div>
      </nav>`;
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
