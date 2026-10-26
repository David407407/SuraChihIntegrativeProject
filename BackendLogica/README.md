# BackendLogica

Librería de acceso a la BD `surachih` (MySQL). No sabe nada de la interfaz: la puede usar la app
la app de escritorio (WPF), una consola de pruebas o, en el futuro, una API.

## Uso

Las pantallas dependen **solo de las interfaces** de `Contratos/`, nunca de `UsuarioDB`, `EventoDB`...:

```csharp
ISuraChihBD bd = FabricaBD.Crear();              // MySQL o datos falsos, según dbsettings.local.json

var usuario  = await bd.Usuarios.IniciarSesionAsync("mariana_r", "Test1234!");
var eventos  = await bd.Eventos.ListarMasGuardadosAsync(limite: 5);
var lugares  = await bd.Lugares.ListarRecomendadosAsync();
bool guardado = await bd.Favoritos.AlternarAsync(usuario!.Id, eventoId: 1);
```

Todos los métodos son asíncronos (`...Async`) para no congelar la ventana.

## Trabajar sin MySQL (repositorio falso)

Agrega esta llave a tu `dbsettings.local.json` y la app usa `Falso/SuraChihFalso`, que tiene en memoria
los **datos de prueba de `BaseDeDatos/surachih_v1.sql`** (mismos ids, textos y relaciones; contraseña de
todos: `Test1234!`):

```json
{ "UseFakeData": true }
```

- Implementa las mismas interfaces y repite las reglas de los triggers con **los mismos mensajes**
  (`Falso/MensajesTrigger.cs`), así que una pantalla probada con el falso se porta igual con MySQL.
- Los cambios viven mientras la app está abierta; al reiniciar vuelven los datos originales.
- En pruebas o en un formulario de ejemplo también puedes crearlo directo: `ISuraChihBD bd = new SuraChihFalso();`

## Contratos (interfaces)

| Interfaz | Real (MySQL) | Para qué pantallas del rediseño |
|---|---|---|
| `IUserRepository` | `UsuarioDB` | Crear cuenta, Iniciar sesión, Mi perfil, Eliminar cuenta |
| `IPasswordResetRepository` | `RecuperacionContrasenaDB` | Recuperar contraseña, Contraseña nueva |
| `IOrganizerRepository` | `OrganizadorDB` | Hazte organizador, encabezado del panel, Moderación / Organizadores |
| `ITagRepository` | `EtiquetaDB` | Chips, Explora por categoría, Filtros |
| `IEventRepository` | `EventoDB` | Inicio, Resultados, Detalle de evento, Panel, Publicar evento, Estadísticas |
| `IPlaceRepository` | `LugarDB` | Detalle de lugar, Mapa, tarjetas de lugar |
| `IFavoriteRepository` | `FavoritoDB` | Corazón y Mis favoritos (Todos / Eventos / Lugares) |
| `IInscriptionRepository` | `InscripcionDB` | Voy a ir, Mis planes, tabla de Inscritos |
| `IReviewRepository` | `ResenaDB` | Reseñas, Escribir reseña, Opiniones del lugar |
| `IReportRepository` | `ReporteDB` | Reportar, Moderación / Reportes |
| `IModerationRepository` | `ModeracionDB` | Moderación / Publicaciones, Historial, "Ver motivo" |

### DTOs alineados con las vistas

| Vista de surachih_v1.sql | Modelo |
|---|---|
| `v_event_card` | `EventoTarjeta` (+ `Interesados` y `Etiquetas`). `cancel_reason` → `MotivoCancelacion` (detalle de un evento cancelado) |
| `v_event_stats` | `EstadisticasEvento` |
| `v_place_rating` | `CalificacionLugar`, y `Calificacion` / `NumResenas` dentro de `LugarTarjeta` |

Los DTOs están en `Modelos/` (namespace `BackendLogica.Modelos`) junto a los demás modelos.

### Lo que el rediseño pide y la BD v1 todavía no guarda

Está en las interfaces y **funciona en el repositorio falso**; el real lanza `PendienteBDException`
("... todavía no está disponible: falta actualizar la base de datos.") hasta que se actualice el SQL:

