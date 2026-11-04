-- =====================================================================
-- SuraChih - Una persona, un reporte por objetivo, y nadie reporta lo suyo.
-- Para quien ya cargo surachih_v1.sql antes de este cambio.
-- (Quien cargue surachih_v1.sql desde cero ya lo tiene; no necesita esto.)
-- Ejecutar en MySQL Workbench UNA SOLA VEZ. La app funciona antes y despues,
-- pero hasta correrlo el servidor no rechaza repetidos ni reportes de lo propio.
--
-- ATENCION: BORRA los reportes repetidos de una misma persona sobre un mismo
-- objetivo. Se queda con uno: el abierto si hay, y si no el mas antiguo. Los
-- detalles escritos en los repetidos se pierden. Revisa antes lo que va a borrar:
-- =====================================================================

USE surachih;

-- Las filas que se van a borrar. Si no sale ninguna, no se borra nada.
SELECT id, reporter_id, event_id, place_id, event_review_id, place_review_id, reason, details, status, created_at
FROM (
    SELECT r.*, ROW_NUMBER() OVER (
        PARTITION BY reporter_id, event_id, place_id, event_review_id, place_review_id
        ORDER BY status = 'open' DESC, created_at, id) AS orden
    FROM report r
) repetidos
WHERE orden > 1;

DELETE FROM report WHERE id IN (
    SELECT id FROM (
        SELECT id, ROW_NUMBER() OVER (
            PARTITION BY reporter_id, event_id, place_id, event_review_id, place_review_id
            ORDER BY status = 'open' DESC, created_at, id) AS orden
        FROM report
    ) repetidos
    WHERE orden > 1
);

-- COALESCE porque en una llave UNIQUE los NULL no chocan.
ALTER TABLE report
    ADD UNIQUE KEY uq_rep_once (reporter_id, (COALESCE(event_id, 0)), (COALESCE(place_id, 0)),
                                (COALESCE(event_review_id, 0)), (COALESCE(place_review_id, 0)));

DELIMITER $$

-- Reporte: nadie reporta su propio evento, lugar o resena.
CREATE TRIGGER trg_report_bi BEFORE INSERT ON report
FOR EACH ROW
BEGIN
    IF EXISTS (SELECT 1 FROM event WHERE id = NEW.event_id AND organizer_id = NEW.reporter_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No puedes reportar tu propio evento.';
    END IF;
    IF EXISTS (SELECT 1 FROM place WHERE id = NEW.place_id AND owner_id = NEW.reporter_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No puedes reportar tu propio lugar.';
    END IF;
    IF EXISTS (SELECT 1 FROM event_review WHERE id = NEW.event_review_id AND user_id = NEW.reporter_id)
       OR EXISTS (SELECT 1 FROM place_review WHERE id = NEW.place_review_id AND user_id = NEW.reporter_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No puedes reportar algo que escribiste.';
    END IF;
END$$

DELIMITER ;

-- Debe salir 0.
SELECT COUNT(*) AS repetidos FROM (
    SELECT 1 FROM report
    GROUP BY reporter_id, event_id, place_id, event_review_id, place_review_id
    HAVING COUNT(*) > 1
) r;
