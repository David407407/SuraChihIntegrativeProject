# Legacy

Código de la versión anterior, conservado como referencia. **No se compila** (está excluido en
`SuraChihIntegrativeProject.csproj`) ni forma parte de la solución.

| Carpeta / archivo | Qué era |
|---|---|
| `WinForms/` | Pantallas Login, Home, Local y Mapa dibujadas con GDI+ (antes del rediseño de Figma) y `Assets/mapa.html` del mapa con WebView2. |
| `PruebaBackend/` | Consola de prueba de la conexión vieja. La reemplaza `BackendLogica.Pruebas/`. |
| `ConexionDB.old.cs` | Clase única con CRUD de la tabla `users` de la BD anterior (contraseñas en texto plano). La reemplaza la librería `BackendLogica`. |

Las tipografías que usaba `Theme.cs` ahora están en `wwwroot/fonts/`.
