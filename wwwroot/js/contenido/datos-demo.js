/**
 * @file Datos de ejemplo para abrir la interfaz en un navegador sin la app de escritorio.
 *
 * Tienen exactamente la misma forma que lo que responde C# (records de BackendLogica
 * serializados en camelCase), así que sirven también como documentación del contrato.
 * Las fotos están en assets/demo; en la app reales vienen de Cloudinary.
 */

const etiqueta = (id, nombre) => ({ id, nombre, icono: '', colorHex: '#0B1B3A', alcance: 'ambos', orden: id });

const evento = (id, titulo, inicio, fin, lugar, precioMin, precioMax, imagen, etiquetas = [], extra = {}) => ({
  id, titulo, inicio, fin, precioMin, precioMax,
  esGratis: precioMax === 0,
  descripcion: '',
  imagenUrl: imagen && `assets/demo/${imagen}`,
  lugarNombre: lugar,
  direccion: null,
  estado: 'aprobado',
  interesados: 0,
  etiquetas,
  ...extra,
});

const lugar = (id, nombre, direccion, tipo, calificacion, precioMin, precioMax, hasta, imagen) => ({
  id, nombre, direccion, calificacion, precioMin, precioMax,
  imagenUrl: `assets/demo/${imagen}`,
  numResenas: 12,
  etiquetas: [etiqueta(100 + id, tipo)],
  horarioAhora: { abierto: true, hasta, abreA: null },
});

const harryPotter = evento(1, 'Harry Potter: Bosque Prohibido', '2026-10-30T19:00:00', '2026-11-30T23:00:00',
  'Parque El Palomar', 450, 650, 'harry-potter.jpg', [etiqueta(1, 'Familiar')], { descripcion: 'Experiencia nocturna' });
const minecraft = evento(2, 'Minecraft Experience: Villager Rescue', '2026-10-23T10:00:00', '2026-10-25T20:00:00',
  'Forum Buenavista', 600, 850, 'minecraft.jpg', [etiqueta(1, 'Familiar')]);
const cafeTercera = lugar(2, 'Café de la Tercera', 'C. Tercera 805, Centro', 'Café', 4.6, 100, 200, '21:00:00', 'cafe-tercera.jpg');

const landing = {
  resumen: { eventosEstaSemana: 21, lugaresParaDescubrir: 17, categorias: 5 },
  collage: [
    { tipo: 'evento', evento: harryPotter },
    { tipo: 'evento', evento: { ...minecraft, titulo: 'Minecraft Experience' } },
    { tipo: 'lugar', lugar: cafeTercera },
  ],
  eventos: [
    evento(3, 'Mass of the Fermenting Dregs', '2027-05-18T21:00:00', '2027-05-18T23:30:00', 'ORBE', 700, 1200,
      'mass-fermenting-dregs.jpg', [etiqueta(2, 'Música')], {
        descripcion: 'Guitarras intensas, cambios de ritmo y atmósferas envolventes en un show que promete llevar la energía al máximo.',
      }),
    minecraft,
    evento(4, 'Mac DeMarco', '2026-11-06T21:00:00', '2026-11-06T23:59:00', 'Quarry Studios', 1450, 2600, 'mac-demarco.jpg'),
    evento(5, 'OKU Sushi Class', '2026-10-16T18:00:00', '2026-12-18T20:00:00', 'OKU', 1280, 1280, 'oku-sushi.jpg'),
    evento(6, 'Campamento del Terror', '2026-11-07T20:00:00', '2026-11-07T23:00:00', 'ORBE', 799, 1100, 'campamento-terror.jpg'),
  ],
  lugares: [
    lugar(10, 'Centro Cultural Quinta Gameros', 'Av. Paseo Bolívar 401', 'Museo', 4.8, 37, 37, '18:00:00', 'quinta-gameros.jpg'),
    lugar(11, 'Catedral Metropolitana de Chihuahua', 'Calle Guadalupe Victoria', 'Templo', 4.8, 0, 0, '20:00:00', 'catedral.jpg'),
    lugar(12, 'Grutas Nombre de Dios', 'Vialidad Sacramento', 'Naturaleza', 4.7, 50, 50, '16:00:00', 'grutas.jpg'),
    lugar(13, 'Dandelion Coffee', 'C. Monte Bello 4334', 'Café', 4.9, 100, 200, '21:00:00', 'dandelion.jpg'),
  ],
};

