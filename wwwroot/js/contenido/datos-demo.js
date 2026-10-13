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

// ---------------------------------------------------------------------
// Cuentas de prueba (mismos usuarios que surachih_v1.sql; contraseña de todos: Test1234!)
// Viven mientras la pestaña está abierta.
// ---------------------------------------------------------------------

const CONTRASENA_DEMO = 'Test1234!';
const cuentas = [
  { id: 1, nombreUsuario: 'mod_ana', correo: 'ana.mod@example.com', esModerador: true, esOrganizador: false },
  { id: 3, nombreUsuario: 'cafe_dande', correo: 'dandelion@example.com', esModerador: false, esOrganizador: true },
  { id: 5, nombreUsuario: 'mariana_r', correo: 'mariana.r@example.com', esModerador: false, esOrganizador: false },
].map((c) => ({ ...c, avatarUrl: null, contrasena: CONTRASENA_DEMO }));

// token: enlace "enviado" sin abrir; abierto: enlace abierto sin usar; de: cuenta a la que pertenece.
const demo = { sesion: null, token: null, abierto: null, de: null, favoritos: new Set() };

const publico = ({ contrasena, ...usuario }) => structuredClone(usuario);   // nunca sale la contraseña

async function errorDemo(codigo, mensaje) {
  const { ErrorPuente } = await import('../nucleo/puente.js');
  return new ErrorPuente(codigo, mensaje);
}

/** Simula las respuestas de C#. Para probar estados vacíos o errores, cámbialo aquí. */
export async function responderDemo(accion, datos) {
  await new Promise((r) => setTimeout(r, 250));   // latencia de mentira para ver los estados de carga
  switch (accion) {
    case 'landing.obtener': return structuredClone(landing);
    case 'sesion.obtener': return demo.sesion && publico(demo.sesion);
    case 'sesion.cerrar': demo.sesion = null; return null;

    case 'cuenta.iniciarSesion': {
      const id = datos.identificador.trim().toLowerCase();
      const cuenta = cuentas.find((c) => c.correo === id || c.nombreUsuario.toLowerCase() === id);
      if (!cuenta || cuenta.contrasena !== datos.contrasena) throw await errorDemo('datos-invalidos', 'Correo o contraseña incorrectos.');
      demo.sesion = cuenta;
      return publico(cuenta);
    }
    case 'cuenta.registrar': {
      const correo = datos.correo.trim().toLowerCase();
      if (cuentas.some((c) => c.correo === correo)) throw await errorDemo('duplicado', 'Ese correo ya está registrado.');
      if (cuentas.some((c) => c.nombreUsuario.toLowerCase() === datos.nombreUsuario.toLowerCase())) {
        throw await errorDemo('duplicado', 'Ese nombre de usuario ya está ocupado.');
      }
      const cuenta = { id: 100 + cuentas.length, nombreUsuario: datos.nombreUsuario, correo, avatarUrl: null,
        esModerador: false, esOrganizador: false, contrasena: datos.contrasena };
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

    case 'favoritos.alternar': {
      if (!demo.sesion) throw await errorDemo('requiere-sesion', 'Inicia sesión para guardar tus favoritos.');
      const llave = `${datos.tipo}:${datos.id}`;
      if (demo.favoritos.has(llave)) demo.favoritos.delete(llave);
      else demo.favoritos.add(llave);
      return demo.favoritos.has(llave);
    }
    default: throw new Error(`Acción demo no implementada: ${accion}`);
  }
}
