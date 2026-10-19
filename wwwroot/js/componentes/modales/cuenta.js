/**
 * @file Modales de cuenta (WBS 4.2). Frames de Figma, página Rediseño:
 *
 * | Figma                                   | Función                    |
 * |-----------------------------------------|----------------------------|
 * | Modal / Iniciar sesión                  | `abrirIniciarSesion()`     |
 * | Modal / Crear cuenta                    | `abrirCrearCuenta()`       |
 * | Modal / Recuperar contraseña            | `abrirRecuperarContrasena()` |
 * | Modal / Revisa tu correo                | (sigue después de Recuperar) |
 * | Modal / Contraseña nueva                | (sigue al abrir el enlace) |
 * | Modal / Inicia sesión para continuar    | `abrirRequiereSesion()`    |
 *
 * Todos comparten la misma ventana (sc-modal.js), así que pasar de uno a otro no la cierra.
 * Los datos van a C# (Controladores/ControladorCuenta.cs) → BackendLogica → tablas `user` y
 * `password_reset`.
 *
 * Al iniciar sesión o crear la cuenta se lanza en `document` el evento `sc:sesion` con
 * `detail = { usuario, nuevo }`; main.js lo usa para cambiar el navbar a "Logueado".
 */

import { html } from '../../nucleo/componente.js';
import { invocar } from '../../nucleo/puente.js';
import { boton, campo, icono, marcarError } from '../atomos.js';
import { mostrarModal, cerrarModal } from '../sc-modal.js';

// ---------------------------------------------------------------------
// Reglas (las mismas de BackendLogica/Seguridad: Validar y Contrasenas, y del CHECK ck_user_username)
// ---------------------------------------------------------------------

export const REGLAS = {
  nombreUsuario: /^[A-Za-z0-9_.]{3,30}$/,
  correo: /^[^@\s]+@[^@\s]+\.[^@\s]+$/,
};

export const MENSAJES = {
  nombreUsuario: 'El nombre de usuario debe tener de 3 a 30 caracteres: letras, números, punto o guion bajo.',
  correo: 'Escribe un correo válido.',
  contrasenaCorta: 'La contraseña debe tener al menos 8 caracteres.',
  contrasenaDebil: 'La contraseña debe combinar letras y números.',
  noCoinciden: 'Las contraseñas no coinciden.',
};

/** @returns {string|null} Mensaje de error de la contraseña, o null si está bien. */
export function errorContrasena(valor) {
  if (valor.length < 8) return MENSAJES.contrasenaCorta;
  if (!/\p{L}/u.test(valor) || !/\d/.test(valor)) return MENSAJES.contrasenaDebil;
  return null;
}

// ---------------------------------------------------------------------
// Piezas compartidas
// ---------------------------------------------------------------------

let siguienteTitulo = 1;

/** Encabezado: icono en círculo (opcional), título y texto. */
function encabezado({ titulo, texto, icono: nombreIcono }) {
  const id = `modal-titulo-${siguienteTitulo++}`;
  return {
    id,
    html: html`
      ${nombreIcono && html`<div class="modal__icono">${icono(nombreIcono, 24)}</div>`}
      <header class="modal__encabezado">
        <h2 class="modal__titulo t-h2" id="${id}">${titulo}</h2>
        ${texto && html`<p class="t-body texto-2">${texto}</p>`}
      </header>`,
  };
}

/** Botones de Google / Apple del diseño. Todavía no hay acceso con cuentas externas. */
function botonesSociales(proveedores) {
  return html`
    <div class="modal__sociales">
      ${proveedores.map((p) => html`
        <button type="button" class="boton boton--secundario boton--m modal__ancho" data-proveedor="${p}"><span>Continuar con ${p}</span></button>`)}
    </div>
    <p class="modal__separador t-caption">o con tu correo</p>`;
}

/** Aviso dentro de la modal (los toasts quedan detrás del <dialog>). */
const avisoVacio = html`<p class="modal__aviso" role="alert" hidden></p>`;

function mostrarAviso(raiz, mensaje, tipo = 'error') {
  const aviso = raiz.querySelector('.modal__aviso');
  if (!aviso) return;
  aviso.textContent = mensaje ?? '';
  aviso.hidden = !mensaje;
  aviso.classList.toggle('modal__aviso--info', tipo === 'info');
}

/** Enlace subrayado que lleva a otro modal (`data-ir` = nombre en `PANTALLAS`). */
function enlace(texto, ir) {
  return html`<button type="button" class="modal__enlace t-small-fuerte" data-ir="${ir}">${texto}</button>`;
}

