# SuraChih: contexto para Claude

App de escritorio para encontrar eventos y lugares de la ciudad de Chihuahua. Es el proyecto integrador UTCH 2026.
El equipo trabaja en español: código, nombres, comentarios, commits y respuestas van en español.
Lee este archivo primero. Los README son largos; abre solo la sección que necesites (ver "Dónde buscar").

## Stack y capas

WPF (.NET 10, Windows) con BlazorWebView. Los componentes Razor usan Fluent UI Blazor con el diseño de Figma.
Una pantalla siempre baja por estas capas; no te saltes ninguna:

1. `Interfaz/Paginas/*.razor`: una página por ruta (`@page`).
2. `Interfaz/Componentes/**`: piezas reutilizables (Base, Tarjetas, Modales, Detalle, Mapa, Moderacion...).
3. `Interfaz/Servicios/Servicio*.cs`: lo que necesita una pantalla, en una sola llamada. Revisan la sesión con
   `sesion.Requerir("...")`. Se registran en `Interfaz/ServiciosInterfaz.cs`.
4. `BackendLogica/Contratos/I*Repository.cs` y `ISuraChihBD`: interfaces. La interfaz nunca ve MySQL.
5. `BackendLogica/Repositorios/*DB.cs`: el SQL, heredando de `Datos/ConexionDB.cs`
   (`ConsultarAsync`, `EjecutarAsync`, `InsertarAsync`, `EnTransaccionAsync`; los parámetros van con `@nombre`, y `IN @lista` se expande).
6. `BackendLogica/Falso/*`: el mismo contrato en memoria (`AlmacenFalso` replica las tablas y los datos de prueba del SQL).
7. `BaseDeDatos/surachih_v1.sql`: tablas, triggers (reglas de negocio) y vistas (`v_event_card`...).

`FabricaBD.Crear()` elige MySQL o datos falsos según `UseFakeData` en `dbsettings.local.json`.

## Estado global de la interfaz (`Interfaz/Estado`, `Interfaz/Servicios/Sesion.cs`)

- `Sesion`: usuario actual y `EsOrganizador`. Su evento `Cambio` hace que se repinten el navbar, el Home y el Perfil.
- `Modales.Abrir<T>(parametros)`: hay una sola modal. Regresa `Task<object?>` con lo que el componente pase a
  `Modales.Cerrar(resultado)`. Agrega un método tipado por cada modal nueva (ver `FiltrosAsync`, `ConfirmarInscripcionAsync`).
- `Hojas.AbrirEvento(id)` / `AbrirLugar(id)`: hoja de detalle encima de la pantalla.
- `Acciones` (rutas en `Acciones.Rutas`), `Avisos.Mostrar(texto, TipoAviso)` (toasts), `InteropJs` (JavaScript).
- Errores: BackendLogica lanza `SuraChihException` y sus hijas con un mensaje listo para mostrar. En la interfaz
  usa `MensajesError.Para(ex)`. Un `SIGNAL` de un trigger (error 1644) llega como `ReglaNegocioException`.

## Reglas del proyecto

- **Secretos:** el repo es público. Nunca hagas commit de `dbsettings.local.json`, nunca lo imprimas ni lo
  muestres (contiene la contraseña de MySQL y las llaves de Google y Cloudinary). Ya está en `.gitignore`.
- **BD compartida (Aiven, MySQL 8.4):** todo el equipo usa la misma.
  - **Nunca** corras `surachih_v1.sql` contra ella: empieza con `DROP DATABASE`.
  - Para cambiar la BD, crea `BaseDeDatos/<verbo>_<cosa>.sql` (por ejemplo `agregar_super_moderador.sql`) en
    una transacción, actualiza también `surachih_v1.sql` y **pide permiso al usuario antes de aplicarlo**.
- **Triggers que hay que conocer:**
  - Editar título, descripción, foto, boletos o ubicación de un evento o lugar aprobado lo regresa a
    `pending` (`trg_event_bu`, `trg_place_bu`); agregar fotos a la galería también.
  - Las decisiones se insertan en `moderation`, y el trigger cambia el estado. En los scripts, inserta en
    `moderation` con `VALUES`: con `INSERT ... SELECT FROM event` da el error 1442.
