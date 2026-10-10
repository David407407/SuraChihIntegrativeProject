-- =====================================================================
-- SuraChih - Base de datos v1
-- Motor: MySQL 8.0.16+ (los CHECK no se aplican en versiones anteriores)
-- Ejecutar completo en MySQL Workbench o en la consola `mysql`.
-- (Los triggers usan DELIMITER, que NO funciona desde MySqlCommand en C#.)
-- =====================================================================

-- Obligatorio: si el cliente usa latin1 (comun en la consola), los acentos
-- y emojis de los datos de prueba se guardan corruptos.
SET NAMES utf8mb4;

DROP DATABASE IF EXISTS surachih;
-- utf8mb4: soporta emojis. _ai_ci: busquedas sin distinguir acentos ni
-- mayusculas ("musica" encuentra "Música").
CREATE DATABASE surachih CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE surachih;

-- ---------------------------------------------------------------------
-- USUARIOS Y ORGANIZADORES
-- ---------------------------------------------------------------------

CREATE TABLE user (
    id             INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    username       VARCHAR(30)  NOT NULL,
    email          VARCHAR(254) NOT NULL,
    password_hash  CHAR(60)     NOT NULL,              -- BCrypt (BCrypt.Net-Next)
    avatar_url     VARCHAR(500) NULL,                  -- URL en la nube
    is_mod         BOOLEAN      NOT NULL DEFAULT FALSE,
    is_active      BOOLEAN      NOT NULL DEFAULT TRUE, -- "borrar cuenta" = FALSE
    created_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT uq_user_username UNIQUE (username),
    CONSTRAINT uq_user_email    UNIQUE (email),
    CONSTRAINT ck_user_username CHECK (REGEXP_LIKE(username, '^[A-Za-z0-9_.]{3,30}$', 'c'))
);

-- Solicitud y perfil de organizador (1 fila por usuario).
-- Organizador = usuario con status 'approved'. Sigue siendo usuario normal.
CREATE TABLE organizer_profile (
    user_id        INT UNSIGNED PRIMARY KEY,
    business_name  VARCHAR(120) NOT NULL,
    description    VARCHAR(500) NULL,
    social_url     VARCHAR(500) NOT NULL,              -- evidencia publica que revisa el moderador
    whatsapp       VARCHAR(15)  NOT NULL,              -- solo digitos con lada: 526141234567
    status         ENUM('pending','approved','rejected','suspended') NOT NULL DEFAULT 'pending',
    is_verified    BOOLEAN      NOT NULL DEFAULT FALSE, -- badge tras historial limpio
    requested_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_orgprof_user FOREIGN KEY (user_id) REFERENCES user(id),
    CONSTRAINT ck_orgprof_whatsapp CHECK (REGEXP_LIKE(whatsapp, '^[0-9]{10,15}$'))
);

-- Recuperacion de contrasena por correo. Se guarda el HASH del token, nunca el token.
CREATE TABLE password_reset (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id     INT UNSIGNED NOT NULL,
    token_hash  CHAR(64)     NOT NULL,                 -- SHA-256 en hex
    expires_at  DATETIME     NOT NULL,
    used_at     DATETIME     NULL,
    created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_pwreset_user FOREIGN KEY (user_id) REFERENCES user(id) ON DELETE CASCADE,
    CONSTRAINT uq_pwreset_token UNIQUE (token_hash)
);

-- ---------------------------------------------------------------------
-- ETIQUETAS (chips del frontend)
-- Evento vs lugar se distingue por tabla (event / place).
-- Un evento o lugar puede tener VARIAS etiquetas (Familiar + Música).
-- ---------------------------------------------------------------------

CREATE TABLE tag (
    id          SMALLINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    name        VARCHAR(50)  NOT NULL,
    icon        VARCHAR(50)  NOT NULL,                 -- nombre del recurso de icono en la app
    color_hex   CHAR(7)      NOT NULL,                 -- color del icono del chip
    scope       ENUM('event','place','both') NOT NULL DEFAULT 'both',
    sort_order  SMALLINT     NOT NULL DEFAULT 0,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    CONSTRAINT uq_tag_name UNIQUE (name),
    CONSTRAINT ck_tag_color CHECK (REGEXP_LIKE(color_hex, '^#[0-9A-Fa-f]{6}$'))
);

-- ---------------------------------------------------------------------
-- LUGARES (locales). Solo organizadores aprobados. Pasan por moderacion.
-- ---------------------------------------------------------------------

CREATE TABLE place (
    id              INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    owner_id        INT UNSIGNED  NOT NULL,
    name            VARCHAR(120)  NOT NULL,
    description     TEXT          NULL,
    address         VARCHAR(255)  NOT NULL,
    latitude        DECIMAL(9,6)  NOT NULL,
    longitude       DECIMAL(9,6)  NOT NULL,
    phone           VARCHAR(15)   NULL,
    website_url     VARCHAR(500)  NULL,
    price_min       DECIMAL(10,2) NULL,                -- rango "$100–200"
    price_max       DECIMAL(10,2) NULL,
    main_image_url  VARCHAR(500)  NULL,
    status          ENUM('pending','approved','rejected','inactive') NOT NULL DEFAULT 'pending',
    created_at      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_place_owner FOREIGN KEY (owner_id) REFERENCES user(id),
    -- Caja aproximada del estado de Chihuahua: atrapa coordenadas invertidas o escaladas.
    CONSTRAINT ck_place_lat   CHECK (latitude  BETWEEN 25.5 AND 31.8),
    CONSTRAINT ck_place_lng   CHECK (longitude BETWEEN -109.2 AND -103.3),
    CONSTRAINT ck_place_price CHECK (price_min IS NULL OR price_max IS NULL
                                     OR (price_min >= 0 AND price_min <= price_max)),
    INDEX ix_place_status (status),
    INDEX ix_place_owner (owner_id)
);

-- Horario por dia. day_of_week: 0 = domingo ... 6 = sabado (igual que DayOfWeek en C#).
-- Sin fila = cerrado ese dia. closes_at < opens_at = cierra despues de medianoche.
CREATE TABLE place_hours (
    place_id     INT UNSIGNED NOT NULL,
    day_of_week  TINYINT      NOT NULL,
    opens_at     TIME         NOT NULL,
    closes_at    TIME         NOT NULL,
    PRIMARY KEY (place_id, day_of_week),
    CONSTRAINT fk_hours_place FOREIGN KEY (place_id) REFERENCES place(id) ON DELETE CASCADE,
    CONSTRAINT ck_hours_day   CHECK (day_of_week BETWEEN 0 AND 6),
    CONSTRAINT ck_hours_range CHECK (opens_at <> closes_at)
);

CREATE TABLE place_image (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    place_id    INT UNSIGNED NOT NULL,
    url         VARCHAR(500) NOT NULL,
    sort_order  SMALLINT     NOT NULL DEFAULT 0,
    created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_placeimg_place FOREIGN KEY (place_id) REFERENCES place(id) ON DELETE CASCADE,
    INDEX ix_placeimg_order (place_id, sort_order)
);

CREATE TABLE place_tag (
    place_id  INT UNSIGNED      NOT NULL,
    tag_id    SMALLINT UNSIGNED NOT NULL,
    PRIMARY KEY (place_id, tag_id),
    CONSTRAINT fk_placetag_place FOREIGN KEY (place_id) REFERENCES place(id) ON DELETE CASCADE,
    CONSTRAINT fk_placetag_tag   FOREIGN KEY (tag_id)   REFERENCES tag(id),
    INDEX ix_placetag_tag (tag_id)
);

-- ---------------------------------------------------------------------
-- EVENTOS. Solo organizadores aprobados. Pasan por moderacion.
-- Si place_id es NULL (parque, explanada), el evento lleva su propia ubicacion.
-- ---------------------------------------------------------------------

CREATE TABLE event (
    id              INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    organizer_id    INT UNSIGNED  NOT NULL,
    place_id        INT UNSIGNED  NULL,
    title           VARCHAR(150)  NOT NULL,
    description     TEXT          NOT NULL,
    start_at        DATETIME      NOT NULL,
    end_at          DATETIME      NOT NULL,
    address         VARCHAR(255)  NULL,
    latitude        DECIMAL(9,6)  NULL,
    longitude       DECIMAL(9,6)  NULL,
    price_min       DECIMAL(10,2) NOT NULL DEFAULT 0,
    price_max       DECIMAL(10,2) NOT NULL DEFAULT 0,
    is_free         BOOLEAN AS (price_max = 0) STORED,  -- derivado, no se escribe
    ticket_url      VARCHAR(500)  NULL,                -- pagina oficial de boletos
    main_image_url  VARCHAR(500)  NULL,
    status          ENUM('draft','pending','approved','rejected','cancelled') NOT NULL DEFAULT 'draft',
    cancel_reason   VARCHAR(300)  NULL,
    is_featured     BOOLEAN       NOT NULL DEFAULT FALSE, -- carrusel del hero (solo moderadores)
    featured_order  TINYINT       NULL,
    created_at      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    -- Sin ON DELETE en estas FK: MySQL prohibe acciones referenciales en columnas usadas en CHECK.
    CONSTRAINT fk_event_organizer FOREIGN KEY (organizer_id) REFERENCES user(id),
    CONSTRAINT fk_event_place     FOREIGN KEY (place_id)     REFERENCES place(id),
    CONSTRAINT ck_event_dates     CHECK (end_at > start_at),
    CONSTRAINT ck_event_location  CHECK (place_id IS NOT NULL
                                         OR (address IS NOT NULL AND latitude IS NOT NULL AND longitude IS NOT NULL)),
    CONSTRAINT ck_event_lat       CHECK (latitude  IS NULL OR latitude  BETWEEN 25.5 AND 31.8),
    CONSTRAINT ck_event_lng       CHECK (longitude IS NULL OR longitude BETWEEN -109.2 AND -103.3),
    CONSTRAINT ck_event_price     CHECK (price_min >= 0 AND price_min <= price_max),
    CONSTRAINT ck_event_cancel    CHECK (status <> 'cancelled' OR cancel_reason IS NOT NULL),
    INDEX ix_event_status_start (status, start_at),
    INDEX ix_event_organizer (organizer_id),
    INDEX ix_event_place (place_id),
    INDEX ix_event_featured (is_featured, featured_order)
);

CREATE TABLE event_image (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    event_id    INT UNSIGNED NOT NULL,
    url         VARCHAR(500) NOT NULL,
    sort_order  SMALLINT     NOT NULL DEFAULT 0,
    created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_eventimg_event FOREIGN KEY (event_id) REFERENCES event(id) ON DELETE CASCADE,
    INDEX ix_eventimg_order (event_id, sort_order)
);

CREATE TABLE event_tag (
    event_id  INT UNSIGNED      NOT NULL,
    tag_id    SMALLINT UNSIGNED NOT NULL,
    PRIMARY KEY (event_id, tag_id),
    CONSTRAINT fk_eventtag_event FOREIGN KEY (event_id) REFERENCES event(id) ON DELETE CASCADE,
    CONSTRAINT fk_eventtag_tag   FOREIGN KEY (tag_id)   REFERENCES tag(id),
    INDEX ix_eventtag_tag (tag_id)
);

-- ---------------------------------------------------------------------
-- MODERACION: historial de decisiones sobre eventos, lugares y solicitudes
-- de organizador. Insertar aqui actualiza el status del objetivo (trigger).
-- ---------------------------------------------------------------------

CREATE TABLE moderation (
    id            INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    mod_id        INT UNSIGNED NOT NULL,
    event_id      INT UNSIGNED NULL,
    place_id      INT UNSIGNED NULL,
    organizer_id  INT UNSIGNED NULL,                   -- organizer_profile.user_id
    decision      ENUM('approved','rejected') NOT NULL,
    comment       VARCHAR(500) NULL,
    decided_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_mod_mod       FOREIGN KEY (mod_id)       REFERENCES user(id),
    CONSTRAINT fk_mod_event     FOREIGN KEY (event_id)     REFERENCES event(id),
    CONSTRAINT fk_mod_place     FOREIGN KEY (place_id)     REFERENCES place(id),
    CONSTRAINT fk_mod_organizer FOREIGN KEY (organizer_id) REFERENCES organizer_profile(user_id),
    CONSTRAINT ck_mod_one_target CHECK ((event_id IS NOT NULL) + (place_id IS NOT NULL)
                                        + (organizer_id IS NOT NULL) = 1),
    CONSTRAINT ck_mod_reject_reason CHECK (decision = 'approved' OR comment IS NOT NULL),
    INDEX ix_mod_decided (decided_at)
);

-- ---------------------------------------------------------------------
-- INTERACCION DEL USUARIO
-- ---------------------------------------------------------------------

-- Favorito = "me interesa": prioriza el evento en el inicio del usuario
-- y cuenta como "interesados" en el panel del organizador.
CREATE TABLE favorite (
    user_id     INT UNSIGNED NOT NULL,
    event_id    INT UNSIGNED NOT NULL,
    created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, event_id),
    CONSTRAINT fk_fav_user  FOREIGN KEY (user_id)  REFERENCES user(id)  ON DELETE CASCADE,
    CONSTRAINT fk_fav_event FOREIGN KEY (event_id) REFERENCES event(id) ON DELETE CASCADE,
    INDEX ix_fav_event (event_id)
);

-- Inscripcion = RSVP "voy a ir". No garantiza cupo; los boletos se compran en ticket_url.
CREATE TABLE inscription (
    user_id       INT UNSIGNED NOT NULL,
    event_id      INT UNSIGNED NOT NULL,
    status        ENUM('active','cancelled') NOT NULL DEFAULT 'active',
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    cancelled_at  DATETIME     NULL,
    PRIMARY KEY (user_id, event_id),
    CONSTRAINT fk_insc_user  FOREIGN KEY (user_id)  REFERENCES user(id)  ON DELETE CASCADE,
    CONSTRAINT fk_insc_event FOREIGN KEY (event_id) REFERENCES event(id) ON DELETE CASCADE,
    INDEX ix_insc_event_status (event_id, status)
);

-- Resenas de eventos: solo despues de que termino (trigger). 1 por usuario.
CREATE TABLE event_review (
    id              INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    event_id        INT UNSIGNED  NOT NULL,
    user_id         INT UNSIGNED  NOT NULL,
    rating          TINYINT       NOT NULL,
    comment         VARCHAR(1000) NULL,
    organizer_reply VARCHAR(1000) NULL,
    replied_at      DATETIME      NULL,
    is_hidden       BOOLEAN       NOT NULL DEFAULT FALSE, -- un moderador la oculta tras un reporte
    created_at      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_evrev_event FOREIGN KEY (event_id) REFERENCES event(id),
    CONSTRAINT fk_evrev_user  FOREIGN KEY (user_id)  REFERENCES user(id),
    CONSTRAINT uq_evrev_once  UNIQUE (event_id, user_id),
    CONSTRAINT ck_evrev_rating CHECK (rating BETWEEN 1 AND 5)
);

-- Resenas de lugares: cualquier usuario, en cualquier momento. 1 por usuario.
CREATE TABLE place_review (
    id              INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    place_id        INT UNSIGNED  NOT NULL,
    user_id         INT UNSIGNED  NOT NULL,
    rating          TINYINT       NOT NULL,
    comment         VARCHAR(1000) NULL,
    owner_reply     VARCHAR(1000) NULL,
    replied_at      DATETIME      NULL,
    is_hidden       BOOLEAN       NOT NULL DEFAULT FALSE,
    created_at      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_plrev_place FOREIGN KEY (place_id) REFERENCES place(id),
    CONSTRAINT fk_plrev_user  FOREIGN KEY (user_id)  REFERENCES user(id),
    CONSTRAINT uq_plrev_once  UNIQUE (place_id, user_id),
    CONSTRAINT ck_plrev_rating CHECK (rating BETWEEN 1 AND 5)
);

-- Reportes de eventos, lugares o resenas. Exactamente un objetivo por fila.
CREATE TABLE report (
    id               INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    reporter_id      INT UNSIGNED NOT NULL,
    event_id         INT UNSIGNED NULL,
    place_id         INT UNSIGNED NULL,
    event_review_id  INT UNSIGNED NULL,
    place_review_id  INT UNSIGNED NULL,
    reason           ENUM('spam','scam','inappropriate','false_info','other') NOT NULL,
    details          VARCHAR(500) NULL,
    status           ENUM('open','resolved','dismissed') NOT NULL DEFAULT 'open',
    resolved_by      INT UNSIGNED NULL,
    resolved_at      DATETIME     NULL,
    created_at       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_rep_reporter FOREIGN KEY (reporter_id)     REFERENCES user(id),
    CONSTRAINT fk_rep_event    FOREIGN KEY (event_id)        REFERENCES event(id),
    CONSTRAINT fk_rep_place    FOREIGN KEY (place_id)        REFERENCES place(id),
    CONSTRAINT fk_rep_evrev    FOREIGN KEY (event_review_id) REFERENCES event_review(id),
    CONSTRAINT fk_rep_plrev    FOREIGN KEY (place_review_id) REFERENCES place_review(id),
    CONSTRAINT fk_rep_resolver FOREIGN KEY (resolved_by)     REFERENCES user(id),
    CONSTRAINT ck_rep_one_target CHECK ((event_id IS NOT NULL) + (place_id IS NOT NULL)
                                        + (event_review_id IS NOT NULL) + (place_review_id IS NOT NULL) = 1),
    CONSTRAINT ck_rep_other_details CHECK (reason <> 'other' OR details IS NOT NULL),
    INDEX ix_rep_status (status, created_at)
);

-- Vistas del evento para el embudo del panel (vistas -> interesados -> inscritos).
-- La app registra maximo 1 vista por usuario por evento por dia.
CREATE TABLE event_view (
    id         BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    event_id   INT UNSIGNED NOT NULL,
    user_id    INT UNSIGNED NULL,                      -- NULL = visitante sin cuenta
    viewed_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_view_event FOREIGN KEY (event_id) REFERENCES event(id) ON DELETE CASCADE,
    CONSTRAINT fk_view_user  FOREIGN KEY (user_id)  REFERENCES user(id)  ON DELETE SET NULL,
    INDEX ix_view_event_time (event_id, viewed_at)
);

-- =====================================================================
-- TRIGGERS: reglas de negocio que no dependen de que la app se acuerde.
-- Todos lanzan SQLSTATE 45000; en C# llegan como MySqlException.Number = 1644
-- y el mensaje se puede mostrar tal cual al usuario.
-- =====================================================================

DELIMITER $$

-- Solo organizadores aprobados publican eventos, y solo en lugares aprobados.
CREATE TRIGGER trg_event_bi BEFORE INSERT ON event
FOR EACH ROW
BEGIN
    IF NOT EXISTS (SELECT 1 FROM organizer_profile
                   WHERE user_id = NEW.organizer_id AND status = 'approved') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Solo organizadores aprobados pueden publicar eventos.';
    END IF;
    IF NEW.place_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM place
                   WHERE id = NEW.place_id AND status = 'approved') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El lugar seleccionado no esta aprobado.';
    END IF;
    IF NEW.status NOT IN ('draft','pending') THEN
        SET NEW.status = 'pending';                       -- nadie crea un evento ya aprobado
    END IF;
