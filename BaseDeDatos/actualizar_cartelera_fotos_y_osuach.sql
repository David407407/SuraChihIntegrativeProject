-- =====================================================================
-- SuraChih - Fotos oficiales y datos de los carteles para la cartelera de noviembre-diciembre 2026
-- (agregar_cartelera_noviembre_2026.sql) y cuatro conciertos mas de la temporada de la OSUACH.
-- Ejecutar UNA sola vez, despues de agregar_cartelera_noviembre_2026.sql (todo va en una transaccion).
--
-- Las fotos ya estan en Cloudinary (preset del equipo). Cambiar foto, texto o boletos de un evento
-- aprobado lo regresa a moderacion (trg_event_bu, trg_eventimg_ai): al final mod_ana los vuelve a aprobar.
-- Fuente: el PDF "Eventos" del equipo (carteles oficiales) y la UACH (temporada de la OSUACH, entrada
-- gratuita con boleto en la app UACH Movil).
-- =====================================================================

USE surachih;
SET NAMES utf8mb4;
START TRANSACTION;

SET @org = (SELECT id FROM user WHERE username = 'surachih_cuu');
SET @moderador = (SELECT id FROM user WHERE username = 'mod_ana');

-- ---------------------------------------------------------------------
-- Fotos y datos de los carteles
-- ---------------------------------------------------------------------
SET @cuulinaria = (SELECT id FROM event WHERE organizer_id = @org AND title = 'Cuulinaria 2026');
UPDATE event SET
  description = 'Octava edición del festival gastronómico de la ciudad, 100 % familiar: cerca de 80 expositores de restaurantes, vino, sotol, cerveza artesanal y comida regional; catas, talleres, altar monumental de Día de Muertos y cuatro escenarios con música local. De 1:00 p. m. a 11:00 p. m. Entrada gratuita.',
  end_at = '2026-11-08 23:00',
  main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722652/surachih/hla0fjppihysmcfgxqqe.png'
