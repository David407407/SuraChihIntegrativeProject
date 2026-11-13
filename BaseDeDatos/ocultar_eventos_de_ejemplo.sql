-- =====================================================================
-- SuraChih - Oculta en la BD compartida los eventos de ejemplo de surachih_v1.sql (foto genérica de
-- Cloudinary), para que la app muestre solo la cartelera real. No borra nada: favoritos, inscripciones,
-- reseñas, reportes e historial se conservan, y las pruebas que los leen por id siguen funcionando.
--   * Aprobados (Misión 404, Cata de café, Carrera nocturna 5K) -> cancelados con motivo: dejan de salir
--     en la Landing, el Inicio, la búsqueda y el mapa.
--   * En revisión (Taller de fotografía urbana) -> borrador: sale de la bandeja de moderación.
--   * Campamento del Terror ya estaba cancelado.
-- Ejecutar UNA sola vez en Aiven (en una transacción). surachih_v1.sql no cambia: en una BD local los
-- ejemplos siguen sirviendo para probar.
-- =====================================================================

USE surachih;
SET NAMES utf8mb4;
START TRANSACTION;

SET @misiones = (SELECT id FROM user WHERE username = 'misiones_cuu');
SET @dandelion = (SELECT id FROM user WHERE username = 'cafe_dande');
SET @motivo = 'Evento de ejemplo de los datos de prueba: se ocultó para dejar solo la cartelera real.';

UPDATE event
SET status = 'cancelled', cancel_reason = @motivo, is_featured = FALSE, featured_order = NULL
WHERE status = 'approved'
  AND ((organizer_id = @misiones AND title IN ('Misión 404 - Juego callejero guiado por app', 'Carrera nocturna 5K'))
    OR (organizer_id = @dandelion AND title = 'Cata de café de especialidad'));

UPDATE event
SET status = 'draft'
WHERE status = 'pending' AND organizer_id = @misiones AND title = 'Taller de fotografía urbana';

COMMIT;
