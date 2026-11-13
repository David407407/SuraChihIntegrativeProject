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
| `BackendLogica/` | Librería de acceso a datos: `Contratos/` (interfaces `I*Repository` e `ISuraChihBD`), `Repositorios/` (el SQL de cada tema: `EventoDB`, `LugarDB`, `UsuarioDB`...), `Falso/` (los mismos contratos con datos de prueba en memoria), `Modelos/`, `Datos/` (conexión, parámetros y traducción de errores de MySQL) y `Seguridad/` (validaciones y BCrypt). |
| `BackendLogica.Pruebas/` | Pruebas de la librería, con datos falsos y contra MySQL. |
| `BaseDeDatos/surachih_v1.sql` | Script completo de la BD: tablas, triggers (reglas de negocio), vistas y datos de prueba. |
| `Interfaz/` | La interfaz: `Paginas/` (una por ruta), `Componentes/` (piezas reutilizables), `Servicios/` (lo que necesita cada pantalla), `Estado/` (sesión, modales, avisos, navegación), `Utilidades/` y `wwwroot/` (CSS, JavaScript del mapa, íconos y fuentes). |
| `App.xaml`, `MainWindow.xaml`, `wwwroot/index.html`, `Resources/AppIcon/` | App de escritorio (WPF): arranque, ventana con BlazorWebView, su página anfitriona y el icono. |

Una pantalla siempre baja por las mismas capas: página → servicio de `Interfaz/Servicios` → contrato de
`BackendLogica/Contratos` → repositorio (`*DB.cs` con MySQL o `Falso*` en memoria) → MySQL. La interfaz nunca
escribe SQL. Cada método nuevo de un repositorio va en la interfaz, en su `*DB.cs` y en su `Falso*Repository.cs`.

Los cambios a la BD compartida se hacen con un script aparte (en una transacción), se aplican en Aiven con
permiso del equipo y se reflejan también en `surachih_v1.sql`, que siempre debe poder crear la BD completa.

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
   usa los datos de prueba en memoria (`BackendLogica/Falso`, los mismos de `surachih_v1.sql`; se pierden al
   cerrar la app).
   Para subir fotos en "Publicar evento" agrega también `"CloudinaryCloudName"` y `"CloudinaryUploadPreset"`
   (un upload preset *unsigned* de la cuenta de Cloudinary del equipo).
3. Verifica la conexión y la librería (con `-- --falso` solo corren las pruebas con datos falsos, sin tocar la BD):
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

   La app solo abre en Windows.

Usuarios de prueba (contraseña `Test1234!`): `mariana_r`, `diego_m`, `sofia_c` (normales),
`cafe_dande` y `misiones_cuu` (organizadores), `mod_luis` (moderador) y `mod_ana` (supermoderadora).

## Diseño

Figma: *Surachih web*, página **Rediseño**. Los colores y tipografías están como variables en
`Interfaz/wwwroot/css/tokens.css` con los mismos nombres que en Figma. La página `/componentes`
muestra todos los componentes base con sus variantes.
