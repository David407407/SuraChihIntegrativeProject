-- =====================================================================
-- SuraChih - Horarios oficiales de los lugares que los publican y la foto de las bebidas de
-- calabaza en la galería de Dandelion Coffee. Se puede correr más de una vez: borra y vuelve a
-- poner los horarios de esos lugares, y la foto solo se agrega si no está.
--
-- place_hours no está en el trigger de moderación: cambiar horarios no regresa el lugar a revisión.
-- Agregar la foto sí (trg_placeimg_ai): mod_ana vuelve a aprobar Dandelion.
-- day_of_week: 0 domingo ... 6 sábado.
--
-- Fuentes (consultadas el 3 nov 2026):
--   * Casa Chihuahua: Gobierno del Estado (abril 2026). Miércoles a lunes 9:00-17:00; martes cerrado.
--   * Museo Semilla: Gobierno del Estado (agosto 2026). Martes a domingo 11:00-18:00.
--   * Quinta Gameros: guías turísticas de la ciudad. Martes a domingo 11:00-19:00; lunes cerrado.
--   * Grutas de Nombre de Dios: DIF Municipal. Martes a domingo 10:00-16:00; lunes cerrado.
--   * Fashion Mall: Visita Chihuahua Capital. Todos los días 10:00-21:00.
-- Sin horario oficial o con fuentes que no coinciden (siguen sin horario): Museo Casa de Villa,
-- Catedral, Teatro de los Héroes, Presa El Rejón, Distrito Uno, Paseo Central, Farolito
-- Aventurero y Parque El Palomar. Los de Dandelion y Café de la Tercera son los de ejemplo.
-- =====================================================================

USE surachih;
SET NAMES utf8mb4;
START TRANSACTION;

SET @moderador = (SELECT id FROM user WHERE username = 'mod_ana');
SET @casa    = (SELECT id FROM place WHERE name = 'Casa Chihuahua Centro de Patrimonio Cultural');
SET @semilla = (SELECT id FROM place WHERE name = 'Museo Semilla');
SET @quinta  = (SELECT id FROM place WHERE name = 'Quinta Gameros');
SET @grutas  = (SELECT id FROM place WHERE name = 'Grutas de Nombre de Dios');
SET @fashion = (SELECT id FROM place WHERE name LIKE '%Fashion Mall%' LIMIT 1);

DELETE FROM place_hours WHERE place_id IN (@casa, @semilla, @quinta, @grutas, @fashion);

INSERT INTO place_hours (place_id, day_of_week, opens_at, closes_at) VALUES
 (@casa, 0, '09:00', '17:00'), (@casa, 1, '09:00', '17:00'), (@casa, 3, '09:00', '17:00'),
 (@casa, 4, '09:00', '17:00'), (@casa, 5, '09:00', '17:00'), (@casa, 6, '09:00', '17:00'),
 (@semilla, 0, '11:00', '18:00'), (@semilla, 2, '11:00', '18:00'), (@semilla, 3, '11:00', '18:00'),
 (@semilla, 4, '11:00', '18:00'), (@semilla, 5, '11:00', '18:00'), (@semilla, 6, '11:00', '18:00'),
 (@quinta, 0, '11:00', '19:00'), (@quinta, 2, '11:00', '19:00'), (@quinta, 3, '11:00', '19:00'),
 (@quinta, 4, '11:00', '19:00'), (@quinta, 5, '11:00', '19:00'), (@quinta, 6, '11:00', '19:00'),
 (@grutas, 0, '10:00', '16:00'), (@grutas, 2, '10:00', '16:00'), (@grutas, 3, '10:00', '16:00'),
 (@grutas, 4, '10:00', '16:00'), (@grutas, 5, '10:00', '16:00'), (@grutas, 6, '10:00', '16:00'),
 (@fashion, 0, '10:00', '21:00'), (@fashion, 1, '10:00', '21:00'), (@fashion, 2, '10:00', '21:00'),
 (@fashion, 3, '10:00', '21:00'), (@fashion, 4, '10:00', '21:00'), (@fashion, 5, '10:00', '21:00'),
 (@fashion, 6, '10:00', '21:00');

-- ---------------------------------------------------------------------
-- Bebidas de calabaza (del PDF "Locales") en la galería de Dandelion Coffee
-- ---------------------------------------------------------------------
SET @dandelion = (SELECT id FROM place WHERE name = 'Dandelion Coffee');
SET @calabazas = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793724080/surachih/ibmfs2fazhoiawee9lap.png';
SET @nueva = NOT EXISTS (SELECT 1 FROM place_image WHERE url = @calabazas);
SET @orden = (SELECT COALESCE(MAX(sort_order), 0) + 1 FROM place_image WHERE place_id = @dandelion);

INSERT INTO place_image (place_id, url, sort_order)
SELECT @dandelion, @calabazas, @orden FROM DUAL WHERE @nueva;
-- Solo si se agregó la foto (si no, el lugar sigue aprobado y no hace falta otra decisión)
INSERT INTO moderation (mod_id, place_id, decision, comment)
SELECT @moderador, @dandelion, 'approved', 'Foto de las bebidas de calabaza.' FROM DUAL WHERE @nueva;

COMMIT;
