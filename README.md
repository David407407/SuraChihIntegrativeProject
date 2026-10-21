# SuraChih

Eventos y lugares de Chihuahua en un solo lugar. Proyecto integrador UTCH 2026.

App de escritorio en C# (.NET 10, WinForms) cuya interfaz está hecha con **Blazor Hybrid**: componentes
Razor (C# + HTML + CSS) que se muestran dentro de la ventana con **BlazorWebView**. Los componentes corren
en el mismo proceso que la app y llaman directo a la librería **BackendLogica**, que habla con MySQL.

```
┌──────────────────────── SuraChihIntegrativeProject (WinForms) ────────────────────────┐
│  VentanaPrincipal ── BlazorWebView ── Interfaz (componentes .razor + CSS aislado)      │
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
| `Ventanas/`, `wwwroot/index.html` | Host de escritorio: la ventana con BlazorWebView y su página anfitriona. |
| `Legacy/` | Pantallas y código de la versión anterior (no se compilan). |

## Cómo correrlo

1. Crea la BD ejecutando `BaseDeDatos/surachih_v1.sql` completo en MySQL Workbench o en la consola
   `mysql` (MySQL 8.0.16+; los triggers usan `DELIMITER`).
2. Copia `dbsettings.example.json` como `dbsettings.local.json` (git lo ignora) y pon tus datos.
   Para trabajar pantallas **sin MySQL**, pon `"UseFakeData": true`: la app usa los datos de prueba
   en memoria (ver [BackendLogica/README.md](BackendLogica/README.md#trabajar-sin-mysql-repositorio-falso)).
3. Verifica la conexión y la librería:
   ```bash
   dotnet run --project BackendLogica.Pruebas
   ```
4. Abre `SuraChihIntegrativeProject.sln` en Visual Studio (Windows) y ejecuta `SuraChihIntegrativeProject`.
   Requiere [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (ya viene con Edge).
   En modo Debug, F12 abre las herramientas de desarrollo de la interfaz.

### Probar la interfaz en el navegador

El mismo proyecto `Interfaz` se puede abrir en un navegador normal, sin Python ni la app de escritorio:

```bash
dotnet run --project Interfaz.Navegador --DatosFalsos true
```

Abre <http://localhost:5180>. Con `--DatosFalsos true` usa los datos de prueba en memoria aunque tu
`dbsettings.local.json` apunte a MySQL; sin esa opción usa tu configuración, igual que la app.
Recargar la pestaña cierra la sesión.

> En macOS/Linux el proyecto de escritorio compila (`EnableWindowsTargeting`), pero WinForms solo se
> ejecuta en Windows. Para trabajar la interfaz sin Windows, usa `Interfaz.Navegador`.

Usuarios de prueba (contraseña `Test1234!`): `mariana_r`, `diego_m`, `sofia_c` (normales),
`cafe_dande` y `misiones_cuu` (organizadores), `mod_ana` y `mod_luis` (moderadores).

## Diseño

Figma: *Surachih web*, página **Rediseño**. Los colores y tipografías están como variables en
`Interfaz/wwwroot/css/tokens.css` con los mismos nombres que en Figma. La página `/componentes`
muestra todos los componentes base con sus variantes.