/**
 * Pinta el contenido en la modal y enlaza lo común: enlaces `data-ir`, botones sociales
 * y el envío del formulario (con el botón en "ocupado" mientras C# responde).
 */
function pintar(contenido, idTitulo, alEnviar) {
  const raiz = mostrarModal(contenido, { titulo: idTitulo });

  raiz.querySelectorAll('[data-ir]').forEach((b) => b.addEventListener('click', () => {
    const correo = raiz.querySelector('input[name=correo], input[name=identificador]')?.value.trim();
    PANTALLAS[b.dataset.ir]({ correo });
  }));

  raiz.querySelectorAll('[data-proveedor]').forEach((b) => b.addEventListener('click', () =>
    mostrarAviso(raiz, `El acceso con ${b.dataset.proveedor} llega en la siguiente versión. Por ahora usa tu correo.`, 'info')));

  const form = raiz.querySelector('form');
  if (form && alEnviar) {
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      mostrarAviso(raiz, null);
      const datos = Object.fromEntries(new FormData(form));
      const enviar = form.querySelector('[type=submit]');
      enviar.disabled = true;
      enviar.classList.add('boton--ocupado');
      try {
        await alEnviar(datos, form);
      } catch (error) {
        mostrarError(raiz, form, error);
      } finally {
        enviar.disabled = false;
        enviar.classList.remove('boton--ocupado');
      }
    });
  }
  return raiz;
}

/** Error de validación en el cliente: se pinta igual que uno de C#. */
class ErrorCampo extends Error {
  constructor(campo, mensaje) {
    super(mensaje);
    this.campo = campo;
  }
}

/**
 * Pinta el error en su campo. Los de C# no dicen el campo, así que se deduce del texto
 * (son los mensajes de BackendLogica: "Ese correo ya está registrado.", "El nombre de usuario...").
 */
function mostrarError(raiz, form, error) {
  const nombre = error.campo ?? campoDeError(form, error.message ?? '');
  const entrada = nombre && form.elements[nombre];
  if (entrada) {
    marcarError(entrada, error.message);
    entrada.focus();
  } else {
    mostrarAviso(raiz, error.message || 'Algo salió mal. Intenta de nuevo.');
  }
}

function campoDeError(form, mensaje) {
  const existe = (n) => (form.elements[n] ? n : null);
  if (/no coinciden/i.test(mensaje)) return existe('confirmacion');
  if (/incorrect/i.test(mensaje)) return existe('contrasena');
  if (/contraseña/i.test(mensaje)) return existe('contrasena');
  if (/nombre de usuario/i.test(mensaje)) return existe('nombreUsuario');
  if (/correo/i.test(mensaje)) return existe('correo') ?? existe('identificador');
  return null;
}

function sesionIniciada(usuario, nuevo) {
  cerrarModal();
  document.dispatchEvent(new CustomEvent('sc:sesion', { detail: { usuario, nuevo } }));
}

// ---------------------------------------------------------------------
// Modal / Iniciar sesión
// ---------------------------------------------------------------------

/** @param {{correo?: string, aviso?: string}} [opciones] Correo para rellenar y aviso opcional arriba del formulario. */
export function abrirIniciarSesion({ correo = '', aviso } = {}) {
  const cab = encabezado({ titulo: 'Inicia sesión', texto: 'Guarda eventos, inscríbete y deja reseñas.' });
  const raiz = pintar(html`
    ${cab.html}
    ${botonesSociales(['Google', 'Apple'])}
    <form class="modal__formulario" novalidate>
      ${avisoVacio}
      ${campo({ nombre: 'identificador', etiqueta: 'Correo electrónico', icono: 'correo', valor: correo, requerido: true, autocompletar: 'username' })}
      ${campo({ nombre: 'contrasena', etiqueta: 'Contraseña', tipo: 'password', icono: 'candado', requerido: true, autocompletar: 'current-password' })}
      <div>${enlace('¿Olvidaste tu contraseña?', 'recuperar')}</div>
      ${boton({ texto: 'Iniciar sesión', variante: 'oscuro', tipo: 'submit', clase: 'modal__ancho' })}
    </form>
    <p class="modal__pie t-small texto-2">¿No tienes cuenta? ${enlace('Crear cuenta', 'crear')}</p>`,
  cab.id, async (datos) => {
    if (!datos.identificador.trim()) throw new ErrorCampo('identificador', 'Escribe tu correo.');
    if (!datos.contrasena) throw new ErrorCampo('contrasena', 'Escribe tu contraseña.');
    const usuario = await invocar('cuenta.iniciarSesion', datos);
    sesionIniciada(usuario, false);
  });

  if (aviso) mostrarAviso(raiz, aviso, 'info');
  // Con el correo ya escrito, el foco va directo a la contraseña.
  if (correo) raiz.querySelector('input[name=contrasena]').focus();
}