/** Agrega a un lugar lo que usa el mapa: coordenadas y el icono de su etiqueta (tag.icon decide el pin). */
const enMapa = (base, latitud, longitud, icono) => ({
  ...base, latitud, longitud,
  etiquetas: base.etiquetas.map((t) => ({ ...t, icono, alcance: 'lugar' })),
});

/** Respuesta de 'mapa.obtener': los 7 lugares del frame "Mapa" de Figma. */
const mapa = {
  lugares: [
    enMapa(lugar(2, 'Café de la Tercera', 'C. Tercera 805, Zona Centro', 'Café', 4.5, 200, 300, '21:00:00', 'cafe-tercera.jpg'), 28.6355, -106.077, 'coffee'),
    enMapa(lugar(10, 'Centro Cultural Quinta Gameros', 'Av. Paseo Bolívar 401', 'Museo', 4.8, 37, 37, '18:00:00', 'quinta-gameros.jpg'), 28.6361, -106.0722, 'museum'),
    enMapa(lugar(11, 'Catedral Metropolitana de Chihuahua', 'Calle Guadalupe Victoria', 'Templo', 4.8, 0, 0, '20:00:00', 'catedral.jpg'), 28.6359, -106.0762, 'museum'),
    enMapa(lugar(14, 'Museo de la Revolución', 'C. 10a 3010', 'Museo', 4.8, 10, 10, '17:00:00', 'museo-revolucion.jpg'), 28.6283, -106.0727, 'museum'),
    enMapa(lugar(15, 'Unidad Deportiva Sur', 'Blvd. José Fuentes Mares', 'Parque', 4.4, 0, 0, '22:00:00', 'deportiva-sur.jpg'), 28.603, -106.035, 'tree'),
    enMapa(lugar(16, 'Parque Metropolitano Presa El Rejón', 'JV7H+88, 31207', 'Parque', 4.9, 0, 0, '22:00:00', 'presa-rejon.jpg'), 28.6133, -106.1217, 'tree'),
    enMapa(lugar(13, 'Dandelion Coffee', 'C. Monte Bello 4334', 'Café', 4.9, 100, 200, '21:00:00', 'dandelion.jpg'), 28.652, -106.119, 'coffee'),
  ],
};
// ---------------------------------------------------------------------
// Detalle de evento ('evento.obtener'): los eventos de la landing con todos sus datos, más uno
// terminado y uno cancelado para ver esos estados (#evento-8 y #evento-9 en la dirección).
// ---------------------------------------------------------------------

/** Las 5 categorías de BaseDeDatos/surachih_v1.sql con su tag.icon (decide el color del chip). */
const categoria = {
  familiar: { ...etiqueta(1, 'Familiar'), icono: 'family', alcance: 'evento' },
  musica: { ...etiqueta(2, 'Música'), icono: 'music', alcance: 'evento' },
  deportes: { ...etiqueta(3, 'Deportes y aire libre'), icono: 'run', alcance: 'evento' },
  tecnologia: { ...etiqueta(4, 'Tecnología y conferencias'), icono: 'robot', alcance: 'evento' },
  gastronomia: { ...etiqueta(5, 'Gastronomía'), icono: 'food', alcance: 'evento' },
};

const conDetalle = (base, extra) => ({
  ...base, latitud: null, longitud: null, boletosUrl: null, terminado: false, motivoCancelacion: null, ...extra,
});

