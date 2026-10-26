-- =====================================================================
-- SuraChih - Eventos y lugares reales de Chihuahua (octubre-noviembre 2026), con su foto
-- principal en Cloudinary. Para quien ya cargo surachih_v1.sql: NO borra nada.
-- (Quien cargue surachih_v1.sql desde cero ya los tiene; no necesita esto.)
-- Ejecutar UNA sola vez en MySQL Workbench con la base surachih.
--
-- Los publica la cuenta organizadora "surachih_cuu" (contraseña de prueba Test1234!) y los
-- aprueba el moderador 1, igual que los datos de prueba.
-- Coordenadas aproximadas (sin direccion exacta): ExpoGan (colonia Labor de Terrazas), Armonico
-- (calle 17 y Av. Venustiano Carranza, junto a la Sagrada Familia) y El Camino Mezcaleria
-- (calle Guadalupe Victoria, Centro).
-- La hora de fin de los conciertos no venia en la fuente: se puso 3 horas despues del inicio
-- (o las 23:59, para que no se vean como de dos dias).
-- =====================================================================

USE surachih;
SET NAMES utf8mb4;

-- Cuenta organizadora del equipo
INSERT INTO user (username, email, password_hash) VALUES
 ('surachih_cuu', 'eventos@surachih.example.com', '$2b$11$C4rR0iUKQKbnm.V0sIsKHeyxIUqVR7g8aqJbWo48UbbeVbh/PeDsC');
SET @org = LAST_INSERT_ID();
INSERT INTO organizer_profile (user_id, business_name, description, social_url, whatsapp) VALUES
 (@org, 'SuraChih Chihuahua', 'Cartelera de eventos y lugares de la ciudad que publica el equipo de SuraChih.',
  'https://instagram.com/ejemplo_surachih', '526141110004');
INSERT INTO moderation (mod_id, organizer_id, decision, comment) VALUES (1, @org, 'approved', 'Cuenta del equipo.');

-- ---------------------------------------------------------------------
-- Lugares
-- ---------------------------------------------------------------------
INSERT INTO place (owner_id, name, description, address, latitude, longitude, website_url, main_image_url) VALUES
 (@org, 'Presa El Rejón', 'Presa con andador, mirador y áreas verdes para caminar, correr o andar en bici.',
  'Mirador, Campestre-Lomas, Chihuahua, Chih.', 28.613653, -106.129740, 'https://trespresas.com.mx',
  'https://res.cloudinary.com/djzpogkar/image/upload/v1792779406/surachih/mkrvk5msnzv4akm1y9dg.png');
SET @rejon = LAST_INSERT_ID();
INSERT INTO place (owner_id, name, description, address, latitude, longitude, website_url, main_image_url) VALUES
 (@org, 'Distrito Uno', 'Centro comercial y de entretenimiento con tiendas, restaurantes y cine.',
  'Perif. de la Juventud 5700, Distrito Uno, 31110 Chihuahua, Chih.', 28.661744, -106.127624, 'https://distritouno.com.mx/directorio',
  'https://res.cloudinary.com/djzpogkar/image/upload/v1792779483/surachih/ptbbytl5accdzfmen1nd.jpg');
SET @distrito = LAST_INSERT_ID();
INSERT INTO place (owner_id, name, description, address, latitude, longitude, website_url, main_image_url) VALUES
 (@org, 'Paseo Central', 'Centro comercial al aire libre con restaurantes, cafés y tiendas.',
  'Perif. de la Juventud, Haciendas, 31216 Chihuahua, Chih.', 28.630699, -106.121085, 'https://www.paseocentral.com.mx',
  'https://res.cloudinary.com/djzpogkar/image/upload/v1792779613/surachih/tloqsdqkrldoen4a7r5f.jpg');
SET @paseo = LAST_INSERT_ID();
INSERT INTO place (owner_id, name, description, address, latitude, longitude, website_url, main_image_url) VALUES
 (@org, 'Fashion Mall Chihuahua', 'Centro comercial con tiendas departamentales, restaurantes y cine.',
  'Perif. de la Juventud 3501, Puerta de Hierro, 31410 Chihuahua, Chih.', 28.623093, -106.115384, NULL,
  'https://res.cloudinary.com/djzpogkar/image/upload/v1792779781/surachih/k5mqxhbpv3r3vtdycrjz.jpg');
SET @fashion = LAST_INSERT_ID();
INSERT INTO place (owner_id, name, description, address, latitude, longitude, website_url, main_image_url) VALUES
 (@org, 'Farolito Aventurero', 'Cafetería de barrio.',
  'C. María Luisa 503, Jardines del Santuario, 31206 Chihuahua, Chih.', 28.628826, -106.087246,
  'https://www.facebook.com/FarolitoCafeAventurero/',
  'https://res.cloudinary.com/djzpogkar/image/upload/v1792779866/surachih/xiyn7vjveqlhgpyk1dk6.jpg');
SET @farolito = LAST_INSERT_ID();

INSERT INTO moderation (mod_id, place_id, decision) VALUES
 (1, @rejon, 'approved'), (1, @distrito, 'approved'), (1, @paseo, 'approved'),
 (1, @fashion, 'approved'), (1, @farolito, 'approved');
-- Etiquetas: 11 Parque, 3 Deportes y aire libre, 6 Cafetería
INSERT INTO place_tag (place_id, tag_id) VALUES (@rejon, 11), (@rejon, 3), (@farolito, 6);

-- ---------------------------------------------------------------------
-- Eventos (con su propia ubicación; ninguno está ligado a un lugar de la app)
-- ---------------------------------------------------------------------
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, ticket_url, main_image_url, status) VALUES
 (@org, 'Vi el brillo del Televisor', 'Presentación en Casa Cultural Los Laureles. Entrada libre.',
  '2026-10-23 19:00', '2026-10-23 21:00',
  'Casa Cultural Los Laureles, Avenida Universidad S/N, Altavista, 31200 Chihuahua, Chih.', 28.649608, -106.083991,
  0, 0, NULL,
  'https://res.cloudinary.com/djzpogkar/image/upload/v1792764536/surachih/jszxp6hkyruxxsjsy0tt.avif', 'pending');
SET @brillo = LAST_INSERT_ID();
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, ticket_url, main_image_url, status) VALUES
 (@org, 'Ricardo Arjona: Lo que el Seco no dijo', 'Concierto de Ricardo Arjona. Apertura de puertas: 19:00 h.',
  '2026-11-23 21:00', '2026-11-23 23:59',
  'Estadio Monumental, Calle Guadalupe Victoria, Ávalos, 31074 Chihuahua, Chih.', 28.625937, -106.022188,
  1060, 1060, 'https://arjona.funticket.mx/onsale/es/comprarEvento?idEvento=405',
  'https://res.cloudinary.com/djzpogkar/image/upload/v1792764621/surachih/aeadshq1us08exklr48v.jpg', 'pending');
SET @arjona = LAST_INSERT_ID();
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, ticket_url, main_image_url, status) VALUES
 (@org, 'Los Tucanes de Tijuana', 'Concierto de Los Tucanes de Tijuana en ExpoGan Chihuahua.',
  '2026-10-23 21:00', '2026-10-23 23:59',
  'ExpoGan Chihuahua, Labor de Terrazas, 31415 Chihuahua, Chih.', 28.602693, -106.118138,
  1170, 1170, 'https://www.bandsintown.com/es/e/108802724-los-tucanes-de-tijuana-at-expogan-chihuahua',
  'https://res.cloudinary.com/djzpogkar/image/upload/v1792764691/surachih/cqydnmtgsycuvpiz2pyi.webp', 'pending');
SET @tucanes = LAST_INSERT_ID();
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, ticket_url, main_image_url, status) VALUES
 (@org, 'Juana Aguirre', 'Concierto de Juana Aguirre en Armónico.',
  '2026-10-24 20:00', '2026-10-24 23:00',
  'Armónico, Calle 17 y Av. Venustiano Carranza, Santo Niño, Chihuahua, Chih.', 28.637000, -106.069200,
  1060, 1060, 'https://www.bandsintown.com/es/e/108718515-juana-aguirre-at-armonico',
  'https://res.cloudinary.com/djzpogkar/image/upload/v1792779035/surachih/m6j9dvob448ioywv8net.webp', 'pending');
SET @juana = LAST_INSERT_ID();
INSERT INTO event (organizer_id, title, description, start_at, end_at, address, latitude, longitude,
                   price_min, price_max, ticket_url, main_image_url, status) VALUES
 (@org, 'Flores Salvajes', 'Concierto de Flores Salvajes en El Camino Mezcalería.',
  '2026-10-24 20:00', '2026-10-24 23:00',
  'El Camino Mezcalería, Calle Guadalupe Victoria 823, Zona Centro, 31000 Chihuahua, Chih.', 28.632000, -106.078000,
  1060, 1060, 'https://www.bandsintown.com/es/e/109030162-flores-salvajes-at-el-camino-mezcaleria',
  'https://res.cloudinary.com/djzpogkar/image/upload/v1792779124/surachih/ysa3xaju4sfturxvta2c.webp', 'pending');
SET @flores = LAST_INSERT_ID();

INSERT INTO moderation (mod_id, event_id, decision) VALUES
 (1, @brillo, 'approved'), (1, @arjona, 'approved'), (1, @tucanes, 'approved'),
 (1, @juana, 'approved'), (1, @flores, 'approved');
-- Etiqueta 2 Música en los conciertos
INSERT INTO event_tag (event_id, tag_id) VALUES (@arjona, 2), (@tucanes, 2), (@juana, 2), (@flores, 2);
