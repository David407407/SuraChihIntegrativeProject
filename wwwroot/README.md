# Interfaz (wwwroot)

HTML/CSS/JS sin frameworks ni compilación. WebView2 la sirve desde `https://app.surachih/`.

## Probar sin la app de escritorio

```bash
python3 -m http.server 4321 --directory wwwroot
```

Abre `http://localhost:4321`. Fuera de WebView2, `js/nucleo/puente.js` responde con
`js/contenido/datos-demo.js` (mismo formato que manda C#).

## Estructura

```
index.html                 la landing: una etiqueta <sc-...> por sección, en el orden de Figma
css/
  tokens.css               colores, tipografías, radios y sombras (variables de Figma)
  base.css                 reset, clases de texto (.t-h1, .t-body...), .contenedor, animación de aparición
  estilos.css              importa todo en orden
  componentes/*.css        un archivo por grupo de componentes
js/
  main.js                  carga datos, conecta secciones y atiende acciones (data-accion)
  nucleo/componente.js     clase base Componente + plantillas html`` con escape automático
  nucleo/puente.js         invocar('accion', datos) → C#
  utilidades/              formato (fechas, precios, horarios), cloudinary, animaciones
  contenido/landing.js     textos fijos de la landing
  contenido/datos-demo.js  datos de ejemplo
  componentes/
    atomos.js              icono(), boton(), campo(), marcarError(), chip(), chipCategoria(), encabezadoSeccion()
    tarjetas.js            tarjetaEvento(), tarjetaLugar(), tarjetaEventoPrincipal(), tarjetaEventoFila()
    sc-imagen.js           <sc-imagen> foto de Cloudinary con carga y relleno
    sc-boton-favorito.js   <sc-boton-favorito> corazón (Vacío / Hover / Lleno)
    sc-chip-categoria.js   <sc-chip-categoria> filtro por categoría (Normal / Activo)
    sc-toast.js            mostrarToast()
    sc-modal.js            mostrarModal() / cerrarModal(): ventana modal base (<dialog>)
    menu-cuenta.js         alternarMenuCuenta(): menú bajo el avatar del navbar
    modales/cuenta.js      modales de cuenta: iniciar sesión, crear cuenta, recuperar contraseña...
    secciones/             <sc-navbar>, <sc-hero>, <sc-collage-hero>, secciones fijas y con datos
assets/iconos              SVG exportados de Figma (conservan tamaño y color)
assets/demo                fotos solo para los datos demo
```

## Componentes base (WBS 4.1)

Úsalos en todas las pantallas en vez de hacer tu propia versión. **`componentes.html`** muestra
cada variante con el código para crearla (ábrela con el servidor de "Probar sin la app de escritorio").

| Figma (página Rediseño) | Cómo se usa | Variantes |
|---|---|---|
| Botón | `boton({ texto, variante, tamano, icono, accion })` | `primario` · `oscuro` · `secundario` · `peligro` · `fantasma` × `m` (48px) · `s` (36px). Además `tipo: 'submit'`, `deshabilitado` |
| Campo | `campo({ nombre, etiqueta, tipo, icono, ayuda, error })` | normal · foco · error · deshabilitado; contraseña con "Mostrar". `marcarError(input, mensaje)` para errores de C# |
| Chip categoría | `chipCategoria(etiqueta, { activo, interactivo })` | Normal / Activo. El color sale de `etiqueta.icono` (family, music, run, robot, food). Emite `sc:categoria` |
| Navbar | `<sc-navbar>` con `navbar.datos = { usuario }` | Visitante (`usuario: null`) / Logueado |
| Card evento | `tarjetaEvento(evento, { orden, favorito })` | Normal / Hover. "Fecha por confirmar" y "Precio por confirmar" incluidos |
| Card lugar | `tarjetaLugar(lugar, { orden, favorito })` | Normal / Hover |
| Botón favorito | `<sc-boton-favorito tipo objetivo tamano activo>` | Vacío / Hover / Lleno × `s` (36px) · `m` (40px). Emite `sc:favorito` |

Las cards reciben los records de BackendLogica tal cual (`EventoTarjeta`, `LugarTarjeta`) y van dentro de
`<div class="rejilla-tarjetas">`, que acomoda 4 por fila a 1200px como en Figma.

Ejemplo de un formulario:

```js
html`
  <form id="login">
    ${campo({ nombre: 'identificador', etiqueta: 'Correo o usuario', icono: 'correo', requerido: true })}
    ${campo({ nombre: 'contrasena', etiqueta: 'Contraseña', tipo: 'password' })}
    ${boton({ texto: 'Iniciar sesión', tipo: 'submit' })}
  </form>`;

form.addEventListener('submit', async (e) => {
  e.preventDefault();
  const datos = Object.fromEntries(new FormData(form));
  try { await invocar('sesion.iniciar', datos); }
  catch (error) { marcarError(form.elements.contrasena, error.message); }   // texto de C# tal cual
});
```

## Modales de cuenta + menú de cuenta (WBS 4.2)

| Figma (página Rediseño) | Cómo se abre | Acción de C# (`ControladorCuenta`) |
|---|---|---|
| Modal / Iniciar sesión | `abrirIniciarSesion({ correo, aviso })` · `data-accion="iniciar-sesion"` | `cuenta.iniciarSesion` |
| Modal / Crear cuenta | `abrirCrearCuenta()` · `data-accion="crear-cuenta"` | `cuenta.registrar` |
| Modal / Recuperar contraseña | `abrirRecuperarContrasena()` · "¿Olvidaste tu contraseña?" | `cuenta.recuperar` |
| Modal / Revisa tu correo | sigue solo después de Recuperar | `cuenta.abrirEnlace` (correo simulado) |
| Modal / Contraseña nueva | `abrirContrasenaNueva(token)` · al abrir el enlace | `cuenta.restablecer` |
| Modal / Inicia sesión para continuar | `abrirRequiereSesion()` · acciones que piden cuenta y errores `requiere-sesion` | — |
| Menú de cuenta | `alternarMenuCuenta(avatar, usuario)` · `data-accion="menu-cuenta"` | `sesion.obtener`, `sesion.cerrar` |

- Todos usan la misma ventana (`sc-modal.js`), así que ir de "Inicia sesión" a "Crea tu cuenta" no la cierra.
- Al entrar o crear la cuenta se lanza `sc:sesion` (`detail: { usuario, nuevo }`); `main.js` cambia el navbar a Logueado.
- Las reglas del formulario son las de la BD: usuario de 3 a 30 caracteres con letras, números, punto o guion bajo
  (`ck_user_username`), correo válido y contraseña de 8+ caracteres con letras y números. Se revisan antes de mandar y
  C# las vuelve a revisar; los errores de C# (correo repetido, credenciales...) se pintan en su campo.
- El `<dialog>` queda por encima de los toasts: para avisar algo con la modal abierta se usa `.modal__aviso`.
- **Correo:** el proyecto aún no tiene servidor de correo. `Servicios/CorreoSimulado.cs` guarda el enlace y el botón
  "Abrir enlace del correo" lo abre directo. Cuando haya SMTP solo cambia esa clase (ver sus comentarios).
- "Continuar con Google / Apple" están como en Figma pero todavía no hacen login (muestran un aviso).
- En el navegador (sin la app) funcionan con cuentas demo: `mariana_r`, `mod_ana`, `cafe_dande`, contraseña `Test1234!`.
- Mi perfil, Mis planes, Mis favoritos, Panel de organizador y Moderación del menú aún muestran "llega en la siguiente versión".
  "Panel de organizador" solo aparece a organizadores aprobados y "Moderación" a moderadores.

## Fotos de eventos y lugares (Cloudinary)

Todas las fotos pasan por `<sc-imagen>`:

```js
html`<sc-imagen src="${evento.imagenUrl}" alt="${evento.titulo}" ancho="588" alto="330"></sc-imagen>`
```

- Sin URL (aún no se sube la foto) o si falla: muestra un relleno con el sol de SuraChih.
- Con URL de Cloudinary: pide la versión del tamaño exacto (`f_auto,q_auto,c_fill,w_…,h_…`).

**Cuando suban las fotos reales** solo hay que guardar la URL de Cloudinary en la BD
(`event.main_image_url`, `place.main_image_url`); no hay que tocar la interfaz.

## Crear un componente nuevo

```js
import { Componente, definir, html } from '../nucleo/componente.js';

class MiSeccion extends Componente {
  plantilla(datos) { return html`<section class="seccion">${datos.titulo}</section>`; }
  alPintar() { /* enlazar eventos si hace falta */ }
}
definir('sc-mi-seccion', MiSeccion);
```

- `html```  escapa todo lo interpolado (seguro con datos de usuarios).
- Para navegar, pon `data-accion="nombre"` en el botón y agrega el nombre en `acciones` de `main.js`.
- Para animar la aparición al hacer scroll, agrega `data-revelar` (y `style="--orden:N"` para escalonar).
- Usa siempre las variables de `tokens.css`, no colores sueltos.

## Animaciones del diseño

| Figma | Implementación |
|---|---|
| Collage hero (Paso 1 → 2 → 3) | `sc-collage-hero.js` rota posiciones cada 4 s; transiciones en `hero.css` |
| Cinta categorías (Posición A → B) | desplazamiento infinito en `cinta.css` |
| Card evento y Card lugar (Normal → Hover) | borde azul, sombra, zoom de la foto, título azul y "Ver detalles" en `tarjetas.css` |
| Botón favorito (Vacío / Hover / Lleno) | `atomos.css` + latido al guardar |
| Chip categoría (Normal → Activo) | borde tinta dibujado por dentro para que el chip no cambie de tamaño |

Todas respetan "reducir movimiento" del sistema operativo.
