-- =====================================================================
-- SuraChih - Cartelera real de la ciudad de Chihuahua (noviembre-diciembre 2026) y lugares
-- conocidos de la capital. Para quien ya cargo agregar_eventos_y_lugares_reales.sql: NO borra nada.
-- Ejecutar UNA sola vez en MySQL Workbench con la base surachih (todo va en una transaccion).
--
-- Los publica la cuenta del equipo "surachih_cuu" y los aprueba mod_ana, como el script anterior.
-- Fuentes (consultadas el 3 nov 2026): agenda de Visita Chihuahua Capital, Songkick, Gobierno
-- Municipal (Cuulinaria) y OpenStreetMap para las coordenadas.
-- Aproximado (sin dato oficial):
--   * Hora de fin de los conciertos: 23:59 (o 2 h despues del inicio); Cuulinaria cierra 22:00.
--   * Coordenadas del Teatro de la Ciudad (calle Ojinaga, Centro), de Pistoleros (calle Lucio
--     Alvarado) y de la Union Ganadera (las mismas de ExpoGan, que se hace en sus instalaciones).
-- Sin foto: la app muestra el relleno con el sol hasta que se suba una a Cloudinary.
-- Los lugares no traen horario: la app los muestra como "Cerrado" hasta que se capture.
-- =====================================================================

USE surachih;
SET NAMES utf8mb4;
START TRANSACTION;

SET @org = (SELECT id FROM user WHERE username = 'surachih_cuu');
SET @moderador = (SELECT id FROM user WHERE username = 'mod_ana');

-- ---------------------------------------------------------------------
-- Lugares (etiquetas: 3 Deportes y aire libre, 9 Foro y venue, 10 Museo y galeria, 11 Parque)
-- ---------------------------------------------------------------------
INSERT INTO place (owner_id, name, description, address, latitude, longitude) VALUES
 (@org, 'Casa Chihuahua Centro de Patrimonio Cultural',
  'Museo en el antiguo Palacio Federal. Conserva el calabozo donde estuvo preso Miguel Hidalgo.',
  'Antiguo Palacio Federal, Av. Juárez, Zona Centro, 31000 Chihuahua, Chih.', 28.639588, -106.073991);
SET @casa = LAST_INSERT_ID();
INSERT INTO place (owner_id, name, description, address, latitude, longitude) VALUES
 (@org, 'Quinta Gameros',
  'Casona art nouveau de principios del siglo XX. Hoy es el Centro Cultural Universitario de la UACH, con exposiciones.',
  'Paseo Simón Bolívar 401, Zona Centro, 31000 Chihuahua, Chih.', 28.632236, -106.073565);
SET @quinta = LAST_INSERT_ID();
INSERT INTO place (owner_id, name, description, address, latitude, longitude) VALUES
 (@org, 'Museo Casa de Villa',
  'Museo Histórico de la Revolución en la antigua casa de Pancho Villa (Quinta Luz). Exhibe el auto en que fue emboscado.',
  'Calle 10a 3010, Santa Rosa, Chihuahua, Chih.', 28.626592, -106.068553);
SET @villa = LAST_INSERT_ID();
INSERT INTO place (owner_id, name, description, address, latitude, longitude) VALUES
 (@org, 'Museo Semilla',
  'Museo interactivo de ciencia y tecnología para niños y familias.',
  'Av. Teófilo Borunda 12, Altavista, Chihuahua, Chih.', 28.637077, -106.084277);
SET @semilla = LAST_INSERT_ID();
INSERT INTO place (owner_id, name, description, address, latitude, longitude) VALUES
 (@org, 'Parque El Palomar',
  'Parque urbano en la loma de Altavista, con áreas verdes, juegos y vista a la ciudad.',
  'Colonia Altavista, Chihuahua, Chih.', 28.640635, -106.083104);
SET @palomar = LAST_INSERT_ID();
INSERT INTO place (owner_id, name, description, address, latitude, longitude) VALUES
 (@org, 'Teatro de los Héroes',
  'El teatro más grande de la ciudad: conciertos, obras y espectáculos.',
  'Av. División del Norte 2301, Altavista, Chihuahua, Chih.', 28.653202, -106.084768);
SET @heroes = LAST_INSERT_ID();
INSERT INTO place (owner_id, name, description, address, latitude, longitude) VALUES
 (@org, 'Grutas de Nombre de Dios',
  'Cuevas con estalactitas y estalagmitas al norte de la ciudad, con recorridos guiados.',
  'Grutas Nombre de Dios, 31146 Chihuahua, Chih.', 28.709313, -106.076996);
SET @grutas = LAST_INSERT_ID();
INSERT INTO place (owner_id, name, description, address, latitude, longitude) VALUES
 (@org, 'Catedral Metropolitana de Chihuahua',
  'Catedral barroca del siglo XVIII frente a la Plaza de Armas.',
  'Calle Libertad 200, Zona Centro, 31000 Chihuahua, Chih.', 28.635902, -106.077405);
SET @catedral = LAST_INSERT_ID();

