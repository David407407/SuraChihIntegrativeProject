/**
 * @file Textos fijos de la landing (copiados del frame "Landing" de Figma).
 * Separados de los componentes para poder corregir redacción sin tocar lógica.
 * Lo que cambia con la BD (eventos, lugares, cifras del hero) llega por el puente.
 */

export const hero = {
  sello: 'Hecho en Chihuahua',
  titulo: 'Descubre lo que pasa en Chihuahua antes de que pase.',
  bajada: 'Conciertos, talleres, cafés y lugares para salir, reunidos en un solo lugar y revisados antes de publicarse.',
  chipInscrito: 'Ya vas a ir',
  chipFavorito: 'Guardado en favoritos',
};

/** Palabras de la cinta azul animada. */
export const cintaCategorias = [
  'Conciertos', 'Talleres', 'Cafés', 'Museos', 'Aire libre',
  'Gastronomía', 'Planes en familia', 'Festivales', 'Parques',
];

export const comoFunciona = {
  titulo: 'Tu plan, en tres pasos',
  bajada: 'Sin buscar en cinco redes distintas ni enterarte cuando ya pasó.',
  pasos: [
    { icono: 'paso-buscar', titulo: 'Descubre', texto: 'Explora conciertos, talleres, cafés y lugares por categoría, fecha o en el mapa.' },
    { icono: 'paso-calendario', titulo: 'Guarda o inscríbete', texto: 'Marca con corazón lo que te interesa y toca “Voy a ir” para tenerlo en Mis planes.' },
    { icono: 'paso-estrella', titulo: 'Ve y opina', texto: 'Asiste, deja tu reseña y ayuda a otras personas a descubrir lugares nuevos.' },
  ],
};

export const eventosDestacados = {
  titulo: 'No te los pierdas',
  bajada: 'Lo más guardado de las próximas semanas.',
  vacio: 'Pronto habrá eventos publicados. ¡Vuelve en unos días!',
};

export const encuesta = {
  titulo: 'Chihuahua tiene mucho que hacer. Encontrarlo no debería ser difícil.',
  datos: [
    { cifra: '78.9%', texto: 'se entera de los eventos cuando ya pasaron o el mismo día.' },
    { cifra: '86.8%', texto: 'cree que los negocios independientes tienen menos alcance que las plazas comerciales.' },
    { cifra: '92.1%', texto: 'visita siempre los mismos lugares por no conocer otras opciones.' },
  ],
  fuente: 'Encuesta a 38 personas de Chihuahua, proyecto integrador UTCH 2026.',
};

export const lugares = {
  titulo: 'Lugares para descubrir',
  bajada: 'Cafés, museos y parques que quizá todavía no conoces.',
  vacio: 'Todavía no hay lugares publicados.',
};

export const organizadores = {
  etiqueta: 'Para organizadores',
  titulo: '¿Organizas eventos en Chihuahua?',
  texto: 'El 86.8% de las personas que encuestamos cree que los negocios independientes tienen menos alcance. Publica tu evento: un moderador lo revisa y llega a quien busca qué hacer.',
};

export const llamadoFinal = {
  titulo: 'Tu próximo plan está a un clic',
  texto: 'Crea tu cuenta y guarda lo que te interesa para no enterarte tarde.',
};

export const pie = {
  lema: 'Eventos y lugares de Chihuahua, en un solo lugar.',
  columnas: [
    { titulo: 'Explorar', enlaces: [
      { texto: 'Eventos', accion: 'explorar-eventos' },
      { texto: 'Lugares', accion: 'explorar-lugares' },
      { texto: 'Mapa', accion: 'mapa' },
    ] },
    { titulo: 'Cuenta', enlaces: [
      { texto: 'Iniciar sesión', accion: 'iniciar-sesion' },
      { texto: 'Crear cuenta', accion: 'crear-cuenta' },
    ] },
    { titulo: 'Proyecto', enlaces: [
      { texto: 'Proyecto integrador UTCH' },
      { texto: '2026' },
    ] },
  ],
};
