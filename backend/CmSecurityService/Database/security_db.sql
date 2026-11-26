CREATE DATABASE IF NOT EXISTS security_db;
USE security_db;

-- 1.  INCEDENTS
CREATE TABLE incident_type (
  incident_type_id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  name             VARCHAR(100) NOT NULL,
  description      VARCHAR(500) NULL,
  PRIMARY KEY (incident_type_id),
  UNIQUE KEY uq_incident_type_name (name)
);
 
CREATE TABLE incident_status (
  incident_status_id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  name               VARCHAR(50)  NOT NULL,
  description        VARCHAR(500) NULL,
  PRIMARY KEY (incident_status_id),
  UNIQUE KEY uq_incident_status_name (name)
);
 
CREATE TABLE incident (
  incident_id        CHAR(36)     NOT NULL DEFAULT (UUID()),
  incident_type_id   INT UNSIGNED NOT NULL,
  incident_title     VARCHAR(200) NOT NULL,
  incident_location  VARCHAR(200) NOT NULL, 
  description        TEXT         NOT NULL,
  incident_status_id INT UNSIGNED NOT NULL,
  created_at         DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (incident_id),
  KEY ix_incident_type (incident_type_id),
  KEY ix_incident_status (incident_status_id),
  KEY ix_incident_created_at (created_at),
  CONSTRAINT fk_incident_type
    FOREIGN KEY (incident_type_id) REFERENCES incident_type (incident_type_id),
  CONSTRAINT fk_incident_status
    FOREIGN KEY (incident_status_id) REFERENCES incident_status (incident_status_id),
  CONSTRAINT ck_incident_title    CHECK (CHAR_LENGTH(TRIM(incident_title)) > 0),
  CONSTRAINT ck_incident_location CHECK (CHAR_LENGTH(TRIM(incident_location)) > 0)
);
 
INSERT INTO incident_status (name, description) VALUES
  ('Open',                'Incident has been reported'),
  ('Under Investigation', 'Officers are investigating'),
  ('Closed',              'Incident file is closed');

INSERT INTO incident_type (name, description) VALUES
  ('Theft',            'Stolen property'),
  ('Vandalism',          'Vandalized Property'),
  ('Burglary',           'Break Ins'),
  ('Assault',          'Physical attack or threat'),
  ('Traffic Accident', 'Collision or road incident'),
  ('Disturbance',      'Noise, public disorder or disputes'),
  ('Other',            'Anything not covered by another type');
  



-- 2. ROLES AND LOGIN
CREATE TABLE role (
  role_id     INT UNSIGNED NOT NULL AUTO_INCREMENT,
  name        VARCHAR(50)  NOT NULL,
  description VARCHAR(500) NULL,
  PRIMARY KEY (role_id),
  UNIQUE KEY uq_role_name (name)
) ENGINE=InnoDB;
 
CREATE TABLE permission (
  permission_id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  name          VARCHAR(100) NOT NULL,
  description   VARCHAR(500) NULL,
  PRIMARY KEY (permission_id),
  UNIQUE KEY uq_permission_name (name)
);
 
CREATE TABLE role_permission (
  role_id       INT UNSIGNED NOT NULL,
  permission_id INT UNSIGNED NOT NULL,
  PRIMARY KEY (role_id, permission_id),
  CONSTRAINT fk_role_permission_role
    FOREIGN KEY (role_id) REFERENCES role (role_id),
  CONSTRAINT fk_role_permission_permission
    FOREIGN KEY (permission_id) REFERENCES permission (permission_id)
);
 
CREATE TABLE user_account (
  user_id       CHAR(36)     NOT NULL DEFAULT (UUID()),
  username      VARCHAR(100) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  is_active     TINYINT(1)   NOT NULL DEFAULT 1,
  last_login_at DATETIME(6)  NULL,
  created_at    DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (user_id),
  UNIQUE KEY uq_user_account_username (username)
);
 

-- 3. REGISTRATION
CREATE TABLE officer (
  officer_id   CHAR(36)     NOT NULL DEFAULT (UUID()),
  user_id      CHAR(36)     NULL,
  badge_number VARCHAR(30)  NOT NULL,
  first_name   VARCHAR(100) NOT NULL,
  last_name    VARCHAR(100) NOT NULL,
  rank_title   VARCHAR(100) NULL,
  phone        VARCHAR(30)  NULL,
  email        VARCHAR(255) NULL,
  hire_date    DATE         NULL,
  is_active    TINYINT(1)   NOT NULL DEFAULT 1,
  created_at   DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (officer_id),
  UNIQUE KEY uq_officer_badge (badge_number),
  UNIQUE KEY uq_officer_user (user_id),
  CONSTRAINT fk_officer_user
    FOREIGN KEY (user_id) REFERENCES user_account (user_id)
);
 
