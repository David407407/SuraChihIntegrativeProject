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

/** Simula las respuestas de C#. Para probar estados vacíos o errores, cámbialo aquí. */
export async function responderDemo(accion) {
  await new Promise((r) => setTimeout(r, 250));   // latencia de mentira para ver los estados de carga
  switch (accion) {
    case 'landing.obtener': return structuredClone(landing);
    case 'sesion.obtener': return null;
    case 'favoritos.alternar': {
      const { ErrorPuente } = await import('../nucleo/puente.js');
      throw new ErrorPuente('requiere-sesion', 'Inicia sesión para guardar tus favoritos.');
    }
    default: throw new Error(`Acción demo no implementada: ${accion}`);
  }
}
