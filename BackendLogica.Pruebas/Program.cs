// Pruebas de humo de BackendLogica contra la BD local con los datos de BaseDeDatos/surachih_v1.sql.
// Uso:  dotnet run --project BackendLogica.Pruebas
// Solo lee datos, salvo el favorito de prueba, que se agrega y se quita (queda igual que antes).

using BackendLogica;
using BackendLogica.Datos;
using BackendLogica.Modelos;

var bd = new SuraChihBD();
int fallas = 0;

async Task Prueba(string nombre, Func<Task<string>> cuerpo)
{
    try
    {
        string detalle = await cuerpo();
        Console.WriteLine($"  ✓ {nombre}{(detalle.Length > 0 ? " — " + detalle : "")}");
    }
    catch (Exception ex)
    {
        fallas++;
        Console.WriteLine($"  ✗ {nombre}: {ex.GetType().Name}: {ex.Message}");
    }
}

void Afirmar(bool condicion, string mensaje)
{
    if (!condicion) throw new Exception(mensaje);
}

Console.WriteLine($"Servidor: {bd.Configuracion.Servidor}:{bd.Configuracion.Puerto} / {bd.Configuracion.BaseDeDatos}\n");

Console.WriteLine("Sin base de datos");
await Prueba("Horario que cruza medianoche sigue abierto a la 1:00", () =>
{
    var horario = new[] { new HorarioDia(DayOfWeek.Friday, new(18, 0, 0), new(2, 0, 0)) };
    var sabado1am = new DateTime(2026, 10, 10, 1, 0, 0);
    var estado = EstadoHorario.Calcular(horario, sabado1am);
    Afirmar(estado.Abierto && estado.Hasta == new TimeSpan(2, 0, 0), "debería estar abierto hasta las 2:00");
    return Task.FromResult("");
});
await Prueba("Antes de abrir indica la hora de apertura", () =>
{
    var horario = new[] { new HorarioDia(DayOfWeek.Monday, new(8, 0, 0), new(21, 0, 0)) };
    var estado = EstadoHorario.Calcular(horario, new DateTime(2026, 10, 12, 7, 0, 0));
    Afirmar(!estado.Abierto && estado.AbreA == new TimeSpan(8, 0, 0), "debería abrir a las 8:00");
    return Task.FromResult("");
});
await Prueba("EnumBD convierte en ambos sentidos", () =>
{
    Afirmar(EnumBD.Escribir(MotivoReporte.InformacionFalsa) == "false_info", "escritura");
    Afirmar(EnumBD.Leer<EstadoEvento>("cancelled") == EstadoEvento.Cancelado, "lectura");
    return Task.FromResult("");
});

Console.WriteLine("\nConexión y lecturas");
Afirmar(await bd.ProbarConexionAsync(), "No hay conexión con MySQL; revisa dbsettings.local.json.");