END$$

-- Editar contenido de un evento aprobado lo regresa a moderacion.
CREATE TRIGGER trg_event_bu BEFORE UPDATE ON event
FOR EACH ROW
BEGIN
    IF OLD.status = 'approved' AND NEW.status = 'approved' AND (
           NOT (NEW.title          <=> OLD.title)
        OR NOT (NEW.description    <=> OLD.description)
        OR NOT (NEW.main_image_url <=> OLD.main_image_url)
        OR NOT (NEW.ticket_url     <=> OLD.ticket_url)
        OR NOT (NEW.place_id       <=> OLD.place_id)
        OR NOT (NEW.address        <=> OLD.address)
        OR NOT (NEW.latitude       <=> OLD.latitude)
        OR NOT (NEW.longitude      <=> OLD.longitude)) THEN
        SET NEW.status = 'pending';
        SET NEW.is_featured = FALSE;
    END IF;
    IF NEW.place_id IS NOT NULL AND NOT (NEW.place_id <=> OLD.place_id)
       AND NOT EXISTS (SELECT 1 FROM place WHERE id = NEW.place_id AND status = 'approved') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El lugar seleccionado no esta aprobado.';
    END IF;
END$$

-- Agregar fotos a la galeria de un evento aprobado tambien lo regresa a moderacion.
CREATE TRIGGER trg_eventimg_ai AFTER INSERT ON event_image
FOR EACH ROW
BEGIN
    UPDATE event SET status = 'pending', is_featured = FALSE
    WHERE id = NEW.event_id AND status = 'approved';
