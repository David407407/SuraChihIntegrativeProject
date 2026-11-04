// Pruebas de humo de BackendLogica: primero contra el repositorio falso (sin MySQL) y luego contra la
// BD local con los datos de BaseDeDatos/surachih_v1.sql.
// Uso:  dotnet run --project BackendLogica.Pruebas              (falso + MySQL)
//       dotnet run --project BackendLogica.Pruebas -- --falso   (solo el falso, sin MySQL)
// Contra MySQL solo lee datos, salvo el favorito de prueba, que se agrega y se quita (queda igual que antes).
//
// Implementación real completa (crea cuentas, inscripciones, vistas...): solo si "Database" en
// dbsettings.local.json termina en "_pruebas". Para tener esa base, carga surachih_v1.sql cambiando
// "surachih" por "surachih_pruebas" en sus tres primeras instrucciones (DROP, CREATE y USE).
// Así nunca se ensucia la base con la que trabajas.

using MySql.Data.MySqlClient;

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
await Prueba("Detalle de evento cancelado con su motivo (cancel_reason de v_event_card)", async () =>
{
    var cancelado = (await bd.Eventos.ObtenerAsync(5))?.Evento;
    Afirmar(cancelado is { Estado: EstadoEvento.Cancelado, MotivoCancelacion: not null }, "el evento 5 debe estar cancelado y traer su motivo");
    return cancelado!.MotivoCancelacion!;
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
await Prueba("Favorito de lugar: alternar dos veces deja todo igual", async () =>
{
    bool antes = (await bd.Favoritos.ListarIdsLugaresAsync(7)).Contains(1);
    bool primero = await bd.Favoritos.AlternarLugarAsync(7, 1);
    var lista = await bd.Favoritos.ListarLugaresAsync(7);
    bool segundo = await bd.Favoritos.AlternarLugarAsync(7, 1);
    bool despues = (await bd.Favoritos.ListarIdsLugaresAsync(7)).Contains(1);
    Afirmar(primero != segundo && antes == despues, "el estado no regresó");
    Afirmar(primero == lista.Any(l => l.Id == 1), "Mis favoritos no coincide");
    return $"{antes} → {primero} → {segundo}";
});

if (bd.Configuracion.BaseDeDatos.EndsWith("_pruebas", StringComparison.OrdinalIgnoreCase))
    await PruebasEscrituraMySql(bd);
else
    Console.WriteLine("\n(Las pruebas que crean cuentas e inscripciones solo corren contra una base *_pruebas; ver el inicio de este archivo.)");

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
    await Prueba("IEventRepository: filtros del rediseño (Gastronomía + rango del 28 nov)", async () =>
    {
        var eventos = await f.Eventos.FiltrarAsync(new FiltroEventos
        {
            EtiquetaIds = [5], Precio = FiltroPrecio.DePago, Cuando = FiltroCuando.Rango,
            Desde = new DateTime(2026, 11, 28), Hasta = new DateTime(2026, 11, 28)
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
    await Prueba("IEventRepository: estados del detalle (cancelado con motivo, terminado)", async () =>
    {
        var cancelado = (await f.Eventos.ObtenerAsync(5))!.Evento;
        var terminado = (await f.Eventos.ObtenerAsync(3))!.Evento;
        var vigente = (await f.Eventos.ObtenerAsync(2))!.Evento;
        Afirmar(cancelado is { Estado: EstadoEvento.Cancelado, Terminado: false, MotivoCancelacion: not null }, "cancelado con motivo");
        Afirmar(terminado is { Terminado: true, MotivoCancelacion: null } && vigente.MotivoCancelacion == null, "solo los cancelados traen motivo");
        Afirmar((await f.Eventos.ListarMasGuardadosAsync(10)).All(e => e.Id != 5), "un cancelado no sale en las listas");
        return cancelado.MotivoCancelacion!;
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
        Afirmar(historial.Count == 8, $"{historial.Count} decisiones");
        return $"{historial.Count} decisiones";
    });
    await Prueba("IModerationRepository: fecha de envío de lo pendiente", async () =>
    {
        var envios = await f.Moderacion.ListarEnviosPendientesAsync();
        Afirmar(envios.Single() is { Tipo: TipoObjetivoModeracion.Evento, ObjetivoId: 4 }, "solo el taller de fotografía está pendiente");
        return $"evento #{envios[0].ObjetivoId} enviado el {envios[0].EnviadoEn:g}";
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

    // --- Mi perfil y Eliminar cuenta ---
    await Prueba("Mi perfil: guardar cambios y duplicados", async () =>
    {
        Afirmar(await f.Usuarios.ActualizarPerfilAsync(6, "diego.m", "Diego.Nuevo@Example.com", null), "debió guardarse");
        var diego = (await f.Usuarios.ObtenerAsync(6))!;
        Afirmar(diego is { NombreUsuario: "diego.m", Correo: "diego.nuevo@example.com" }, "nombre y correo nuevos (correo en minúsculas)");
        await EsperarError<DatosInvalidosException>(() => f.Usuarios.ActualizarPerfilAsync(6, "di", diego.Correo, null));
        return await EsperarError<DuplicadoException>(() => f.Usuarios.ActualizarPerfilAsync(6, "diego.m", "mariana.r@example.com", null),
            "Ese correo ya está registrado.");
    });
    await Prueba("Mi perfil: actualizar contraseña revisa la actual y la nueva", async () =>
    {
        Afirmar(!await f.Usuarios.CambiarContrasenaAsync(6, "equivocada", "OtraClave2"), "con la actual incorrecta no cambia");
        await EsperarError<DatosInvalidosException>(() => f.Usuarios.CambiarContrasenaAsync(6, "NuevaClave1", "corta"));
        Afirmar(await f.Usuarios.CambiarContrasenaAsync(6, "NuevaClave1", "OtraClave2"), "con la actual correcta cambia");
        Afirmar(await f.Usuarios.IniciarSesionAsync("diego.m", "OtraClave2") != null, "entra con la nueva");
        return "";
    });
    await Prueba("Eliminar cuenta: is_active = FALSE, no borra y ya no puede entrar", async () =>
    {
        Afirmar(await f.Usuarios.DesactivarAsync(6), "debió desactivarse");
        Afirmar(await f.Usuarios.ObtenerAsync(6) is { Activo: false }, "la fila sigue ahí, desactivada");
        Afirmar(await f.Usuarios.IniciarSesionAsync("diego.m", "OtraClave2") == null, "una cuenta desactivada no inicia sesión");
        Afirmar(!await f.Usuarios.ActualizarPerfilAsync(6, "diego.m", "diego.nuevo@example.com", null), "ni edita su perfil");
        return "";
    });
    await Prueba("Eliminar moderadores: solo un supermoderador, y nadie elimina la suya", async () =>
    {
        var g = new SuraChihFalso();   // aparte: las demás pruebas usan a estos moderadores
        Afirmar(!await g.Usuarios.DesactivarAsync(2), "un moderador no elimina su propia cuenta");
        await EsperarError<ReglaNegocioException>(() => g.Usuarios.DesactivarModeradorAsync(2, 1));
        await EsperarError<ReglaNegocioException>(() => g.Usuarios.DesactivarModeradorAsync(5, 2));
        Afirmar(!await g.Usuarios.DesactivarModeradorAsync(1, 1), "la cuenta de un supermoderador no se elimina");
        Afirmar(!await g.Usuarios.DesactivarModeradorAsync(1, 5), "mariana no es moderadora");
        Afirmar((await g.Usuarios.ListarModeradoresAsync())[0] is { NombreUsuario: "mod_ana", EsSuperModerador: true }, "ana va primero");
        Afirmar(await g.Usuarios.DesactivarModeradorAsync(1, 2), "ana elimina a luis");
        Afirmar(await g.Usuarios.IniciarSesionAsync("mod_luis", "Test1234!") == null, "luis ya no entra");
        Afirmar((await g.Usuarios.ListarModeradoresAsync()).Count == 1, "solo queda ana");
        return "";
    });
    await Prueba("Las reseñas de una cuenta eliminada no se muestran ni cuentan", async () =>
    {
        // diego (6) dejó 4★ en Dandelion (lugar 1) junto con 5★ de mariana: antes 4.5 de 2, ahora 5.0 de 1.
        var resenas = await f.Resenas.ListarAsync(TipoResena.Lugar, 1);
        var resumen = await f.Resenas.ObtenerResumenAsync(TipoResena.Lugar, 1);
        var calificacion = await f.Lugares.ObtenerCalificacionAsync(1);
        Afirmar(resenas.All(r => r.UsuarioId != 6), "no sale en la lista");
        Afirmar(resumen is { Promedio: 5.0m, Total: 1 } && calificacion is { Calificacion: 5.0m, NumResenas: 1 }, "no cuenta en el promedio");
        Afirmar((await f.Resenas.ListarAsync(TipoResena.Lugar, 1, incluirOcultas: true)).Any(r => r.UsuarioId == 6), "sigue guardada (no se borró)");
        return $"{calificacion!.Calificacion}★ de {calificacion.NumResenas}";
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
    await Prueba("Favoritos de eventos y de lugares", async () =>
    {
        bool evento = await f.Favoritos.AlternarAsync(7, 1);
        bool lugar = await f.Favoritos.AlternarLugarAsync(7, 2);
        Afirmar(evento && lugar && (await f.Favoritos.ListarLugaresAsync(7)).Single().Id == 2, "quedaron guardados");
        Afirmar(!await f.Favoritos.AlternarLugarAsync(7, 2), "alternar otra vez lo quita");
        return "";
    });
    await Prueba("Panel: estadísticas y vistas por día son solo del organizador", async () =>
    {
        var deDandelion = await f.Eventos.ListarEstadisticasAsync(3);
        Afirmar(deDandelion.Any(s => s.EventoId == 2) && deDandelion.All(s => s.EventoId != 1), "cada quien ve sus eventos");
        Afirmar((await f.Inscripciones.ListarInscritosAsync(2, organizadorId: 4)).Count == 0, "los inscritos ajenos no se ven");
        var dias = await f.Eventos.ListarVistasPorDiaAsync(1, 14);
        Afirmar(dias.Count == 14 && dias.Sum(d => d.Vistas) == 4, "14 días para comparar con la semana anterior, con los días en 0");
        return "";
    });
    await Prueba("Cancelar evento: solo el dueño, con motivo, y los inscritos lo ven en Mis planes", async () =>
    {
        // Un evento propio de la prueba: otras pruebas usan los de los datos de ejemplo.
        DatosEvento Datos(string titulo) => new()
        {
            Titulo = titulo, Descripcion = "Para cancelar", LugarId = 1,
            Inicio = DateTime.Now.AddDays(5), Fin = DateTime.Now.AddDays(5).AddHours(2)
        };
        int id = await f.Eventos.CrearAsync(3, Datos("Evento que se cancela"));
        await f.Moderacion.AprobarAsync(1, TipoObjetivoModeracion.Evento, id);
        await f.Inscripciones.InscribirAsync(7, id);

        await EsperarError<DatosInvalidosException>(() => f.Eventos.CancelarAsync(id, 3, "  "));
        Afirmar(!await f.Eventos.CancelarAsync(id, 4, "No es mío"), "un organizador no cancela eventos ajenos");
        Afirmar((await f.Eventos.ObtenerAsync(id))!.Evento.Estado == EstadoEvento.Aprobado, "sigue publicado");

        Afirmar(await f.Eventos.CancelarAsync(id, 3, "  Se inundó el local. "), "el dueño sí");
        var cancelado = (await f.Eventos.ObtenerAsync(id))!.Evento;
        Afirmar(cancelado is { Estado: EstadoEvento.Cancelado, MotivoCancelacion: "Se inundó el local." }, "estado y motivo (v_event_card)");
        Afirmar(!await f.Eventos.CancelarAsync(id, 3, "Otra vez"), "no se cancela dos veces");

        var planes = await f.Inscripciones.ListarPlanesAsync(7, PestanaPlanes.Cancelados);
        Afirmar(planes.Any(p => p.Evento.Id == id && p.Evento.MotivoCancelacion != null), "el inscrito lo ve cancelado con el motivo");
        Afirmar((await f.Inscripciones.ListarPlanesAsync(7, PestanaPlanes.Proximos)).All(p => p.Evento.Id != id), "ya no está en Próximos");
        Afirmar(await f.Eventos.ActualizarAsync(id, 3, Datos("Otro título")) == null, "un cancelado ya no se edita");
        return await EsperarError<ReglaNegocioException>(() => f.Inscripciones.InscribirAsync(5, id), MensajesTrigger.NoPuedesInscribirte);
    });
    await Prueba("Hazte organizador: solicitar, pendiente y duplicado", async () =>
    {
        var nueva = new SolicitudOrganizador(5, "  Café Mariana ", "Café de barrio", "https://instagram.com/cafe_mariana", "526141230000");
        await EsperarError<DatosInvalidosException>(() => f.Organizadores.SolicitarAsync(nueva with { WhatsApp = "52 614 123" }));
        Afirmar(await f.Organizadores.ObtenerAsync(5) == null, "con datos inválidos no se guarda");
        await f.Organizadores.SolicitarAsync(nueva);
        var perfil = await f.Organizadores.ObtenerAsync(5);
        Afirmar(perfil is { NombreNegocio: "Café Mariana", Estado: EstadoOrganizador.Pendiente }, "queda pendiente, sin espacios");
        Afirmar(!await f.Organizadores.EsAprobadoAsync(5), "pendiente todavía no publica");
        Afirmar((await f.Organizadores.ListarPorEstadoAsync(EstadoOrganizador.Pendiente)).Any(p => p.UsuarioId == 5), "el moderador la ve");
        return await EsperarError<DuplicadoException>(() => f.Organizadores.SolicitarAsync(nueva), "Ya enviaste una solicitud de organizador.");
    });
    await Prueba("Moderación rechaza con motivo y el usuario reenvía la solicitud", async () =>
    {
        await EsperarError<DatosInvalidosException>(() => f.Moderacion.RechazarAsync(1, TipoObjetivoModeracion.Organizador, 5, "  "));
        await f.Moderacion.RechazarAsync(1, TipoObjetivoModeracion.Organizador, 5, "No se pudo verificar: la cuenta no tiene publicaciones.");
        Afirmar((await f.Organizadores.ObtenerAsync(5))!.Estado == EstadoOrganizador.Rechazado, "quedó rechazada");
        var motivo = await f.Moderacion.ObtenerUltimaDecisionAsync(TipoObjetivoModeracion.Organizador, 5);
        Afirmar(motivo is { Decision: DecisionModeracion.Rechazado, ModeradorNombre: "mod_ana" } && motivo.Comentario!.StartsWith("No se pudo"), "el usuario ve el motivo");
        await EsperarError<ReglaNegocioException>(() => f.Moderacion.AprobarAsync(1, TipoObjetivoModeracion.Organizador, 5), MensajesTrigger.SolicitudNoPendiente);
        await f.Organizadores.SolicitarAsync(new SolicitudOrganizador(5, "Café Mariana", null, "https://facebook.com/cafe_mariana", "526141230000"));
        var reenviada = (await f.Organizadores.ObtenerAsync(5))!;
        Afirmar(reenviada is { Estado: EstadoOrganizador.Pendiente, RedSocialUrl: "https://facebook.com/cafe_mariana" }, "reenviar la deja pendiente con los datos nuevos");
        return "";
    });
    await Prueba("Moderación aprueba: ya puede publicar y no puede volver a solicitar", async () =>
    {
        await EsperarError<ReglaNegocioException>(() => f.Moderacion.AprobarAsync(6, TipoObjetivoModeracion.Organizador, 5), MensajesTrigger.SoloModeradores);
        await f.Moderacion.AprobarAsync(2, TipoObjetivoModeracion.Organizador, 5);
        Afirmar(await f.Organizadores.EsAprobadoAsync(5), "quedó aprobada");
        Afirmar((await f.Organizadores.ListarPorEstadoAsync(EstadoOrganizador.Pendiente)).All(p => p.UsuarioId != 5), "ya no está pendiente");
        var historial = await f.Moderacion.ListarHistorialAsync();
        Afirmar(historial.Count(h => h.Tipo == TipoObjetivoModeracion.Organizador && h.ObjetivoId == 5) == 2, "las dos decisiones quedan en el historial");
        int evento = await f.Eventos.CrearAsync(5, new DatosEvento
        {
            Titulo = "Primer evento de Mariana", Descripcion = "Ya es organizadora", LugarId = 1,
            Inicio = DateTime.Now.AddDays(3), Fin = DateTime.Now.AddDays(3).AddHours(2)
        }, enviarARevision: false);
        Afirmar(evento > 0, "el trigger ya la deja publicar");
        return await EsperarError<DuplicadoException>(() => f.Organizadores.SolicitarAsync(new SolicitudOrganizador(5, "Otro", null, "https://x.com/otro", "526141230000")));
    });
    await Prueba("Nadie aprueba su propia solicitud (trigger trg_moderation_bi)", async () =>
    {
        await f.Organizadores.SolicitarAsync(new SolicitudOrganizador(2, "Colectivo de Luis", null, "https://instagram.com/luis", "526141230001"));
        await EsperarError<ReglaNegocioException>(() => f.Moderacion.AprobarAsync(2, TipoObjetivoModeracion.Organizador, 2), MensajesTrigger.PropiaSolicitud);
        await EsperarError<ReglaNegocioException>(() => f.Moderacion.RechazarAsync(2, TipoObjetivoModeracion.Organizador, 2, "No"), MensajesTrigger.PropiaSolicitud);
        Afirmar((await f.Organizadores.ObtenerAsync(2))!.Estado == EstadoOrganizador.Pendiente, "sigue pendiente");
        await f.Moderacion.AprobarAsync(1, TipoObjetivoModeracion.Organizador, 2);
        Afirmar(await f.Organizadores.EsAprobadoAsync(2), "otro moderador sí puede");
        return "";
    });
    await Prueba("Una cuenta eliminada no aparece entre las solicitudes pendientes", async () =>
    {
        var nuevo = await f.Usuarios.RegistrarAsync("se_va", "se.va@example.com", "Clave1234");
        await f.Organizadores.SolicitarAsync(new SolicitudOrganizador(nuevo.Id, "Se Va", null, "https://x.com/se_va", "526141230002"));
        Afirmar((await f.Organizadores.ListarPorEstadoAsync(EstadoOrganizador.Pendiente)).Any(p => p.UsuarioId == nuevo.Id), "antes sí");
        await f.Usuarios.DesactivarAsync(nuevo.Id);
        Afirmar((await f.Organizadores.ListarPorEstadoAsync(EstadoOrganizador.Pendiente)).All(p => p.UsuarioId != nuevo.Id), "después ya no");
        return "";
    });
    await Prueba("Reportes: crear y resolver", async () =>
    {
        int id = await f.Reportes.CrearAsync(6, TipoObjetivoReporte.ResenaLugar, 3, MotivoReporte.Spam, null);
        await EsperarError<ReglaNegocioException>(() => f.Reportes.ResolverAsync(id, 6, EstadoReporte.Descartado), ConexionDB.SoloModeradores);
        Afirmar((await f.Reportes.ListarAsync()).Any(r => r.Id == id), "un usuario normal no lo cierra");
        Afirmar(await f.Reportes.ResolverAsync(id, 1, EstadoReporte.Descartado), "resolver");
        Afirmar((await f.Reportes.ListarAsync()).All(r => r.Id != id), "ya no está abierto");
        Afirmar((await f.Reportes.ListarAsync(EstadoReporte.Descartado)).Any(r => r.Id == id && r.ResueltoPor == 1), "queda quién lo cerró");
        return "";
    });
    await Prueba("Reportes: cada uno queda ligado a su evento o lugar", async () =>
    {
        int deEvento = await f.Reportes.CrearAsync(6, TipoObjetivoReporte.ResenaEvento, 1, MotivoReporte.Inapropiado, null);
        var lista = await f.Reportes.ListarDetalleAsync();
        var resenaEvento = lista.Single(r => r.Reporte.Id == deEvento);
        Afirmar(resenaEvento is { EventoId: 3, LugarId: null, ResenaAutor: "mariana_r", ResenaCalificacion: 5, ResenaComentario: "Muy bien organizada." }, "reseña de evento → su evento");
        Afirmar(lista.Single(r => r.Reporte.Tipo == TipoObjetivoReporte.ResenaLugar && r.Reporte.ObjetivoId == 3) is { LugarId: 2, EventoId: null, ResenaAutor: "sofia_c" }, "reseña de lugar → su lugar");
        Afirmar(lista.Single(r => r.Reporte.Tipo == TipoObjetivoReporte.Evento) is { EventoId: 1, ResenaAutor: null }, "evento");
        Afirmar(lista.Single(r => r.Reporte.Tipo == TipoObjetivoReporte.Lugar) is { LugarId: 2, ResenaAutor: null, ReportanteNombre: "sofia_c" }, "lugar");
        Afirmar(lista.Zip(lista.Skip(1)).All(p => p.First.Reporte.CreadoEn <= p.Second.Reporte.CreadoEn), "el que lleva más tiempo esperando primero");
        return "";
    });
    await Prueba("Reportes: reabrir uno cerrado, solo moderadores", async () =>
    {
        int id = await f.Reportes.CrearAsync(6, TipoObjetivoReporte.Lugar, 1, MotivoReporte.Spam, null);
        Afirmar(!await f.Reportes.ReabrirAsync(id, 1), "ya estaba abierto");
        Afirmar(await f.Reportes.ResolverAsync(id, 1, EstadoReporte.Resuelto), "cerrar");
        await EsperarError<ReglaNegocioException>(() => f.Reportes.ReabrirAsync(id, 6), ConexionDB.SoloModeradores);
        Afirmar(await f.Reportes.ReabrirAsync(id, 2), "reabrir");
        Afirmar((await f.Reportes.ListarAsync()).Any(r => r.Id == id && r.ResueltoPor == null && r.ResueltoEn == null), "abierto y sin quién lo cerró");
        return "";
    });
    await Prueba("Ocultar reseña: solo moderadores, cierra sus reportes y queda en el historial", async () =>
    {
        int a = await f.Reportes.CrearAsync(5, TipoObjetivoReporte.ResenaLugar, 3, MotivoReporte.Spam, null);
        int b = await f.Reportes.CrearAsync(6, TipoObjetivoReporte.ResenaLugar, 3, MotivoReporte.Estafa, null);
        int otra = await f.Reportes.CrearAsync(6, TipoObjetivoReporte.ResenaEvento, 1, MotivoReporte.Spam, null);

        await EsperarError<ReglaNegocioException>(() => f.Resenas.CambiarVisibilidadAsync(6, TipoResena.Lugar, 3, true), ConexionDB.SoloModeradores);
        Afirmar((await f.Resenas.ListarAsync(TipoResena.Lugar, 2)).Any(r => r.Id == 3), "un usuario normal no la oculta");

        Afirmar(await f.Resenas.CambiarVisibilidadAsync(2, TipoResena.Lugar, 3, true), "ocultar");
        Afirmar((await f.Resenas.ListarAsync(TipoResena.Lugar, 2)).All(r => r.Id != 3), "ya no se ve");
        Afirmar((await f.Resenas.ListarAsync(TipoResena.Lugar, 2, incluirOcultas: true)).Any(r => r.Id == 3), "sigue guardada");
        var abiertos = await f.Reportes.ListarAsync();
        Afirmar(abiertos.All(r => r.Id != a && r.Id != b), "sus reportes se cerraron");
        Afirmar(abiertos.Any(r => r.Id == otra), "los de otra reseña (con el mismo id, en la otra tabla) siguen abiertos");
        Afirmar(!await f.Resenas.CambiarVisibilidadAsync(2, TipoResena.Lugar, 3, true), "ocultarla otra vez no hace nada");

        await EsperarError<ReglaNegocioException>(() => f.Moderacion.ListarHistorialResenasAsync(2), ModeracionDB.SoloSuperHistorial);
        await EsperarError<ReglaNegocioException>(() => f.Moderacion.ContarHistorialResenasAsync(6), ModeracionDB.SoloSuperHistorial);
        var historial = await f.Moderacion.ListarHistorialResenasAsync(1);
        Afirmar(historial.Count == 1 && historial[0] is { ModeradorNombre: "mod_luis", Tipo: TipoResena.Lugar, ResenaId: 3, ObjetivoId: 2, AutorNombre: "sofia_c", Accion: AccionResena.Ocultada }, "historial");

        Afirmar(await f.Resenas.CambiarVisibilidadAsync(1, TipoResena.Lugar, 3, false), "volver a mostrar");
        Afirmar((await f.Moderacion.ListarHistorialResenasAsync(1))[0].Accion == AccionResena.Mostrada, "lo más nuevo primero");
        Afirmar(await f.Moderacion.ContarHistorialResenasAsync(1) == 2, "conteo");
        return "";
    });
    await Prueba("Historial de moderación: filtros, nombre del objetivo y total", async () =>
    {
        int total = await f.Moderacion.ContarHistorialAsync();
        var rechazos = await f.Moderacion.ListarHistorialAsync(decision: DecisionModeracion.Rechazado);
        Afirmar(rechazos.All(r => r.Decision == DecisionModeracion.Rechazado), "solo rechazos");
        Afirmar(rechazos.Count == await f.Moderacion.ContarHistorialAsync(DecisionModeracion.Rechazado), "conteo de rechazos");
        var organizadores = await f.Moderacion.ListarHistorialAsync(tipo: TipoObjetivoModeracion.Organizador);
        Afirmar(organizadores.Count > 0 && organizadores.All(r => r.Tipo == TipoObjetivoModeracion.Organizador), "solo organizadores");
        Afirmar(organizadores.Any(r => r.ObjetivoNombre == "Dandelion Coffee"), "con el nombre del negocio");
        Afirmar((await f.Moderacion.ListarHistorialAsync(tipo: TipoObjetivoModeracion.Evento)).All(r => r.ObjetivoNombre.Length > 0), "con el título del evento");
        Afirmar((await f.Moderacion.ListarHistorialAsync(limite: 1)).Count == 1 && total > 1, "el límite no cambia el total");
        return "";
    });

    // --- Semanas 3–4: publicar con galería, avisar al editar y bandejas por antigüedad ---
    // Van al final porque cambian el evento 2 (aprobado), que usan las pruebas de arriba.
    const string Foto1 = "https://res.cloudinary.com/demo/image/upload/v1/surachih/prueba1.jpg";
    const string Foto2 = "https://res.cloudinary.com/demo/image/upload/v1/surachih/prueba2.jpg";
    await Prueba("Publicar con galería guarda las fotos de Cloudinary en orden", async () =>
    {
        int id = await f.Eventos.CrearAsync(4, EventoDePrueba("Con galería", DateTime.Now.AddDays(10), [Foto1, Foto2]));
        var galeria = (await f.Eventos.ObtenerAsync(id))!.Galeria;
        Afirmar(galeria.SequenceEqual([Foto1, Foto2]), $"galería: {string.Join(", ", galeria)}");
        return $"{galeria.Count} fotos";
    });
    await Prueba("La galería solo acepta URLs https", () =>
        EsperarError<DatosInvalidosException>(() =>
            f.Eventos.CrearAsync(4, EventoDePrueba("URL mala", DateTime.Now.AddDays(10), ["foto.jpg"]))));
    await Prueba("Bandeja de eventos pendientes: el que lleva más tiempo esperando va primero", async () =>
    {
        // El primero en enviarse es el de fecha más lejana: ordenar por fecha los invertiría.
        int primero = await f.Eventos.CrearAsync(4, EventoDePrueba("Enviado primero", DateTime.Now.AddDays(40)));
        int segundo = await f.Eventos.CrearAsync(4, EventoDePrueba("Enviado después", DateTime.Now.AddDays(5)));
        var ids = (await f.Eventos.ListarPorEstadoAsync(EstadoEvento.Pendiente)).Select(e => e.Id).ToList();
        Afirmar(ids.IndexOf(primero) < ids.IndexOf(segundo), $"orden: {string.Join(", ", ids)}");
        return "";
    });
    await Prueba("Editar un aprobado avisa que volvió a revisión; editar uno pendiente no", async () =>
    {
        var antes = (await f.Eventos.ObtenerAsync(4))!;   // aprobado por la prueba de moderación de arriba
        var datos = DatosDe(antes) with { Titulo = antes.Evento.Titulo + " (editado)" };
        var r1 = await f.Eventos.ActualizarAsync(4, 4, datos);
        var r2 = await f.Eventos.ActualizarAsync(4, 4, datos with { Descripcion = "Otra descripción" });
        Afirmar(r1 is { Estado: EstadoEvento.Pendiente, VolvioARevision: true }, $"primera edición: {r1}");
        Afirmar(r2 is { Estado: EstadoEvento.Pendiente, VolvioARevision: false }, $"segunda edición: {r2}");
        Afirmar(await f.Eventos.ActualizarAsync(4, 3, datos) == null, "otro organizador no puede editarlo");
        return "";
    });
    await Prueba("Galería al editar: quitar una foto no regresa a revisión; agregar una sí", async () =>
    {
        var cata = (await f.Eventos.ObtenerAsync(2))!;     // aprobada, con 1 foto
        var quitar = await f.Eventos.ActualizarAsync(2, 3, DatosDe(cata) with { Galeria = [] });
        Afirmar(quitar is { Estado: EstadoEvento.Aprobado, VolvioARevision: false }, $"quitar: {quitar}");
        Afirmar((await f.Eventos.ObtenerAsync(2))!.Galeria.Count == 0, "la foto se quitó");
        var agregar = await f.Eventos.ActualizarAsync(2, 3, DatosDe(cata) with { Galeria = [Foto1] });
        Afirmar(agregar is { Estado: EstadoEvento.Pendiente, VolvioARevision: true }, $"agregar: {agregar}");
        return "";
    });
    await Prueba("AgregarImagenAsync solo funciona para el dueño del evento", async () =>
    {
        Afirmar(await f.Eventos.AgregarImagenAsync(2, organizadorId: 4, Foto2) == null, "otro organizador no agrega fotos");
        Afirmar(await f.Eventos.AgregarImagenAsync(2, organizadorId: 3, Foto2) > 0, "el dueño sí");
        return "";
    });
}

// Datos completos de un evento de la organizadora 4 (Misiones) en el lugar 1 (aprobado).
static DatosEvento EventoDePrueba(string titulo, DateTime inicio, IReadOnlyList<string>? galeria = null) => new()
{
    Titulo = titulo, Descripcion = "Evento de prueba", LugarId = 1,
    Inicio = inicio, Fin = inicio.AddHours(2), PrecioMin = 0, PrecioMax = 0, Galeria = galeria ?? []
};

// El formulario "Editar evento" lleno con lo que ya tiene el evento.
static DatosEvento DatosDe(EventoDetalle d) => new()
{
    LugarId = d.Evento.LugarId, Titulo = d.Evento.Titulo, Descripcion = d.Evento.Descripcion,
    Inicio = d.Evento.Inicio, Fin = d.Evento.Fin,
    Direccion = d.Evento.LugarId == null ? d.Evento.Direccion : null,
    Latitud = d.Evento.LugarId == null ? d.Evento.Latitud : null,
    Longitud = d.Evento.LugarId == null ? d.Evento.Longitud : null,
    PrecioMin = d.Evento.PrecioMin, PrecioMax = d.Evento.PrecioMax, BoletosUrl = d.Evento.BoletosUrl,
    ImagenUrl = d.Evento.ImagenUrl, EtiquetaIds = d.Evento.Etiquetas.Select(t => t.Id).ToList(), Galeria = d.Galeria
};

// ----------------------------------------------------------------------
// Implementación real contra MySQL (escribe: solo en una base *_pruebas)
// Lo que consumen las pantallas de las semanas 1–4: cuenta, lectura, favoritos, inscripción, vistas,
// Hazte organizador, Publicar evento, Moderación y bandejas.
// ----------------------------------------------------------------------

async Task PruebasEscrituraMySql(SuraChihBD bd)
{
    Console.WriteLine("\nImplementación real (MySQL, base de pruebas)");
    string sufijo = DateTime.Now.ToString("HHmmssfff");

    // Lectura directa de la BD para revisar lo que guardó la librería.
    async Task<object?> Sql(string sql, object? parametros = null)
    {
        await using var c = new MySqlConnection(bd.Configuracion.CadenaConexion);
        await c.OpenAsync();
        await using var cmd = new MySqlCommand(sql, c);
        foreach (var p in parametros?.GetType().GetProperties() ?? [])
            cmd.Parameters.AddWithValue("@" + p.Name, p.GetValue(parametros));
        object? valor = await cmd.ExecuteScalarAsync();
        return valor is DBNull ? null : valor;
    }

    async Task<Usuario> CuentaNueva(string nombre) =>
        await bd.Usuarios.RegistrarAsync($"{nombre}_{sufijo}", $"{nombre}.{sufijo}@prueba.mx", "Prueba2026");

    // --- Cuenta ---
    await Prueba("Registro guarda BCrypt (nunca la contraseña) y el login acepta usuario o correo", async () =>
    {
        var u = await CuentaNueva("registro");
        string hash = (string)(await Sql("SELECT password_hash FROM user WHERE id = @id", new { id = u.Id }))!;
        Afirmar(hash.StartsWith("$2") && hash != "Prueba2026", "el hash no es BCrypt");
        Afirmar((await bd.Usuarios.IniciarSesionAsync(u.NombreUsuario, "Prueba2026"))?.Id == u.Id, "login con usuario");
        Afirmar((await bd.Usuarios.IniciarSesionAsync(u.Correo.ToUpperInvariant(), "Prueba2026"))?.Id == u.Id, "login con correo en mayúsculas");
        Afirmar(await bd.Usuarios.IniciarSesionAsync(u.NombreUsuario, "Otra2026") == null, "contraseña incorrecta");
        await EsperarError<DuplicadoException>(() => bd.Usuarios.RegistrarAsync($"otro_{sufijo}", u.Correo, "Prueba2026"));
        return hash[..7] + "…";
    });
    await Prueba("Recuperar contraseña guarda solo el SHA-256 del token y sirve una vez", async () =>
    {
        var u = await CuentaNueva("recupera");
        Afirmar(await bd.RecuperacionContrasena.SolicitarAsync($"nadie.{sufijo}@prueba.mx") == null, "un correo sin cuenta no da token");
        string viejo = (await bd.RecuperacionContrasena.SolicitarAsync(u.Correo))!;
        string token = (await bd.RecuperacionContrasena.SolicitarAsync(u.Correo))!;

        string guardado = (string)(await Sql(
            "SELECT token_hash FROM password_reset WHERE user_id = @id AND used_at IS NULL", new { id = u.Id }))!;
        Afirmar(guardado == BackendLogica.Seguridad.Tokens.Hashear(token) && guardado.Length == 64, "debe guardar el SHA-256 en hex");
        Afirmar(await Sql("SELECT COUNT(*) FROM password_reset WHERE token_hash = @token", new { token }) is 0L, "el token en claro no se guarda");

        Afirmar(!await bd.RecuperacionContrasena.RestablecerAsync(viejo, "Nueva2026"), "pedir otro enlace invalida el anterior");
        Afirmar(await bd.RecuperacionContrasena.RestablecerAsync(token, "Nueva2026"), "el enlace vigente cambia la contraseña");
        Afirmar(!await bd.RecuperacionContrasena.RestablecerAsync(token, "Otra2026"), "el enlace no sirve dos veces");
        Afirmar(await bd.Usuarios.IniciarSesionAsync(u.NombreUsuario, "Nueva2026") != null, "entra con la nueva");
        Afirmar(await bd.Usuarios.IniciarSesionAsync(u.NombreUsuario, "Prueba2026") == null, "ya no entra con la anterior");
        return "";
    });
    await Prueba("Desactivar cuenta: is_active = FALSE, no borra, no entra y su enlace pendiente ya no sirve", async () =>
    {
        var u = await CuentaNueva("baja");
        string token = (await bd.RecuperacionContrasena.SolicitarAsync(u.Correo))!;
        Afirmar(await bd.Usuarios.DesactivarAsync(u.Id), "debió desactivarse");
        Afirmar(!await bd.Usuarios.DesactivarAsync(u.Id), "la segunda vez no hay nada que desactivar");
        Afirmar(await Sql("SELECT is_active FROM user WHERE id = @id", new { id = u.Id }) is false or 0 or (sbyte)0, "la fila sigue, con is_active = FALSE");
        Afirmar(await bd.Usuarios.IniciarSesionAsync(u.NombreUsuario, "Prueba2026") == null, "una cuenta desactivada no entra");
        Afirmar(!await bd.RecuperacionContrasena.RestablecerAsync(token, "Nueva2026"), "el enlace pedido antes de darse de baja no sirve");
        Afirmar(await bd.RecuperacionContrasena.SolicitarAsync(u.Correo) == null, "no se dan enlaces a cuentas desactivadas");
        return "";
    });

    // --- Lectura ---
    await Prueba("Lugares aprobados con place_hours y v_place_rating", async () =>
    {
        var lugares = await bd.Lugares.ListarAprobadosAsync();
        Afirmar(lugares.Count > 0 && lugares.All(l => l.Estado == EstadoLugar.Aprobado), "solo aprobados");
        foreach (var l in lugares)
        {
            long dias = (long)(await Sql("SELECT COUNT(*) FROM place_hours WHERE place_id = @id", new { id = l.Id }))!;
            Afirmar(l.Horario.Count == dias, $"{l.Nombre}: horario incompleto");
            object? promedio = await Sql("SELECT avg_rating FROM v_place_rating WHERE place_id = @id", new { id = l.Id });
            Afirmar(l.Calificacion == (promedio == null ? null : Convert.ToDecimal(promedio)), $"{l.Nombre}: calificación distinta a v_place_rating");
        }
        return string.Join(" | ", lugares.Select(l => $"{l.Nombre}: {l.Horario.Count} días, {l.Calificacion?.ToString() ?? "sin"}★"));
    });
    await Prueba("Solo etiquetas activas (lista, tarjetas de evento y de lugar)", async () =>
    {
        // Se apaga una etiqueta que usan eventos y lugares, y se vuelve a prender al final.
        int id = Convert.ToInt32(await Sql("SELECT tag_id FROM event_tag LIMIT 1"));
        await Sql("UPDATE tag SET is_active = FALSE WHERE id = @id", new { id });
        try
        {
            Afirmar((await bd.Etiquetas.ListarAsync()).All(t => t.Id != id), "la lista trae la etiqueta apagada");
            Afirmar((await bd.Eventos.ListarProximosAsync()).All(e => e.Etiquetas.All(t => t.Id != id)), "una tarjeta de evento la trae");
            Afirmar((await bd.Lugares.ListarAprobadosAsync()).All(l => l.Etiquetas.All(t => t.Id != id)), "una tarjeta de lugar la trae");
        }
        finally
        {
            await Sql("UPDATE tag SET is_active = TRUE WHERE id = @id", new { id });
        }
        return $"etiqueta {id} oculta mientras estuvo apagada";
    });
    await Prueba("Destacados: solo aprobados, vigentes y marcados, en su orden (v_event_card)", async () =>
    {
        var destacados = await bd.Eventos.ListarDestacadosAsync(10);
        Afirmar(destacados.All(e => e is { Destacado: true, Estado: EstadoEvento.Aprobado, Terminado: false }), "se coló uno que no debía");
        long esperados = (long)(await Sql("""
            SELECT COUNT(*) FROM v_event_card WHERE is_featured AND status = 'approved' AND end_at > NOW()
            """))!;
        Afirmar(destacados.Count == esperados, $"{destacados.Count} de {esperados}");
        return string.Join(" | ", destacados.Select(e => e.Titulo));
    });

    // --- Favoritos ---
    await Prueba("Favoritos: agregar (sin duplicar), listar y quitar", async () =>
    {
        var u = await CuentaNueva("favs");
        await bd.Favoritos.AgregarAsync(u.Id, 1);
        Afirmar(await bd.Favoritos.AgregarAsync(u.Id, 1) == 0, "agregar dos veces no duplica");
        Afirmar((await bd.Favoritos.ListarIdsAsync(u.Id)).SetEquals([1]), "ids");
        Afirmar((await bd.Favoritos.ListarAsync(u.Id)).Single().Id == 1, "Mis favoritos");
        Afirmar(await bd.Favoritos.QuitarAsync(u.Id, 1) == 1 && (await bd.Favoritos.ListarAsync(u.Id)).Count == 0, "quitar");
        return "";
    });

    // --- Inscripción ---
    await Prueba("Inscribir, cancelar y volver a inscribirse; Mis planes refleja cada paso", async () =>
    {
        var u = await CuentaNueva("planes");
        int vigente = (await bd.Eventos.ListarProximosAsync()).First().Id;
        await bd.Inscripciones.InscribirAsync(u.Id, vigente);
        Afirmar(await bd.Inscripciones.EstaInscritoAsync(u.Id, vigente), "inscrito");
        Afirmar((await bd.Inscripciones.ListarPlanesAsync(u.Id, PestanaPlanes.Proximos)).Any(p => p.Evento.Id == vigente), "en Próximos");
        Afirmar(await bd.Inscripciones.CancelarAsync(u.Id, vigente) && !await bd.Inscripciones.EstaInscritoAsync(u.Id, vigente), "cancelar");
        Afirmar(!await bd.Inscripciones.CancelarAsync(u.Id, vigente), "cancelar dos veces no hace nada");
        Afirmar((await bd.Inscripciones.ListarPlanesAsync(u.Id, PestanaPlanes.Cancelados)).Any(p => p.Evento.Id == vigente), "en Cancelados");
        await bd.Inscripciones.InscribirAsync(u.Id, vigente);
        Afirmar(await bd.Inscripciones.EstaInscritoAsync(u.Id, vigente), "se reactiva");
        return $"evento {vigente}";
    });
    await Prueba("trg_inscription_bi rechaza eventos no aprobados o terminados (también al reactivar)", async () =>
    {
        var u = await CuentaNueva("rechazo");
        var ids = (await Sql("""
            SELECT GROUP_CONCAT(id) FROM event WHERE status <> 'approved' OR end_at <= NOW()
            """) as string ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
        Afirmar(ids.Count > 0, "los datos de prueba deben traer eventos terminados o no aprobados");
        foreach (int id in ids)
            await EsperarError<ReglaNegocioException>(() => bd.Inscripciones.InscribirAsync(u.Id, id), "No puedes inscribirte a este evento.");

        // Reactivar una inscripción cancelada usa INSERT ... ON DUPLICATE KEY UPDATE, que también dispara el
        // trigger BEFORE INSERT: si el evento ya terminó, no se reactiva. Se simula que un evento vigente
        // (ya empezado) terminó hace una hora y al final se le regresa su fecha (cambiar end_at no lo manda a moderación).
        int evento = Convert.ToInt32(await Sql("""
            SELECT id FROM event WHERE status = 'approved' AND end_at > NOW() AND start_at < NOW() - INTERVAL 1 DAY LIMIT 1
            """));
        object fin = (await Sql("SELECT end_at FROM event WHERE id = @evento", new { evento }))!;
        await bd.Inscripciones.InscribirAsync(u.Id, evento);
        await bd.Inscripciones.CancelarAsync(u.Id, evento);
        await Sql("UPDATE event SET end_at = NOW() - INTERVAL 1 HOUR WHERE id = @evento", new { evento });
        try
        {
            await EsperarError<ReglaNegocioException>(() => bd.Inscripciones.InscribirAsync(u.Id, evento), "No puedes inscribirte a este evento.");
            Afirmar(!await bd.Inscripciones.EstaInscritoAsync(u.Id, evento), "no debió reactivarse");
        }
        finally
        {
            await Sql("UPDATE event SET end_at = @fin WHERE id = @evento", new { fin, evento });
        }
        return $"eventos {string.Join(", ", ids)}; reactivar en el {evento} también se rechaza";
    });

    // --- Vistas (event_view) ---
    await Prueba("event_view: máximo 1 por usuario por evento por día; los visitantes siempre cuentan", async () =>
    {
        var u = await CuentaNueva("vistas");
        async Task<long> Vistas(int? usuario) => (long)(await Sql(
            "SELECT COUNT(*) FROM event_view WHERE event_id = 2 AND user_id <=> @usuario", new { usuario }))!;

        Afirmar(await bd.Eventos.RegistrarVistaAsync(2, u.Id) == 1, "la primera cuenta");
        Afirmar(await bd.Eventos.RegistrarVistaAsync(2, u.Id) == 0, "la segunda del día no");
        Afirmar(await Vistas(u.Id) == 1, "una sola fila");

        long visitantes = await Vistas(null);
        await bd.Eventos.RegistrarVistaAsync(2, null);
        await bd.Eventos.RegistrarVistaAsync(2, null);
        Afirmar(await Vistas(null) == visitantes + 2, "las de visitantes cuentan siempre");
        return "";
    });
    await Prueba("event_view: la llave uq_view_user_day lo garantiza aunque lleguen al mismo tiempo", async () =>
    {
        var u = await CuentaNueva("simultaneas");
        int[] resultados = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => bd.Eventos.RegistrarVistaAsync(1, u.Id)));
        long filas = (long)(await Sql("SELECT COUNT(*) FROM event_view WHERE event_id = 1 AND user_id = @id", new { id = u.Id }))!;
        Afirmar(resultados.Sum() == 1 && filas == 1, $"contaron {resultados.Sum()} y hay {filas} filas");

        // Aunque alguien inserte directo con SQL, la BD no deja la segunda del día.
        await EsperarError<MySqlException>(() => Sql("INSERT INTO event_view (event_id, user_id) VALUES (1, @id)", new { id = u.Id }));
        return "8 aperturas → 1 vista";
    });

    // --- Semanas 3–4: Hazte organizador, Publicar evento, Moderación y bandejas ---
    const string Foto1 = "https://res.cloudinary.com/demo/image/upload/v1/surachih/prueba1.jpg";
    const string Foto2 = "https://res.cloudinary.com/demo/image/upload/v1/surachih/prueba2.jpg";

    await Prueba("Solicitud de organizador: WhatsApp de 10–15 dígitos, pending, rechazo con motivo y reenvío", async () =>
    {
        var u = await CuentaNueva("solicita");
        SolicitudOrganizador Solicitud(string whatsApp) => new(u.Id, $"Negocio {sufijo}", null, "https://instagram.com/prueba", whatsApp);

        await EsperarError<DatosInvalidosException>(() => bd.Organizadores.SolicitarAsync(Solicitud("614-123-4567")));
        await EsperarError<DatosInvalidosException>(() => bd.Organizadores.SolicitarAsync(Solicitud("123456789")));
        await bd.Organizadores.SolicitarAsync(Solicitud("526141234567"));
        Afirmar((await bd.Organizadores.ObtenerAsync(u.Id))?.Estado == EstadoOrganizador.Pendiente, "debe quedar pending");
        Afirmar((await bd.Organizadores.ListarPorEstadoAsync(EstadoOrganizador.Pendiente)).Any(o => o.UsuarioId == u.Id), "sale en la bandeja");
        await EsperarError<DuplicadoException>(() => bd.Organizadores.SolicitarAsync(Solicitud("526141234567")));

        await EsperarError<DatosInvalidosException>(() => bd.Moderacion.RechazarAsync(1, TipoObjetivoModeracion.Organizador, u.Id, "  "), "Escribe el motivo del rechazo.");
        await bd.Moderacion.RechazarAsync(1, TipoObjetivoModeracion.Organizador, u.Id, "Falta tu red social");
        Afirmar((await bd.Organizadores.ObtenerAsync(u.Id))?.Estado == EstadoOrganizador.Rechazado, "el trigger aplicó el rechazo");
        await bd.Organizadores.SolicitarAsync(Solicitud("5261412345678"));
        await bd.Moderacion.AprobarAsync(2, TipoObjetivoModeracion.Organizador, u.Id);
        Afirmar(await bd.Organizadores.EsAprobadoAsync(u.Id), "el reenvío se aprobó");
        return "";
    });
    await Prueba("Bandeja de organizadores: la solicitud más antigua primero", async () =>
    {
        var lista = await bd.Organizadores.ListarPorEstadoAsync(EstadoOrganizador.Pendiente);
        Afirmar(lista.Zip(lista.Skip(1)).All(p => p.First.SolicitadoEn <= p.Second.SolicitadoEn), "fuera de orden");
        return $"{lista.Count} pendientes";
    });
    await Prueba("Publicar: event + event_tag + event_image en un paso; borrador y envío a revisión", async () =>
    {
        int id = await bd.Eventos.CrearAsync(4, EventoDePrueba("Publicado con galería", DateTime.Now.AddDays(12), [Foto1, Foto2]) with { EtiquetaIds = [1, 3] });
        var d = (await bd.Eventos.ObtenerAsync(id))!;
        Afirmar(d.Evento.Estado == EstadoEvento.Pendiente, "enviar a revisión deja pending");
        Afirmar(d.Galeria.SequenceEqual([Foto1, Foto2]), $"galería: {string.Join(", ", d.Galeria)}");
        Afirmar(d.Evento.Etiquetas.Select(t => t.Id).Order().SequenceEqual([1, 3]), "etiquetas");

        int borrador = await bd.Eventos.CrearAsync(4, EventoDePrueba("Borrador", DateTime.Now.AddDays(12)), enviarARevision: false);
        Afirmar((await bd.Eventos.ObtenerAsync(borrador))!.Evento.Estado == EstadoEvento.Borrador, "borrador = draft");
        Afirmar(await bd.Eventos.EnviarARevisionAsync(borrador, 4), "enviar el borrador");
        Afirmar((await bd.Eventos.ObtenerAsync(borrador))!.Evento.Estado == EstadoEvento.Pendiente, "ya en revisión");

        await EsperarError<DatosInvalidosException>(() =>
            bd.Eventos.CrearAsync(4, EventoDePrueba("URL mala", DateTime.Now.AddDays(12), ["http://res.cloudinary.com/demo/x.jpg"])));
        return "";
    });
    await Prueba("Editar un aprobado: trg_event_bu lo regresa a pending y el resultado lo avisa", async () =>
    {
        int id = await bd.Eventos.CrearAsync(4, EventoDePrueba("Para editar", DateTime.Now.AddDays(15), [Foto1]));
        await bd.Moderacion.AprobarAsync(1, TipoObjetivoModeracion.Evento, id);
        await bd.Moderacion.DestacarEventoAsync(id, 9);
        var d = (await bd.Eventos.ObtenerAsync(id))!;

        var soloPrecio = await bd.Eventos.ActualizarAsync(id, 4, DatosDe(d) with { PrecioMin = 50, PrecioMax = 50 });
        Afirmar(soloPrecio is { Estado: EstadoEvento.Aprobado, VolvioARevision: false }, $"cambiar el precio no lo regresa: {soloPrecio}");
        var quitarFoto = await bd.Eventos.ActualizarAsync(id, 4, DatosDe(d) with { Galeria = [] });
        Afirmar(quitarFoto is { Estado: EstadoEvento.Aprobado, VolvioARevision: false }, $"quitar una foto no lo regresa: {quitarFoto}");

        var titulo = await bd.Eventos.ActualizarAsync(id, 4, DatosDe(d) with { Titulo = "Para editar (nuevo título)" });
        Afirmar(titulo is { Estado: EstadoEvento.Pendiente, VolvioARevision: true }, $"cambiar el título sí: {titulo}");
        Afirmar(!(await bd.Eventos.ObtenerAsync(id))!.Evento.Destacado, "sale del carrusel");
        Afirmar(await bd.Eventos.ActualizarAsync(id, 3, DatosDe(d)) == null, "otro organizador no puede editarlo");

        await bd.Moderacion.AprobarAsync(1, TipoObjetivoModeracion.Evento, id);
        var foto = await bd.Eventos.ActualizarAsync(id, 4, DatosDe((await bd.Eventos.ObtenerAsync(id))!) with { Galeria = [Foto1, Foto2] });
        Afirmar(foto is { Estado: EstadoEvento.Pendiente, VolvioARevision: true }, $"agregar una foto (trg_eventimg_ai) también: {foto}");
        Afirmar((await bd.Eventos.ObtenerAsync(id))!.Galeria.SequenceEqual([Foto1, Foto2]), "galería final");
        return "";
    });
    await Prueba("AgregarImagenAsync solo para el dueño (y regresa a revisión un aprobado)", async () =>
    {
        int id = await bd.Eventos.CrearAsync(4, EventoDePrueba("Fotos sueltas", DateTime.Now.AddDays(16)));
        await bd.Moderacion.AprobarAsync(1, TipoObjetivoModeracion.Evento, id);
        Afirmar(await bd.Eventos.AgregarImagenAsync(id, organizadorId: 3, Foto1) == null, "otro organizador no agrega fotos");
        Afirmar(await bd.Eventos.AgregarImagenAsync(id, organizadorId: 4, Foto1) > 0, "el dueño sí");
        Afirmar((await bd.Eventos.ObtenerAsync(id))!.Evento.Estado == EstadoEvento.Pendiente, "volvió a revisión");
        return "";
    });
    await Prueba("Moderación: rechazo exige motivo y nadie modera lo suyo (trg_moderation_bi)", async () =>
    {
        int id = await bd.Eventos.CrearAsync(4, EventoDePrueba("Para moderar", DateTime.Now.AddDays(17)));
        await EsperarError<DatosInvalidosException>(() => bd.Moderacion.DecidirAsync(1, TipoObjetivoModeracion.Evento, id, DecisionModeracion.Rechazado));
        await bd.Moderacion.RechazarAsync(1, TipoObjetivoModeracion.Evento, id, "La foto no corresponde al evento");
        Afirmar((await bd.Eventos.ObtenerAsync(id))!.Evento.Estado == EstadoEvento.Rechazado, "el trigger aplicó el rechazo");
        Afirmar((await bd.Moderacion.ObtenerUltimaDecisionAsync(TipoObjetivoModeracion.Evento, id))?.Comentario == "La foto no corresponde al evento", "Ver motivo");

        await Sql("UPDATE user SET is_mod = TRUE WHERE id = 4");   // la organizadora 4 también modera
        try
        {
            Afirmar(await bd.Eventos.EnviarARevisionAsync(id, 4), "reenviar el rechazado");
            await EsperarError<ReglaNegocioException>(() => bd.Moderacion.AprobarAsync(4, TipoObjetivoModeracion.Evento, id), "No puedes moderar tu propio evento.");
        }
        finally
        {
            await Sql("UPDATE user SET is_mod = FALSE WHERE id = 4");
        }
        return "";
    });
    await Prueba("Bandeja de eventos pendientes: el que lleva más tiempo esperando va primero", async () =>
    {
        int primero = await bd.Eventos.CrearAsync(4, EventoDePrueba("Enviado primero", DateTime.Now.AddDays(40)));
        int segundo = await bd.Eventos.CrearAsync(4, EventoDePrueba("Enviado después", DateTime.Now.AddDays(5)));
        var ids = (await bd.Eventos.ListarPorEstadoAsync(EstadoEvento.Pendiente)).Select(e => e.Id).ToList();
        Afirmar(ids.IndexOf(primero) < ids.IndexOf(segundo), $"orden: {string.Join(", ", ids)}");
        return $"{ids.Count} pendientes";
    });
    await Prueba("Reportes: cada uno queda ligado a su evento o lugar (con los JOIN reales)", async () =>
    {
        int deLugar = await bd.Reportes.CrearAsync(6, TipoObjetivoReporte.ResenaLugar, 1, MotivoReporte.Spam, null);
        int deEvento = await bd.Reportes.CrearAsync(6, TipoObjetivoReporte.Evento, 1, MotivoReporte.Otro, "Detalle");
        var lista = await bd.Reportes.ListarDetalleAsync();
        var resena = lista.Single(r => r.Reporte.Id == deLugar);
        var eResena = (await bd.Resenas.ListarAsync(TipoResena.Lugar, resena.LugarId!.Value)).Single(r => r.Id == 1);
        Afirmar(resena is { EventoId: null, ResenaOculta: false } && resena.ObjetivoNombre.Length > 0, "reseña → su lugar");
        Afirmar(resena.ResenaAutor == eResena.NombreUsuario && resena.ResenaCalificacion == eResena.Calificacion, "datos de la reseña");
        Afirmar(lista.Single(r => r.Reporte.Id == deEvento) is { EventoId: 1, LugarId: null, ResenaAutor: null, ReportanteNombre: "diego_m" }, "evento");
        Afirmar(await bd.Reportes.ResolverAsync(deEvento, 1, EstadoReporte.Resuelto), "resolver");
        await EsperarError<ReglaNegocioException>(() => bd.Reportes.ResolverAsync(deLugar, 6, EstadoReporte.Descartado), ConexionDB.SoloModeradores);
        return "";
    });
    await Prueba("Reportes: orden y reabrir con la BD real", async () =>
    {
        int id = await bd.Reportes.CrearAsync(6, TipoObjetivoReporte.Lugar, 1, MotivoReporte.Spam, null);
        var abiertos = await bd.Reportes.ListarDetalleAsync();
        Afirmar(abiertos.Zip(abiertos.Skip(1)).All(p => p.First.Reporte.CreadoEn <= p.Second.Reporte.CreadoEn), "el más antiguo primero");
        Afirmar(await bd.Reportes.ResolverAsync(id, 1, EstadoReporte.Descartado), "cerrar");
        Afirmar((await bd.Reportes.ListarDetalleAsync(EstadoReporte.Descartado))[0].Reporte.Id == id, "el último en cerrarse primero");
        await EsperarError<ReglaNegocioException>(() => bd.Reportes.ReabrirAsync(id, 6), ConexionDB.SoloModeradores);
        Afirmar(await bd.Reportes.ReabrirAsync(id, 2) && !await bd.Reportes.ReabrirAsync(id, 2), "reabrir una vez");
        Afirmar((await bd.Reportes.ListarAsync()).Any(r => r.Id == id && r.ResueltoPor == null), "abierto otra vez");
        Afirmar(await bd.Reportes.ResolverAsync(id, 1, EstadoReporte.Descartado), "dejarlo cerrado");
        return "";
    });
    await Prueba("Ocultar reseña: todo junto y queda en el historial de reseñas", async () =>
    {
        int reporte = (await bd.Reportes.ListarDetalleAsync()).First(r => r.Reporte.Tipo == TipoObjetivoReporte.ResenaLugar && r.Reporte.ObjetivoId == 1).Reporte.Id;
        await EsperarError<ReglaNegocioException>(() => bd.Resenas.CambiarVisibilidadAsync(6, TipoResena.Lugar, 1, true), ConexionDB.SoloModeradores);
        Afirmar(await bd.Resenas.CambiarVisibilidadAsync(2, TipoResena.Lugar, 1, true), "ocultar");
        Afirmar(!await bd.Resenas.CambiarVisibilidadAsync(2, TipoResena.Lugar, 1, true), "dos veces no");
        Afirmar((await bd.Reportes.ListarAsync(EstadoReporte.Resuelto)).Any(r => r.Id == reporte && r.ResueltoPor == 2), "su reporte se cerró");

        await EsperarError<ReglaNegocioException>(() => bd.Moderacion.ListarHistorialResenasAsync(2), ModeracionDB.SoloSuperHistorial);
        int antes = await bd.Moderacion.ContarHistorialResenasAsync(1);
        var historial = await bd.Moderacion.ListarHistorialResenasAsync(1);
        Afirmar(historial[0] is { ModeradorNombre: "mod_luis", Tipo: TipoResena.Lugar, ResenaId: 1, Accion: AccionResena.Ocultada } && historial[0].ObjetivoNombre.Length > 0, "historial");

        Afirmar(await bd.Resenas.CambiarVisibilidadAsync(2, TipoResena.Lugar, 1, false), "volver a mostrar");
        Afirmar(await bd.Moderacion.ContarHistorialResenasAsync(1) == antes + 1, "también queda");
        return "";
    });
    await Prueba("Historial de moderación: filtros y total con los JOIN reales", async () =>
    {
        var todo = await bd.Moderacion.ListarHistorialAsync(limite: 500);
        Afirmar(todo.Count == await bd.Moderacion.ContarHistorialAsync() && todo.All(r => r.ObjetivoNombre.Length > 0), "todo con nombre");
        foreach (var decision in new[] { DecisionModeracion.Aprobado, DecisionModeracion.Rechazado })
        {
            var filtrado = await bd.Moderacion.ListarHistorialAsync(limite: 500, decision: decision);
            Afirmar(filtrado.All(r => r.Decision == decision) && filtrado.Count == todo.Count(r => r.Decision == decision), "por decisión");
            Afirmar(await bd.Moderacion.ContarHistorialAsync(decision) == filtrado.Count, "conteo por decisión");
        }
        foreach (var tipo in Enum.GetValues<TipoObjetivoModeracion>())
            Afirmar(await bd.Moderacion.ContarHistorialAsync(tipo: tipo) == todo.Count(r => r.Tipo == tipo), $"conteo de {tipo}");
        return $"{todo.Count} decisiones";
    });
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