const eventosDetalle = [
  conDetalle(harryPotter, {
    titulo: 'Harry Potter™: La Experiencia del Bosque Prohibido', fechaPorConfirmar: true, precioPorConfirmar: true,
    lugarNombre: null, direccion: 'Chihuahua', etiquetas: [categoria.familiar],
    descripcion: 'Recorrido nocturno con luces, sonido y criaturas inspiradas en el mundo mágico.',
  }),
  conDetalle(minecraft, {
    inicio: '2026-10-23T14:00:00', fin: '2026-10-25T21:00:00', latitud: 28.6587, longitud: -106.0879,
    etiquetas: [categoria.familiar, categoria.tecnologia], boletosUrl: 'https://boletos.example.com/minecraft',
    descripcion: 'Embárcate en una misión inmersiva e interactiva de Minecraft junto a tus amigos y familia. '
      + '¡Salva el día en Minecraft Experience: Villager Rescue!',
  }),
  conDetalle(landing.eventos[0], {
    latitud: 28.6413, longitud: -106.0731, etiquetas: [categoria.musica], boletosUrl: 'https://boletos.example.com/mfd',
  }),
  conDetalle(landing.eventos[2], {
    latitud: 28.6302, longitud: -106.0702, etiquetas: [categoria.musica], boletosUrl: 'https://boletos.example.com/mac',
    descripcion: 'El cantautor canadiense regresa con su gira de guitarras relajadas y melodías pegajosas.',
  }),
  conDetalle(landing.eventos[3], {
    latitud: 28.6449, longitud: -106.0888, etiquetas: [categoria.gastronomia],
    descripcion: 'Vive una experiencia gastronómica japonesa íntima: aprende a preparar nigiri y makis con el chef.',
  }),
  conDetalle(landing.eventos[4], {
    latitud: 28.6133, longitud: -106.1217, etiquetas: [categoria.deportes, categoria.familiar],
    boletosUrl: 'https://boletos.example.com/campamento',
    descripcion: 'Adéntrate con tu equipo en el bosque y enfrenta la Zona Infectada y el Bosque del Misterio. '
      + 'Tendrás que completar misiones, superar desafíos de supervivencia, resolver búsquedas y enfrentarte a criaturas.',
  }),
  conDetalle(evento(8, 'Las Ligas Menores', '2025-11-27T21:00:00', '2025-11-27T23:30:00', 'Foro Indie Rocks!', 450, 700, null), {
    terminado: true, latitud: 28.6365, longitud: -106.0791, etiquetas: [categoria.musica],
    descripcion: 'La banda argentina de indie rock Las Ligas Menores en concierto.',
  }),
  conDetalle(evento(9, 'Saramalacara', '2026-11-14T21:00:00', '2026-11-14T23:30:00', 'ORBE', 550, 800, null), {
    estado: 'cancelado', latitud: 28.6413, longitud: -106.0731, etiquetas: [categoria.musica],
    descripcion: 'Rap y electrónica desde Argentina en su primera visita a Chihuahua.',
    motivoCancelacion: 'El organizador canceló el evento por causas de fuerza mayor. Te avisaremos si se reprograma.',
  }),
];

const vigente = (e) => e.estado === 'aprobado' && !e.terminado;

// ---------------------------------------------------------------------
// Detalle de lugar ('lugar.obtener'): los lugares del mapa con descripción y opiniones.
// ---------------------------------------------------------------------

const DESCRIPCIONES = {
  2: 'Café de especialidad en el centro, con un espacio tranquilo para trabajar o pasar la tarde.',
  10: 'Mansión art nouveau de principios del siglo XX con salas de arte e historia de Chihuahua.',
  11: 'La catedral barroca más importante del norte de México, frente a la Plaza de Armas.',
  12: 'Cavernas con formaciones de estalactitas y estalagmitas a unos minutos de la ciudad.',
  13: 'Cafetería de barrio con postres caseros y métodos de filtrado.',
  14: 'Antigua casa de Pancho Villa convertida en museo de la Revolución Mexicana.',
  15: 'Pistas, canchas y áreas verdes para correr, andar en bici o pasar el día en familia.',
  16: 'Presa con malecón, ciclovía y miradores; ideal para el atardecer.',
};

const opinionDemo = (id, nombreUsuario, calificacion, comentario) => ({
  id, tipo: 'lugar', nombreUsuario, avatarUrl: null, calificacion, comentario, respuesta: null, oculta: false,
  creadaEn: '2026-10-01T12:00:00',
});
const OPINIONES = [
  opinionDemo(1, 'mariana_r', 5, 'Muy buen lugar, regresaría con amigos.'),
  opinionDemo(2, 'diego_m', 4, 'Tranquilo y bonito; vale la pena la visita.'),
];

const lugaresDetalle = [
  ...mapa.lugares,
  enMapa(landing.lugares[2], 28.7983, -106.1048, 'tree'),   // Grutas Nombre de Dios
].map((l) => ({ ...l, descripcion: DESCRIPCIONES[l.id] ?? null, estado: 'aprobado' }));

/** Respuesta de 'lugar.obtener' (DatosDetalleLugar de Controladores/ControladorLugar.cs). */
function detalleLugar(id) {
  const lugar = lugaresDetalle.find((l) => l.id === id);
  if (!lugar) return null;
  const tipo = lugar.etiquetas[0]?.icono;
  const recomendados = [...lugaresDetalle.filter((l) => l.etiquetas[0]?.icono === tipo), ...lugaresDetalle]
    .filter((l, i, todos) => l.id !== id && todos.findIndex((o) => o.id === l.id) === i)
    .slice(0, 3);
  return structuredClone({
    lugar,
    galeria: [],
    favorito: demo.favoritos.has(`lugar:${id}`),
    opiniones: lugar.calificacion != null ? OPINIONES : [],
    recomendados,
    favoritos: [...demo.favoritos].filter((f) => f.startsWith('lugar:')).map((f) => Number(f.split(':')[1])),
  });
}

