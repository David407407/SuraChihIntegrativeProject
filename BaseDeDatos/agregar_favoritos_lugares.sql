-- =====================================================================
-- SuraChih - Agrega la tabla de favoritos de lugares a una BD ya creada.
-- Para quien ya cargo surachih_v1.sql antes de este cambio: NO borra nada.
-- (Quien cargue surachih_v1.sql desde cero ya la tiene; no necesita esto.)
-- Ejecutar en MySQL Workbench con la base surachih.
-- =====================================================================

USE surachih;

CREATE TABLE IF NOT EXISTS place_favorite (
    user_id     INT UNSIGNED NOT NULL,
    place_id    INT UNSIGNED NOT NULL,
    created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, place_id),
    CONSTRAINT fk_plfav_user  FOREIGN KEY (user_id)  REFERENCES user(id)  ON DELETE CASCADE,
    CONSTRAINT fk_plfav_place FOREIGN KEY (place_id) REFERENCES place(id) ON DELETE CASCADE,
    INDEX ix_plfav_place (place_id)
);

-- Mismo dato de prueba que surachih_v1.sql (mariana_r guarda Dandelion Coffee).
INSERT IGNORE INTO place_favorite (user_id, place_id) VALUES (5,1);
