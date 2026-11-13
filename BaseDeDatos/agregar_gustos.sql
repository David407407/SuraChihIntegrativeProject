-- =====================================================================
-- SuraChih - Gustos del usuario: las categorías (tag) que elige al crear su cuenta o en Mi perfil.
-- Con ellas el inicio ordena "Te podrían interesar" y arma "Recomendados para ti".
-- Ejecutar UNA sola vez en la BD compartida (no borra nada). surachih_v1.sql ya lo trae.
--
-- notify_interests: el usuario aceptó que le avisemos de eventos nuevos de sus gustos. Hoy nadie
-- manda avisos; queda guardado para la app de celular (ver la consulta de referencia al final).
-- =====================================================================

USE surachih;

CREATE TABLE user_interest (
    user_id     INT UNSIGNED      NOT NULL,
    tag_id      SMALLINT UNSIGNED NOT NULL,
    created_at  DATETIME          NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, tag_id),
    CONSTRAINT fk_interest_user FOREIGN KEY (user_id) REFERENCES user(id) ON DELETE CASCADE,
    CONSTRAINT fk_interest_tag  FOREIGN KEY (tag_id)  REFERENCES tag(id),
    INDEX ix_interest_tag (tag_id)
);

ALTER TABLE user
    ADD COLUMN notify_interests BOOLEAN NOT NULL DEFAULT FALSE AFTER is_active;

-- Consulta de referencia para los avisos (app de celular): eventos aprobados desde @desde que
-- tienen alguna etiqueta de los gustos de cada usuario que aceptó avisos.
--
-- SELECT u.id AS user_id, e.id AS event_id, e.title, COUNT(*) AS coincidencias
-- FROM user u
-- JOIN user_interest ui ON ui.user_id = u.id
-- JOIN event_tag et     ON et.tag_id = ui.tag_id
-- JOIN event e          ON e.id = et.event_id
-- WHERE u.is_active AND u.notify_interests
--   AND e.status = 'approved' AND e.end_at > NOW()
--   AND (SELECT MAX(m.decided_at) FROM moderation m WHERE m.event_id = e.id) >= @desde
-- GROUP BY u.id, e.id, e.title;
