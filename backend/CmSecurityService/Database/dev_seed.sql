-- DEV ONLY: roles and two test officers so the secured endpoints can be tried locally.
-- Runs once on a fresh MySQL volume (after security_db.sql). Do not use in production.
USE security_db;

INSERT IGNORE INTO role (name, description) VALUES
  ('Police Officer',         'Front-line officer'),
  ('Sergeant',               'Supervising officer'),
  ('Police Chief',           'Head of the police service'),
  ('Security Administrator', 'Manages personnel and security settings');

-- The user_id values below are what goes in the JWT "sub" claim (see scripts/make-dev-token.js).
INSERT INTO user_account (user_id, username, password_hash) VALUES
  ('d0000000-0000-4000-8000-000000000001', 'dev.admin',   'DEV-ONLY-NOT-A-REAL-HASH'),
  ('d0000000-0000-4000-8000-000000000002', 'dev.officer', 'DEV-ONLY-NOT-A-REAL-HASH');

INSERT INTO officer (officer_id, user_id, badge_number, first_name, last_name, rank_title) VALUES
  ('e0000000-0000-4000-8000-000000000001', 'd0000000-0000-4000-8000-000000000001', 'DEV-001', 'Dev', 'Admin',   'Administrator'),
  ('e0000000-0000-4000-8000-000000000002', 'd0000000-0000-4000-8000-000000000002', 'DEV-002', 'Dev', 'Officer', 'Constable');

INSERT INTO officer_role (officer_id, role_id)
  SELECT 'e0000000-0000-4000-8000-000000000001', role_id FROM role WHERE name = 'Security Administrator';
INSERT INTO officer_role (officer_id, role_id)
  SELECT 'e0000000-0000-4000-8000-000000000002', role_id FROM role WHERE name = 'Police Officer';
