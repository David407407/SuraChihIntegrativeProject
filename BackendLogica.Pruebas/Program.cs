// Pruebas de humo de BackendLogica: primero contra el repositorio falso (sin MySQL) y luego contra la
// BD local con los datos de BaseDeDatos/surachih_v1.sql.
// Uso:  dotnet run --project BackendLogica.Pruebas              (falso + MySQL)
//       dotnet run --project BackendLogica.Pruebas -- --falso   (solo el falso, sin MySQL)
// Contra MySQL solo lee datos, salvo el favorito de prueba, que se agrega y se quita (queda igual que antes).

using BackendLogica;
using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Falso;
using BackendLogica.Modelos;
using BackendLogica.Repositorios;

bool soloFalso = args.Contains("--falso");
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

async Task<string> EsperarError<TError>(Func<Task> accion, string? mensajeExacto = null) where TError : Exception
{
    try { await accion(); }
    catch (TError ex)
    {
        Afirmar(mensajeExacto == null || ex.Message == mensajeExacto, $"mensaje distinto: \"{ex.Message}\"");
        return ex.Message;
    }
    throw new Exception($"debió lanzar {typeof(TError).Name}");
}

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

await PruebasFalso();

if (soloFalso)
{
    Console.WriteLine(fallas == 0 ? "\nTodo bien ✓" : $"\n{fallas} prueba(s) fallaron ✗");
    return fallas == 0 ? 0 : 1;
}

var bd = new SuraChihBD();
Console.WriteLine($"\nServidor: {bd.Configuracion.Servidor}:{bd.Configuracion.Puerto} / {bd.Configuracion.BaseDeDatos}");
Console.WriteLine("\nConexión y lecturas");
if (!await bd.ProbarConexionAsync())
{
    Console.WriteLine("  ✗ No hay conexión con MySQL; revisa dbsettings.local.json (o usa --falso).");
    return 1;
}

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
    var planes = await bd.Inscripciones.ListarPlanesAsync(5, PestanaPlanes.Proximos);
    return string.Join(" | ", planes.Select(p => p.Evento.Titulo));
});
await Prueba("Reseñas del lugar 1", async () =>
{
    var resenas = await bd.Resenas.ListarAsync(TipoResena.Lugar, 1);
    return string.Join(" | ", resenas.Select(r => $"{r.NombreUsuario}: {r.Calificacion}★"));
});
await Prueba("Historial de moderación", async () => $"{(await bd.Moderacion.ListarHistorialAsync()).Count} decisiones");

Console.WriteLine("\nConsultas nuevas del rediseño (solo lectura)");
await Prueba("FiltrarAsync: Gastronomía, de pago, cualquier fecha", async () =>
{
    var eventos = await bd.Eventos.FiltrarAsync(new FiltroEventos { EtiquetaIds = [5], Precio = FiltroPrecio.DePago });
    Afirmar(eventos.All(e => !e.EsGratis && e.Etiquetas.Any(t => t.Id == 5)), "se coló un evento que no cumple");
    return string.Join(" | ", eventos.Select(e => e.Titulo));
});
await Prueba("FiltrarAsync: texto + este fin de semana", async () =>
    $"{(await bd.Eventos.FiltrarAsync(new FiltroEventos { Texto = "mision", Cuando = FiltroCuando.EsteFinDeSemana })).Count} evento(s)");
await Prueba("ListarVistasPorDiaAsync (evento 1)", async () =>
{
    var dias = await bd.Eventos.ListarVistasPorDiaAsync(1);
    Afirmar(dias.Count == 7, "deben ser 7 días");
    return string.Join(" ", dias.Select(d => d.Vistas));
});
await Prueba("ListarInscritosAsync solo para el dueño", async () =>
{
    var inscritos = await bd.Inscripciones.ListarInscritosAsync(2, organizadorId: 3);
    Afirmar((await bd.Inscripciones.ListarInscritosAsync(2, organizadorId: 4)).Count == 0, "otro organizador no debe verlos");
    return string.Join(", ", inscritos.Select(i => "@" + i.NombreUsuario));
});
await Prueba("ObtenerUltimaDecisionAsync (organizador 3)", async () =>
    (await bd.Moderacion.ObtenerUltimaDecisionAsync(TipoObjetivoModeracion.Organizador, 3))?.Comentario ?? "sin decisión");