| Del diseño | En la librería | Qué le falta a la BD |
|---|---|---|
| "Fecha por confirmar" / "Precio por confirmar" | `EventoTarjeta.FechaPorConfirmar` / `PrecioPorConfirmar` y lo mismo en `DatosEvento` | `start_at` / `end_at` son NOT NULL y el precio 0 significa "Gratis" |
| Borrador "Sin fecha" / "Sin ubicación" en el panel | `CrearAsync(..., enviarARevision: false)` con datos incompletos | Los CHECK de fecha y ubicación aplican también a borradores |

Tampoco están en la BD (se quedaron fuera de las interfaces): las unidades del precio de un lugar
("por persona", "la entrada"), las vistas "Últimos 30 días" del panel (la vista cuenta todas) y la fecha
"Enviado 8 oct" de un evento en revisión (`updated_at` no sale en `v_event_card`).

## Implementación real (MySQL) para las pantallas de las semanas 1–2

Todo esto funciona contra MySQL (repositorios de `Repositorios/`) y se comporta igual en el repositorio falso.

| Qué | Métodos | Tablas / vistas | Quién lo consume |
|---|---|---|---|
| Crear cuenta | `Usuarios.RegistrarAsync` (valida y guarda el hash **BCrypt**, costo 11) | `user` (`uq_user_username`, `uq_user_email`) | Ivan (cuenta) |
| Iniciar sesión | `Usuarios.IniciarSesionAsync` (usuario o correo; solo cuentas activas) | `user` | Ivan (cuenta) |
| Recuperar contraseña | `RecuperacionContrasena.SolicitarAsync` → token por correo; `RestablecerAsync(token, nueva)` | `password_reset`: se guarda **solo el SHA-256** del token, vale 1 hora, un solo uso; pedir otro invalida el anterior | Ivan (cuenta) |
| Eliminar cuenta | `Usuarios.DesactivarAsync` → `is_active = FALSE` (nunca DELETE) e invalida sus enlaces pendientes | `user`, `password_reset` | Ivan (Mi perfil) |
| Tarjetas y detalle de evento | `Eventos.ListarMasGuardadosAsync`, `ListarProximosAsync`, `FiltrarAsync`, `ObtenerAsync` | `v_event_card` (+ etiquetas activas, galería) | Andy (Landing), Carlos (Home), Betsy (detalle) |
| Destacados | `Eventos.ListarDestacadosAsync` (aprobados, vigentes, `is_featured`, en su `featured_order`) | `v_event_card` | Andy (Landing), Carlos (Home) |
| Lugares | `Lugares.ListarAprobadosAsync`, `ListarRecomendadosAsync`, `ObtenerAsync` | `place` aprobados + `place_hours` ("abierto ahora") + `v_place_rating` | Carlos (Mapa, Home), Andy (Landing) |
| Etiquetas | `Etiquetas.ListarAsync(alcance)` | `tag` con `is_active` (las apagadas tampoco salen en las tarjetas) | Carlos (Home), Andy (Landing) |
| Favoritos | `Favoritos.AgregarAsync` (sin duplicar), `QuitarAsync`, `AlternarAsync`, `ListarIdsAsync`, `ListarAsync` (y los de lugares) | `favorite`, `place_favorite` | Ivan (favoritos) |
| Inscripción | `Inscripciones.InscribirAsync` (reactiva si había cancelado), `CancelarAsync`, `EstaInscritoAsync`, `ListarPlanesAsync` | `inscription`; el trigger `trg_inscription_bi` rechaza eventos no aprobados o terminados con "No puedes inscribirte a este evento." (también al reactivar) | Betsy (detalle, Mis planes) |
| Vistas del evento | `Eventos.RegistrarVistaAsync` → 1 si contó, 0 si ese usuario ya lo vio hoy | `event_view`: **máximo 1 por usuario por evento por día**, garantizado por la llave `uq_view_user_day` (el día lo pone `trg_view_bi`); los visitantes siempre cuentan | Betsy (detalle) |

> **¿Ya tenías la BD cargada?** Corre `BaseDeDatos/vista_unica_por_dia.sql` una vez (no borra nada; quita
> vistas repetidas del mismo día y agrega la llave). Quien cargue `surachih_v1.sql` desde cero ya lo tiene.