-- Role assignment (an officer can hold several roles over time)
CREATE TABLE officer_role (
  officer_role_id CHAR(36)     NOT NULL DEFAULT (UUID()),
  officer_id      CHAR(36)     NOT NULL,
  role_id         INT UNSIGNED NOT NULL,
  assigned_at     DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  revoked_at      DATETIME(6)  NULL,
  PRIMARY KEY (officer_role_id),
  KEY ix_officer_role_officer (officer_id),
  KEY ix_officer_role_role (role_id),
  CONSTRAINT fk_officer_role_officer
    FOREIGN KEY (officer_id) REFERENCES officer (officer_id),
  CONSTRAINT fk_officer_role_role
    FOREIGN KEY (role_id) REFERENCES role (role_id)
);
 
CREATE TABLE detective (
  detective_id   CHAR(36)    NOT NULL DEFAULT (UUID()),
  officer_id     CHAR(36)    NOT NULL,
  specialization VARCHAR(100) NULL,
  PRIMARY KEY (detective_id),
  UNIQUE KEY uq_detective_officer (officer_id),
  CONSTRAINT fk_detective_officer
    FOREIGN KEY (officer_id) REFERENCES officer (officer_id)
) ENGINE=InnoDB;
 
CREATE TABLE forensic_staff (
  forensic_staff_id CHAR(36)     NOT NULL DEFAULT (UUID()),
  user_id           CHAR(36)     NULL,
  first_name        VARCHAR(100) NOT NULL,
  last_name         VARCHAR(100) NOT NULL,
  specialty         VARCHAR(100) NULL,
  phone             VARCHAR(30)  NULL,
  email             VARCHAR(255) NULL,
  is_active         TINYINT(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (forensic_staff_id),
  UNIQUE KEY uq_forensic_staff_user (user_id),
  CONSTRAINT fk_forensic_staff_user
    FOREIGN KEY (user_id) REFERENCES user_account (user_id)
);

CREATE TABLE suspect (
  suspect_id          CHAR(36)     NOT NULL DEFAULT (UUID()),
  first_name          VARCHAR(100) NULL,
  last_name           VARCHAR(100) NULL,
  date_of_birth       DATE         NULL,
  physical_description TEXT        NULL,
  known_aliases       VARCHAR(500) NULL,
  status              VARCHAR(50)  NOT NULL DEFAULT 'Under investigation',
  created_at          DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (suspect_id)
);
 
CREATE TABLE detainee (
  detainee_id          CHAR(36)     NOT NULL DEFAULT (UUID()),
  suspect_id           CHAR(36)     NULL,
  first_name           VARCHAR(100) NOT NULL,
  last_name            VARCHAR(100) NOT NULL,
  date_of_birth        DATE         NULL,
  booking_number       VARCHAR(50)  NOT NULL,
  detained_at          DATETIME(6)  NOT NULL,
  released_at          DATETIME(6)  NULL,
  detaining_officer_id CHAR(36)     NULL,
  cell_location        VARCHAR(100) NULL,
  PRIMARY KEY (detainee_id),
  UNIQUE KEY uq_detainee_booking (booking_number),
  KEY ix_detainee_suspect (suspect_id),
  KEY ix_detainee_officer (detaining_officer_id),
  CONSTRAINT fk_detainee_suspect
    FOREIGN KEY (suspect_id) REFERENCES suspect (suspect_id),
  CONSTRAINT fk_detainee_officer
    FOREIGN KEY (detaining_officer_id) REFERENCES officer (officer_id)
);



-- 4. POLICE VEHICLE TELEMETRY
-- vehicle_id is the unique vehicle identifier exposed by the API (AC 2).
-- callsign and license_plate are also unique so the same car cannot be registered twice.
CREATE TABLE vehicle (
  vehicle_id    CHAR(36)     NOT NULL DEFAULT (UUID()),
  callsign      VARCHAR(30)  NOT NULL,
  license_plate VARCHAR(20)  NOT NULL,
  status        VARCHAR(30)  NOT NULL DEFAULT 'Available',
  is_active     TINYINT(1)   NOT NULL DEFAULT 1,
  created_at    DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (vehicle_id),
  UNIQUE KEY uq_vehicle_callsign (callsign),
  UNIQUE KEY uq_vehicle_license_plate (license_plate),
  CONSTRAINT ck_vehicle_status
    CHECK (status IN ('Available', 'OnPatrol', 'Dispatched', 'OutOfService')),
  CONSTRAINT ck_vehicle_callsign CHECK (CHAR_LENGTH(TRIM(callsign)) > 0)
);

-- Append-only history of location updates (AC 3, 4). Rows are never overwritten:
-- the "current location" is simply the row with the newest recorded_at.
--   recorded_at = when the vehicle reported the position (UTC, from the device)
--   received_at = when the server stored it (UTC)
CREATE TABLE vehicle_telemetry (
  telemetry_id    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  vehicle_id      CHAR(36)        NOT NULL,

  street_number   VARCHAR(10)     NOT NULL,
  street_name     VARCHAR(100)    NOT NULL,
  city            VARCHAR(50)     NOT NULL DEFAULT 'Waterloo',
  postal_code     VARCHAR(7)      NOT NULL,

  speed_kmh       DECIMAL(5,1)    NULL,
  heading_degrees DECIMAL(5,2)    NULL,

  recorded_at     DATETIME(6)     NOT NULL,
  received_at     DATETIME(6)     NOT NULL DEFAULT CURRENT_TIMESTAMP(6),

  PRIMARY KEY (telemetry_id),

  UNIQUE KEY uq_telemetry_vehicle_recorded (vehicle_id, recorded_at DESC),

  CONSTRAINT fk_telemetry_vehicle
    FOREIGN KEY (vehicle_id) REFERENCES vehicle (vehicle_id),

  CONSTRAINT ck_telemetry_city
    CHECK (city = 'Waterloo'),

  CONSTRAINT ck_telemetry_street_number
    CHECK (CHAR_LENGTH(TRIM(street_number)) > 0),

  CONSTRAINT ck_telemetry_street_name
    CHECK (CHAR_LENGTH(TRIM(street_name)) > 0),

  CONSTRAINT ck_telemetry_postal_code
    CHECK (postal_code REGEXP '^[A-Z][0-9][A-Z] ?[0-9][A-Z][0-9]$'),

  CONSTRAINT ck_telemetry_speed
    CHECK (speed_kmh IS NULL OR speed_kmh >= 0),

  CONSTRAINT ck_telemetry_heading
    CHECK (heading_degrees IS NULL OR (heading_degrees >= 0 AND heading_degrees < 360))
);

-- Demo data (fixed ids so API examples and tests are predictable)
INSERT INTO vehicle (vehicle_id, callsign, license_plate, status) VALUES
  ('11111111-1111-4111-8111-111111111111', 'UNIT-101', 'CPL 101', 'OnPatrol'),
  ('22222222-2222-4222-8222-222222222222', 'UNIT-102', 'CPL 102', 'Dispatched'),
  ('33333333-3333-4333-8333-333333333333', 'UNIT-103', 'CPL 103', 'OutOfService');

-- UNIT-101 and UNIT-102 have history; UNIT-103 deliberately has none (tests the "no telemetry yet" case).
INSERT INTO vehicle_telemetry(vehicle_id, street_number, street_name, city, postal_code, speed_kmh, heading_degrees, recorded_at) VALUES
('11111111-1111-4111-8111-111111111111', '200', 'University Avenue West', 'Waterloo', 'N2L 3G1', 40.0, 90.00, UTC_TIMESTAMP(6) - INTERVAL 10 MINUTE),
('11111111-1111-4111-8111-111111111111', '50', 'King Street North', 'Waterloo', 'N2J 2W9', 35.5, 95.00, UTC_TIMESTAMP(6) - INTERVAL 5 MINUTE),
('11111111-1111-4111-8111-111111111111', '300', 'Erb Street West', 'Waterloo', 'N2L 1W3', 0.0, 95.00, UTC_TIMESTAMP(6) - INTERVAL 1 MINUTE),
('22222222-2222-4222-8222-222222222222', '75', 'Weber Street North', 'Waterloo', 'N2J 3H7', 62.0, 180.00, UTC_TIMESTAMP(6) - INTERVAL 2 MINUTE);


