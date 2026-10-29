# Interfaz (Blazor)

Componentes Razor de SuraChih. Es una **biblioteca Razor** que usan dos anfitriones:

- `SuraChihIntegrativeProject` (la app de escritorio): la app WPF (Windows, un .exe) la muestra con BlazorWebView.
- `Interfaz.Navegador`: un servidor de prueba para abrirla en el navegador
  (`dotnet run --project Interfaz.Navegador --DatosFalsos true` → <http://localhost:5180>).

Los componentes corren en el mismo proceso que .NET: llaman directo a `ISuraChihBD` (BackendLogica) a
través de los servicios de `Servicios/`. No hay puente de mensajes ni controladores.

Los controles son de **Fluent UI Blazor** (`Microsoft.FluentUI.AspNetCore.Components` v5) con la forma y
los colores de Figma: ver [Fluent UI](#fluent-ui).

## Estructura

```
Rutas.razor                  raíz: el Router elige la página según la dirección
_Imports.razor               usings comunes de todos los .razor
ServiciosInterfaz.cs         AgregarInterfaz(bd): registra todos los servicios (lo llaman los dos anfitriones)
Diseno/PlantillaPrincipal    navbar arriba + ventana modal, hoja de detalle y FluentProviders (avisos)
Paginas/                     una página por pantalla, con su @page
  Landing.razor  /           Home.razor  /home        MapaPagina.razor  /mapa
  Perfil.razor   /perfil     Favoritos.razor  /favoritos                 Catalogo.razor  /componentes
  MisPlanes.razor  /mis-planes
  Resenas.razor  /resenas/evento/{id}  y  /resenas/lugar/{id}
  Moderacion.razor  /moderacion              RevisarEvento.razor  /moderacion/evento/{id}
  PublicarEvento.razor  /organizador/evento/nuevo  y  /organizador/evento/{id}/editar
  Buscar.razor  /buscar?q=...  (resultados de búsqueda, filtros y calendario)
Componentes/
  Base/                      Boton, Campo, Chip, ChipCategoria, Pildora, BotonFavorito, Imagen, Icono,
                             EncabezadoSeccion, VolverArriba, Interruptor, Opcion, AreaTexto
  Moderacion/                FilaPendiente (renglón de la bandeja), ListaRevision, CajaDestacar
  Tarjetas/                  Tarjeta (Card evento / Card lugar), TarjetaRecomendada, ItemLugar,
                             TarjetaEventoPrincipal, TarjetaEventoFila
  Navegacion/                Navbar, MenuCuenta, Pie
  Modales/                   VentanaModal (un FluentDialog) y cada modal: ModalIniciarSesion, ModalCrearCuenta...
  Detalle/                   HojaDetalle (el FluentDialog grande), DetalleEvento, DetalleLugar
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
  css/fluent.css             tema de Fluent UI con los tokens de SuraChih (marca, neutros, fuente)
  css/controles.css          los controles de Fluent con la forma de Figma (botón, campo, píldora, chips,
                             favorito, modal, hoja, avisos)
  css/mapa.css               el mapa de Leaflet (lo pinta JavaScript, por eso es global)
  css/estilos.css            junta los anteriores
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
nombres cortos sin chocar con otros. Solo `tokens.css`, `base.css`, `fluent.css`, `controles.css` y
`mapa.css` son globales.

- Usa siempre las variables de `tokens.css`, no colores sueltos.
- Para dar estilo a un **componente hijo** desde el padre, pásale una clase (`Clase="tarjeta__favorito"`)
  y escribe la regla con `::deep`: `.tarjeta__imagen ::deep .tarjeta__favorito { ... }`. Sin `::deep` la
  regla no le llega, porque el hijo tiene otro atributo.
- Los componentes de Fluent (`FluentButton`, `FluentTextInput`...) son como un componente hijo: su regla
  necesita `::deep` desde un elemento del padre (`.navbar ::deep .navbar__enlace`), o ir en
  `css/controles.css` si el componente de Fluent es la raíz (como en `Boton` o `Campo`).
- La ventana modal y la hoja de detalle pintan a otros componentes dentro: por eso las clases comunes de
  los modales (`modal__formulario`, `modal__ancho`, `modal__aviso`...) están en `VentanaModal.razor.css`
  y las del detalle (`boleto`, `hoja__info`...) en `HojaDetalle.razor.css`, con `::deep`.
- Forma: todo lo que se toca o se escribe (botones, campos, píldoras, chips) usa
  `border-radius: var(--radio-control)`; las cajas que contienen cosas (tarjetas, modales, menús) usan
  `--radio-s` a `--radio-3xl`.
- Para animar la aparición al hacer scroll, pon `data-revelar` (y `style="--orden:N"` para escalonar).

## Fluent UI

La interfaz usa **Fluent UI Blazor** (`Microsoft.FluentUI.AspNetCore.Components` 5.0.1). Lo registra
`AgregarInterfaz` (`AddFluentUIComponents()`) y `<FluentProviders />` va en la plantilla.

| Nuestro componente | De Fluent |
|---|---|
| `Boton` | `FluentButton` (con `Href`: `FluentAnchorButton`); `Ocupado` muestra un `FluentSpinner` |
| `Campo` | `FluentTextInput` (etiqueta, ayuda y error con el mensaje de Fluent) |
| `Pildora`, `ChipCategoria`, segmentos del Home | `FluentToggleButton` (`Pressed`) |
| `Chip`, `ChipCategoria` estático | `FluentBadge` |
| `BotonFavorito`, `VolverArriba`, flechas del carrusel, cerrar | `FluentButton` / `FluentToggleButton` de solo icono |
| Navbar | enlaces `FluentButton` transparentes y buscador `FluentTextInput` |
| `VentanaModal`, `HojaDetalle` | `FluentDialog` (`ShowAsync` / `HideAsync`) |
| `Avisos.Mostrar(...)` | `FluentToast` (`INotificationService.ShowToastAsync`) |

Para que se vea como en Figma:

- `css/fluent.css` cambia los tokens de Fluent (`--colorBrandBackground`, `--fontFamilyBase`,
  `--borderRadiusCircular`...) por los de `tokens.css`. Se definen en `body` para ganarle a los de Fluent.
- `css/controles.css` da la forma de Figma a cada control. Los de Fluent pintan su interior en un
  shadow DOM: una regla sobre el elemento (`.boton`) le gana a sus estilos internos, y lo de dentro se
  alcanza con `::part(...)` (`fluent-text-input::part(root)`, `fluent-dialog::part(dialog)`).
- El CSS de Fluent llega después del nuestro (`<App>.styles.css`): si una regla suya empata con la nuestra,
  sube la especificidad (por eso `fluent-dialog.modal[fuib]::part(dialog)`).
- `<body no-fuib-style>` apaga el CSS base de Fluent, que fija el alto de la página y quita el scroll.

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
| Interruptor | `<Interruptor @bind-Activo="x" Etiqueta="Destacar en el inicio" />` | Apagado (gris) / Encendido (verde). Es un `<button role="switch">` |
| Opción | `<Opcion Texto="Otro" Seleccionada="..." OnClick="..." />` dentro de `role="radiogroup"` | Normal / Seleccionada (sol suave). Una por grupo |
| Área | `<AreaTexto Nombre="comentario" Etiqueta="..." @bind-Valor="x" Errores="errores" />` | Normal / foco / error. Texto de varias líneas (`<textarea>`) |

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
| Búsqueda (WBS 4.5 y 4.7) | `await Modales.FiltrosAsync(filtro)` → `FiltroBusqueda` o null · `await Modales.ElegirFechaAsync()` → día o null |
| Moderación (WBS 4.3) | `await Modales.AprobarEventoAsync(e, comentario, destacar)` / `RechazarEventoAsync(e, comentario)` → true si se decidió · `DecisionRegistradaAsync()` (se cierra sola) · `MotivoRechazo(titulo, registro)` (lo ve el organizador) |
| Detalle de evento / de lugar | `Hojas.AbrirEvento(id)` · `Hojas.AbrirEvento(id, inscribir: true)` · `Hojas.AbrirLugar(id)` |

- Solo hay una modal a la vez: abrir otra cambia el contenido sin cerrar la ventana.
- Los avisos (FluentToast) salen encima de todo, incluso de la modal. Para un error de un formulario de la
  modal usa mejor `ErroresFormulario.PonerAviso(...)` y `<AvisoModal>`, que queda junto a los campos.
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

## Búsqueda, filtros y calendario (WBS 4.5 y 4.7)

`Paginas/Buscar.razor` (`/buscar`) con `Servicios/ServicioBusqueda.cs`. **Lo que se busca va en la dirección**
(`FiltroBusqueda.Ruta()` / `DesdeConsulta(...)`), así Atrás regresa a la búsqueda anterior y cualquier pantalla
puede abrir resultados ya filtrados con `Acciones.Buscar(texto)` o `Acciones.Buscar(filtro)`:

| Parámetro | Valores | Ejemplo |
|---|---|---|
| `q` | texto (título, lugar o categoría; lugares por nombre o dirección) | `q=música` |
| `cat` | ids de etiquetas, separados por coma (eventos con alguna) | `cat=2,3` |
| `precio` | `gratis` · `pago` | `precio=gratis` |
| `cuando` | `hoy` · `manana` · `finde` · `proxima` · `fecha` (con `fecha=2026-11-12`) | `cuando=finde` |
| `tipo` | `eventos` · `cafes` · `museos` · `parques`, separados por coma | `tipo=cafes,museos` |

- Entradas: buscador del navbar (Enter), "Ver todo" de las filas de eventos del Home, y "Fecha" / "Filtros"
  de los filtros rápidos del Home (empiezan con lo que ya estaba puesto ahí).
- Los eventos los filtra `Eventos.FiltrarAsync` en una sola consulta (solo vigentes, por fecha). Los lugares
  salen si se eligió un tipo de lugar o, sin tipo, mientras no haya fecha ni categoría (no aplican a lugares).
- **Calendario**: días pasados apagados, hoy con anillo sol; elegir un día muestra "Planes para el 12 de noviembre"
  (eventos que ocurren ese día, también los de varios días).
- Sin resultados: "Quitar filtros" (deja el texto) y "Explorar todo".

## Moderación (WBS 4.3)

Solo para moderadores (`Usuario.EsModerador`; en los datos de prueba `mod_ana` y `mod_luis`). Se entra desde
el menú de cuenta → "Moderación". Los datos los da `Servicios/ServicioModeracion.cs`.

- **Publicaciones** (`/moderacion`): eventos y lugares pendientes, los más antiguos primero (se puede
  invertir). "Hace 2 días" sale en rojo. Etiquetas: "Primer evento" (su organizador no tiene eventos
  publicados), "Lugar nuevo" y "Editado: vuelve a revisión" (ya estaba aprobado). Lo propio sale con
  candado: la BD no deja que alguien modere lo suyo. Las pestañas Organizadores, Reportes e Historial
  muestran su conteo, pero sus pantallas todavía no existen (igual que revisar lugares).
- **Revisar evento** (`/moderacion/evento/{id}`): vista previa como la verán los usuarios, datos del
  organizador, "Lista de revisión" (solo visual, no se guarda) y la decisión. Aprobar puede destacar el
  evento en el carrusel (`is_featured`, al final del orden). Rechazar pide motivo y comentario; el
  organizador recibe "Motivo: comentario". Después de "Decisión registrada" pasa al siguiente pendiente.
- **No pruebes aprobando o rechazando contra la BD compartida** sin avisar: cambia eventos reales del
  equipo. Usa `--DatosFalsos true` (el evento 4 está pendiente).

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

- cerrar los FluentDialog con Esc pasando por C# (para que `Modales` y `Hojas` sepan) y enfocar el primer campo;
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

## Publicar / editar evento (WBS 4.6)

Solo organizadores aprobados (en los datos de prueba `cafe_dande` y `misiones_cuu`). Se entra con
"Publicar mi evento" de la landing o con el menú de cuenta → "Publicar evento" (`Acciones.PublicarEvento`).
La misma página edita un evento propio: `Acciones.EditarEvento(id)`. Los datos los da
`Servicios/ServicioPublicacion.cs` y las fotos las sube `Servicios/ServicioImagenes.cs`.

- Cinco secciones: información básica (título con contador de 150), imágenes (la primera es la portada),
  fecha y hora, ubicación ("Lugar registrado" o "Otra dirección" con el pin arrastrable de `<Mapa>`) y
  precio y boletos. A la derecha: vista previa de la tarjeta, "Antes de enviar" (se palomea solo) y los botones.
- Antes de guardar se revisa lo mismo que la BD: fin posterior al inicio, coordenadas dentro de Chihuahua,
  precio mínimo ≤ máximo y título de hasta 150 caracteres. El error sale en su campo y la página baja a él.
- Nuevo, borrador o rechazado: "Enviar a revisión" / "Guardar borrador". Pendiente o publicado: "Guardar cambios".
  Al enviar sale "Tu evento está en revisión" (`ModalEnviadoRevision`). Al entrar a editar uno publicado sale
  "Si lo editas, vuelve a revisión" (`ModalEditarPublicado`).
- **Fotos**: se suben a Cloudinary con un *upload preset unsigned*. Pon `"CloudinaryCloudName"` y
  `"CloudinaryUploadPreset"` en tu `dbsettings.local.json`. Con datos de prueba y sin esas llaves se guarda
  una foto de ejemplo para poder probar la pantalla.

## Inscripción, boletos y Mis planes (WBS 4.4)

- **"Voy a ir" / "Cancelar inscripción"** en el detalle de evento abren `ModalInscripcion` (tabla `inscription`,
  estados active / cancelled).
- **"Comprar boletos"** abre `ModalBoletos`. SuraChih no vende boletos: el cliente confirmó que se quita el
  selector de cantidad, así que la modal muestra el precio y "Continuar al sitio oficial" abre `event.ticket_url`
  en el navegador y cambia a `ModalRedirigiendo` ("Te estamos llevando al sitio oficial").
- **Mis planes** (`/mis-planes`, navbar y menú de cuenta): pestañas Próximos / Pasados / Cancelados con
  `Servicios/ServicioPlanes.cs` (→ `Inscripciones.ListarPlanesAsync`). Próximos permite cancelar la inscripción;
  Pasados muestra "Asististe" + "Escribir reseña"; Cancelados distingue "Cancelado por el organizador".

## Detalle de lugar, compartir y reseñas

- **Detalle de lugar** (`Componentes/Detalle/DetalleLugar.razor`): la misma hoja que el evento y el mismo `<Mapa>`.
  El horario sale de `place_hours` (si `closes_at < opens_at` cierra después de medianoche; lo calcula
  `EstadoHorario.Calcular`) y la calificación de `v_place_rating`.
- **Compartir** (botón de la portada, evento o lugar): `ModalCompartir` con el enlace `https://surachih.com/e/N`
  o `/l/N` (`Acciones.SitioPublico`) y "Copiar" (portapapeles, `copiarTexto` en js/interop.js).
- **Escribir reseña**: `ModalEscribirResena` con el componente `<Calificacion>` (5 estrellas). Los eventos solo
  se reseñan cuando ya terminaron; si el usuario ya tiene su reseña, la modal la edita. Los errores de los
  triggers (lo propio, repetida...) salen tal cual.
- **Reseñas** (`/resenas/{evento|lugar}/{id}`): resumen con promedio, barras por estrellas y la lista
  (`Servicios/ServicioResenas.cs`). "Volver" regresa a la pantalla de antes con la hoja abierta.
- `<Estrellas Valor="4.6" Tamano="14" />` pinta estrellas de solo lectura (12, 14 o 20px).
