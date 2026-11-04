-- =====================================================================
-- SuraChih - Agrega el historial de reseñas ocultas a una BD ya creada.
-- Para quien ya cargo surachih_v1.sql antes de este cambio: NO borra nada.
-- (Quien cargue surachih_v1.sql desde cero ya lo tiene; no necesita esto.)
-- Ejecutar UNA SOLA VEZ y ANTES de usar la version nueva de la app: sin esta
-- tabla no se puede ocultar una reseña.
-- =====================================================================

USE surachih;

-- Historial de reseñas ocultadas o vueltas a mostrar. moderation solo cubre eventos, lugares y organizadores.
-- Lo lee únicamente un supermoderador (lo revisa BackendLogica). Exactamente una reseña por fila.
CREATE TABLE review_moderation (
    id               INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    mod_id           INT UNSIGNED NOT NULL,
    event_review_id  INT UNSIGNED NULL,
    place_review_id  INT UNSIGNED NULL,
    action           ENUM('hidden','shown') NOT NULL,
    decided_at       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_revmod_mod   FOREIGN KEY (mod_id)          REFERENCES user(id),
    CONSTRAINT fk_revmod_evrev FOREIGN KEY (event_review_id) REFERENCES event_review(id),
    CONSTRAINT fk_revmod_plrev FOREIGN KEY (place_review_id) REFERENCES place_review(id),
    CONSTRAINT ck_revmod_one_target CHECK ((event_review_id IS NOT NULL) + (place_review_id IS NOT NULL) = 1),
    INDEX ix_revmod_decided (decided_at)
);

-- Debe salir la tabla vacia, con 0 filas.
SELECT COUNT(*) AS filas FROM review_moderation;
