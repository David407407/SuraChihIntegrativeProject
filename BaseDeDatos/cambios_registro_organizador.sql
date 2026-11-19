-- =====================================================================
-- SuraChih - Cambios para una BD que YA existe (creada con surachih_v1.sql antes de estos cambios)
--   1. Crear cuenta: nombre, apellidos y fecha de nacimiento.
--   2. Hazte organizador: RFC y videos del local.
-- Ejecutar UNA sola vez en MySQL Workbench. Si creas la BD desde cero con surachih_v1.sql, NO hace falta.
-- =====================================================================
SET NAMES utf8mb4;
USE surachih;

ALTER TABLE user
    ADD COLUMN first_name VARCHAR(60) NULL AFTER password_hash,
    ADD COLUMN last_name  VARCHAR(80) NULL AFTER first_name,
    ADD COLUMN birth_date DATE        NULL AFTER last_name;

ALTER TABLE organizer_profile
    ADD COLUMN rfc VARCHAR(13) NULL AFTER whatsapp,
    ADD CONSTRAINT ck_orgprof_rfc CHECK (rfc IS NULL OR REGEXP_LIKE(rfc, '^[A-Z&Ñ]{3,4}[0-9]{6}[A-Z0-9]{3}$', 'c'));

CREATE TABLE organizer_video (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id     INT UNSIGNED     NOT NULL,
    url         VARCHAR(500)     NOT NULL,
    position    TINYINT UNSIGNED NOT NULL DEFAULT 0,
    CONSTRAINT fk_orgvideo_profile FOREIGN KEY (user_id) REFERENCES organizer_profile(user_id) ON DELETE CASCADE,
    INDEX ix_orgvideo_user (user_id, position)
);