/** Respuesta de 'evento.obtener' (DatosDetalleEvento de Controladores/ControladorEvento.cs). */
function detalleEvento(id) {
  const evento = eventosDetalle.find((e) => e.id === id);
  if (!evento) return null;
  const ids = new Set(evento.etiquetas.map((t) => t.id));
  const parecidos = eventosDetalle.filter((e) => vigente(e) && e.etiquetas.some((t) => ids.has(t.id)));
  const recomendados = [...parecidos, ...eventosDetalle.filter(vigente)]
    .filter((e, i, todos) => e.id !== id && todos.findIndex((o) => o.id === e.id) === i)
    .slice(0, 3);
  const favoritos = [...demo.favoritos].filter((f) => f.startsWith('evento:')).map((f) => Number(f.split(':')[1]));
  return structuredClone({
    evento,
    galeria: [],
    inscrito: Boolean(demo.sesion) && demo.inscripciones.has(id),
    favorito: favoritos.includes(id),
    numResenas: evento.terminado ? 12 : null,
    recomendados,
    favoritos,
  });
}

// ---------------------------------------------------------------------
// Home ('home.obtener'): los mismos eventos y lugares, repartidos en sus secciones.
// ---------------------------------------------------------------------

/** Respuesta de 'home.obtener' (DatosHome de Controladores/ControladorHome.cs). */
function homeDemo() {
  const ids = (prefijo) => new Set([...demo.favoritos].filter((f) => f.startsWith(prefijo)).map((f) => Number(f.split(':')[1])));
  const favoritosEventos = ids('evento:');
  const favoritosLugares = ids('lugar:');
  const eventoPorId = (id) => eventosDetalle.find((e) => e.id === id);
  const lugarPorId = (id) => lugaresDetalle.find((l) => l.id === id);
  const vigentes = eventosDetalle.filter(vigente);
  const eventosCon = (iconos) => vigentes.filter((e) => e.etiquetas.some((t) => iconos.includes(t.icono)));
  const lugaresCon = (iconos) => lugaresDetalle
    .filter((l) => l.etiquetas.some((t) => iconos.includes(t.icono)))
    .sort((a, b) => (b.calificacion ?? 0) - (a.calificacion ?? 0));
  const deEvento = (id) => ({ tipo: 'evento', evento: eventoPorId(id) });
  const deLugar = (id) => ({ tipo: 'lugar', lugar: lugarPorId(id) });

  return structuredClone({
    usuarioId: demo.sesion?.id ?? null,
    carrusel: [deEvento(2), deEvento(3), deEvento(5), deEvento(6), deLugar(2)],
    recomendados: [deEvento(3), deEvento(5), deLugar(10), deLugar(2)],
    categorias: [
      categoria.musica, categoria.familiar, categoria.deportes, categoria.tecnologia, categoria.gastronomia,
      { ...etiqueta(6, 'Cafetería'), icono: 'coffee', alcance: 'lugar' },
      { ...etiqueta(10, 'Museo y galería'), icono: 'museum', alcance: 'lugar' },
    ],
    // «Inicio del usuario»: con sesión, primero sus favoritos; después por fecha.
    tePodrianInteresar: [...vigentes].sort((a, b) =>
      (favoritosEventos.has(b.id) - favoritosEventos.has(a.id)) || a.inicio.localeCompare(b.inicio)),
    musica: eventosCon(['music']),
    actividades: eventosCon(['family', 'run', 'robot', 'food']),
    cafes: lugaresCon(['coffee']),
    paraLaTarde: lugaresCon(['museum', 'tree', 'stage']),
    favoritosEventos: [...favoritosEventos],
    favoritosLugares: [...favoritosLugares],
  });
}

// ---------------------------------------------------------------------
// Cuentas de prueba (mismos usuarios que surachih_v1.sql; contraseña de todos: Test1234!)
// Viven mientras la pestaña está abierta.
// ---------------------------------------------------------------------

