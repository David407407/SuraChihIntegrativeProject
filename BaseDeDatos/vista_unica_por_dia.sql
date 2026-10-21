-- =====================================================================
-- SuraChih - Una vista por usuario por evento por dia, garantizada por la BD.
-- Para quien ya cargo surachih_v1.sql antes de este cambio: NO borra eventos ni usuarios.
-- (Quien cargue surachih_v1.sql desde cero ya lo tiene; no necesita esto.)
-- Ejecutar en MySQL Workbench con la base surachih.
--
-- Antes la regla solo la revisaba la app; dos aperturas del detalle al mismo tiempo podian
-- contar doble. Ahora la llave UNIQUE uq_view_user_day lo impide. Los visitantes
-- (user_id NULL) siguen contando cada vez.
-- =====================================================================

USE surachih;

-- 1) Quita las vistas repetidas que ya existan (deja la primera de cada usuario por dia).
DELETE v FROM event_view v
JOIN event_view primera
  ON primera.event_id = v.event_id
 AND primera.user_id = v.user_id
 AND DATE(primera.viewed_at) = DATE(v.viewed_at)
 AND primera.id < v.id;

-- 2) Columna con el dia de la vista (para las filas que ya existen) y la llave unica.
--    No es columna generada: MySQL la calcularia antes del DEFAULT CURRENT_TIMESTAMP de viewed_at.
ALTER TABLE event_view ADD COLUMN view_date DATE NOT NULL DEFAULT (CURRENT_DATE) AFTER viewed_at;
UPDATE event_view SET view_date = DATE(viewed_at);
ALTER TABLE event_view ADD CONSTRAINT uq_view_user_day UNIQUE (event_id, user_id, view_date);

-- 3) Las vistas nuevas toman el dia de viewed_at.
DROP TRIGGER IF EXISTS trg_view_bi;
DELIMITER $$
CREATE TRIGGER trg_view_bi BEFORE INSERT ON event_view
FOR EACH ROW
BEGIN
    SET NEW.view_date = DATE(NEW.viewed_at);
END$$
DELIMITER ;