INSERT INTO moderation (mod_id, place_id, decision) VALUES
 (@moderador, @casa, 'approved'), (@moderador, @quinta, 'approved'), (@moderador, @villa, 'approved'),
 (@moderador, @semilla, 'approved'), (@moderador, @palomar, 'approved'), (@moderador, @heroes, 'approved'),
 (@moderador, @grutas, 'approved'), (@moderador, @catedral, 'approved');
INSERT INTO place_tag (place_id, tag_id) VALUES
 (@casa, 10), (@quinta, 10), (@villa, 10), (@semilla, 10), (@palomar, 11), (@heroes, 9), (@grutas, 3);

-- ---------------------------------------------------------------------
-- Eventos (etiquetas: 1 Familiar, 2 Musica, 5 Gastronomia)
-- ---------------------------------------------------------------------
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, ticket_url, status) VALUES
 (@org, 'Cuulinaria 2026',
  'Octava edición del festival gastronómico de la ciudad: unos 80 expositores de restaurantes, vino, cerveza artesanal y sotol, catas, talleres y cuatro escenarios con música local. Abre a la 1:00 p. m. Entrada gratuita.',
  '2026-11-07 13:00', '2026-11-08 22:00',
  'Unión Ganadera Regional de Chihuahua (sede de ExpoGan), Labor de Terrazas, 31415 Chihuahua, Chih.', 28.602693, -106.118138,
  0, 0, NULL, 'pending');
SET @cuulinaria = LAST_INSERT_ID();
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, ticket_url, status) VALUES
 (@org, 'Manuel Medrano: Superior Tour',
  'Concierto de Manuel Medrano en la Arena Corner Sport. Puertas a las 7:00 p. m. Boletos desde $826.',
  '2026-11-21 21:00', '2026-11-21 23:59',
  'Arena Corner Sport, Av. Antonio de Montes 4503, Presidentes, 31210 Chihuahua, Chih.', 28.649343, -106.103102,
  826, 826, 'https://www.songkick.com/es/concerts/43315609-manuel-medrano-at-arena-corner-sport', 'pending');
SET @medrano = LAST_INSERT_ID();
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, ticket_url, status) VALUES
 (@org, 'El Cascanueces – Ballet Ruso Mari El',
  'El ballet clásico de Tchaikovsky por el Ballet Ruso Mari El. Función apta desde los 3 años. Boletos desde $860.',
  '2026-11-30 19:00', '2026-11-30 21:00',
  'Teatro de la Ciudad, Calle Ojinaga 106, Zona Centro, 31000 Chihuahua, Chih.', 28.630887, -106.079485,
  860, 860, 'https://ventas.donboleton.com/eventperformances.asp?evt=3845', 'pending');
SET @cascanueces = LAST_INSERT_ID();
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, ticket_url, status) VALUES
 (@org, 'Concierto navideño de la OSUACH en Vistas Cerro Grande',
  'La Orquesta Sinfónica de la UACH y el Coro Sinfónico interpretan obras de Tchaikovsky, Bach y Tormé, entre otras. Entrada gratuita: el boleto se reserva en la app UACH Móvil.',
  '2026-12-10 19:00', '2026-12-10 21:00',
  'Corredor Vistas Cerro Grande, Chihuahua, Chih.', 28.578928, -106.034515,
  0, 0, NULL, 'pending');
SET @osuach = LAST_INSERT_ID();
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, ticket_url, status) VALUES
 (@org, 'Revenge – Metallica Experience: Wrath of Man Tour 2026',
  'Tributo a Metallica en Pistoleros House of Shows.',
  '2026-12-11 21:00', '2026-12-11 23:59',
  'Pistoleros House of Shows, Calle Lucio Alvarado, Deportistas, 31125 Chihuahua, Chih.', 28.696413, -106.122601,
  250, 350, NULL, 'pending');
SET @revenge = LAST_INSERT_ID();
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, ticket_url, status) VALUES
 (@org, 'Calle 24 – Eterno Tour',
  'Concierto de Calle 24 en el Palenque de la Feria Santa Rita. Boletos desde $750.',
  '2026-12-12 21:00', '2026-12-12 23:59',
  'Palenque Feria Santa Rita, Ávalos, 31074 Chihuahua, Chih.', 28.620611, -106.012697,
  750, 750, 'https://arema.mx/e/21152', 'pending');
SET @calle24 = LAST_INSERT_ID();

INSERT INTO moderation (mod_id, event_id, decision) VALUES
 (@moderador, @cuulinaria, 'approved'), (@moderador, @medrano, 'approved'), (@moderador, @cascanueces, 'approved'),
 (@moderador, @osuach, 'approved'), (@moderador, @revenge, 'approved'), (@moderador, @calle24, 'approved');
INSERT INTO event_tag (event_id, tag_id) VALUES
 (@cuulinaria, 5), (@cuulinaria, 2), (@medrano, 2), (@cascanueces, 1), (@osuach, 2), (@osuach, 1),
 (@revenge, 2), (@calle24, 2);

COMMIT;
