using BackendLogica;
using BackendLogica.Configuracion;
using Interfaz;
using Interfaz.Navegador.Componentes;

// Servidor de prueba: la misma interfaz de la app de escritorio, en el navegador (Blazor Server).
// Usa dbsettings.local.json igual que la app. Con "--DatosFalsos true" usa los datos de prueba en
// memoria aunque el archivo diga otra cosa (sin MySQL y sin editar tu configuración).
var builder = WebApplication.CreateBuilder(args);

var configuracionBD = ConfiguracionBD.Predeterminada;
if (builder.Configuration.GetValue<bool>("DatosFalsos")) configuracionBD = configuracionBD with { UsarDatosFalsos = true };

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AgregarInterfaz(FabricaBD.Crear(configuracionBD));

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Rutas).Assembly);

app.Run();