await Prueba("Resumen de la landing", async () =>
{
    var r = await bd.ObtenerResumenAsync();
    Afirmar(r.LugaresParaDescubrir >= 1 && r.Categorias >= 1, "conteos vacíos");
    return $"{r.EventosEstaSemana} eventos esta semana, {r.LugaresParaDescubrir} lugares, {r.Categorias} categorías";
});
await Prueba("Etiquetas para eventos", async () =>
{
    var etiquetas = await bd.Etiquetas.ListarAsync(AlcanceEtiqueta.Evento);
    Afirmar(etiquetas.All(e => e.Alcance != AlcanceEtiqueta.Lugar), "se colaron etiquetas solo de lugar");
    return string.Join(", ", etiquetas.Select(e => e.Nombre));
});
await Prueba("Eventos destacados", async () =>
{
    var eventos = await bd.Eventos.ListarDestacadosAsync();
    return string.Join(" | ", eventos.Select(e => $"{e.Titulo} [{string.Join(",", e.Etiquetas.Select(t => t.Nombre))}]"));
});
await Prueba("Eventos más guardados", async () =>
{
    var eventos = await bd.Eventos.ListarMasGuardadosAsync();
    Afirmar(eventos.All(e => e.Estado == EstadoEvento.Aprobado && !e.Terminado), "solo deben salir vigentes");
    return string.Join(" | ", eventos.Select(e => $"{e.Titulo} ({e.Interesados}♥, {e.LugarNombre ?? e.Direccion})"));
});
await Prueba("Búsqueda sin acentos ('cafe')", async () =>
{
    var eventos = await bd.Eventos.BuscarAsync("cafe");
    Afirmar(eventos.Count > 0, "debería encontrar la cata de café");
    return string.Join(" | ", eventos.Select(e => e.Titulo));
});
await Prueba("Filtro por etiquetas (Familiar)", async () =>
{
    var eventos = await bd.Eventos.FiltrarPorEtiquetasAsync([1]);
    return $"{eventos.Count} evento(s)";
});
await Prueba("Detalle de evento con galería", async () =>
{
    var detalle = await bd.Eventos.ObtenerAsync(2);
    Afirmar(detalle != null, "no existe el evento 2");
    return $"{detalle!.Evento.Titulo}, {detalle.Galeria.Count} foto(s)";
});
await Prueba("Lugares recomendados con horario", async () =>
{
    var lugares = await bd.Lugares.ListarRecomendadosAsync();
    Afirmar(lugares.Count > 0, "sin lugares");
    return string.Join(" | ", lugares.Select(l =>
        $"{l.Nombre} ★{l.Calificacion} {(l.HorarioAhora.Abierto ? $"abierto hasta {l.HorarioAhora.Hasta:hh\\:mm}" : "cerrado")}"));
});
await Prueba("Estadísticas del organizador 4", async () =>
{
    var stats = await bd.Eventos.ListarEstadisticasAsync(4);
    return string.Join(" | ", stats.Select(s => $"#{s.EventoId}: {s.Vistas} vistas, {s.Interesados}♥, {s.Inscritos} inscritos"));
});
await Prueba("Mis planes (próximos) de mariana_r", async () =>
{
    var planes = await bd.Inscripciones.ListarPlanesAsync(5, BackendLogica.Repositorios.InscripcionDB.Pestana.Proximos);
    return string.Join(" | ", planes.Select(p => p.Evento.Titulo));
});
await Prueba("Reseñas del lugar 1", async () =>
{
    var resenas = await bd.Resenas.ListarAsync(TipoResena.Lugar, 1);
    return string.Join(" | ", resenas.Select(r => $"{r.NombreUsuario}: {r.Calificacion}★"));
});
await Prueba("Historial de moderación", async () => $"{(await bd.Moderacion.ListarHistorialAsync()).Count} decisiones");
await Prueba("Solicitudes de organizador pendientes", async () =>
    string.Join(", ", (await bd.Organizadores.ListarPorEstadoAsync(EstadoOrganizador.Pendiente)).Select(o => o.NombreNegocio)));

Console.WriteLine("\nSesión");
await Prueba("Login correcto con usuario y BCrypt $2b$", async () =>
{
    var usuario = await bd.Usuarios.IniciarSesionAsync("mariana_r", "Test1234!");
    Afirmar(usuario != null, "debería iniciar sesión");
    return $"{usuario!.NombreUsuario} <{usuario.Correo}>";
});
await Prueba("Login correcto con correo", async () =>
{
    Afirmar(await bd.Usuarios.IniciarSesionAsync("ana.mod@example.com", "Test1234!") is { EsModerador: true }, "ana es moderadora");
    return "";
});
await Prueba("Login con contraseña incorrecta", async () =>
{
    Afirmar(await bd.Usuarios.IniciarSesionAsync("mariana_r", "otra") == null, "no debería entrar");
    return "";
});
await Prueba("Registro rechaza contraseña débil (sin tocar la BD)", async () =>
{
    try { await bd.Usuarios.RegistrarAsync("nuevo_user", "nuevo@example.com", "123"); }
    catch (DatosInvalidosException ex) { return ex.Message; }
    throw new Exception("debió rechazarse");
});

Console.WriteLine("\nReglas de negocio y escrituras reversibles");
await Prueba("Un usuario normal no puede publicar eventos (trigger → ReglaNegocioException)", async () =>
{
    try
    {
        await bd.Eventos.CrearAsync(5, new DatosEvento
        {
            Titulo = "Prueba", Descripcion = "No debería guardarse", LugarId = 1,
            Inicio = DateTime.Now.AddDays(3), Fin = DateTime.Now.AddDays(3).AddHours(2)
        });
    }
    catch (ReglaNegocioException ex) { return ex.Message; }
    throw new Exception("debió rechazarse");
});
await Prueba("Favorito: alternar dos veces deja todo igual", async () =>
{
    bool antes = (await bd.Favoritos.ListarIdsAsync(7)).Contains(1);
    bool primero = await bd.Favoritos.AlternarAsync(7, 1);
    bool segundo = await bd.Favoritos.AlternarAsync(7, 1);
    bool despues = (await bd.Favoritos.ListarIdsAsync(7)).Contains(1);
    Afirmar(primero != segundo && antes == despues, "el estado no regresó");
    return $"{antes} → {primero} → {segundo}";
});

Console.WriteLine(fallas == 0 ? "\nTodo bien ✓" : $"\n{fallas} prueba(s) fallaron ✗");
return fallas == 0 ? 0 : 1;