await Prueba("ObtenerResumenAsync (reseñas del lugar 1) y v_place_rating", async () =>
{
    var resumen = await bd.Resenas.ObtenerResumenAsync(TipoResena.Lugar, 1);
    var vista = await bd.Lugares.ObtenerCalificacionAsync(1);
    Afirmar(vista != null && vista.Calificacion == resumen.Promedio && vista.NumResenas == resumen.Total, "no coincide con la vista");
    return $"{resumen.Promedio}★ de {resumen.Total}";
});
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

// ----------------------------------------------------------------------
// Repositorio falso: las mismas interfaces, sin MySQL
// ----------------------------------------------------------------------

async Task PruebasFalso()
{
    ISuraChihBD f = new SuraChihFalso();
    Console.WriteLine("\nRepositorio falso (sin MySQL)");

    await Prueba("FabricaBD elige el falso con UseFakeData", () =>
    {
        Afirmar(FabricaBD.Crear(new ConfiguracionBD { UsarDatosFalsos = true }) is SuraChihFalso, "no devolvió el falso");
        Afirmar(FabricaBD.Crear(new ConfiguracionBD()) is SuraChihBD, "no devolvió el real");
        return Task.FromResult("");
    });
    await Prueba("Los mensajes de trigger del falso son idénticos a los del SQL", () =>
    {
        string sql = File.ReadAllText(BuscarHaciaArriba(Path.Combine("BaseDeDatos", "surachih_v1.sql")));
        var mensajes = typeof(MensajesTrigger).GetFields().Select(c => (string)c.GetValue(null)!).ToList();
        var faltan = mensajes.Where(m => !sql.Contains($"MESSAGE_TEXT = '{m}'")).ToList();
        Afirmar(faltan.Count == 0, "no están en el SQL: " + string.Join(" | ", faltan));
        return Task.FromResult($"{mensajes.Count} mensajes");
    });

    // --- Lecturas (mismos datos que surachih_v1.sql) ---
    await Prueba("Resumen de la landing", async () =>
    {
        var r = await f.ObtenerResumenAsync();
        Afirmar(r.LugaresParaDescubrir == 2 && r.Categorias == 5, $"{r.LugaresParaDescubrir} lugares, {r.Categorias} categorías");
        return $"{r.EventosEstaSemana} eventos esta semana, {r.LugaresParaDescubrir} lugares, {r.Categorias} categorías";
    });
    await Prueba("ITagRepository: etiquetas para eventos", async () =>
    {
        var etiquetas = await f.Etiquetas.ListarAsync(AlcanceEtiqueta.Evento);
        Afirmar(etiquetas.Count == 5 && etiquetas.All(e => e.Alcance != AlcanceEtiqueta.Lugar), "deberían ser 5, sin las de lugar");
        return string.Join(", ", etiquetas.Select(e => e.Nombre));
    });
    await Prueba("IEventRepository: destacados y más guardados", async () =>
    {
        var destacados = await f.Eventos.ListarDestacadosAsync();
        var guardados = await f.Eventos.ListarMasGuardadosAsync();
        Afirmar(destacados.Single().Id == 1, "el destacado es Misión 404");
        Afirmar(guardados.First().Interesados == 2 && guardados.All(e => !e.Terminado), "orden por corazones, solo vigentes");
        return string.Join(" | ", guardados.Select(e => $"{e.Titulo} ({e.Interesados}♥, {e.LugarNombre ?? e.Direccion})"));
    });
    await Prueba("IEventRepository: búsqueda sin acentos ('cafe')", async () =>
    {
        var eventos = await f.Eventos.BuscarAsync("cafe");
        Afirmar(eventos.Any(e => e.Id == 2), "debería encontrar la cata de café");
        return string.Join(" | ", eventos.Select(e => e.Titulo));
    });
    await Prueba("IEventRepository: filtros del rediseño (Gastronomía + rango del 24 oct)", async () =>
    {
        var eventos = await f.Eventos.FiltrarAsync(new FiltroEventos
        {
            EtiquetaIds = [5], Precio = FiltroPrecio.DePago, Cuando = FiltroCuando.Rango,
            Desde = new DateTime(2026, 10, 24), Hasta = new DateTime(2026, 10, 24)
        });
        Afirmar(eventos.Count == 1 && eventos[0].Id == 2, "debería salir solo la cata");
        var gratis = await f.Eventos.FiltrarAsync(new FiltroEventos { Precio = FiltroPrecio.Gratis });
        Afirmar(gratis.All(e => e.EsGratis), "Gratis solo debe traer eventos gratis");
        return eventos[0].Titulo;
    });
    await Prueba("IEventRepository: detalle con galería", async () =>
    {
        var detalle = await f.Eventos.ObtenerAsync(2);
        Afirmar(detalle != null && detalle.Galeria.Count == 1 && detalle.Evento.Direccion == "C. Monte Bello 4334, Chihuahua",
            "la dirección debe venir del lugar (COALESCE de v_event_card)");
        return $"{detalle!.Evento.Titulo}, {detalle.Galeria.Count} foto(s)";
    });
    await Prueba("IEventRepository: estadísticas (v_event_stats) y últimos 7 días", async () =>
    {
        var stats = await f.Eventos.ListarEstadisticasAsync(4);
        var mision = stats.Single(s => s.EventoId == 1);
        var carrera = stats.Single(s => s.EventoId == 3);
        Afirmar(mision is { Vistas: 4, Interesados: 2, Inscritos: 1 }, "embudo de Misión 404");
        Afirmar(carrera is { Calificacion: 5.0m, NumResenas: 1 }, "calificación de la carrera");
        var dias = await f.Eventos.ListarVistasPorDiaAsync(1);
        Afirmar(dias.Count == 7 && dias[^1].Vistas == 4, "7 días, hoy con 4 vistas");
        return string.Join(" | ", stats.Select(s => $"#{s.EventoId}: {s.Vistas} vistas, {s.Interesados}♥, {s.Inscritos} inscritos"));
    });
    await Prueba("IPlaceRepository: recomendados y v_place_rating", async () =>
    {
        var lugares = await f.Lugares.ListarRecomendadosAsync();
        var calificacion = await f.Lugares.ObtenerCalificacionAsync(1);
        Afirmar(calificacion is { Calificacion: 4.5m, NumResenas: 2 }, "Dandelion: (5 + 4) / 2 = 4.5");
        return string.Join(" | ", lugares.Select(l =>
            $"{l.Nombre} ★{l.Calificacion} {(l.HorarioAhora.Abierto ? $"abierto hasta {l.HorarioAhora.Hasta:hh\\:mm}" : "cerrado")}"));
    });
    await Prueba("IReviewRepository: reseñas y resumen por estrellas", async () =>
    {
        var resenas = await f.Resenas.ListarAsync(TipoResena.Lugar, 1);
        var resumen = await f.Resenas.ObtenerResumenAsync(TipoResena.Lugar, 1);
        Afirmar(resenas.Count == 2 && resumen is { Promedio: 4.5m, Total: 2 } && resumen.PorEstrellas[5] == 1, "resumen del lugar 1");
        return string.Join(" | ", resenas.Select(r => $"{r.NombreUsuario}: {r.Calificacion}★"));
    });
    await Prueba("IModerationRepository: historial de los datos de prueba", async () =>
    {
        var historial = await f.Moderacion.ListarHistorialAsync();
        Afirmar(historial.Count == 7, $"{historial.Count} decisiones");
        return $"{historial.Count} decisiones";
    });
    await Prueba("IOrganizerRepository: solicitudes pendientes", async () =>
    {
        var pendientes = await f.Organizadores.ListarPorEstadoAsync(EstadoOrganizador.Pendiente);
        Afirmar(pendientes.Single().UsuarioId == 7, "sofia_c está pendiente");
        return pendientes[0].NombreNegocio;
    });

    // --- Sesión ---
    await Prueba("IUserRepository: login con usuario y con correo", async () =>
    {
        Afirmar(await f.Usuarios.IniciarSesionAsync("mariana_r", "Test1234!") is { Id: 5 }, "mariana_r");
        Afirmar(await f.Usuarios.IniciarSesionAsync("ANA.MOD@example.com", "Test1234!") is { EsModerador: true }, "correo sin distinguir mayúsculas");
        Afirmar(await f.Usuarios.IniciarSesionAsync("mariana_r", "otra") == null, "contraseña incorrecta");
        return "";
    });
    await Prueba("IUserRepository: registro y duplicados", async () =>
    {
        await EsperarError<DatosInvalidosException>(() => f.Usuarios.RegistrarAsync("nuevo_user", "nuevo@example.com", "123"));
        var nuevo = await f.Usuarios.RegistrarAsync("nuevo_user", "Nuevo@Example.com", "Clave1234");
        Afirmar(nuevo.Correo == "nuevo@example.com", "el correo se guarda en minúsculas");
        return await EsperarError<DuplicadoException>(() => f.Usuarios.RegistrarAsync("otro_user", "mariana.r@example.com", "Clave1234"),
            "Ese correo ya está registrado.");
    });
    await Prueba("IPasswordResetRepository: el token sirve una sola vez", async () =>
    {
        string? token = await f.RecuperacionContrasena.SolicitarAsync("diego.m@example.com");
        Afirmar(token != null && await f.RecuperacionContrasena.RestablecerAsync(token, "NuevaClave1"), "primer uso");
        Afirmar(!await f.RecuperacionContrasena.RestablecerAsync(token!, "OtraClave1"), "segundo uso");
        Afirmar(await f.Usuarios.IniciarSesionAsync("diego_m", "NuevaClave1") != null, "entra con la nueva");
        return "";
    });

    // --- Reglas de negocio: misma excepción y mismo texto que el trigger (1644) ---
    await Prueba("Un usuario normal no puede publicar (trigger → ReglaNegocioException)", () =>
        EsperarError<ReglaNegocioException>(() => f.Eventos.CrearAsync(5, new DatosEvento
        {
            Titulo = "Prueba", Descripcion = "No debería guardarse", LugarId = 1,
            Inicio = DateTime.Now.AddDays(3), Fin = DateTime.Now.AddDays(3).AddHours(2)
        }), MensajesTrigger.SoloOrganizadoresEventos));
    await Prueba("No puedes inscribirte a un evento terminado", () =>
        EsperarError<ReglaNegocioException>(() => f.Inscripciones.InscribirAsync(6, 3), MensajesTrigger.NoPuedesInscribirte));
    await Prueba("Solo puedes reseñar eventos que ya terminaron", () =>
        EsperarError<ReglaNegocioException>(() => f.Resenas.CrearAsync(TipoResena.Evento, 1, 6, 5, null), MensajesTrigger.ResenaEventoNoTerminado));
    await Prueba("Una reseña por usuario", () =>
        EsperarError<DuplicadoException>(() => f.Resenas.CrearAsync(TipoResena.Lugar, 1, 5, 3, "otra"), "Ya dejaste una reseña aquí."));
    await Prueba("Solo moderadores pueden moderar", () =>
        EsperarError<ReglaNegocioException>(() => f.Moderacion.AprobarAsync(5, TipoObjetivoModeracion.Evento, 4), MensajesTrigger.SoloModeradores));

    // --- Flujos completos ---
    await Prueba("Borrador sin fecha ni ubicación → enviar a revisión exige completarlo", async () =>
    {
        int id = await f.Eventos.CrearAsync(4, new DatosEvento
        {
            Titulo = "Noche de acertijos en el Centro", Descripcion = "Borrador", FechaPorConfirmar = true,
            PrecioPorConfirmar = true, Inicio = default, Fin = default
        }, enviarARevision: false);
        var borrador = (await f.Eventos.ObtenerAsync(id))!.Evento;
        Afirmar(borrador is { Estado: EstadoEvento.Borrador, FechaPorConfirmar: true, PrecioPorConfirmar: true }, "borrador incompleto");
        return await EsperarError<DatosInvalidosException>(() => f.Eventos.EnviarARevisionAsync(id, 4));
    });
    await Prueba("Moderación aprueba el evento 4 y \"Ver motivo\" ve la decisión", async () =>
    {
        await f.Moderacion.AprobarAsync(1, TipoObjetivoModeracion.Evento, 4);
        var decision = await f.Moderacion.ObtenerUltimaDecisionAsync(TipoObjetivoModeracion.Evento, 4);
        Afirmar((await f.Eventos.ObtenerAsync(4))!.Evento.Estado == EstadoEvento.Aprobado, "quedó aprobado");
        Afirmar(decision is { ModeradorNombre: "mod_ana", Decision: DecisionModeracion.Aprobado }, "última decisión");
        return "";
    });
    await Prueba("Editar un evento aprobado lo regresa a moderación y lo saca del carrusel", async () =>
    {
        var mision = (await f.Eventos.ObtenerAsync(1))!.Evento;
        await f.Eventos.ActualizarAsync(1, 4, new DatosEvento
        {
            Titulo = mision.Titulo + " (nueva edición)", Descripcion = mision.Descripcion, Inicio = mision.Inicio, Fin = mision.Fin,
            Direccion = mision.Direccion, Latitud = mision.Latitud, Longitud = mision.Longitud,
            PrecioMin = mision.PrecioMin, PrecioMax = mision.PrecioMax, BoletosUrl = mision.BoletosUrl, ImagenUrl = mision.ImagenUrl,
            EtiquetaIds = mision.Etiquetas.Select(t => t.Id).ToList()
        });
        var editado = (await f.Eventos.ObtenerAsync(1))!.Evento;
        Afirmar(editado is { Estado: EstadoEvento.Pendiente, Destacado: false }, "debió volver a Pendiente");
        return "";
    });
    await Prueba("Inscripción, Mis planes e inscritos del organizador", async () =>
    {
        await f.Inscripciones.InscribirAsync(7, 2);
        var planes = await f.Inscripciones.ListarPlanesAsync(7, PestanaPlanes.Proximos);
        var inscritos = await f.Inscripciones.ListarInscritosAsync(2, organizadorId: 3);
        Afirmar(planes.Any(p => p.Evento.Id == 2), "la cata está en Mis planes");
        Afirmar(inscritos.Count == 2 && (await f.Inscripciones.ListarInscritosAsync(2, organizadorId: 4)).Count == 0,
            "solo el dueño ve los inscritos");
        return string.Join(", ", inscritos.Select(i => "@" + i.NombreUsuario));
    });
    await Prueba("Favoritos de eventos y de lugares (solo en el falso)", async () =>
    {
        bool evento = await f.Favoritos.AlternarAsync(7, 1);
        bool lugar = await f.Favoritos.AlternarLugarAsync(7, 2);
        Afirmar(evento && lugar && (await f.Favoritos.ListarLugaresAsync(7)).Single().Id == 2, "quedaron guardados");
        Afirmar(!await f.Favoritos.AlternarLugarAsync(7, 2), "alternar otra vez lo quita");
        return "";
    });
    await Prueba("Reportes: crear y resolver", async () =>
    {
        int id = await f.Reportes.CrearAsync(6, TipoObjetivoReporte.ResenaLugar, 3, MotivoReporte.Spam, null);
        Afirmar(await f.Reportes.ResolverAsync(id, 1, EstadoReporte.Descartado), "resolver");
        Afirmar((await f.Reportes.ListarAsync()).Count == 0, "ya no está abierto");
        return "";
    });
    await Prueba("Repositorio real: favoritos de lugares → PendienteBDException (sin tocar MySQL)", () =>
        EsperarError<PendienteBDException>(() => new FavoritoDB().AlternarLugarAsync(5, 1)));
}

static string BuscarHaciaArriba(string rutaRelativa)
{
    for (var carpeta = new DirectoryInfo(AppContext.BaseDirectory); carpeta != null; carpeta = carpeta.Parent)
    {
        string candidato = Path.Combine(carpeta.FullName, rutaRelativa);
        if (File.Exists(candidato)) return candidato;
    }
    throw new FileNotFoundException(rutaRelativa);
}
