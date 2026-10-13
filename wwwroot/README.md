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
    sc-mapa.js             <sc-mapa> mapa con pines (explorar, detalle y pin arrastrable)
    sc-boton-favorito.js   <sc-boton-favorito> corazón (Vacío / Hover / Lleno)
    sc-chip-categoria.js   <sc-chip-categoria> filtro por categoría (Normal / Activo)
    sc-toast.js            mostrarToast()
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

**Estilo del mapa.** Sin configurar nada se usan las teselas estándar de OpenStreetMap. Para el
estilo Voyager de CARTO (más limpio y nítido en pantallas con escala), pide una llave gratuita en
<https://carto.com/basemaps/apikey/>, copia `js/contenido/claves.example.js` como
`claves.local.js` (git lo ignora) y ponla en `CARTO_KEY`. La atribución cambia sola a
«© OpenStreetMap contributors © CARTO».

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

## Animaciones del diseño

| Figma | Implementación |
|---|---|
| Collage hero (Paso 1 → 2 → 3) | `sc-collage-hero.js` rota posiciones cada 4 s; transiciones en `hero.css` |
| Cinta categorías (Posición A → B) | desplazamiento infinito en `cinta.css` |
| Card evento y Card lugar (Normal → Hover) | borde azul, sombra, zoom de la foto, título azul y "Ver detalles" en `tarjetas.css` |
| Botón favorito (Vacío / Hover / Lleno) | `atomos.css` + latido al guardar |
| Chip categoría (Normal → Activo) | borde tinta dibujado por dentro para que el chip no cambie de tamaño |

Todas respetan "reducir movimiento" del sistema operativo.