END$$

-- Solo organizadores aprobados registran lugares.
CREATE TRIGGER trg_place_bi BEFORE INSERT ON place
FOR EACH ROW
BEGIN
    IF NOT EXISTS (SELECT 1 FROM organizer_profile
                   WHERE user_id = NEW.owner_id AND status = 'approved') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Solo organizadores aprobados pueden registrar lugares.';
    END IF;
    SET NEW.status = 'pending';
END$$

-- Editar contenido de un lugar aprobado lo regresa a moderacion.
CREATE TRIGGER trg_place_bu BEFORE UPDATE ON place
FOR EACH ROW
BEGIN
    IF OLD.status = 'approved' AND NEW.status = 'approved' AND (
           NOT (NEW.name           <=> OLD.name)
        OR NOT (NEW.description    <=> OLD.description)
        OR NOT (NEW.main_image_url <=> OLD.main_image_url)
        OR NOT (NEW.website_url    <=> OLD.website_url)
        OR NOT (NEW.address        <=> OLD.address)
        OR NOT (NEW.latitude       <=> OLD.latitude)
        OR NOT (NEW.longitude      <=> OLD.longitude)) THEN
        SET NEW.status = 'pending';
    END IF;
END$$

CREATE TRIGGER trg_placeimg_ai AFTER INSERT ON place_image
FOR EACH ROW
BEGIN
    UPDATE place SET status = 'pending' WHERE id = NEW.place_id AND status = 'approved';
