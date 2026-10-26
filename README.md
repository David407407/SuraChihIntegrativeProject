# SuraChih

Eventos y lugares de Chihuahua en un solo lugar. Proyecto integrador UTCH 2026.

App de escritorio en C# hecha con **WPF** (.NET 10, Windows; el resultado es un .exe) y **Blazor Hybrid**:
componentes Razor (C# + HTML + CSS) que se muestran dentro de la ventana con **BlazorWebView**. Los controles
son de **Fluent UI Blazor** con el diseño de Figma. Los componentes corren en el mismo proceso que la app y
llaman directo a la librería **BackendLogica**, que habla con MySQL.

```
┌───────────────────── SuraChihIntegrativeProject (WPF, Windows) ───────────────────────┐
│  MainWindow ── BlazorWebView ── Interfaz (componentes .razor + Fluent UI Blazor)       │
│                                            │                                          │
│                                  Interfaz/Servicios (Sesion, ServicioHome, ...)        │
└────────────────────────────────────────────┬──────────────────────────────────────────┘
                                             ▼
                BackendLogica (librería aparte, sin nada de interfaz)
                SuraChihBD → EventoDB, LugarDB, UsuarioDB, ... → ConexionDB → MySQL
```

## Estructura

| Carpeta | Qué hay |
|---|---|
| `BackendLogica/` | Librería de acceso a datos. Ver [BackendLogica/README.md](BackendLogica/README.md). |
| `BackendLogica.Pruebas/` | Pruebas de humo de la librería contra la BD local. |
| `BaseDeDatos/surachih_v1.sql` | Script de la BD (tablas, triggers, vistas y datos de prueba). |
| `Interfaz/` | La interfaz: componentes Razor, páginas, estilos y recursos. Ver [Interfaz/README.md](Interfaz/README.md). |
| `Interfaz.Navegador/` | Servidor de prueba para abrir la misma interfaz en un navegador (sin la app de escritorio). |
| `App.xaml`, `MainWindow.xaml`, `wwwroot/index.html`, `Resources/AppIcon/` | App de escritorio (WPF): arranque, ventana con BlazorWebView, su página anfitriona y el icono. |
| `Legacy/` | Pantallas y código de la versión anterior (no se compilan). |

## Cómo correrlo

1. La BD es **compartida**: vive en Aiven (MySQL 8.4) y todo el equipo usa la misma, así los eventos
   que alguien agrega los ven todos. Pide a quien administra el proyecto el archivo
   `dbsettings.local.json` (por WhatsApp o Drive, **nunca** en git: el repositorio es público) y ponlo
   en la raíz de la solución. Git ya lo ignora. La conexión a Aiven siempre va cifrada (SSL).

   **No corras `BaseDeDatos/surachih_v1.sql` contra la BD compartida:** empieza con
   `DROP DATABASE surachih` y borra los eventos de todo el equipo. Solo sirve para crear una BD
   nueva (por ejemplo, una local con `localhost` en tu `dbsettings.local.json`; requiere MySQL 8.0.16+
   y los triggers usan `DELIMITER`). La BD compartida tiene la zona horaria de Chihuahua (`-06:00`)
   porque la app compara `DateTime.Now` con `NOW()` de MySQL.
2. Para trabajar pantallas **sin MySQL**, pon `"UseFakeData": true` en `dbsettings.local.json`: la app
   usa los datos de prueba en memoria (ver [BackendLogica/README.md](BackendLogica/README.md#trabajar-sin-mysql-repositorio-falso)).
3. Verifica la conexión y la librería:
   ```bash
   dotnet run --project BackendLogica.Pruebas
   ```
4. Abre `SuraChihIntegrativeProject.sln` en Visual Studio (Windows) con la carga de trabajo
   **Desarrollo de escritorio de .NET** y ejecuta `SuraChihIntegrativeProject`. Desde la consola:
   ```bash
   dotnet run --project SuraChihIntegrativeProject.csproj
   ```
   Requiere [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (ya viene con Edge).
   En modo Debug, F12 abre las herramientas de desarrollo de la interfaz.

   Para entregar la app (no necesita .NET instalado en la otra computadora):
   ```bash
   dotnet publish SuraChihIntegrativeProject.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publicar
   ```
   En `publicar/` queda `SuraChih.exe` (todo .NET y las librerías van dentro) junto con la carpeta
   `wwwroot` y `SuraChih.staticwebassets.endpoints.json`, que son la interfaz: se copian juntos. Los
   `.pdb` y `.xml` no hacen falta. `dbsettings.local.json` va junto al .exe (o en una carpeta de arriba).

   La app WPF solo abre en Windows. Desde macOS o Linux se puede compilar (para revisar errores) pero no
   abrir: para ver la interfaz ahí usa `Interfaz.Navegador` (abajo).

### Probar la interfaz en el navegador

El mismo proyecto `Interfaz` se puede abrir en un navegador normal, sin Python ni la app de escritorio:

```bash
dotnet run --project Interfaz.Navegador --DatosFalsos true
```

Abre <http://localhost:5180>. Con `--DatosFalsos true` usa los datos de prueba en memoria aunque tu
`dbsettings.local.json` apunte a MySQL; sin esa opción usa tu configuración, igual que la app.
Recargar la pestaña cierra la sesión.

Usuarios de prueba (contraseña `Test1234!`): `mariana_r`, `diego_m`, `sofia_c` (normales),
`cafe_dande` y `misiones_cuu` (organizadores), `mod_ana` y `mod_luis` (moderadores).

## Diseño

Figma: *Surachih web*, página **Rediseño**. Los colores y tipografías están como variables en
`Interfaz/wwwroot/css/tokens.css` con los mismos nombres que en Figma. La página `/componentes`
muestra todos los componentes base con sus variantes.
