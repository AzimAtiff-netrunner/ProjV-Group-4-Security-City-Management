USE security_db;

INSERT INTO role (name, description) VALUES
  ('Police Officer',         'Front-line officer'),
  ('Sergeant',               'Supervising officer'),
  ('Police Chief',           'Head of the police service'),
  ('Security Administrator', 'Manages personnel and security settings')
ON DUPLICATE KEY UPDATE description = VALUES(description);

INSERT INTO user_account (user_id, username, password_hash, is_active) VALUES
  ('d0000000-0000-4000-8000-000000000001', 'dev.admin',   'DEV-ONLY-NOT-A-REAL-HASH', 1),
  ('d0000000-0000-4000-8000-000000000002', 'dev.officer', 'DEV-ONLY-NOT-A-REAL-HASH', 1)
ON DUPLICATE KEY UPDATE is_active = 1;

INSERT INTO officer (officer_id, user_id, badge_number, first_name, last_name, rank_title, is_active) VALUES
  ('e0000000-0000-4000-8000-000000000001', 'd0000000-0000-4000-8000-000000000001', 'DEV-001', 'Dev', 'Admin',   'Administrator', 1),
  ('e0000000-0000-4000-8000-000000000002', 'd0000000-0000-4000-8000-000000000002', 'DEV-002', 'Dev', 'Officer', 'Constable', 1)
ON DUPLICATE KEY UPDATE is_active = 1;

-- Un-revoke the dev role if it exists, otherwise create it.
-- Looks the officer up by user_id, in case an old row has a different officer_id.
UPDATE officer_role orr
  JOIN officer o ON o.officer_id = orr.officer_id
  JOIN role r    ON r.role_id = orr.role_id
SET orr.revoked_at = NULL
WHERE (o.user_id, r.name) IN (
  ('d0000000-0000-4000-8000-000000000001', 'Security Administrator'),
  ('d0000000-0000-4000-8000-000000000002', 'Police Officer'));

INSERT INTO officer_role (officer_id, role_id)
SELECT o.officer_id, r.role_id
FROM officer o JOIN role r
  ON (o.user_id = 'd0000000-0000-4000-8000-000000000001' AND r.name = 'Security Administrator')
  OR (o.user_id = 'd0000000-0000-4000-8000-000000000002' AND r.name = 'Police Officer')
WHERE NOT EXISTS (
  SELECT 1 FROM officer_role x WHERE x.officer_id = o.officer_id AND x.role_id = r.role_id);