/**
 * @file Comunicación con la app de escritorio (C#) a través de WebView2.
 *
 * La página manda `{ id, accion, datos }` con `chrome.webview.postMessage` y C# responde
 * `{ id, ok, datos }` o `{ id, ok: false, error: { codigo, mensaje } }` (ver Puente/PuenteWeb.cs).
 *
 * Si la página se abre en un navegador normal (sin WebView2), se usan los datos de
 * `contenido/datos-demo.js`. Así se puede diseñar y probar la interfaz sin la BD.
 */

import { responderDemo } from '../contenido/datos-demo.js';

const webview = window.chrome?.webview;
const TIEMPO_MAXIMO_MS = 15000;

/** true cuando la página corre dentro de la app de escritorio. */
export const enApp = Boolean(webview);

/** Error que viene de C#. `codigo`: 'sin-conexion', 'requiere-sesion', 'regla-negocio', 'datos-invalidos', 'duplicado', 'interno'. */
export class ErrorPuente extends Error {
  constructor(codigo, mensaje) {
    super(mensaje);
    this.codigo = codigo;
  }
}

const pendientes = new Map();
let siguienteId = 1;

if (webview) {
  webview.addEventListener('message', ({ data }) => {
    const pendiente = pendientes.get(data?.id);
    if (!pendiente) return;
    pendientes.delete(data.id);
    clearTimeout(pendiente.temporizador);
    if (data.ok) pendiente.resolver(data.datos);
    else pendiente.rechazar(new ErrorPuente(data.error?.codigo ?? 'interno', data.error?.mensaje ?? 'Algo salió mal.'));
  });
}

/**
 * Pide algo a C# y espera la respuesta.
 * @param {string} accion Nombre registrado en C#, p. ej. 'landing.obtener'.
 * @param {object} [datos]
 * @returns {Promise<any>}
 */
export function invocar(accion, datos = null) {
  if (!webview) return responderDemo(accion, datos);

  return new Promise((resolver, rechazar) => {
    const id = siguienteId++;
    const temporizador = setTimeout(() => {
      pendientes.delete(id);
      rechazar(new ErrorPuente('tiempo-agotado', 'La app tardó demasiado en responder.'));
    }, TIEMPO_MAXIMO_MS);

    pendientes.set(id, { resolver, rechazar, temporizador });
    webview.postMessage({ id, accion, datos });
  });
}