**Probarlo contra MySQL de verdad:** `BackendLogica.Pruebas` tiene una sección que crea cuentas,
inscripciones y vistas, y que solo corre si la base se llama `*_pruebas` (para no ensuciar la tuya).
Carga `surachih_v1.sql` cambiando `surachih` por `surachih_pruebas` en sus tres primeras instrucciones,
pon `"Database": "surachih_pruebas"` en `dbsettings.local.json` y corre `dotnet run --project BackendLogica.Pruebas`.

## Implementación real (MySQL) para las pantallas de las semanas 3–4

| Qué | Métodos | Tablas / triggers | Quién lo consume |
|---|---|---|---|
| Solicitud de organizador | `Organizadores.SolicitarAsync` → queda `pending`; si la anterior fue rechazada se reenvía. WhatsApp: solo dígitos, 10 a 15 (`Validar.WhatsApp` y `ck_orgprof_whatsapp`) | `organizer_profile` | Ivan (Hazte organizador) |
| Publicar evento | `Eventos.CrearAsync(organizadorId, datos, enviarARevision)`: `true` → `pending` ("Enviar a revisión"), `false` → `draft` ("Guardar borrador"). Guarda evento, etiquetas y galería en una sola transacción. `EnviarARevisionAsync` manda un borrador o un rechazado corregido | `event`, `event_tag`, `event_image`; `trg_event_bi` (solo organizadores aprobados, lugar aprobado) | Betsy (Publicar evento) |
| Editar evento | `Eventos.ActualizarAsync` → `ResultadoEdicionEvento` (`null` si no es suyo o está cancelado). Si `VolvioARevision` es `true`, **avisar en la UI** que el evento regresó a moderación y salió del carrusel | `trg_event_bu` (título, descripción, foto principal, boletos o ubicación), `trg_eventimg_ai` (fotos nuevas). Fechas, precio, etiquetas o quitar fotos no lo regresan | Betsy (Publicar evento) |
| Fotos | `DatosEvento.Galeria`: las `secure_url` (https) que devuelve Cloudinary al subirlas, en orden. Al editar es la galería completa: las que faltan se quitan de la BD (el archivo sigue en Cloudinary) y solo las nuevas se insertan. `AgregarImagenAsync(eventoId, organizadorId, url)` → `null` si el evento no es suyo | `event_image` | Betsy (Publicar evento) |
| Moderación | `Moderacion.AprobarAsync` / `RechazarAsync` / `DecidirAsync` insertan en `moderation`; el trigger aplica la decisión. Rechazar exige comentario ("Escribe el motivo del rechazo.") | `trg_moderation_bi` (solo moderadores, objetivo pendiente, **nadie modera lo suyo**), `trg_moderation_ai`, `ck_mod_reject_reason` | Ivan (Moderación / Organizadores), Carlos (Publicaciones, Revisar evento) |
| Bandejas | `Eventos.ListarPorEstadoAsync(Pendiente)`, `Lugares.ListarPorEstadoAsync(Pendiente)`, `Organizadores.ListarPorEstadoAsync(Pendiente)`: **el que lleva más tiempo esperando primero** | eventos por `updated_at` (cuándo se envió o editó), lugares por `created_at`, organizadores por `requested_at` | Ivan (Moderación / Organizadores), Carlos (Publicaciones) |

Las pruebas de `BackendLogica.Pruebas` cubren todo esto contra el falso y, en una base `*_pruebas`, contra los triggers reales.

## Jerarquía de clases

