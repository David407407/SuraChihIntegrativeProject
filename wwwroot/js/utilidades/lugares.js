/**
 * @file Tipo de un lugar (café, museo, parque...) y su paso a puntos de <sc-mapa>.
 *
 * El tipo sale de las etiquetas del lugar: la primera cuyo `icono` (tag.icon en la BD) esté en
 * TIPOS. Los que no están (restaurant, bar, stage...) usan el pin genérico y no tienen píldora
 * en el mapa; para agregar uno, exporta su icono de Figma a assets/iconos y agrégalo aquí.
 */

/** Por tag.icon: texto de la píldora del mapa e icono del pin (assets/iconos). En el orden de Figma. */
const TIPOS = {
  coffee: { plural: 'Cafés', pin: 'pin-cafe' },
  museum: { plural: 'Museos', pin: 'pin-museo' },
  tree: { plural: 'Parques', pin: 'pin-parque' },
};
const PIN_GENERICO = 'ubicacion';

/** Etiqueta que define el tipo del lugar o, si ninguna es de un tipo conocido, la primera. */
function etiquetaPrincipal(lugar) {
  const etiquetas = lugar.etiquetas ?? [];
  return etiquetas.find((t) => TIPOS[t.icono]) ?? etiquetas[0] ?? null;
}

/**
 * Tipo de un lugar para filtrar y pintar su pin.
 * @param {object} lugar LugarTarjeta
 * @returns {string|null} Clave del tipo ('coffee', 'museum', 'tree') o null si no es de un tipo conocido.
 */
export function tipoLugar(lugar) {
  const icono = etiquetaPrincipal(lugar)?.icono;
  return TIPOS[icono] ? icono : null;
}

/** "Café, C. Tercera 805, Zona Centro": tipo y dirección para la segunda línea de una fila. */
export function subtituloLugar(lugar) {
  return [etiquetaPrincipal(lugar)?.nombre, lugar.direccion].filter(Boolean).join(', ');
}

/**
 * Píldoras de tipo que tiene sentido mostrar: solo los tipos que hay en la lista.
 * @param {object[]} lugares LugarTarjeta[]
 * @returns {{valor:string, texto:string}[]}
 */
export function tiposPresentes(lugares) {
  const presentes = new Set(lugares.map(tipoLugar));
  return Object.entries(TIPOS)
    .filter(([clave]) => presentes.has(clave))
    .map(([clave, tipo]) => ({ valor: clave, texto: tipo.plural }));
}

/** Punto de <sc-mapa> para un LugarTarjeta. */
export function puntoDeLugar(lugar) {
  return {
    id: lugar.id,
    tipo: 'lugar',
    lat: lugar.latitud,
    lng: lugar.longitud,
    titulo: lugar.nombre,
    icono: TIPOS[tipoLugar(lugar)]?.pin ?? PIN_GENERICO,
  };
}

/**
 * Punto de <sc-mapa> para un EventoTarjeta (v_event_card ya trae la ubicación del evento o la de
 * su lugar). Si el evento aún no tiene coordenadas, <sc-mapa> lo ignora.
 */
export function puntoDeEvento(evento) {
  return {
    id: evento.id,
    tipo: 'evento',
    lat: evento.latitud,
    lng: evento.longitud,
    titulo: evento.lugarNombre ?? evento.direccion ?? evento.titulo,
    icono: PIN_GENERICO,
  };
}
