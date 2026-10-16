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
mapa.html                  pantalla Mapa (visitante y logueado): panel de lugares + <sc-mapa>
css/
  tokens.css               colores, tipografías, radios y sombras (variables de Figma)
  base.css                 reset, clases de texto (.t-h1, .t-body...), .contenedor, animación de aparición
  estilos.css              importa todo en orden
  componentes/*.css        un archivo por grupo de componentes
js/
  main.js                  entrada de index.html: carga datos y conecta las secciones
  mapa.js                  entrada de mapa.html: buscador, píldoras y lista sincronizados con el mapa
  nucleo/componente.js     clase base Componente + plantillas html`` con escape automático
  nucleo/puente.js         invocar('accion', datos) → C#
  nucleo/navegacion.js     cambiar de pantalla, acciones data-accion comunes y sesión del navbar
  utilidades/              formato (fechas, precios, horarios), cloudinary, animaciones, lugares (tipo y pin)
  contenido/landing.js     textos fijos de la landing
  contenido/datos-demo.js  datos de ejemplo
  componentes/
    atomos.js              icono(), boton(), campo(), marcarError(), chip(), chipCategoria(), pildora(), encabezadoSeccion()
    tarjetas.js            tarjetaEvento(), tarjetaLugar(), tarjetaEventoPrincipal(), tarjetaEventoFila(), itemLugar()
    sc-imagen.js           <sc-imagen> foto de Cloudinary con carga y relleno
    hoja.js                ventana de detalle que comparten evento y lugar (historial, carga, cerrar)
    sc-detalle-evento.js   abrirDetalleEvento(id): hoja "Evento / ..." encima de cualquier pantalla
    sc-detalle-lugar.js    abrirDetalleLugar(id): hoja "Lugar / ..." (pines del mapa y Card lugar)
    sc-mapa.js             <sc-mapa> mapa con pines (explorar, detalle y pin arrastrable)
    sc-boton-favorito.js   <sc-boton-favorito> corazón (Vacío / Hover / Lleno)
    sc-chip-categoria.js   <sc-chip-categoria> filtro por categoría (Normal / Activo)
    sc-toast.js            mostrarToast()
    sc-modal.js            mostrarModal() / cerrarModal(): ventana modal base (<dialog>)
    menu-cuenta.js         alternarMenuCuenta(): menú bajo el avatar del navbar
    modales/cuenta.js      modales de cuenta: iniciar sesión, crear cuenta, recuperar contraseña...
    modales/inscripcion.js "¿Vas a ir?" y "¿Cancelar tu inscripción?"
    secciones/             <sc-navbar>, <sc-hero>, <sc-collage-hero>, secciones fijas y con datos
assets/iconos              SVG exportados de Figma (conservan tamaño y color)
assets/demo                fotos solo para los datos demo
vendor/leaflet             Leaflet 1.9.4 tal cual se distribuye (no editar; ver "Mapa")
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
| Card recomendada | `tarjetaRecomendada(item, { tipo, orden })` dentro de `.rejilla-recomendadas` | Normal / Hover. `tipo: 'evento'` (por defecto) o `'lugar'` |
| Botón favorito | `<sc-boton-favorito tipo objetivo tamano activo>` | Vacío / Hover / Lleno × `s` (36px) · `m` (40px). Emite `sc:favorito` |
| Pildora | `pildora({ texto, valor, activa })` | Normal / Activa. Una activa por grupo; se lee con `data-valor` |
| Item (lista del mapa) | `itemLugar(lugar, { activo })` | Normal / Activo. Botón con `data-lugar="id"` |
| Mapa, Pin, Zoom | `<sc-mapa>` (ver "Mapa") | Pin Normal / Activo; explorar · detalle · pin arrastrable |

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

## Mapa (`<sc-mapa>`)

Leaflet 1.9.4 va empaquetado en `vendor/leaflet` y lo importa el propio componente: no hay que
agregar ningún `<script>`. Solo las teselas (las imágenes del mapa) se piden a OpenStreetMap, por
eso el componente siempre muestra «© OpenStreetMap contributors» (es obligatorio; no lo ocultes).
El alto se lo da el contenedor: ponle `height` con CSS.

**Estilo del mapa.** Se usa el estilo Voyager de CARTO (hecho con datos de OpenStreetMap, nítido en
pantallas con escala) con la llave gratuita del equipo, que está en `js/contenido/claves.js`: todos
ven el mismo mapa sin configurar nada y la atribución es «© OpenStreetMap contributors © CARTO».
Si la llave deja de funcionar (cuota agotada o borrada), se pide otra en
<https://carto.com/basemaps/apikey/> y se cambia ahí. Con `CARTO_KEY` vacía el componente usa las
teselas estándar de OpenStreetMap.

**Detalle de evento o de lugar** (un pin fijo). Bastan los atributos:

```js
html`<sc-mapa lat="${evento.latitud}" lng="${evento.longitud}" titulo="${evento.lugarNombre ?? evento.direccion}"></sc-mapa>`
```

`EventoTarjeta.latitud/longitud` vienen de `v_event_card`, que ya resuelve la ubicación del evento
o la de su lugar. Si vienen vacías (borrador sin ubicación), el mapa se muestra sin pin.

**Publicar evento** (pin arrastrable). El pin se arrastra o se coloca con un clic:

```js
html`<sc-mapa arrastrable lat="${datos.latitud ?? ''}" lng="${datos.longitud ?? ''}"></sc-mapa>`

mapa.addEventListener('sc:mapa-ubicacion', ({ detail }) => {
  datos.latitud = detail.lat;    // ya con 6 decimales y dentro del estado de Chihuahua,
  datos.longitud = detail.lng;   // como lo exigen DECIMAL(9,6) y los CHECK de la BD
});
```

**Explorar** (varios pines, como `mapa.html`):

```js
import { puntoDeLugar, puntoDeEvento } from './utilidades/lugares.js';

mapa.puntos = lugares.map(puntoDeLugar);                  // o eventos.map(puntoDeEvento)
mapa.addEventListener('sc:mapa-pin', ({ detail }) => { /* detail.punto.id, detail.punto.tipo */ });
mapa.addEventListener('sc:mapa-zona', ({ detail }) => { /* detail.visibles: puntos que se ven ahora */ });
mapa.seleccionar({ id, tipo: 'lugar' }, { mover: true }); // pin Activo (tinta)
```

Un punto es `{ id, tipo, lat, lng, titulo, icono }`; `icono` es un archivo de `assets/iconos`. El pin
de un lugar sale de `tag.icon` (`coffee`, `museum`, `tree`); los demás usan el pin genérico. Para
agregar un tipo (y su píldora en la pantalla Mapa), exporta su icono de Figma y agrégalo en
`utilidades/lugares.js`.

Los datos llegan de C# como JSON por el puente: `invocar('mapa.obtener')` → `{ lugares }`
(`Controladores/ControladorMapa.cs`).

## Pantallas y navegación

Cada pantalla es un `.html` con su script de entrada. Para ir de una a otra se usa `data-accion`
(`'inicio'`, `'mapa'`...), que resuelve `nucleo/navegacion.js`. Al crear una pantalla nueva:

1. Crea `mi-pantalla.html` (copia el `<head>` de `mapa.html`) y `js/mi-pantalla.js`.
2. En el script llama a `atenderAcciones()` y `cargarSesion(navbar)`.
3. Agrega la página en `PAGINAS` y su acción en `acciones` de `nucleo/navegacion.js`.
4. Si necesita datos, registra la acción en un controlador de C# y agrega su ejemplo en
   `contenido/datos-demo.js`.
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
- Al entrar o crear la cuenta se lanza `sc:sesion` (`detail: { usuario, nuevo }`); `nucleo/navegacion.js` cambia el navbar a Logueado
  (en todas las pantallas, también en el mapa).
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

## Detalle de evento

Los 24 frames "Evento / ..." de Figma son **una sola plantilla** (`js/componentes/sc-detalle-evento.js`,
estilos en `css/componentes/detalle-evento.css`). Es una hoja que se abre encima de la pantalla en la que
estés, igual que el overlay del prototipo, así que funciona desde la landing, el mapa o cualquier pantalla nueva:

- `data-accion="ver-evento" data-id="3"` (tarjetas, "Ver detalles") → `abrirDetalleEvento(3)`.
- `data-accion="inscribirse" data-id="3"` ("Voy a ir" de las tarjetas) → abre el detalle con "¿Vas a ir?" encima
  (o "Inicia sesión para continuar" si no hay sesión).
- Mientras está abierta la dirección termina en `#evento-3`: Atrás la cierra y abrir `index.html#evento-3` la muestra.

