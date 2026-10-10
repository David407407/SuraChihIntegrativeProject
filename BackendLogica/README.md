# BackendLogica

Librería de acceso a la BD `surachih` (MySQL). No sabe nada de la interfaz: la puede usar la app
WinForms, una consola de pruebas o, en el futuro, una API.

## Uso

```csharp
var bd = new SuraChihBD();                       // lee dbsettings.local.json

var usuario  = await bd.Usuarios.IniciarSesionAsync("mariana_r", "Test1234!");
var eventos  = await bd.Eventos.ListarMasGuardadosAsync(limite: 5);
var lugares  = await bd.Lugares.ListarRecomendadosAsync();
bool guardado = await bd.Favoritos.AlternarAsync(usuario!.Id, eventoId: 1);
```

Todos los métodos son asíncronos (`...Async`) para no congelar la ventana.

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

SuraChihBD   fachada con una instancia de cada repositorio
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
| `SinConexionException` | MySQL apagado o `dbsettings.local.json` incorrecto |

Las reglas de negocio importantes viven en los **triggers** de la BD (no dependen de que la app se
acuerde); la librería solo valida antes para dar mensajes claros.

## Agregar un repositorio nuevo

1. Crea `Repositorios/AlgoDB.cs` heredando de `ConexionDB` (o de `ConsultasEventoDB` si devuelve eventos).
2. Constructor: `public AlgoDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }`
3. Modelos en `Modelos/` como `record` (inmutables). Enums con `[ValorBD("texto_en_mysql")]`.
4. Agrégalo como propiedad en `SuraChihBD` y una prueba en `BackendLogica.Pruebas`.
