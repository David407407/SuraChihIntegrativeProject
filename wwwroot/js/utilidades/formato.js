/**
 * @file Formatos de fechas, precios y horarios en español de México.
 * C# manda datos crudos (fechas ISO, números); aquí se convierten a texto de interfaz.
 */

const MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
const numero = new Intl.NumberFormat('es-MX', { maximumFractionDigits: 0 });

/** Convierte "2026-10-24T17:00:00" (hora local) o Date en Date. */
export function aFecha(valor) {
  return valor instanceof Date ? valor : new Date(valor);
}

/** { dia: '18', mes: 'may' } para la etiqueta de fecha de las tarjetas. */
export function diaYMes(fecha) {
  const f = aFecha(fecha);
  return { dia: String(f.getDate()), mes: MESES[f.getMonth()] };
}

/**
 * Rango de fechas compacto:
 * mismo día → "6 nov" · mismo mes → "23–25 oct" · distinto mes → "16 oct – 18 dic".
 */
export function rangoFechas(inicio, fin) {
  const a = aFecha(inicio);
  const b = aFecha(fin ?? inicio);
  const mismoDia = a.toDateString() === b.toDateString();
  const mismoMes = a.getMonth() === b.getMonth() && a.getFullYear() === b.getFullYear();

  if (mismoDia) return `${a.getDate()} ${MESES[a.getMonth()]}`;
  if (mismoMes) return `${a.getDate()}–${b.getDate()} ${MESES[a.getMonth()]}`;
  return `${a.getDate()} ${MESES[a.getMonth()]} – ${b.getDate()} ${MESES[b.getMonth()]}`;
}

/** "$1,450" */
export function dinero(cantidad) {
  return `$${numero.format(cantidad)}`;
}

/**
 * Texto del precio.
 * @param {number|null} min
 * @param {number|null} max
 * @param {'rango'|'desde'} [estilo] 'rango' → "$600–850" · 'desde' → "Desde $600"
 * @returns {string} '' si no hay dato de precio.
 */
export function precio(min, max, estilo = 'rango') {
  if (min == null && max == null) return '';
  const bajo = min ?? max;
  const alto = max ?? min;
  if (alto === 0) return 'Gratis';
  if (estilo === 'desde') return `Desde ${dinero(bajo)}`;
  if (bajo === alto) return dinero(bajo);
  return `${dinero(bajo)}–${numero.format(alto)}`;
}

/** Fecha de un EventoTarjeta: rango compacto o "Fecha por confirmar". */
export function fechaEvento(e) {
  return e.fechaPorConfirmar ? 'Fecha por confirmar' : rangoFechas(e.inicio, e.fin);
}

/** Precio de un EventoTarjeta: "$600–850", "Gratis" o "Precio por confirmar". */
export function precioEvento(e, estilo = 'rango') {
  return e.precioPorConfirmar ? 'Precio por confirmar' : precio(e.precioMin, e.precioMax, estilo);
}

/** "18:00:00" → "6 p.m." · "21:30:00" → "9:30 p.m." · "00:00:00" → "12 a.m." */
export function hora(valor) {
  const [h, m] = String(valor).split(':').map(Number);
  const sufijo = h < 12 ? 'a.m.' : 'p.m.';
  const h12 = h % 12 === 0 ? 12 : h % 12;
  return m ? `${h12}:${String(m).padStart(2, '0')} ${sufijo}` : `${h12} ${sufijo}`;
}

/**
 * Estado de apertura de un lugar (viene calculado de C#: EstadoHorario).
 * @param {{abierto:boolean, hasta?:string, abreA?:string}|null} estado
 * @returns {{texto:string, abierto:boolean}}
 */
export function horarioLugar(estado) {
  if (estado?.abierto && estado.hasta) return { texto: `Abierto hasta ${hora(estado.hasta)}`, abierto: true };
  if (estado?.abreA) return { texto: `Abre a las ${hora(estado.abreA)}`, abierto: false };
  return { texto: 'Cerrado', abierto: false };
}

/** Calificación con un decimal: 4.8 */
export function calificacion(valor) {
  return valor == null ? '' : Number(valor).toFixed(1);
}