- **Repositorio falso:** cada método nuevo va en la interfaz, en `*DB.cs` **y** en `Falso*Repository.cs`, con el
  mismo comportamiento. Los datos sembrados en `AlmacenFalso` deben ser iguales a los de `surachih_v1.sql`
  (las pruebas lo revisan), y los mensajes de `Falso/MensajesTrigger.cs`, idénticos a los del SQL.
- **Pruebas:** agrega una a `BackendLogica.Pruebas/Program.cs` (script con `Prueba("...", async () => ...)`).
  Las que escriben en MySQL deben dejar los datos como estaban.
- **CSS:** aislado por componente (`X.razor.css`), con `::deep` para entrar a componentes hijos. Colores y
  tipografía de `Interfaz/wwwroot/css/tokens.css` (`var(--color-tinta)`...); no pongas hex sueltos. Lo global
  va en `wwwroot/css/controles.css`.
- **Fluent UI:** los botones `icon-only` ya tienen el arreglo de `max-width` en `controles.css`. Los íconos
  propios están en `wwwroot/assets/iconos/*.svg` (`<Icono Nombre="cerrar-16" />`).
- **Comentarios:** cada `.razor` empieza con un `@* ... *@` que explica qué frame de Figma es y qué hace.
  Sigue la misma densidad y el mismo tono.
- **Validación en 3 niveles:** en pantalla (`Utilidades/ReglasCuenta.cs`), en BackendLogica
  (`Seguridad/Seguridad.cs`: `Validar`, `Contrasenas` con BCrypt) y con CHECK o trigger en MySQL.
- El proyecto WPF necesita el TFM `net10.0-windows10.0.19041.0`; si lo bajas, sale un `FileNotFoundException` de
  `Microsoft.Windows.SDK.NET`.

## Comandos

```bash
dotnet build SuraChihIntegrativeProject.sln
dotnet run --project BackendLogica.Pruebas -- --falso
dotnet run --project Interfaz.Navegador --DatosFalsos true
```

- La primera línea compila todo (las advertencias CA1416 de `ServicioGoogle.cs` son normales).
- La segunda corre las pruebas solo con datos falsos, sin tocar Aiven. Sin `--falso` también corre las de MySQL.
- La tercera abre la interfaz en el navegador, en http://localhost:5180; `.claude/launch.json` la tiene como
  `interfaz-navegador`. Recargar la pestaña cierra la sesión.

Usuarios de prueba (contraseña en el README): `mariana_r`, `diego_m`, `sofia_c` (normales), `cafe_dande` y
`misiones_cuu` (organizadores), `mod_luis` (moderador) y `mod_ana` (supermoderadora).

## Git

- Las ramas `feature/<tema>` salen de **`version-0.2`**, que es la de integración, y los PR van hacia
  `version-0.2`, no hacia `master`.
- Revisa qué hay con `git log --oneline -15 origin/version-0.2` y `git branch -r --sort=-committerdate`.
- Estado al 13 nov 2026: en `version-0.2` ya están moderación (reportes únicos e historial), panel de
  organizador, registrar lugar y búsqueda.
- Ramas subidas pero sin merge: `feature/gustos` (gustos del usuario; requiere aplicar
  `BaseDeDatos/agregar_gustos.sql` en Aiven antes del merge) y `feature/cartelera-noviembre` (scripts de datos
  reales; los datos ya están en Aiven).

## Dónde buscar (abre solo la sección que necesites)

| Tema | Archivo |
| --- | --- |
| Cómo correrlo, entregar el .exe | `README.md` |
| Contratos, DTOs, errores, agregar un repositorio | `BackendLogica/README.md` (secciones `## Contratos`, `## Errores`, `## Agregar un repositorio nuevo`) |
| Estructura de la interfaz, estilos, Fluent | `Interfaz/README.md` (`## Estructura`, `## Estilos`, `## Fluent UI`) |
| Una pantalla concreta (moderación, mapa, publicar, registrar lugar...) | `Interfaz/README.md`, la sección con ese nombre |
| Componentes base y sus variantes | página `/componentes` (`Interfaz/Paginas/Catalogo.razor`) |
| Esquema, triggers y vistas | `BaseDeDatos/surachih_v1.sql` (busca `CREATE TRIGGER` o `CREATE VIEW`) |

## En qué trabajo yo

<!-- Cada quien llena esto en su copia local (o en su rama) antes de pedirle algo a Claude. -->
- Tarea actual:
- Rama:
- Archivos principales:
