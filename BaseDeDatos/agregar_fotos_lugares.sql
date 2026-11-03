-- =====================================================================
-- SuraChih - Fotos de los lugares (PDF "Locales" del equipo, ya en Cloudinary): portada y galería
-- de los 8 lugares de agregar_cartelera_noviembre_2026.sql y de Dandelion Coffee y Café de la
-- Tercera (que tenían la foto de ejemplo de Cloudinary).
-- Ejecutar UNA sola vez (todo va en una transacción). Cambiar la portada o agregar fotos regresa el
-- lugar a moderación (trg_place_bu, trg_placeimg_ai): al final mod_ana los vuelve a aprobar.
-- =====================================================================

USE surachih;
SET NAMES utf8mb4;
START TRANSACTION;

SET @moderador = (SELECT id FROM user WHERE username = 'mod_ana');

-- Casa Chihuahua Centro de Patrimonio Cultural
SET @lugar1 = (SELECT id FROM place WHERE name = 'Casa Chihuahua Centro de Patrimonio Cultural');
UPDATE place SET main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723884/surachih/gjq78ig0lcasc2uvh4er.png' WHERE id = @lugar1;
INSERT INTO place_image (place_id, url, sort_order) VALUES
 (@lugar1, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723887/surachih/j7mlioilzldmvp7ouewy.png', 1),
 (@lugar1, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723890/surachih/xdhdoac4vny1ampshkbq.png', 2),
 (@lugar1, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723893/surachih/u4zchtelmkljkr7qrlou.png', 3);

-- Quinta Gameros
SET @lugar2 = (SELECT id FROM place WHERE name = 'Quinta Gameros');
UPDATE place SET main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723897/surachih/xqyylggf0sg0lbvjjcti.png' WHERE id = @lugar2;
INSERT INTO place_image (place_id, url, sort_order) VALUES
 (@lugar2, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723899/surachih/wkmjylpmhttvtcuhueoj.png', 1),
 (@lugar2, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723901/surachih/y0jumitmrfcgvni9uyvd.png', 2),
 (@lugar2, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723903/surachih/ghnmbarwzvruwutlubsa.png', 3);

-- Museo Casa de Villa
SET @lugar3 = (SELECT id FROM place WHERE name = 'Museo Casa de Villa');
UPDATE place SET main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723904/surachih/is9dw3iashfvmdgeaz7r.png' WHERE id = @lugar3;
INSERT INTO place_image (place_id, url, sort_order) VALUES
 (@lugar3, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723905/surachih/ikznycd4u2z0ir3zstag.png', 1),
 (@lugar3, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723906/surachih/xxab7opbqft0vunywrgz.png', 2),
 (@lugar3, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723908/surachih/kbk2ad54xhbspui30e8w.png', 3);

-- Museo Semilla
SET @lugar4 = (SELECT id FROM place WHERE name = 'Museo Semilla');
UPDATE place SET main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723909/surachih/axebis01nfj3n3frnetn.png' WHERE id = @lugar4;
INSERT INTO place_image (place_id, url, sort_order) VALUES
 (@lugar4, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723910/surachih/zcij3nuemki7m3tj75ob.png', 1),
 (@lugar4, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723912/surachih/gpkjkg6ebehmlurlzgxe.png', 2);

-- Parque El Palomar
SET @lugar5 = (SELECT id FROM place WHERE name = 'Parque El Palomar');
UPDATE place SET main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723914/surachih/qscqgbwq27angwksreol.png' WHERE id = @lugar5;
INSERT INTO place_image (place_id, url, sort_order) VALUES
 (@lugar5, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723915/surachih/iao38nunttb1w3j5henw.png', 1),
 (@lugar5, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723917/surachih/kfqdxzgcmasii0j6aggq.png', 2);

-- Teatro de los Héroes
SET @lugar6 = (SELECT id FROM place WHERE name = 'Teatro de los Héroes');
UPDATE place SET main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723918/surachih/fiusv2pi0my5l8ey6bib.png' WHERE id = @lugar6;
INSERT INTO place_image (place_id, url, sort_order) VALUES
 (@lugar6, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723920/surachih/qubd8ihyuciftaqzm3zc.png', 1),
 (@lugar6, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723921/surachih/uc3xqrpivwyriq4vkuqg.png', 2),
 (@lugar6, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723923/surachih/opnqtsenpcnheeianr46.png', 3);

-- Grutas de Nombre de Dios
SET @lugar7 = (SELECT id FROM place WHERE name = 'Grutas de Nombre de Dios');
UPDATE place SET main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723924/surachih/mgmdj7uek3det6zti2hk.png' WHERE id = @lugar7;
INSERT INTO place_image (place_id, url, sort_order) VALUES
 (@lugar7, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723926/surachih/ihqmiu5t34s1mrubio5w.png', 1),
 (@lugar7, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723927/surachih/e4s3wauxgi24zwxphnq6.png', 2),
 (@lugar7, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723929/surachih/kzl6ns5dxzvoytbmreeb.png', 3);

-- Catedral Metropolitana de Chihuahua
SET @lugar8 = (SELECT id FROM place WHERE name = 'Catedral Metropolitana de Chihuahua');
UPDATE place SET main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723930/surachih/ffdmo1xmhbgo2cefbxj6.png' WHERE id = @lugar8;
INSERT INTO place_image (place_id, url, sort_order) VALUES
 (@lugar8, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723931/surachih/wzwm7spav6cnqede7qld.png', 1),
 (@lugar8, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723933/surachih/i9hvp1tlgubffl7n5ypq.png', 2),
 (@lugar8, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723934/surachih/s1at3ovljm9ocwafq1nz.png', 3);

-- Dandelion Coffee
SET @lugar9 = (SELECT id FROM place WHERE name = 'Dandelion Coffee');
UPDATE place SET main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723936/surachih/baq8sfzw4dhzfphlirjq.png' WHERE id = @lugar9;
INSERT INTO place_image (place_id, url, sort_order) VALUES
 (@lugar9, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723937/surachih/sw0y2wbt5oefd2n9qodo.png', 1),
 (@lugar9, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723938/surachih/budzb0dtsjjxlqwt3won.png', 2),
 (@lugar9, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723939/surachih/im3x6rzfbmjqllu3okrg.png', 3),
 (@lugar9, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723940/surachih/hkvztet0c1acbkq1yh8f.png', 4);

-- Café de la Tercera
SET @lugar10 = (SELECT id FROM place WHERE name = 'Café de la Tercera');
UPDATE place SET main_image_url = 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723941/surachih/shar3klv696k90jgvbq4.png' WHERE id = @lugar10;
INSERT INTO place_image (place_id, url, sort_order) VALUES
 (@lugar10, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723943/surachih/pjuumhpmgkoeltuotmg0.png', 1),
 (@lugar10, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723944/surachih/c3xzugfzbjwm4wdmuqci.png', 2),
 (@lugar10, 'https://res.cloudinary.com/djzpogkar/image/upload/v1793723945/surachih/mcuulastv4z6modti4ug.png', 3);

-- Aprobación (con VALUES: el trigger de moderation actualiza place)
SET @nota = 'Fotos del lugar.';
INSERT INTO moderation (mod_id, place_id, decision, comment) VALUES
 (@moderador, @lugar1, 'approved', @nota),
 (@moderador, @lugar2, 'approved', @nota),
 (@moderador, @lugar3, 'approved', @nota),
 (@moderador, @lugar4, 'approved', @nota),
 (@moderador, @lugar5, 'approved', @nota),
 (@moderador, @lugar6, 'approved', @nota),
 (@moderador, @lugar7, 'approved', @nota),
 (@moderador, @lugar8, 'approved', @nota),
 (@moderador, @lugar9, 'approved', @nota),
 (@moderador, @lugar10, 'approved', @nota);

COMMIT;
