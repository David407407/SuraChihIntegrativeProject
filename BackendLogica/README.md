# BackendLogica

Librería de acceso a la BD `surachih` (MySQL). No sabe nada de la interfaz: la puede usar la app
WinForms, una consola de pruebas o, en el futuro, una API.

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
| `v_event_card` | `EventoTarjeta` (+ `Interesados` y `Etiquetas`) |
| `v_event_stats` | `EstadisticasEvento` |
| `v_place_rating` | `CalificacionLugar`, y `Calificacion` / `NumResenas` dentro de `LugarTarjeta` |

Los DTOs están en `Modelos/` (namespace `BackendLogica.Modelos`) junto a los demás modelos.

### Lo que el rediseño pide y la BD v1 todavía no guarda

Está en las interfaces y **funciona en el repositorio falso**; el real lanza `PendienteBDException`
("... todavía no está disponible: falta actualizar la base de datos.") hasta que se actualice el SQL:

| Del diseño | En la librería | Qué le falta a la BD |
|---|---|---|
| Corazón en tarjetas de lugar, pestaña "Lugares" de Mis favoritos | `IFavoriteRepository.AlternarLugarAsync`, `ListarIdsLugaresAsync`, `ListarLugaresAsync` | Tabla de favoritos de lugares |
| "Fecha por confirmar" / "Precio por confirmar" | `EventoTarjeta.FechaPorConfirmar` / `PrecioPorConfirmar` y lo mismo en `DatosEvento` | `start_at` / `end_at` son NOT NULL y el precio 0 significa "Gratis" |
| Borrador "Sin fecha" / "Sin ubicación" en el panel | `CrearAsync(..., enviarARevision: false)` con datos incompletos | Los CHECK de fecha y ubicación aplican también a borradores |

Tampoco están en la BD (se quedaron fuera de las interfaces): las unidades del precio de un lugar
("por persona", "la entrada"), las vistas "Últimos 30 días" del panel (la vista cuenta todas) y la fecha
"Enviado 8 oct" de un evento en revisión (`updated_at` no sale en `v_event_card`).

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