| Parte de Figma | Implementación |
|---|---|
| Portada + Compartir / favorito / Cerrar | `<sc-imagen>` 1040 × 440, `<sc-boton-favorito tamano="m">` |
| Categorías, título, Acerca del evento | `chipCategoria(etiqueta, { interactivo: false })` |
| Ubicación (mini mapa) | `<sc-mapa lat lng titulo>` de Carlos (un pin fijo). Sin coordenadas no se pinta el mapa |
| Boleto: Fecha, Horario, Lugar, Precio | "Fecha por confirmar" / "Precio por confirmar" con `fechaEvento` / `precioEvento` |
| Te podría interesar | hasta 3 `tarjetaEvento()` que elige C# (misma categoría y, si faltan, los más guardados) |

Estados (columnas `status`, `is_finished` y `cancel_reason` de `v_event_card`, más la inscripción del usuario):

| Estado | Cuándo | Qué muestra el boleto |
|---|---|---|
| Normal | aprobado y sin terminar | "Voy a ir" + "Comprar boletos" (solo si hay `boletosUrl`; se abre en el navegador) |
| Ya vas a ir | además el usuario está inscrito | botón verde "Ya vas a ir" + "Cancelar inscripción" (abren "¿Cancelar tu inscripción?") |
| Finalizado | `terminado` | aviso "Este evento ya terminó" + "Escribir reseña" + "Ver reseñas (N)" |
| Cancelado | `estado: 'cancelado'` | aviso "Este evento fue cancelado" + "Motivo" (`motivoCancelacion`) |