const CONTRASENA_DEMO = 'Test1234!';
const cuentas = [
  { id: 1, nombreUsuario: 'mod_ana', correo: 'ana.mod@example.com', esModerador: true, esOrganizador: false },
  { id: 3, nombreUsuario: 'cafe_dande', correo: 'dandelion@example.com', esModerador: false, esOrganizador: true },
  { id: 5, nombreUsuario: 'mariana_r', correo: 'mariana.r@example.com', esModerador: false, esOrganizador: false },
].map((c) => ({ ...c, avatarUrl: null, contrasena: CONTRASENA_DEMO, activo: true }));

// token: enlace "enviado" sin abrir; abierto: enlace abierto sin usar; de: cuenta a la que pertenece.
const demo = { sesion: null, token: null, abierto: null, de: null, favoritos: new Set(), inscripciones: new Set() };

// En la app la sesión vive en C#. Aquí se guarda en sessionStorage para que siga al cambiar de
// pantalla (p. ej. del Home a Mi perfil) mientras la pestaña esté abierta.
const LLAVE_DEMO = 'surachih-demo';

function guardarDemo() {
  try {
    sessionStorage.setItem(LLAVE_DEMO, JSON.stringify({
      sesionId: demo.sesion?.id ?? null, cuentas, favoritos: [...demo.favoritos], inscripciones: [...demo.inscripciones],
    }));
  } catch { /* sin almacenamiento (modo privado): la sesión demo dura lo que la página */ }
}

(function cargarDemo() {
  try {
    const guardado = JSON.parse(sessionStorage.getItem(LLAVE_DEMO) ?? 'null');
    if (!guardado) return;
    cuentas.splice(0, cuentas.length, ...guardado.cuentas);
    demo.sesion = cuentas.find((c) => c.id === guardado.sesionId) ?? null;
    demo.favoritos = new Set(guardado.favoritos);
    demo.inscripciones = new Set(guardado.inscripciones);
  } catch { /* datos dañados o sin almacenamiento: se empieza de cero */ }
}());

const publico = ({ contrasena, activo, ...usuario }) => structuredClone(usuario);   // nunca sale la contraseña

/** Lo que haría la llave UNIQUE de user (mismos mensajes que BackendLogica). */
async function revisarDuplicados(nombreUsuario, correo, excepto = null) {
  const otras = cuentas.filter((c) => c !== excepto);
  if (otras.some((c) => c.correo === correo)) throw await errorDemo('duplicado', 'Ese correo ya está registrado.');
  if (otras.some((c) => c.nombreUsuario.toLowerCase() === nombreUsuario.toLowerCase())) {
    throw await errorDemo('duplicado', 'Ese nombre de usuario ya está ocupado.');
  }
}

/** Respuesta de 'favoritos.listar' (DatosFavoritos de Controladores/ControladorPerfil.cs), lo más reciente primero. */
function favoritosDemo() {
  const guardados = [...demo.favoritos].reverse().map((f) => f.split(':'));
  const de = (tipo, lista) => guardados
    .filter(([t]) => t === tipo)
    .map(([, id]) => lista.find((item) => item.id === Number(id)))
    .filter(Boolean);
  return structuredClone({ eventos: de('evento', eventosDetalle), lugares: de('lugar', lugaresDetalle) });
}

async function errorDemo(codigo, mensaje) {
  const { ErrorPuente } = await import('../nucleo/puente.js');
  return new ErrorPuente(codigo, mensaje);
}

/** Simula las respuestas de C#. Para probar estados vacíos o errores, cámbialo en `responder`. */
export async function responderDemo(accion, datos) {
  await new Promise((r) => setTimeout(r, 250));   // latencia de mentira para ver los estados de carga
  try {
    return await responder(accion, datos);
  } finally {
    guardarDemo();
  }
}

