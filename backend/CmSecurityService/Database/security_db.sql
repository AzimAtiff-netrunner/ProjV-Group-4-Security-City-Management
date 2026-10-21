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
    FOREIGN KEY (incident_status_id) REFERENCES incident_status (incident_status_id)
);
 
INSERT INTO incident_status (name, description) VALUES
  ('Reported',    'Incident has been registered'),
  ('In Progress', 'Officers are responding or investigating'),
  ('Resolved',    'Incident has been dealt with'),
  ('Closed',      'Incident file is closed');

INSERT INTO incident_type (name, description) VALUES
  ('Theft',            'Stolen property'),
  ('Assault',          'Physical attack or threat'),
  ('Traffic Accident', 'Collision or road incident'),
  ('Disturbance',      'Noise, public disorder or disputes'),
  ('Other',            'Anything not covered by another type');
  
INSERT INTO incident (incident_type_id, description, incident_status_id) VALUES 
(1, 'Description of what happened', 1);


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

