/**
 * @file Punto de entrada de Mi perfil (perfil.html), frame "Mi perfil" de Figma.
 *
 * - Columna izquierda: avatar (foto o inicial), nombre, correo y "Cambiar foto".
 * - "Datos de la cuenta": nombre de usuario y correo → `perfil.actualizar` y toast "Cambios guardados".
 * - "Contraseña": actual y nueva → `perfil.contrasena`.
 * - "Eliminar cuenta": abre "Modal / Eliminar cuenta" (modales/eliminar-cuenta.js) → `perfil.eliminar`.
 *
 * Se pinta con el usuario de la sesión (evento `sc:usuario` de nucleo/navegacion.js). Sin sesión
 * pide iniciarla; si la cierra estando aquí, vuelve al Home.
 */

import './componentes/secciones/sc-navbar.js';

import { html } from './nucleo/componente.js';
import { invocar, enApp } from './nucleo/puente.js';
import { atenderAcciones, cargarSesion, proximamente, actualizarUsuario, irA } from './nucleo/navegacion.js';
import { boton, campo, icono, marcarError } from './componentes/atomos.js';
import { mostrarToast } from './componentes/sc-toast.js';
import { abrirRequiereSesion, REGLAS, MENSAJES, errorContrasena } from './componentes/modales/cuenta.js';
import { confirmarEliminarCuenta } from './componentes/modales/eliminar-cuenta.js';

const contenedor = document.getElementById('perfil');
/** true después de pintar a un usuario: si luego llega null es que cerró sesión aquí. */
let huboSesion = false;
/** Al eliminar la cuenta se espera a que se lea el aviso antes de ir al Home. */
let cuentaEliminada = false;

// ---------------------------------------------------------------------
// Plantilla
// ---------------------------------------------------------------------

function avatar(usuario) {
  const nombre = usuario.nombreUsuario ?? '';
  return html`
    <span class="perfil__avatar" aria-hidden="true">
      ${usuario.avatarUrl
        ? html`<img src="${usuario.avatarUrl}" alt="" width="112" height="112">`
        : nombre.charAt(0).toUpperCase()}
    </span>`;
}

function plantilla(usuario) {
  return html`
    <section class="perfil__foto" aria-label="Foto de perfil">
      ${avatar(usuario)}
      <p class="t-h2 perfil__nombre">${usuario.nombreUsuario}</p>
      <p class="t-small texto-2 perfil__correo">${usuario.correo}</p>
      ${boton({ texto: 'Cambiar foto', variante: 'secundario', tamano: 's', icono: 'camara', accion: 'cambiar-foto' })}
    </section>

    <div class="perfil__formulario">
      <form class="perfil__bloque" id="form-datos" novalidate>
        <h2 class="t-h3">Datos de la cuenta</h2>
        ${campo({ nombre: 'nombreUsuario', etiqueta: 'Nombre de usuario', etiquetaVisible: true, icono: 'persona', valor: usuario.nombreUsuario, requerido: true, autocompletar: 'username', maximo: 30 })}
        ${campo({ nombre: 'correo', etiqueta: 'Correo electrónico', etiquetaVisible: true, tipo: 'email', icono: 'correo', valor: usuario.correo, requerido: true, autocompletar: 'email', maximo: 254 })}
        ${boton({ texto: 'Guardar cambios', variante: 'oscuro', tipo: 'submit' })}
      </form>

      <hr class="perfil__divisor">

      <form class="perfil__bloque" id="form-contrasena" novalidate>
        <h2 class="t-h3">Contraseña</h2>
        <input type="text" name="usuario" value="${usuario.nombreUsuario}" autocomplete="username" hidden>
        ${campo({ nombre: 'actual', etiqueta: 'Contraseña actual', etiquetaVisible: true, tipo: 'password', icono: 'candado-16', placeholder: '••••••••', requerido: true, autocompletar: 'current-password' })}
        ${campo({ nombre: 'nueva', etiqueta: 'Contraseña nueva', etiquetaVisible: true, tipo: 'password', icono: 'llave', placeholder: 'Mínimo 8 caracteres', requerido: true, autocompletar: 'new-password' })}
        ${boton({ texto: 'Actualizar contraseña', variante: 'secundario', tipo: 'submit' })}
      </form>

      <hr class="perfil__divisor">

      <section class="perfil__bloque" aria-labelledby="titulo-eliminar">
        <h2 class="t-h3" id="titulo-eliminar">Eliminar cuenta</h2>
        <p class="t-small texto-2">Se borrarán tu perfil, tus planes, favoritos y reseñas.</p>
        ${boton({ texto: 'Eliminar mi cuenta', variante: 'peligro', icono: 'bote-blanco', accion: 'eliminar-cuenta' })}
      </section>
    </div>`;
}

