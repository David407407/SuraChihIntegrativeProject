# Interfaz (Blazor)

Componentes Razor de SuraChih. Es una **biblioteca Razor** que usan dos anfitriones:

- `SuraChihIntegrativeProject` (la app de escritorio): la app .NET MAUI (solo Windows) la muestra con BlazorWebView.
- `Interfaz.Navegador`: un servidor de prueba para abrirla en el navegador
  (`dotnet run --project Interfaz.Navegador --DatosFalsos true` → <http://localhost:5180>).

Los componentes corren en el mismo proceso que .NET: llaman directo a `ISuraChihBD` (BackendLogica) a
través de los servicios de `Servicios/`. No hay puente de mensajes ni controladores.

## Estructura

```
Rutas.razor                  raíz: el Router elige la página según la dirección
_Imports.razor               usings comunes de todos los .razor
ServiciosInterfaz.cs         AgregarInterfaz(bd): registra todos los servicios (lo llaman los dos anfitriones)
Diseno/PlantillaPrincipal    navbar arriba + avisos, ventana modal y hoja de detalle (encima de todo)
Paginas/                     una página por pantalla, con su @page
  Landing.razor  /           Home.razor  /home        MapaPagina.razor  /mapa
  Perfil.razor   /perfil     Favoritos.razor  /favoritos                 Catalogo.razor  /componentes
Componentes/
  Base/                      Boton, Campo, Chip, ChipCategoria, Pildora, BotonFavorito, Imagen, Icono,
                             EncabezadoSeccion, VolverArriba
  Tarjetas/                  Tarjeta (Card evento / Card lugar), TarjetaRecomendada, ItemLugar,
                             TarjetaEventoPrincipal, TarjetaEventoFila
  Navegacion/                Navbar, MenuCuenta, Pie
  Avisos/Toasts.razor        avisos flotantes
  Modales/                   VentanaModal (el <dialog>) y cada modal: ModalIniciarSesion, ModalCrearCuenta...
  Detalle/                   HojaDetalle (el <dialog> grande), DetalleEvento, DetalleLugar
  Mapa/Mapa.razor            el mapa (Leaflet) conectado con C#
  Landing/, Home/            secciones de esas pantallas
Servicios/                   qué datos pedir a BackendLogica para cada pantalla (antes, los controladores)
                             + Sesion (usuario con sesión) y CorreoSimulado
Estado/                      lo que comparte la interfaz: Acciones (botones comunes y navegación), Avisos,
                             Modales, Hojas (detalle de evento/lugar) e InteropJs
Utilidades/                  Formato (fechas, precios, horarios), Cloudinary, Lugares (tipo y pin),
                             ReglasCuenta, ErroresFormulario, TextosLanding, Recursos (rutas de wwwroot)
wwwroot/
  css/tokens.css             colores, tipografías, radios y sombras (variables de Figma)
  css/base.css               reset, clases de texto (.t-h1, .t-body...), .contenedor, .esqueleto,
                             cuadrículas de tarjetas y animación de aparición
  css/mapa.css               el mapa de Leaflet (lo pinta JavaScript, por eso es global)
  css/estilos.css            junta los tres anteriores
  js/interop.js              lo poco que Blazor no hace solo (ver "JavaScript que queda")
  js/sc-mapa.js              Web Component del mapa sobre Leaflet
  js/claves.js               llave compartida de CARTO para el estilo del mapa
  assets/iconos              SVG exportados de Figma (conservan tamaño y color)
  assets/demo                fotos de ejemplo (solo el catálogo /componentes)
  fonts/, vendor/leaflet     Plus Jakarta Sans de respaldo y Leaflet 1.9.4 (no editar)
```

Todo lo de `wwwroot` se sirve como `_content/Interfaz/...`. En C# usa `Recursos.Icono("nombre")`.

## Estilos: CSS aislado por componente

Cada componente trae su `Componente.razor.css`. Blazor agrega un atributo propio a cada elemento del
componente (`b-xxxx`) y reescribe sus reglas, así que **un componente solo ve sus estilos**: puedes usar
nombres cortos sin chocar con otros. Solo `tokens.css`, `base.css` y `mapa.css` son globales.

- Usa siempre las variables de `tokens.css`, no colores sueltos.
- Para dar estilo a un **componente hijo** desde el padre, pásale una clase (`Clase="tarjeta__favorito"`)
  y escribe la regla con `::deep`: `.tarjeta__imagen ::deep .tarjeta__favorito { ... }`. Sin `::deep` la
  regla no le llega, porque el hijo tiene otro atributo.
- La ventana modal y la hoja de detalle pintan a otros componentes dentro: por eso las clases comunes de
  los modales (`modal__formulario`, `modal__ancho`, `modal__aviso`...) están en `VentanaModal.razor.css`
  y las del detalle (`boleto`, `hoja__info`...) en `HojaDetalle.razor.css`, con `::deep`.
- Forma: todo lo que se toca o se escribe (botones, campos, píldoras, chips) usa
  `border-radius: var(--radio-control)`; las cajas que contienen cosas (tarjetas, modales, menús) usan
  `--radio-s` a `--radio-3xl`.
- Para animar la aparición al hacer scroll, pon `data-revelar` (y `style="--orden:N"` para escalonar).

## Componentes base (WBS 4.1)

Úsalos en todas las pantallas en vez de hacer tu propia versión. La página **`/componentes`** muestra cada
variante con el código para usarla.

| Figma (página Rediseño) | Cómo se usa | Variantes |
|---|---|---|
| Botón | `<Boton Texto="..." Variante="primario" Tamano="m" Icono="..." OnClick="..." />` | `primario` · `oscuro` · `secundario` · `peligro` · `fantasma` × `m` (48px) · `s` (36px). Además `Tipo="submit"`, `Deshabilitado`, `Ocupado`, `Href` + `Externo` |
| Campo | `<Campo Nombre="correo" Etiqueta="..." Tipo="email" Icono="correo" @bind-Valor="x" Errores="errores" />` | normal · foco · error · deshabilitado; contraseña con "Mostrar". Los errores van en `ErroresFormulario` |
| Chip categoría | `<ChipCategoria Etiqueta="t" @bind-Activo="activo" />` · `Interactivo="false"` | Normal / Activo. El color sale de `Etiqueta.Icono` (family, music, run, robot, food) |
| Navbar | va en la plantilla | Visitante / Logueado (lo decide `Sesion`) |
| Card evento / Card lugar | `<Tarjeta Evento="e" Orden="i" Favorito="..." />` · `<Tarjeta Lugar="l" />` | Normal / Hover. "Fecha por confirmar" y "Precio por confirmar" incluidos |
| Card recomendada | `<TarjetaRecomendada Elemento="ElementoCollage.DeEvento(e)" />` en `.rejilla-recomendadas` | Normal / Hover. Evento o lugar |
| Botón favorito | `<BotonFavorito Tipo="TipoFavorito.Evento" Id="e.Id" Activo="..." />` | Vacío / Hover / Lleno × `s` · `m`. Guarda solo (ServicioFavoritos) |
| Pildora | `<Pildora Texto="Cafés" Activa="..." OnClick="..." />` | Normal / Activa. Una activa por grupo |
| Item (lista del mapa) | `<ItemLugar Lugar="l" Activo="..." OnClick="..." />` | Normal / Activo |
| Mapa, Pin, Zoom | `<Mapa ... />` (ver "Mapa") | Pin Normal / Activo; detalle · explorar · pin arrastrable |

Las cards reciben los records de BackendLogica tal cual (`EventoTarjeta`, `LugarTarjeta`) y van dentro de
`<div class="rejilla-tarjetas">`, que acomoda 4 por fila a 1200px como en Figma.

Ejemplo de un formulario (las reglas de cuenta son las mismas de BackendLogica, en `ReglasCuenta`):

```razor
<EditForm Model="_datos" OnSubmit="EnviarAsync" class="modal__formulario" novalidate>
    <Campo Nombre="correo" Etiqueta="Correo electrónico" Tipo="email" Icono="correo" @bind-Valor="_datos.Correo" Errores="_errores" />
    <Boton Texto="Enviar" Tipo="submit" Ocupado="_enviando" />
</EditForm>

@code {
    private async Task EnviarAsync()
    {
        _errores.Limpiar();
        if (ReglasCuenta.ErrorCorreo(_datos.Correo ?? "") is { } error) { _errores.Poner("correo", error); return; }
        try { await Servicio.GuardarAsync(_datos.Correo); }
        catch (Exception ex) { _errores.PonerAviso(MensajesError.Para(ex)); }   // texto de C# tal cual
    }
}
```

## Pantallas y navegación

Cada pantalla es una página de `Paginas/` con su `@page`. Para cambiar de pantalla usa `Acciones`
(`Acciones.Inicio()`, `Acciones.Mapa()`, `Acciones.IrA(Acciones.Rutas.Perfil)`...): si ya estás en ella,
sube al inicio. El logo y "Inicio" del navbar llevan al Home. Al crear una pantalla nueva:

1. Crea `Paginas/MiPantalla.razor` con `@page "/mi-pantalla"` y su `MiPantalla.razor.css`.
2. Agrega su ruta en `Acciones.Rutas` (y, si va en el navbar, su enlace en `Navbar.razor`).
3. Si necesita datos, crea su servicio en `Servicios/` (usa `ISuraChihBD`) y regístralo en `ServiciosInterfaz.cs`.
4. Si es larga, pon `<VolverArriba />` al final. Si cambia con la sesión, suscríbete a `Sesion.Cambio`
   (y quita la suscripción en `Dispose`).

Botones que todavía no tienen pantalla muestran "llega en la siguiente versión" con `Acciones.Proximamente(...)`.

## Modales, avisos y detalle

| Qué | Cómo |
|---|---|
| Avisos ("Toast / Cambios guardados") | `Avisos.Mostrar("Cambios guardados")` · `Avisos.Mostrar(texto, TipoAviso.Error)` |
| Modales de cuenta (WBS 4.2) | `Modales.IniciarSesion(correo, aviso)` · `CrearCuenta()` · `RecuperarContrasena()` → "Revisa tu correo" → `ContrasenaNueva(token)` · `RequiereSesion(texto)` |
| "¿Vas a ir?" / "¿Cancelar tu inscripción?" | `await Modales.ConfirmarInscripcionAsync(evento)` / `ConfirmarCancelacionAsync(evento)` → estado final |
| "¿Eliminar tu cuenta?" | `await Modales.ConfirmarEliminarCuentaAsync()` → true si se desactivó |
| Detalle de evento / de lugar | `Hojas.AbrirEvento(id)` · `Hojas.AbrirEvento(id, inscribir: true)` · `Hojas.AbrirLugar(id)` |

- Solo hay una modal a la vez: abrir otra cambia el contenido sin cerrar la ventana.
- La modal (`<dialog>`) queda por encima de los avisos: para avisar algo con ella abierta usa
  `ErroresFormulario.PonerAviso(...)` y `<AvisoModal>`.
- La hoja de detalle pone `#evento-N` / `#lugar-N` en la dirección: Atrás la cierra y Adelante la vuelve a
  abrir. Al iniciar sesión con la hoja abierta se recarga (inscripción y corazón de ese usuario).
- **Correo:** el proyecto aún no tiene servidor de correo. `Servicios/CorreoSimulado.cs` guarda el enlace y el
  botón "Abrir enlace del correo" lo abre directo. Cuando haya SMTP solo cambia esa clase (ver sus comentarios).
- "Continuar con Google / Apple" están como en Figma pero todavía no hacen login (muestran un aviso).

Estados del detalle de evento (columnas `status`, `is_finished` y `cancel_reason` de `v_event_card`):

| Estado | Cuándo | Qué muestra el boleto |
|---|---|---|
| Normal | aprobado y sin terminar | "Voy a ir" + "Comprar boletos" (solo si hay `BoletosUrl`; se abre en el navegador) |
| Ya vas a ir | además el usuario está inscrito | botón verde "Ya vas a ir" + "Cancelar inscripción" |
| Finalizado | `Terminado` | aviso "Este evento ya terminó" + "Escribir reseña" + "Ver reseñas (N)" |
| Cancelado | `Estado == Cancelado` | aviso "Este evento fue cancelado" + "Motivo" (`MotivoCancelacion`) |

## Mapa

`Componentes/Mapa/Mapa.razor` pinta el Web Component `<sc-mapa>` (`wwwroot/js/sc-mapa.js`, Leaflet 1.9.4
empaquetado en `vendor/leaflet`) y lo conecta con C#. Solo las teselas (las imágenes del mapa) se piden a
internet, por eso siempre se muestra «© OpenStreetMap contributors» (es obligatorio; no lo ocultes).
El alto lo da el contenedor o la clase que le pases con `Clase`.

```razor
@* Detalle (un pin fijo) *@
<Mapa Clase="hoja__mapa" Lat="evento.Latitud" Lng="evento.Longitud" Titulo="@evento.LugarNombre" />

@* Explorar (varios pines, como la pantalla Mapa) *@
<Mapa @ref="_mapa" AlElegirPin="AlElegirPin" AlCambiarZona="ids => _visibles = [.. ids]" />
await _mapa.MostrarAsync(lugares.Select(Lugares.Punto), ajustar: true);
await _mapa.SeleccionarAsync(punto, mover: true);

@* Publicar evento (pin arrastrable): llega con 6 decimales y dentro del estado de Chihuahua *@
<Mapa Arrastrable Lat="_datos.Latitud" Lng="_datos.Longitud" AlMoverUbicacion="u => { _datos.Latitud = u.Lat; _datos.Longitud = u.Lng; }" />
```

**Estilo del mapa.** Se usa el estilo Voyager de CARTO con la llave gratuita del equipo (`js/claves.js`).
Si deja de funcionar, se pide otra en <https://carto.com/basemaps/apikey/>; con `CARTO_KEY` vacía usa
las teselas estándar de OpenStreetMap. El pin de un lugar sale de `tag.icon` (`coffee`, `museum`, `tree`);
para agregar un tipo (y su píldora en la pantalla Mapa), exporta su icono de Figma y agrégalo en `Utilidades/Lugares.cs`.

## Fotos (Cloudinary)

Todas las fotos pasan por `<Imagen Src="@evento.ImagenUrl" Alt="@evento.Titulo" Ancho="588" Alto="330" />`:
sin URL o si falla muestra un relleno con el sol de SuraChih; con URL de Cloudinary pide la versión del
tamaño exacto. Cuando suban las fotos reales solo hay que guardar la URL en la BD
(`event.main_image_url`, `place.main_image_url`).

## JavaScript que queda

Casi todo es C#. `wwwroot/js/interop.js` (lo llama `Estado/InteropJs.cs`) solo hace:

- abrir y cerrar los `<dialog>` con animación (Esc, clic en el velo y devolver el foco);
- poner `#evento-N` en la dirección sin que la página suba hasta arriba;
- desplazar la página (volver arriba, saltar a una fila del Home);
- la aparición al hacer scroll (`data-revelar`);
- conectar el mapa de Leaflet (`sc-mapa.js`) con `Mapa.razor`.

Sin JavaScript: el menú de cuenta (popover nativo con anclaje CSS), el botón "Volver arriba" (aparece con
una animación ligada al scroll), el conteo del hero (contador CSS) y la escala del collage (container queries).

## Animaciones del diseño

| Figma | Implementación |
|---|---|
| Collage hero (Paso 1 → 2 → 3) | `CollageHero.razor` rota posiciones cada 4 s; transiciones en su `.razor.css` |
| Cinta categorías (Posición A → B) | desplazamiento infinito en `CintaCategorias.razor.css` |
| Card evento y Card lugar (Normal → Hover) | borde azul, sombra, zoom de la foto, título azul y "Ver detalles" en `Tarjeta.razor.css` |
| Botón favorito (Vacío / Hover / Lleno) | `BotonFavorito.razor.css` + latido al guardar |
| Chip categoría (Normal → Activo) | borde tinta dibujado por dentro para que el chip no cambie de tamaño |

Todas respetan "reducir movimiento" del sistema operativo (los carruseles dejan de avanzar solos).
