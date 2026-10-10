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
    atomos.js              icono(), boton(), chip(), encabezadoSeccion()
    tarjetas.js            tarjetaEventoPrincipal(), tarjetaEventoFila(), tarjetaLugar()
    sc-imagen.js           <sc-imagen> foto de Cloudinary con carga y relleno
    sc-boton-favorito.js   <sc-boton-favorito> corazón (Vacío / Hover / Lleno)
    sc-toast.js            mostrarToast()
    secciones/             <sc-navbar>, <sc-hero>, <sc-collage-hero>, secciones fijas y con datos
assets/iconos              SVG exportados de Figma (conservan tamaño y color)
assets/demo                fotos solo para los datos demo
```

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
| Card lugar (Normal → Hover) | borde azul, sombra, título azul y "Ver detalles" en `tarjetas.css` |
| Botón favorito (Vacío / Hover / Lleno) | `atomos.css` + latido al guardar |

Todas respetan "reducir movimiento" del sistema operativo.
