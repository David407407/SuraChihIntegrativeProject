/**
 * @file "Modal / Eliminar cuenta" de Figma: insignia roja, "¿Eliminar tu cuenta?", contraseña para
 * confirmar, "Eliminar mi cuenta" (rojo) y "Cancelar".
 *
 * La abre el botón "Eliminar mi cuenta" de Mi perfil (perfil.js). C# (`perfil.eliminar`,
 * Controladores/ControladorPerfil.cs) revisa la contraseña, desactiva la cuenta (`is_active = FALSE`,
 * no se borra) y cierra la sesión.
 *
 * @example
 * const eliminada = await confirmarEliminarCuenta();   // true si C# la desactivó
 */

import { html } from '../../nucleo/componente.js';
import { invocar } from '../../nucleo/puente.js';
import { campo, icono, marcarError } from '../atomos.js';
import { mostrarModal, cerrarModal } from '../sc-modal.js';

/** @returns {Promise<boolean>} true si la cuenta quedó eliminada; false si se cerró o canceló. */
export function confirmarEliminarCuenta() {
  return new Promise((resolver) => {
    let eliminada = false;
    const raiz = mostrarModal(html`
      <div class="modal__icono modal__icono--peligro">${icono('alerta-error', 24)}</div>
      <header class="modal__encabezado">
        <h2 class="modal__titulo t-h2" id="modal-eliminar-cuenta">¿Eliminar tu cuenta?</h2>
        <p class="t-body texto-2">Se borrarán tu perfil, tus planes, favoritos y reseñas. No se puede deshacer.</p>
      </header>
      <form class="modal__formulario" novalidate>
        ${campo({ nombre: 'contrasena', etiqueta: 'Escribe tu contraseña', tipo: 'password', icono: 'candado-16', requerido: true, autocompletar: 'current-password' })}
        <p class="modal__aviso" role="alert" hidden></p>
        <div class="modal__botones">
          <button type="submit" class="boton boton--peligro boton--m modal__ancho">${icono('bote-blanco', 18)}<span>Eliminar mi cuenta</span></button>
          <button type="button" class="boton boton--secundario boton--m modal__ancho" data-cerrar-modal><span>Cancelar</span></button>
        </div>
      </form>`, { titulo: 'modal-eliminar-cuenta', alCerrar: () => resolver(eliminada) });

    const form = raiz.querySelector('form');
    const entrada = form.elements.contrasena;
    const aviso = raiz.querySelector('.modal__aviso');
    const enviar = form.querySelector('[type=submit]');

    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      aviso.hidden = true;
      if (!entrada.value) {
        marcarError(entrada, 'Escribe tu contraseña para confirmar.');
        entrada.focus();
        return;
      }
      enviar.disabled = true;
      enviar.classList.add('boton--ocupado');
      try {
        await invocar('perfil.eliminar', { contrasena: entrada.value });
        eliminada = true;
        cerrarModal();
      } catch (error) {
        if (/contraseña/i.test(error.message ?? '')) {
          marcarError(entrada, error.message);   // texto de C# tal cual
          entrada.focus();
        } else {
          aviso.textContent = error.message || 'Algo salió mal. Intenta de nuevo.';
          aviso.hidden = false;
        }
      } finally {
        enviar.disabled = false;
        enviar.classList.remove('boton--ocupado');
      }
    });
  });
}
