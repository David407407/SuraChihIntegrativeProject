-- =====================================================================
-- SuraChih - Agrega el rol de supermoderador a una BD ya creada.
-- Para quien ya cargo surachih_v1.sql antes de este cambio: NO borra nada.
-- (Quien cargue surachih_v1.sql desde cero ya lo tiene; no necesita esto.)
-- Ejecutar en MySQL Workbench UNA SOLA VEZ y ANTES de usar la version nueva de
-- la app: sin esta columna nadie puede iniciar sesion.
-- =====================================================================

USE surachih;

-- Un supermoderador siempre es moderador (lo revisa ck_user_super_mod).
ALTER TABLE user
    ADD COLUMN is_super_mod BOOLEAN NOT NULL DEFAULT FALSE AFTER is_mod,
    ADD CONSTRAINT ck_user_super_mod CHECK (NOT is_super_mod OR is_mod);

-- El primer supermoderador. Cambia 'mod_ana' por el usuario que el equipo elija.
UPDATE user SET is_mod = TRUE, is_super_mod = TRUE WHERE username = 'mod_ana';

-- Debe salir una fila con is_mod = 1 e is_super_mod = 1.
SELECT id, username, is_mod, is_super_mod FROM user WHERE is_super_mod;