END$$

-- Moderacion: valida al moderador y que el objetivo este pendiente.
CREATE TRIGGER trg_moderation_bi BEFORE INSERT ON moderation
FOR EACH ROW
BEGIN
    IF NOT EXISTS (SELECT 1 FROM user WHERE id = NEW.mod_id AND is_mod = TRUE AND is_active = TRUE) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Solo moderadores pueden moderar.';
    END IF;
    IF NEW.event_id IS NOT NULL THEN
        IF NOT EXISTS (SELECT 1 FROM event WHERE id = NEW.event_id AND status = 'pending') THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El evento no esta pendiente de moderacion.';
        END IF;
        IF EXISTS (SELECT 1 FROM event WHERE id = NEW.event_id AND organizer_id = NEW.mod_id) THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No puedes moderar tu propio evento.';
        END IF;
    ELSEIF NEW.place_id IS NOT NULL THEN
        IF NOT EXISTS (SELECT 1 FROM place WHERE id = NEW.place_id AND status = 'pending') THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El lugar no esta pendiente de moderacion.';
        END IF;
        IF EXISTS (SELECT 1 FROM place WHERE id = NEW.place_id AND owner_id = NEW.mod_id) THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No puedes moderar tu propio lugar.';
        END IF;
    ELSE
        IF NOT EXISTS (SELECT 1 FROM organizer_profile WHERE user_id = NEW.organizer_id AND status = 'pending') THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La solicitud no esta pendiente.';
        END IF;
        IF NEW.organizer_id = NEW.mod_id THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No puedes aprobar tu propia solicitud.';
        END IF;
    END IF;