// ---------------------------------------------------------------------
// Modal / Crear cuenta
// ---------------------------------------------------------------------

export function abrirCrearCuenta({ correo = '' } = {}) {
  const cab = encabezado({ titulo: 'Crea tu cuenta', texto: 'Es gratis y toma menos de un minuto.' });
  // El correo solo se rellena si lo que traía era un correo (no un nombre de usuario).
  const correoInicial = REGLAS.correo.test(correo) ? correo : '';
  pintar(html`
    ${cab.html}
    ${botonesSociales(['Google'])}
    <form class="modal__formulario" novalidate>
      ${avisoVacio}
      ${campo({ nombre: 'nombreUsuario', etiqueta: 'Nombre de usuario', icono: 'usuario', requerido: true, autocompletar: 'username', maximo: 30 })}
      ${campo({ nombre: 'correo', etiqueta: 'Correo electrónico', tipo: 'email', icono: 'correo', valor: correoInicial, requerido: true, autocompletar: 'email', maximo: 254 })}
      ${campo({ nombre: 'contrasena', etiqueta: 'Contraseña', tipo: 'password', icono: 'candado', requerido: true, autocompletar: 'new-password' })}
      ${campo({ nombre: 'confirmacion', etiqueta: 'Confirma tu contraseña', tipo: 'password', icono: 'candado', requerido: true, autocompletar: 'new-password' })}
      ${boton({ texto: 'Crear cuenta', variante: 'oscuro', tipo: 'submit', clase: 'modal__ancho' })}
    </form>
    <p class="modal__pie t-small texto-2">¿Ya tienes cuenta? ${enlace('Inicia sesión', 'iniciar')}</p>`,
  cab.id, async (datos) => {
    datos.nombreUsuario = datos.nombreUsuario.trim();
    datos.correo = datos.correo.trim();
    if (!REGLAS.nombreUsuario.test(datos.nombreUsuario)) throw new ErrorCampo('nombreUsuario', MENSAJES.nombreUsuario);
    if (!REGLAS.correo.test(datos.correo)) throw new ErrorCampo('correo', MENSAJES.correo);
    const debil = errorContrasena(datos.contrasena);
    if (debil) throw new ErrorCampo('contrasena', debil);
    if (datos.contrasena !== datos.confirmacion) throw new ErrorCampo('confirmacion', MENSAJES.noCoinciden);

    const usuario = await invocar('cuenta.registrar', datos);
    sesionIniciada(usuario, true);
  });
}

// ---------------------------------------------------------------------
// Modal / Recuperar contraseña → Revisa tu correo → Contraseña nueva
// ---------------------------------------------------------------------

export function abrirRecuperarContrasena({ correo = '' } = {}) {
  const cab = encabezado({
    titulo: 'Recupera tu contraseña',
    texto: 'Escribe el correo con el que te registraste y te enviaremos un enlace.',
    icono: 'llave-cielo',
  });
  const correoInicial = REGLAS.correo.test(correo) ? correo : '';
  pintar(html`
    ${cab.html}
    <form class="modal__formulario" novalidate>
      ${avisoVacio}
      ${campo({ nombre: 'correo', etiqueta: 'Correo electrónico', tipo: 'email', icono: 'correo', valor: correoInicial, requerido: true, autocompletar: 'email' })}
      ${boton({ texto: 'Enviar enlace', variante: 'oscuro', tipo: 'submit', clase: 'modal__ancho' })}
    </form>
    <div>${enlace('Volver a iniciar sesión', 'iniciar')}</div>`,
  cab.id, async (datos) => {
    const correoLimpio = datos.correo.trim();
    if (!REGLAS.correo.test(correoLimpio)) throw new ErrorCampo('correo', MENSAJES.correo);
    // C# responde igual exista o no la cuenta (no revela qué correos están registrados).
    await invocar('cuenta.recuperar', { correo: correoLimpio });
    abrirRevisaTuCorreo(correoLimpio);
  });
}

