-- =====================================================================
-- SuraChih - Las reseñas de cuentas eliminadas (user.is_active = FALSE) ya no se
-- muestran ni cuentan en las calificaciones. Para quien ya cargo surachih_v1.sql
-- antes de este cambio: solo reemplaza dos vistas, NO borra nada.
-- (Quien cargue surachih_v1.sql desde cero ya lo tiene; no necesita esto.)
-- Ejecutar en MySQL Workbench con la base surachih.
-- =====================================================================

USE surachih;

-- Panel del organizador: embudo y calificacion por evento.
CREATE OR REPLACE VIEW v_event_stats AS
SELECT e.id AS event_id, e.organizer_id,
       (SELECT COUNT(*) FROM event_view v  WHERE v.event_id = e.id)                         AS views,
       (SELECT COUNT(*) FROM favorite f    WHERE f.event_id = e.id)                         AS interested,
       (SELECT COUNT(*) FROM inscription i WHERE i.event_id = e.id AND i.status = 'active') AS inscribed,
       (SELECT ROUND(AVG(r.rating), 1) FROM event_review r JOIN user u ON u.id = r.user_id
         WHERE r.event_id = e.id AND NOT r.is_hidden AND u.is_active)                      AS avg_rating,
       (SELECT COUNT(*) FROM event_review r JOIN user u ON u.id = r.user_id
         WHERE r.event_id = e.id AND NOT r.is_hidden AND u.is_active)                      AS review_count
FROM event e;

-- Calificacion de lugares (para la tarjeta "4.9 ★").
CREATE OR REPLACE VIEW v_place_rating AS
SELECT p.id AS place_id,
       ROUND(AVG(r.rating), 1) AS avg_rating,
       COUNT(r.id)             AS review_count
FROM place p
LEFT JOIN (place_review r JOIN user u ON u.id = r.user_id AND u.is_active)
       ON r.place_id = p.id AND NOT r.is_hidden
GROUP BY p.id;
