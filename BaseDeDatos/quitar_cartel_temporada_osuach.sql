-- =====================================================================
-- SuraChih - Quita el cartel de horarios de la temporada de la OSUACH (portada de los 4 conciertos
-- nuevos y galeria del concierto navideño). En su lugar van las fotos de la orquesta.
-- Ejecutar UNA sola vez, despues de actualizar_cartelera_fotos_y_osuach.sql (en una transaccion).
-- Cambiar la portada regresa el evento a moderacion (trg_event_bu): mod_ana los vuelve a aprobar.
-- =====================================================================

USE surachih;
SET NAMES utf8mb4;
START TRANSACTION;

SET @org = (SELECT id FROM user WHERE username = 'surachih_cuu');
SET @moderador = (SELECT id FROM user WHERE username = 'mod_ana');
SET @cartel = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722668/surachih/oqpzqfmficzege3aum10.png';
SET @orquesta = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722665/surachih/xdtyezndjb79inaitrd1.png';
SET @solista = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793722666/surachih/apxmvprequzuxyufpczl.png';

SET @barroco  = (SELECT id FROM event WHERE organizer_id = @org AND title = 'OSUACH: Diálogos musicales centenarios – Barroco, clásico y romántico');
SET @historia = (SELECT id FROM event WHERE organizer_id = @org AND title = 'OSUACH: Concierto del XLVIII Encuentro Nacional de Estudiantes de Historia');
SET @ultima   = (SELECT id FROM event WHERE organizer_id = @org AND title = 'OSUACH: La última y nos vamos');
SET @arbol    = (SELECT id FROM event WHERE organizer_id = @org AND title = 'Encendido del árbol navideño en Quinta Gameros');

UPDATE event SET main_image_url = @solista  WHERE id IN (@barroco, @ultima)   AND main_image_url = @cartel;
UPDATE event SET main_image_url = @orquesta WHERE id IN (@historia, @arbol)   AND main_image_url = @cartel;
DELETE FROM event_image WHERE url = @cartel;

SET @nota = 'Portada: foto de la orquesta en lugar del cartel de horarios.';
INSERT INTO moderation (mod_id, event_id, decision, comment) VALUES
 (@moderador, @barroco, 'approved', @nota), (@moderador, @historia, 'approved', @nota),
 (@moderador, @ultima, 'approved', @nota), (@moderador, @arbol, 'approved', @nota);

COMMIT;
