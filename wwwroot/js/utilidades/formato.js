/**
 * @file Formatos de fechas, precios y horarios en español de México.
 * C# manda datos crudos (fechas ISO, números); aquí se convierten a texto de interfaz.
 */

const MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
const MESES_LARGOS = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre',
  'octubre', 'noviembre', 'diciembre'];
const UN_DIA_MS = 24 * 60 * 60 * 1000;
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

/**
 * Fecha escrita completa de un EventoTarjeta (resumen de los modales "¿Vas a ir?" y "¿Cancelar tu
 * inscripción?"): "6 de noviembre" · "23, 24 y 25 de octubre" · "Del 1 al 29 de noviembre" ·
 * "Del 16 de octubre al 18 de diciembre" · "Fecha por confirmar".
 */
export function fechaLarga(e) {
  if (e.fechaPorConfirmar) return 'Fecha por confirmar';
  const a = aFecha(e.inicio);
  const b = aFecha(e.fin ?? e.inicio);
  const dias = Math.round((new Date(b.getFullYear(), b.getMonth(), b.getDate())
    - new Date(a.getFullYear(), a.getMonth(), a.getDate())) / UN_DIA_MS);
  const mismoMes = a.getMonth() === b.getMonth() && a.getFullYear() === b.getFullYear();
  const mesA = MESES_LARGOS[a.getMonth()];

  if (dias <= 0) return `${a.getDate()} de ${mesA}`;
  if (mismoMes && dias <= 2) {
    const lista = Array.from({ length: dias + 1 }, (_, i) => a.getDate() + i);
    return `${lista.slice(0, -1).join(', ')} y ${lista.at(-1)} de ${mesA}`;
  }
  if (mismoMes) return `Del ${a.getDate()} al ${b.getDate()} de ${mesA}`;
  return `Del ${a.getDate()} de ${mesA} al ${b.getDate()} de ${MESES_LARGOS[b.getMonth()]}`;
}

/** Hora con minutos, como en el dato "Horario" del detalle: 14:00 → "2:00 p.m.", 0:30 → "12:30 a.m." */
export function horaCompleta(fecha) {
  const f = aFecha(fecha);
  const h = f.getHours();
  const h12 = h % 12 === 0 ? 12 : h % 12;
  return `${h12}:${String(f.getMinutes()).padStart(2, '0')} ${h < 12 ? 'a.m.' : 'p.m.'}`;
}

/** "2:00 p.m. a 9:00 p.m." para el dato "Horario" del detalle; '' con fecha por confirmar. */
export function horarioEvento(e) {
  if (e.fechaPorConfirmar) return '';
  return `${horaCompleta(e.inicio)} a ${horaCompleta(e.fin ?? e.inicio)}`;
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