function plantillaSinSesion() {
  return html`
    <div class="perfil__sin-sesion">
      <p class="t-body texto-2">Inicia sesión para ver y editar tu perfil.</p>
      ${boton({ texto: 'Iniciar sesión', variante: 'oscuro', accion: 'iniciar-sesion' })}
    </div>`;
}

// ---------------------------------------------------------------------
// Formularios
// ---------------------------------------------------------------------

/** El mensaje de C# no dice el campo: se deduce del texto (mensajes de BackendLogica y ControladorPerfil). */
function campoDeError(form, mensaje) {
  const existe = (n) => (form.elements[n] ? n : null);
  if (/actual/i.test(mensaje)) return existe('actual');
  if (/contraseña/i.test(mensaje)) return existe('nueva');
  if (/nombre de usuario/i.test(mensaje)) return existe('nombreUsuario');
  if (/correo/i.test(mensaje)) return existe('correo');
  return null;
}

/**
 * Envía un formulario con el botón en "ocupado" y pinta el error en su campo (o en un toast).
 * @param {HTMLFormElement} form
 * @param {(datos: object) => (string[]|null)} validar Devuelve [campo, mensaje] si algo está mal.
 * @param {(datos: object) => Promise<void>} enviar
 */
function enlazar(form, validar, enviar) {
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    form.querySelectorAll('.campo--error').forEach((c) => marcarError(c, null));
    const datos = Object.fromEntries(new FormData(form));
    const error = validar(datos);
    if (error) {
      marcarError(form.elements[error[0]], error[1]);
      form.elements[error[0]].focus();
      return;
    }
    const boton = form.querySelector('[type=submit]');
    boton.disabled = true;
    boton.classList.add('boton--ocupado');
    try {
      await enviar(datos);
    } catch (err) {
      const nombre = campoDeError(form, err.message ?? '');
      if (nombre) {
        marcarError(form.elements[nombre], err.message);   // texto de C# tal cual
        form.elements[nombre].focus();
      } else if (err.codigo === 'requiere-sesion') {
        abrirRequiereSesion();
      } else {
        mostrarToast(err.message || 'Algo salió mal. Intenta de nuevo.', { tipo: 'error' });
      }
    } finally {
      boton.disabled = false;
      boton.classList.remove('boton--ocupado');
    }
  });
}

function enlazarFormularios() {
  // Mismas reglas que BackendLogica (Validar y Contrasenas); C# las vuelve a revisar.
  enlazar(contenedor.querySelector('#form-datos'), (d) => {
    if (!REGLAS.nombreUsuario.test(d.nombreUsuario.trim())) return ['nombreUsuario', MENSAJES.nombreUsuario];
    if (!REGLAS.correo.test(d.correo.trim())) return ['correo', MENSAJES.correo];
    return null;
  }, async (d) => {
    const usuario = await invocar('perfil.actualizar', { nombreUsuario: d.nombreUsuario.trim(), correo: d.correo.trim() });
    actualizarUsuario(usuario);   // navbar, menú de cuenta y esta pantalla
    mostrarToast('Cambios guardados');
  });

  const formContrasena = contenedor.querySelector('#form-contrasena');
  enlazar(formContrasena, (d) => {
    if (!d.actual) return ['actual', 'Escribe tu contraseña actual.'];
    const debil = errorContrasena(d.nueva);
    return debil ? ['nueva', debil] : null;
  }, async (d) => {
    await invocar('perfil.contrasena', { actual: d.actual, nueva: d.nueva });
    formContrasena.reset();
    mostrarToast('Contraseña actualizada');
  });
}

async function eliminarCuenta() {
  if (!await confirmarEliminarCuenta()) return;
  huboSesion = false;
  cuentaEliminada = true;
  actualizarUsuario(null);
  mostrarToast('Tu cuenta se eliminó. ¡Gracias por usar SuraChih!');
  setTimeout(() => irA('home'), 1600);   // que alcance a leerse el aviso
}

// ---------------------------------------------------------------------
// Sesión
// ---------------------------------------------------------------------

document.addEventListener('sc:usuario', ({ detail }) => {
  if (detail.usuario) {
    huboSesion = true;
    contenedor.innerHTML = String(plantilla(detail.usuario));
    enlazarFormularios();
  } else if (huboSesion) {
    irA('home');   // cerró sesión estando en su perfil
  } else if (cuentaEliminada) {
    contenedor.innerHTML = '';
  } else {
    contenedor.innerHTML = String(plantillaSinSesion());
    abrirRequiereSesion({ texto: 'Inicia sesión para ver y editar tu perfil.' });
  }
});

// ---------------------------------------------------------------------
// Inicio
// ---------------------------------------------------------------------

document.documentElement.dataset.entorno = enApp ? 'app' : 'navegador';
atenderAcciones({
  'cambiar-foto': () => proximamente('Cambiar la foto'),
  'eliminar-cuenta': () => eliminarCuenta(),
});
cargarSesion(document.querySelector('sc-navbar'));