function abrirRevisaTuCorreo(correo) {
  const cab = encabezado({
    titulo: 'Revisa tu correo',
    texto: 'Te enviamos un enlace para crear una contraseña nueva. Puede tardar unos minutos.',
    icono: 'correo-cielo',
  });
  // "Abrir enlace del correo": mientras el proyecto no tenga servidor de correo, C# guarda el enlace
  // y aquí se abre directo (ver Servicios/CorreoSimulado.cs).
  const raiz = pintar(html`
    ${cab.html}
    ${avisoVacio}
    ${boton({ texto: 'Abrir enlace del correo', variante: 'oscuro', clase: 'modal__ancho modal__abrir-enlace' })}
    <div>${enlace('Volver a iniciar sesión', 'iniciar')}</div>`, cab.id);

  const abrir = raiz.querySelector('.modal__abrir-enlace');
  abrir.addEventListener('click', async () => {
    abrir.disabled = true;
    abrir.classList.add('boton--ocupado');
    try {
      const { token } = await invocar('cuenta.abrirEnlace');
      abrirContrasenaNueva(token, correo);
    } catch (error) {
      mostrarAviso(raiz, error.message);
      abrir.disabled = false;
      abrir.classList.remove('boton--ocupado');
    }
  });
}

/**
 * @param {string} token Token de un solo uso del enlace (C# solo guarda su SHA-256 en password_reset).
 * @param {string} [correo] Para rellenar "Inicia sesión" al terminar.
 */
export function abrirContrasenaNueva(token, correo = '') {
  const cab = encabezado({ titulo: 'Crea una contraseña nueva', texto: 'Usa al menos 8 caracteres, con letras y números.' });
  pintar(html`
    ${cab.html}
    <form class="modal__formulario" novalidate>
      ${avisoVacio}
      <input type="hidden" name="token" value="${token}">
      ${campo({ nombre: 'contrasena', etiqueta: 'Contraseña nueva', tipo: 'password', icono: 'candado', requerido: true, autocompletar: 'new-password' })}
      ${campo({ nombre: 'confirmacion', etiqueta: 'Confirma tu contraseña', tipo: 'password', icono: 'candado', requerido: true, autocompletar: 'new-password' })}
      ${boton({ texto: 'Guardar contraseña', variante: 'oscuro', tipo: 'submit', clase: 'modal__ancho' })}
    </form>`,
  cab.id, async (datos) => {
    const debil = errorContrasena(datos.contrasena);
    if (debil) throw new ErrorCampo('contrasena', debil);
    if (datos.contrasena !== datos.confirmacion) throw new ErrorCampo('confirmacion', MENSAJES.noCoinciden);

    try {
      await invocar('cuenta.restablecer', datos);
    } catch (error) {
      // Enlace vencido o ya usado: no es culpa de ningún campo, se avisa arriba.
      if (error.codigo === 'regla-negocio') throw new ErrorCampo('aviso', error.message);
      throw error;
    }
    abrirIniciarSesion({ correo, aviso: 'Listo, tu contraseña cambió. Ya puedes iniciar sesión.' });
  });
}

// ---------------------------------------------------------------------
// Modal / Inicia sesión para continuar
// ---------------------------------------------------------------------

/**
 * Se abre cuando un visitante intenta algo que necesita cuenta (inscribirse, favoritos, reseñas...).
 * @param {object} [opciones]
 * @param {string} [opciones.texto] Para cambiar el texto del diseño si hace falta.
 */
export function abrirRequiereSesion({ texto = 'Necesitas una cuenta para inscribirte, guardar favoritos y escribir reseñas.' } = {}) {
  const cab = encabezado({ titulo: 'Inicia sesión para continuar', texto, icono: 'candado-cielo' });
  const raiz = pintar(html`
    ${cab.html}
    <div class="modal__botones">
      <button type="button" class="boton boton--oscuro boton--m modal__ancho" data-ir="iniciar"><span>Iniciar sesión</span></button>
      <button type="button" class="boton boton--secundario boton--m modal__ancho" data-ir="crear"><span>Crear cuenta</span></button>
    </div>
    <div><button type="button" class="modal__enlace modal__enlace--suave t-small-fuerte" data-cerrar-modal>Ahora no</button></div>`, cab.id);
  return raiz;
}

/** A dónde lleva cada enlace `data-ir`. */
const PANTALLAS = {
  iniciar: (o) => abrirIniciarSesion(o),
  crear: (o) => abrirCrearCuenta(o),
  recuperar: (o) => abrirRecuperarContrasena(o),
};