```
ConexionDB  (abstracta: conexión, parámetros, lectura, transacciones, traducción de errores)
├── UsuarioDB                 registro (BCrypt), login, perfil, desactivar cuenta
├── RecuperacionContrasenaDB  tokens de un solo uso (se guarda solo el SHA-256)
├── OrganizadorDB             "Hazte organizador", perfiles, solicitudes por estado
├── EtiquetaDB                categorías / chips
├── LugarDB                   lugares + etiquetas + horario + "abierto ahora"
├── ResenaDB                  reseñas de eventos y de lugares (mismo código para ambas tablas)
├── ReporteDB                 reportes y su resolución
├── ModeracionDB              aprobar / rechazar / destacar, historial
└── ConsultasEventoDB  (abstracta: SELECT de v_event_card + etiquetas sin N+1)
    ├── EventoDB              destacados, más guardados, búsqueda, filtros, CRUD del organizador
    ├── FavoritoDB            corazón ("me interesa")
    └── InscripcionDB         "Voy a ir" y "Mis planes"

SuraChihBD     fachada real (ISuraChihBD) con una instancia de cada repositorio
SuraChihFalso  fachada falsa (ISuraChihBD) con los repositorios Falso*Repository en memoria
FabricaBD      elige una u otra según "UseFakeData"
```

### Qué resuelve `ConexionDB` para las clases hijas

| Método protegido | Para qué |
|---|---|
| `ConsultarAsync(sql, mapear, parametros)` | SELECT → `List<T>` |
| `ConsultarUnoAsync(...)` | primera fila o `null` |
| `EscalarAsync<T>(...)` | `COUNT(*)`, un id, un texto |
| `EjecutarAsync(...)` | INSERT/UPDATE/DELETE → filas afectadas |
| `InsertarAsync(...)` | INSERT → id autoincremental |
| `EnTransaccionAsync(tx => ...)` | varias operaciones todo-o-nada |

Parámetros con objeto anónimo (siempre parametrizados, nunca concatenados):

```csharp
ConsultarAsync("SELECT * FROM event WHERE id IN @ids AND status = @estado",
               Mapear, new { ids = new[] { 1, 2 }, estado = EstadoEvento.Aprobado });
// @ids se expande a (@ids0, @ids1) y el enum se guarda como 'approved'
```

Para leer columnas: `r.Texto("title")`, `r.DecimalONulo("price_min")`, `r.Enum<EstadoEvento>("status")`
(ver `Datos/LectorExtensiones.cs`).

## Errores

Todo error sale como `SuraChihException` (o una hija) con un mensaje listo para mostrar:

| Excepción | Cuándo |
|---|---|
| `ReglaNegocioException` | Un trigger rechazó la operación (ej. "Solo organizadores aprobados pueden publicar eventos.") |
| `DuplicadoException` | Usuario/correo repetido, segunda reseña, solicitud repetida |
| `DatosInvalidosException` | Validación previa o CHECK de la BD |
| `PendienteBDException` | El diseño lo pide pero la BD v1 todavía no lo guarda (solo el repositorio real) |
| `SinConexionException` | MySQL apagado o `dbsettings.local.json` incorrecto |

Las reglas de negocio importantes viven en los **triggers** de la BD (no dependen de que la app se
acuerde); la librería solo valida antes para dar mensajes claros.

**Convención del equipo:** `MySqlException.Number == 1644` (un `SIGNAL` de trigger) llega como
`ReglaNegocioException` con el mensaje del trigger **tal cual**. Las pantallas lo muestran sin reescribirlo:

```csharp
try { await bd.Inscripciones.InscribirAsync(usuario.Id, evento.Id); }
catch (SuraChihException ex) { MostrarError(ex.Message); }   // "No puedes inscribirte a este evento."
```

El repositorio falso lanza la misma excepción con los mismos textos (`Falso/MensajesTrigger.cs`). Si
cambias un trigger, cambia ahí su texto; una prueba en `BackendLogica.Pruebas` revisa que coincidan.

## Agregar un repositorio nuevo

1. Declara la interfaz en `Contratos/IAlgoRepository.cs`.
2. Crea `Repositorios/AlgoDB.cs` heredando de `ConexionDB` (o de `ConsultasEventoDB` si devuelve eventos)
   e implementando la interfaz. Constructor: `public AlgoDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }`
3. Crea `Falso/FalsoAlgoRepository.cs` (hereda de `RepositorioFalso`) con la misma lógica sobre `AlmacenFalso`.
4. Modelos en `Modelos/` como `record` (inmutables). Enums con `[ValorBD("texto_en_mysql")]`.
5. Agrégalo a `ISuraChihBD`, `SuraChihBD` y `SuraChihFalso`, y una prueba en `BackendLogica.Pruebas`.