END$$

-- Moderacion: aplica la decision al objetivo.
CREATE TRIGGER trg_moderation_ai AFTER INSERT ON moderation
FOR EACH ROW
BEGIN
    IF NEW.event_id IS NOT NULL THEN
        UPDATE event SET status = NEW.decision WHERE id = NEW.event_id;
    ELSEIF NEW.place_id IS NOT NULL THEN
        UPDATE place SET status = NEW.decision WHERE id = NEW.place_id;
    ELSE
        UPDATE organizer_profile SET status = NEW.decision WHERE user_id = NEW.organizer_id;
    END IF;
END$$

-- RSVP solo a eventos aprobados que no han terminado.
CREATE TRIGGER trg_inscription_bi BEFORE INSERT ON inscription
FOR EACH ROW
BEGIN
    IF NOT EXISTS (SELECT 1 FROM event WHERE id = NEW.event_id
                   AND status = 'approved' AND end_at > NOW()) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No puedes inscribirte a este evento.';
    END IF;
END$$

-- Resena de evento: solo si ya termino, esta aprobado y no eres el organizador.
CREATE TRIGGER trg_evrev_bi BEFORE INSERT ON event_review
FOR EACH ROW
BEGIN
    IF NOT EXISTS (SELECT 1 FROM event WHERE id = NEW.event_id
                   AND status = 'approved' AND end_at < NOW()) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Solo puedes resenar eventos que ya terminaron.';
    END IF;
    IF EXISTS (SELECT 1 FROM event WHERE id = NEW.event_id AND organizer_id = NEW.user_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No puedes resenar tu propio evento.';
    END IF;
END$$

-- Resena de lugar: lugar aprobado y no eres el dueno.
CREATE TRIGGER trg_plrev_bi BEFORE INSERT ON place_review
FOR EACH ROW
BEGIN
    IF NOT EXISTS (SELECT 1 FROM place WHERE id = NEW.place_id AND status = 'approved') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este lugar no esta disponible para resenas.';
    END IF;
    IF EXISTS (SELECT 1 FROM place WHERE id = NEW.place_id AND owner_id = NEW.user_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No puedes resenar tu propio lugar.';
    END IF;
END$$

DELIMITER ;

-- =====================================================================
-- VISTAS (las consultas que la app usa todo el tiempo)
-- =====================================================================

-- Tarjeta / detalle de evento con ubicacion resuelta y estado "finished" derivado.
CREATE VIEW v_event_card AS
SELECT e.id, e.title, e.description, e.start_at, e.end_at,
       e.price_min, e.price_max, e.is_free, e.ticket_url, e.main_image_url,
       e.status,
       (e.status = 'approved' AND e.end_at < NOW())   AS is_finished,
       e.is_featured, e.featured_order,
       e.place_id, p.name                             AS place_name,
       COALESCE(e.address,   p.address)               AS address,
       COALESCE(e.latitude,  p.latitude)              AS latitude,
       COALESCE(e.longitude, p.longitude)             AS longitude,
       e.organizer_id, op.business_name               AS organizer_name,
       op.whatsapp                                    AS organizer_whatsapp,
       op.is_verified                                 AS organizer_verified
FROM event e
JOIN organizer_profile op ON op.user_id = e.organizer_id
LEFT JOIN place p         ON p.id = e.place_id;

-- Panel del organizador: embudo y calificacion por evento.
CREATE VIEW v_event_stats AS
SELECT e.id AS event_id, e.organizer_id,
       (SELECT COUNT(*) FROM event_view v  WHERE v.event_id = e.id)                         AS views,
       (SELECT COUNT(*) FROM favorite f    WHERE f.event_id = e.id)                         AS interested,
       (SELECT COUNT(*) FROM inscription i WHERE i.event_id = e.id AND i.status = 'active') AS inscribed,
       (SELECT ROUND(AVG(r.rating), 1) FROM event_review r WHERE r.event_id = e.id AND NOT r.is_hidden) AS avg_rating,
       (SELECT COUNT(*) FROM event_review r WHERE r.event_id = e.id AND NOT r.is_hidden)    AS review_count
FROM event e;

-- Calificacion de lugares (para la tarjeta "4.9 ★").
CREATE VIEW v_place_rating AS
SELECT p.id AS place_id,
       ROUND(AVG(r.rating), 1) AS avg_rating,
       COUNT(r.id)             AS review_count
FROM place p
LEFT JOIN place_review r ON r.place_id = p.id AND NOT r.is_hidden
GROUP BY p.id;

-- =====================================================================
-- DATOS DE PRUEBA
-- Contrasena de todos los usuarios: Test1234!
-- Coordenadas APROXIMADAS: verificarlas en el mapa antes de la demo.
-- =====================================================================

INSERT INTO tag (name, icon, color_hex, scope, sort_order) VALUES
 ('Familiar',                  'family',     '#9F1239', 'both',  1),
 ('Música',                    'music',      '#5B21B6', 'both',  2),
 ('Deportes y aire libre',     'run',        '#065F46', 'both',  3),
 ('Tecnología y conferencias', 'robot',      '#075985', 'event', 4),
 ('Gastronomía',               'food',       '#92400E', 'both',  5),
 ('Cafetería',                 'coffee',     '#92400E', 'place', 10),
 ('Restaurante',               'restaurant', '#9A3412', 'place', 11),
 ('Bar',                       'bar',        '#5B21B6', 'place', 12),
 ('Foro y venue',              'stage',      '#86198F', 'place', 13),
 ('Museo y galería',           'museum',     '#155E75', 'place', 14),
 ('Parque',                    'tree',       '#065F46', 'place', 15);

SET @pw = '$2b$11$C4rR0iUKQKbnm.V0sIsKHeyxIUqVR7g8aqJbWo48UbbeVbh/PeDsC';
INSERT INTO user (id, username, email, password_hash, is_mod) VALUES
 (1, 'mod_ana',      'ana.mod@example.com',     @pw, TRUE),
 (2, 'mod_luis',     'luis.mod@example.com',    @pw, TRUE),
 (3, 'cafe_dande',   'dandelion@example.com',   @pw, FALSE),
 (4, 'misiones_cuu', 'misiones@example.com',    @pw, FALSE),
 (5, 'mariana_r',    'mariana.r@example.com',   @pw, FALSE),
 (6, 'diego_m',      'diego.m@example.com',     @pw, FALSE),
 (7, 'sofia_c',      'sofia.c@example.com',     @pw, FALSE);

-- Solicitudes de organizador (3 y 4 se aprueban; 7 queda pendiente para probar el flujo).
INSERT INTO organizer_profile (user_id, business_name, social_url, whatsapp) VALUES
 (3, 'Dandelion Coffee',    'https://instagram.com/ejemplo_dandelion', '526141110001'),
 (4, 'Misiones Chihuahua',  'https://instagram.com/ejemplo_misiones',  '526141110002'),
 (7, 'Colectivo Sofía',     'https://instagram.com/ejemplo_sofia',     '526141110003');
INSERT INTO moderation (mod_id, organizer_id, decision, comment) VALUES
 (1, 3, 'approved', 'Perfil de Instagram activo y coherente con el negocio.'),
 (2, 4, 'approved', NULL);

-- Lugares
INSERT INTO place (id, owner_id, name, description, address, latitude, longitude, price_min, price_max, main_image_url) VALUES
 (1, 3, 'Dandelion Coffee', 'Cafetería de especialidad.', 'C. Monte Bello 4334, Chihuahua', 28.652000, -106.119000, 100, 200,
  'https://res.cloudinary.com/demo/image/upload/sample.jpg'),
 (2, 3, 'Café de la Tercera', 'Café y terraza en el centro.', 'C. Tercera 805, Centro, Chihuahua', 28.635500, -106.077000, 200, 300,
  'https://res.cloudinary.com/demo/image/upload/sample.jpg');
INSERT INTO moderation (mod_id, place_id, decision) VALUES (1, 1, 'approved'), (2, 2, 'approved');
INSERT INTO place_tag VALUES (1, 6), (2, 6), (2, 5);
INSERT INTO place_hours (place_id, day_of_week, opens_at, closes_at) VALUES
 (1,1,'08:00','21:00'),(1,2,'08:00','21:00'),(1,3,'08:00','21:00'),(1,4,'08:00','21:00'),
 (1,5,'08:00','21:00'),(1,6,'09:00','21:00'),
 (2,4,'18:00','01:00'),(2,5,'18:00','02:00'),(2,6,'18:00','02:00');   -- cruza medianoche

-- Eventos
INSERT INTO event (id, organizer_id, place_id, title, description, start_at, end_at,
                   address, latitude, longitude, price_min, price_max, ticket_url, main_image_url, status) VALUES
 (1, 4, NULL, 'Misión 404 - Juego callejero guiado por app',
  'Recorre el centro resolviendo acertijos 🔍🧩', '2026-10-01 10:00', '2026-11-29 20:00',
  'Plaza de Armas, Centro, Chihuahua', 28.635300, -106.075600, 370, 370,
  'https://boletos.example.com/mision404', 'https://res.cloudinary.com/demo/image/upload/sample.jpg', 'pending'),
 (2, 3, 1, 'Cata de café de especialidad',
  'Tres orígenes, tres métodos de extracción ☕', '2026-10-24 17:00', '2026-10-24 19:00',
  NULL, NULL, NULL, 250, 250, NULL, 'https://res.cloudinary.com/demo/image/upload/sample.jpg', 'pending'),
 (3, 4, NULL, 'Carrera nocturna 5K',
  'Carrera recreativa familiar 🏃', '2026-09-20 19:00', '2026-09-20 22:00',
  'Parque El Palomar, Chihuahua', 28.640000, -106.085000, 0, 0, NULL,
  'https://res.cloudinary.com/demo/image/upload/sample.jpg', 'pending'),
 (4, 4, NULL, 'Taller de fotografía urbana',
  'Salida fotográfica por el centro histórico 📷', '2026-11-08 09:00', '2026-11-08 13:00',
  'Catedral de Chihuahua', 28.635900, -106.076200, 450, 600, NULL, NULL, 'pending');
INSERT INTO event_tag VALUES (1,1),(2,5),(3,1),(3,3);
INSERT INTO event_image (event_id, url, sort_order) VALUES
 (2, 'https://res.cloudinary.com/demo/image/upload/sample.jpg', 1);
INSERT INTO moderation (mod_id, event_id, decision, comment) VALUES
 (1, 1, 'approved', NULL),
 (2, 2, 'approved', NULL),
 (1, 3, 'approved', NULL);
-- El evento 4 queda pendiente para probar el dashboard de moderacion.
UPDATE event SET is_featured = TRUE, featured_order = 1 WHERE id = 1;

INSERT INTO favorite (user_id, event_id) VALUES (5,1),(6,1),(5,2);
INSERT INTO inscription (user_id, event_id) VALUES (5,1),(6,2),(7,2);
INSERT INTO event_view (event_id, user_id) VALUES (1,5),(1,6),(1,7),(1,NULL),(2,5),(2,6);
INSERT INTO event_review (event_id, user_id, rating, comment) VALUES (3, 5, 5, 'Muy bien organizada.');
INSERT INTO place_review (place_id, user_id, rating, comment) VALUES
 (1, 5, 5, 'El mejor flat white.'), (1, 6, 4, NULL), (2, 7, 4, 'Buen ambiente.');

-- =====================================================================
-- CUENTA DE LA APP (minimo privilegio: sin DROP/ALTER/CREATE)
-- Cambiar la contrasena antes de usarla.
-- =====================================================================
-- CREATE USER 'surachih_app'@'localhost' IDENTIFIED BY 'CambiaEstaContrasena!';
-- GRANT SELECT, INSERT, UPDATE, DELETE ON surachih.* TO 'surachih_app'@'localhost';

-- =====================================================================
-- CONSULTAS DE REFERENCIA PARA LA APP
-- =====================================================================
-- Busqueda "estilo Netflix" (disparar con debounce de ~300 ms en TextChanged):
--   SELECT c.* FROM v_event_card c
--   WHERE c.status = 'approved' AND c.end_at > NOW()
--     AND (c.title LIKE CONCAT('%', @q, '%') OR c.place_name LIKE CONCAT('%', @q, '%')
--          OR EXISTS (SELECT 1 FROM event_tag et JOIN tag t ON t.id = et.tag_id
--                     WHERE et.event_id = c.id AND t.name LIKE CONCAT('%', @q, '%')))
--   ORDER BY c.start_at LIMIT 30;
--
-- Filtro por chips (eventos con TODAS las etiquetas seleccionadas, p. ej. 1 y 3):
--   SELECT c.* FROM v_event_card c JOIN event_tag et ON et.event_id = c.id
--   WHERE c.status = 'approved' AND c.end_at > NOW() AND et.tag_id IN (1, 3)
--   GROUP BY c.id HAVING COUNT(DISTINCT et.tag_id) = 2;
--
-- Inicio del usuario: primero sus favoritos, luego el resto:
--   SELECT c.*, (f.user_id IS NOT NULL) AS is_favorite FROM v_event_card c
--   LEFT JOIN favorite f ON f.event_id = c.id AND f.user_id = @userId
--   WHERE c.status = 'approved' AND c.end_at > NOW()
--   ORDER BY is_favorite DESC, c.start_at;
--
-- Panel del organizador:
--   SELECT c.title, c.status, s.* FROM v_event_stats s JOIN v_event_card c ON c.id = s.event_id
--   WHERE s.organizer_id = @userId ORDER BY c.start_at DESC;
