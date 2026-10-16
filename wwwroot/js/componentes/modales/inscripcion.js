/**
 * @file Modales de inscripción. Frames de Figma, página Rediseño:
 *
 * | Figma                          | Función                          | Acción de C# (ControladorEvento) |
 * |--------------------------------|----------------------------------|----------------------------------|
 * | Modal / Confirmar inscripción  | `confirmarInscripcion(evento)`   | `inscripcion.inscribir`          |
 * | Modal / Cancelar inscripción   | `confirmarCancelacion(evento)`   | `inscripcion.cancelar`           |
 *
 * Los abre el botón "Voy a ir" / "Ya vas a ir" del detalle de evento (sc-detalle-evento.js). Ambos
 * devuelven una promesa con el estado final de la inscripción, así quien los abre solo pinta el botón:
 *
 * @example
 * const inscrito = await confirmarInscripcion(evento);   // true si dijo "Sí, voy a ir" y C# lo guardó
 *
 * Usan la misma ventana que los modales de cuenta (sc-modal.js); si C# responde que hace falta sesión,
 * la modal cambia a "Inicia sesión para continuar" sin cerrarse.
 */

import { html } from '../../nucleo/componente.js';
import { invocar } from '../../nucleo/puente.js';
import { icono } from '../atomos.js';
import { mostrarModal, cerrarModal } from '../sc-modal.js';
import { abrirRequiereSesion } from './cuenta.js';
import { fechaLarga } from '../../utilidades/formato.js';

let siguienteTitulo = 1;

/** Caja gris con el título, la fecha escrita completa y el lugar del evento ("Resumen" en Figma). */
function resumen(e) {
  const lugar = e.lugarNombre ?? e.direccion;
  return html`
    <div class="modal__resumen">
      <p class="t-body-fuerte">${e.titulo}</p>
      <p class="modal__resumen-dato t-small texto-2">${icono('calendario-14', 14)}<span>${fechaLarga(e)}</span></p>
      ${lugar && html`<p class="modal__resumen-dato t-small texto-2">${icono('ubicacion', 14)}<span>${lugar}</span></p>`}
    </div>`;
}

/**
 * Pinta una de las dos modales y resuelve con el estado de la inscripción al cerrarse.
 * @param {object} p
 * @param {boolean} p.estadoInicial Lo que se devuelve si el usuario cierra sin confirmar.
 * @param {() => Promise<boolean>} p.alConfirmar Llama a C# y devuelve el estado nuevo.
 */
function abrir({ evento, titulo, texto, insignia, peligro, confirmar, iconoConfirmar, quedarse, estadoInicial, alConfirmar }) {
  const id = `modal-inscripcion-${siguienteTitulo++}`;
  return new Promise((resolver) => {
    let estado = estadoInicial;
    const raiz = mostrarModal(html`
      <div class="modal__icono${peligro ? ' modal__icono--peligro' : ''}">${icono(insignia, 24)}</div>
      <header class="modal__encabezado">
        <h2 class="modal__titulo t-h2" id="${id}">${titulo}</h2>
        <p class="t-body texto-2">${texto}</p>
      </header>
      ${resumen(evento)}
      <p class="modal__aviso" role="alert" hidden></p>
      <div class="modal__botones">
        <button type="button" class="boton boton--${peligro ? 'peligro' : 'primario'} boton--m modal__ancho" data-confirmar>
          ${iconoConfirmar && icono(iconoConfirmar, 18)}<span>${confirmar}</span>
        </button>
        <button type="button" class="boton boton--secundario boton--m modal__ancho" data-cerrar-modal><span>${quedarse}</span></button>
      </div>`, { titulo: id, alCerrar: () => resolver(estado) });

    const boton = raiz.querySelector('[data-confirmar]');
    const aviso = raiz.querySelector('.modal__aviso');
    boton.addEventListener('click', async () => {
      aviso.hidden = true;
      boton.disabled = true;
      boton.classList.add('boton--ocupado');
      try {
        estado = await alConfirmar();
        cerrarModal();
      } catch (error) {
        if (error.codigo === 'requiere-sesion') {
          abrirRequiereSesion();          // misma ventana: el alCerrar de arriba ya no aplica
          resolver(estadoInicial);
          return;
        }
        aviso.textContent = error.message;   // texto de C# (o del trigger) tal cual
        aviso.hidden = false;
      } finally {
        boton.disabled = false;
        boton.classList.remove('boton--ocupado');
      }
    });
  });
}

/**
 * "Modal / Confirmar inscripción" (¿Vas a ir?).
 * @param {object} evento EventoTarjeta
 * @returns {Promise<boolean>} true si quedó inscrito.
 */
export function confirmarInscripcion(evento) {
  return abrir({
    evento,
    titulo: '¿Vas a ir?',
    texto: 'Lo guardaremos en Mis planes y te avisaremos si hay cambios.',
    insignia: 'calendario-check-cielo',
    confirmar: 'Sí, voy a ir',
    iconoConfirmar: 'check-circulo',
    quedarse: 'Ahora no',
    estadoInicial: false,
    alConfirmar: () => invocar('inscripcion.inscribir', { id: evento.id }),
  });
}

/**
 * "Modal / Cancelar inscripción" (¿Cancelar tu inscripción?).
 * @param {object} evento EventoTarjeta
 * @returns {Promise<boolean>} true si sigue inscrito ("Mantener mi lugar" o cerrar).
 */
export function confirmarCancelacion(evento) {
  return abrir({
    evento,
    titulo: '¿Cancelar tu inscripción?',
    texto: 'Se quitará de Mis planes. Puedes volver a inscribirte después.',
    insignia: 'calendario-x-error',
    peligro: true,
    confirmar: 'Cancelar inscripción',
    quedarse: 'Mantener mi lugar',
    estadoInicial: true,
    alConfirmar: () => invocar('inscripcion.cancelar', { id: evento.id }),
  });
}
