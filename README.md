# SuraChih

Eventos y lugares de Chihuahua en un solo lugar. Proyecto integrador UTCH 2026.

App de escritorio en C# (.NET 10, WinForms) cuya interfaz está hecha en HTML/CSS/JS y se muestra
con **WebView2**. Los datos vienen de MySQL a través de la librería **BackendLogica**.

```
┌──────────────────────── SuraChihIntegrativeProject (WinForms) ───────────────────────┐
│  VentanaPrincipal ── WebView2 ── wwwroot/ (landing en componentes HTML/CSS/JS)       │
│         │                              ▲  postMessage { id, accion, datos }          │
│         ▼                              │                                             │
│  Puente/PuenteWeb ── Controladores/ControladorLanding                                │
└───────────────────────────────┬──────────────────────────────────────────────────────┘
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
| `Ventanas/`, `Puente/`, `Controladores/`, `Servicios/` | Host de escritorio: ventana, canal JS ↔ C#, acciones y sesión. |
| `wwwroot/` | Interfaz. Ver [wwwroot/README.md](wwwroot/README.md). |
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
4. Abre `SuraChihIntegrativeProject.sln` en Visual Studio (Windows) y ejecuta. Requiere
   [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (ya viene con Edge).
   En modo Debug, F12 abre las herramientas de desarrollo de la página.

> En macOS/Linux el proyecto compila (`EnableWindowsTargeting`), pero WinForms solo se ejecuta en
> Windows. Para trabajar la interfaz sin Windows, abre `wwwroot` en un navegador (usa datos demo).

Usuarios de prueba (contraseña `Test1234!`): `mariana_r`, `diego_m`, `sofia_c` (normales),
`cafe_dande` y `misiones_cuu` (organizadores), `mod_ana` y `mod_luis` (moderadores).

## Diseño

Figma: *Surachih web*, página **Rediseño**, frame **Landing**. Los colores y tipografías están
como variables en `wwwroot/css/tokens.css` con los mismos nombres que en Figma.