WHERE id = @cuulinaria;
INSERT INTO event_image (event_id, url, sort_order) VALUES
 (@cuulinaria, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722653/surachih/t8quiboypzula7kika6q.png', 1);

SET @medrano = (SELECT id FROM event WHERE organizer_id = @org AND title = 'Manuel Medrano: Superior Tour');
UPDATE event SET
  ticket_url = 'https://boletomovil.com/evento/manuel-medrano-superior-tour-chihuahua-2026-11-21/recinto',
  main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722657/surachih/oghragulrtytfnj9bzpm.png'
WHERE id = @medrano;
INSERT INTO event_image (event_id, url, sort_order) VALUES
 (@medrano, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722658/surachih/obblnrtes34i6hye3sva.png', 1),
 (@medrano, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722660/surachih/r5pfxvzawp6kftp5owmw.png', 2);

SET @cascanueces = (SELECT id FROM event WHERE organizer_id = @org AND title = 'El Cascanueces – Ballet Ruso Mari El');
UPDATE event SET
  main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722662/surachih/imgxycvizlujfv8bfmbl.png'
WHERE id = @cascanueces;
INSERT INTO event_image (event_id, url, sort_order) VALUES
 (@cascanueces, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722663/surachih/ngmbqmduadeessxserlr.png', 1);

SET @osuach = (SELECT id FROM event WHERE organizer_id = @org AND title = 'Concierto navideño de la OSUACH en Vistas Cerro Grande');
UPDATE event SET
  main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722665/surachih/xdtyezndjb79inaitrd1.png'
WHERE id = @osuach;
INSERT INTO event_image (event_id, url, sort_order) VALUES
 (@osuach, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722666/surachih/apxmvprequzuxyufpczl.png', 1),
 (@osuach, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722668/surachih/oqpzqfmficzege3aum10.png', 2);

SET @revenge = (SELECT id FROM event WHERE organizer_id = @org AND title = 'Revenge – Metallica Experience: Wrath of Man Tour 2026');
UPDATE event SET
  description = 'Tributo a Metallica con su Wrath of Man Tour 2026. Bandas abridoras: Up the Irons (tributo a Iron Maiden) y Black Stones (tributo a AC/DC). Acceso 8:00 p. m.; bandas desde las 9:00 p. m. Preventa especial $250, VIP $350 (VIP por teléfono: 614 604 5493 o 614 606 7999).',
  ticket_url = 'https://reu.mx/eventos/revenge-metallica-experience-2',
  main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722669/surachih/iu7kugfyrdxob4gteggu.png'
WHERE id = @revenge;
INSERT INTO event_image (event_id, url, sort_order) VALUES
 (@revenge, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722670/surachih/oprkqgctobford6emlz6.png', 1);

SET @calle24 = (SELECT id FROM event WHERE organizer_id = @org AND title = 'Calle 24 – Eterno Tour');
UPDATE event SET
  description = 'Calle 24, el proyecto de corridos tumbados del chihuahuense Diego Millán, presenta en vivo su nuevo material ETERNO en el Palenque de la Feria Santa Rita. Boletos desde $750.',
  ticket_url = 'https://arema.mx/e/21152-calle-24-eterno-tour-en-chihuahua',
  main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722671/surachih/facud8ygfqitkxhhy3hm.png'
WHERE id = @calle24;
INSERT INTO event_image (event_id, url, sort_order) VALUES
 (@calle24, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722673/surachih/dovqiniisxxhnpkoqsq8.png', 1),
 (@calle24, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722674/surachih/pvsneece7edkncciyyix.png', 2);

-- ---------------------------------------------------------------------
-- Segunda temporada 2026 de la OSUACH (gratis con boleto en UACH Móvil). Fin: 2 h después del inicio.
-- El 26 nov (60 aniversario de la Compañía de Danza) aún no tiene sede: no se agrega.
-- ---------------------------------------------------------------------
SET @temporada = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722668/surachih/oqpzqfmficzege3aum10.png';
SET @paraninfo = 'Paraninfo Universitario UACH, Paseo Simón Bolívar, Zona Centro, 31000 Chihuahua, Chih.';

INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, main_image_url, status) VALUES
 (@org, 'OSUACH: Diálogos musicales centenarios – Barroco, clásico y romántico',
  'Concierto de la Orquesta Sinfónica de la UACH, dirigida por David Pérez Olmedo. Entrada gratuita: el boleto se reserva en la app UACH Móvil.',
  '2026-11-05 19:00', '2026-11-05 21:00', @paraninfo, 28.638199, -106.072285, 0, 0, @temporada, 'pending');
SET @barroco = LAST_INSERT_ID();
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, main_image_url, status) VALUES
 (@org, 'OSUACH: Concierto del XLVIII Encuentro Nacional de Estudiantes de Historia',
  'La OSUACH en concierto con la Facultad de Filosofía y Letras de la UACH. Entrada gratuita: el boleto se reserva en la app UACH Móvil.',
  '2026-11-13 15:45', '2026-11-13 17:45', @paraninfo, 28.638199, -106.072285, 0, 0, @temporada, 'pending');
SET @historia = LAST_INSERT_ID();
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, main_image_url, status) VALUES
 (@org, 'OSUACH: La última y nos vamos',
  'Último concierto de temporada de la Orquesta Sinfónica de la UACH en el Paraninfo. Entrada gratuita: el boleto se reserva en la app UACH Móvil.',
  '2026-12-03 19:00', '2026-12-03 21:00', @paraninfo, 28.638199, -106.072285, 0, 0, @temporada, 'pending');
SET @ultima = LAST_INSERT_ID();
INSERT INTO event (organizer_id, place_id, title, description, start_at, end_at,
                   price_min, price_max, main_image_url, status) VALUES
 (@org, (SELECT id FROM place WHERE name = 'Quinta Gameros' AND owner_id = @org), 'Encendido del árbol navideño en Quinta Gameros',
  'Cierre de la temporada de la OSUACH con el tradicional encendido del árbol navideño del Centro Cultural Universitario Quinta Gameros. Entrada gratuita.',
  '2026-12-17 19:00', '2026-12-17 21:00', 0, 0, @temporada, 'pending');
SET @arbol = LAST_INSERT_ID();

INSERT INTO event_tag (event_id, tag_id) VALUES (@barroco, 2), (@historia, 2), (@ultima, 2), (@arbol, 2), (@arbol, 1);

-- ---------------------------------------------------------------------
-- Aprobación: los editados regresaron a 'pending' por los triggers; los nuevos entran así
-- ---------------------------------------------------------------------
-- (Con VALUES y no INSERT ... SELECT FROM event: el trigger de moderation actualiza event y MySQL no
-- permite que lo haga dentro de una sentencia que lee esa misma tabla.)
SET @nota = 'Fotos y datos del cartel oficial.';
INSERT INTO moderation (mod_id, event_id, decision, comment) VALUES
 (@moderador, @cuulinaria, 'approved', @nota), (@moderador, @medrano, 'approved', @nota),
 (@moderador, @cascanueces, 'approved', @nota), (@moderador, @osuach, 'approved', @nota),
 (@moderador, @revenge, 'approved', @nota), (@moderador, @calle24, 'approved', @nota),
 (@moderador, @barroco, 'approved', @nota), (@moderador, @historia, 'approved', @nota),
 (@moderador, @ultima, 'approved', @nota), (@moderador, @arbol, 'approved', @nota);

COMMIT;