async function responder(accion, datos) {
  switch (accion) {
    case 'landing.obtener': return structuredClone(landing);
    case 'mapa.obtener': return structuredClone(mapa);
    case 'home.obtener': return homeDemo();
    case 'sesion.obtener': return demo.sesion && publico(demo.sesion);
    case 'sesion.cerrar': demo.sesion = null; return null;

    case 'cuenta.iniciarSesion': {
      const id = datos.identificador.trim().toLowerCase();
      const cuenta = cuentas.find((c) => c.correo === id || c.nombreUsuario.toLowerCase() === id);
      if (!cuenta?.activo || cuenta.contrasena !== datos.contrasena) throw await errorDemo('datos-invalidos', 'Correo o contraseña incorrectos.');
      demo.sesion = cuenta;
      return publico(cuenta);
    }
    case 'cuenta.registrar': {
      const correo = datos.correo.trim().toLowerCase();
      await revisarDuplicados(datos.nombreUsuario, correo);
      const cuenta = { id: 100 + cuentas.length, nombreUsuario: datos.nombreUsuario, correo, avatarUrl: null,
        esModerador: false, esOrganizador: false, contrasena: datos.contrasena, activo: true };
      cuentas.push(cuenta);
      demo.sesion = cuenta;
      return publico(cuenta);
    }
    case 'cuenta.recuperar': {
      const cuenta = cuentas.find((c) => c.correo === datos.correo.trim().toLowerCase());
      demo.token = cuenta ? crypto.randomUUID() : null;
      demo.abierto = null;
      demo.de = cuenta ?? null;
      return null;
    }
    case 'cuenta.abrirEnlace': {
      if (!demo.token) throw await errorDemo('datos-invalidos', 'No encontramos un enlace reciente. Revisa el correo que escribiste y pide uno nuevo.');
      demo.abierto = demo.token;
      demo.token = null;
      return { token: demo.abierto };
    }
    case 'cuenta.restablecer': {
      if (!demo.abierto || datos.token !== demo.abierto) throw await errorDemo('regla-negocio', 'El enlace ya expiró o ya se usó. Pide uno nuevo.');
      demo.de.contrasena = datos.contrasena;
      demo.abierto = null;
      return null;
    }

    // Mi perfil, Eliminar cuenta y Mis favoritos (ControladorPerfil.cs)
    case 'perfil.actualizar': {
      if (!demo.sesion) throw await errorDemo('requiere-sesion', 'Inicia sesión para editar tu perfil.');
      const correo = datos.correo.trim().toLowerCase();
      await revisarDuplicados(datos.nombreUsuario.trim(), correo, demo.sesion);
      demo.sesion.nombreUsuario = datos.nombreUsuario.trim();
      demo.sesion.correo = correo;
      return publico(demo.sesion);
    }
    case 'perfil.contrasena': {
      if (!demo.sesion) throw await errorDemo('requiere-sesion', 'Inicia sesión para cambiar tu contraseña.');
      if (demo.sesion.contrasena !== datos.actual) throw await errorDemo('datos-invalidos', 'La contraseña actual no es correcta.');
      demo.sesion.contrasena = datos.nueva;
      return null;
    }
    case 'perfil.eliminar': {
      if (!demo.sesion) throw await errorDemo('requiere-sesion', 'Inicia sesión para eliminar tu cuenta.');
      if (demo.sesion.contrasena !== datos.contrasena) throw await errorDemo('datos-invalidos', 'La contraseña no es correcta.');
      demo.sesion.activo = false;   // is_active = FALSE: no se borra
      demo.sesion = null;
      return null;
    }
    case 'favoritos.listar': {
      if (!demo.sesion) throw await errorDemo('requiere-sesion', 'Inicia sesión para ver tus favoritos.');
      return favoritosDemo();
    }

    case 'favoritos.alternar': {
      if (!demo.sesion) throw await errorDemo('requiere-sesion', 'Inicia sesión para guardar tus favoritos.');
      const llave = `${datos.tipo}:${datos.id}`;
      if (demo.favoritos.has(llave)) demo.favoritos.delete(llave);
      else demo.favoritos.add(llave);
      return demo.favoritos.has(llave);
    }
    case 'evento.obtener': {
      const detalle = detalleEvento(datos.id);
      if (!detalle) throw await errorDemo('regla-negocio', 'Este evento ya no existe.');
      return detalle;
    }
    case 'lugar.obtener': {
      const detalle = detalleLugar(datos.id);
      if (!detalle) throw await errorDemo('regla-negocio', 'Este lugar ya no existe.');
      return detalle;
    }
    case 'inscripcion.inscribir': {
      if (!demo.sesion) throw await errorDemo('requiere-sesion', 'Inicia sesión para apartar tu lugar.');
      const evento = eventosDetalle.find((e) => e.id === datos.id);
      // Mismo texto que el trigger trg_inscription_bi.
      if (!evento || !vigente(evento)) throw await errorDemo('regla-negocio', 'No puedes inscribirte a este evento.');
      demo.inscripciones.add(datos.id);
      return true;
    }
    case 'inscripcion.cancelar': {
      if (!demo.sesion) throw await errorDemo('requiere-sesion', 'Inicia sesión para cancelar tu inscripción.');
      demo.inscripciones.delete(datos.id);
      return false;
    }
    default: throw new Error(`Acción demo no implementada: ${accion}`);
  }
}
