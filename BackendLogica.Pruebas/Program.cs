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
    await Prueba("Reportes: crear y resolver", async () =>
    {
        int id = await f.Reportes.CrearAsync(6, TipoObjetivoReporte.ResenaLugar, 3, MotivoReporte.Spam, null);
        Afirmar(await f.Reportes.ResolverAsync(id, 1, EstadoReporte.Descartado), "resolver");
        Afirmar((await f.Reportes.ListarAsync()).Count == 0, "ya no está abierto");
        return "";
    });
}

// ----------------------------------------------------------------------
// Implementación real contra MySQL (escribe: solo en una base *_pruebas)
// Lo que consumen las pantallas de las semanas 1–2: cuenta, lectura, favoritos, inscripción y vistas.
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