Datos: `invocar('evento.obtener', { id })` → `{ evento, galeria, inscrito, favorito, numResenas, recomendados, favoritos }`
y `inscripcion.inscribir` / `inscripcion.cancelar` (`Controladores/ControladorEvento.cs`). En el navegador los datos
demo traen un evento terminado (`#evento-8`) y uno cancelado (`#evento-9`); también hay botones en `componentes.html`.

Todavía muestran "llega en la siguiente versión": Compartir, Reportar evento, Escribir reseña y Ver reseñas
(sus modales están en Figma: "Modal / Compartir", "Modal / Reportar", "Modal / Escribir reseña").

## Detalle de lugar

Los frames "Lugar / ..." son la misma hoja con otro contenido (`js/componentes/sc-detalle-lugar.js`; la
ventana la comparten con el de evento en `js/componentes/hoja.js`). Se abre:

- al elegir un **pin del mapa** (`mapa.js`, evento `sc:mapa-pin`; también resalta su fila de la lista),
- con `data-accion="ver-lugar" data-id="2"` (Card lugar) o con `#lugar-2` en la dirección.

Info: chip de tipo, "Acerca del lugar", mini mapa y "Opiniones" (hasta 2 reseñas). Boleto: Horario (abierto
ahora), Precio promedio, Calificación, Dirección, "Cómo llegar" (Google Maps en el navegador), "Escribir reseña"
y "Reportar lugar". Abajo, "También te puede gustar" con hasta 3 `tarjetaLugar()`.
Datos: `invocar('lugar.obtener', { id })` → `{ lugar, galeria, favorito, opiniones, recomendados }`
(`Controladores/ControladorLugar.cs`). El corazón de lugares sigue sin guardarse en la BD v1.

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
- Para navegar, pon `data-accion="nombre"` en el botón y agrega el nombre en `acciones` de `nucleo/navegacion.js`.
- Para animar la aparición al hacer scroll, agrega `data-revelar` (y `style="--orden:N"` para escalonar).
- Usa siempre las variables de `tokens.css`, no colores sueltos.
- Forma: todo lo que se toca o se escribe (botones, campos, píldoras, chips) usa
  `border-radius: var(--radio-control)`; las cajas que contienen cosas (tarjetas, modales, menús) usan
  `--radio-s` a `--radio-3xl`. Así toda la app se ve igual y la forma se cambia en un solo lugar.

## Animaciones del diseño

| Figma | Implementación |
|---|---|
| Collage hero (Paso 1 → 2 → 3) | `sc-collage-hero.js` rota posiciones cada 4 s; transiciones en `hero.css` |
| Cinta categorías (Posición A → B) | desplazamiento infinito en `cinta.css` |
| Card evento y Card lugar (Normal → Hover) | borde azul, sombra, zoom de la foto, título azul y "Ver detalles" en `tarjetas.css` |
| Botón favorito (Vacío / Hover / Lleno) | `atomos.css` + latido al guardar |
| Chip categoría (Normal → Activo) | borde tinta dibujado por dentro para que el chip no cambie de tamaño |

Todas respetan "reducir movimiento" del sistema operativo.
