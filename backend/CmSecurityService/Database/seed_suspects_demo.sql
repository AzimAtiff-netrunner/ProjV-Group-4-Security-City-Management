-- Demo data for story #225: suspect records and neighborhood crime rankings.
-- Run AFTER security_db.sql. Safe to run twice: INSERT IGNORE skips rows that already exist.
USE security_db;

-- 6 suspects (fixed ids so GET /suspects/{id} is easy to demo)
INSERT IGNORE INTO suspect
  (suspect_id, first_name, last_name, date_of_birth, physical_description, known_aliases, status, created_at)
VALUES
  ('22222222-2222-2222-2222-000000000001', 'Marcus', 'Hale',  '1991-03-14', 'Tall, slim build, short dark hair, grey hoodie', 'Mac, M-Dog', 'Under investigation', '2026-11-02 09:15:00'),
  ('22222222-2222-2222-2222-000000000002', 'Lena',   'Ortiz', '1988-07-22', 'Medium build, shoulder-length red hair',         NULL,         'Charged',             '2026-11-04 14:40:00'),
  ('22222222-2222-2222-2222-000000000003', NULL,     NULL,    NULL,         'Unknown male, about 30, black jacket, limps on left leg', 'The Limper', 'Under investigation', '2026-11-06 22:05:00'),
  ('22222222-2222-2222-2222-000000000004', 'Darnell','Price', '1995-11-30', 'Stocky, tattoo on right forearm',                'D',          'Cleared',             '2026-11-08 11:30:00'),
  ('22222222-2222-2222-2222-000000000005', 'Priya',  'Nair',  '1999-01-05', 'Short, wears glasses, blue backpack',            NULL,         'Under investigation', '2026-11-10 16:20:00'),
  ('22222222-2222-2222-2222-000000000006', 'Tomas',  'Hale',  '1993-05-19', 'Beard, green work jacket',                       'Tom',        'Wanted',              '2026-11-12 08:45:00');

-- 10 incidents across 5 neighborhoods so the ranking has something to rank:
-- Downtown 4, Eastside 3, Westside 2, Uptown 1, Northgate 0 (not listed)
INSERT IGNORE INTO incident
  (incident_id, incident_type_id, incident_title, incident_location, description, incident_status_id, created_at)
VALUES
  ('33333333-3333-3333-3333-000000000001', (SELECT incident_type_id FROM incident_type WHERE name = 'Theft'),     'Bike stolen from rack',         'Downtown', 'Locked bicycle taken outside the library.',      (SELECT incident_status_id FROM incident_status WHERE name = 'Open'),                '2026-11-01 09:30:00'),
  ('33333333-3333-3333-3333-000000000002', (SELECT incident_type_id FROM incident_type WHERE name = 'Theft'),     'Wallet taken on transit',       'Downtown', 'Wallet stolen on the route 7 bus.',              (SELECT incident_status_id FROM incident_status WHERE name = 'Under Investigation'), '2026-11-03 12:00:00'),
  ('33333333-3333-3333-3333-000000000003', (SELECT incident_type_id FROM incident_type WHERE name = 'Vandalism'), 'Graffiti on storefront',        'Downtown', 'Spray paint on a shop window on King St.',       (SELECT incident_status_id FROM incident_status WHERE name = 'Closed'),              '2026-11-05 07:10:00'),
  ('33333333-3333-3333-3333-000000000004', (SELECT incident_type_id FROM incident_type WHERE name = 'Assault'),   'Fight outside bar',             'Downtown', 'Two people fighting after closing time.',        (SELECT incident_status_id FROM incident_status WHERE name = 'Open'),                '2026-11-09 02:15:00'),
  ('33333333-3333-3333-3333-000000000005', (SELECT incident_type_id FROM incident_type WHERE name = 'Burglary'),  'Break-in at convenience store', 'Eastside', 'Rear door forced, register emptied.',            (SELECT incident_status_id FROM incident_status WHERE name = 'Under Investigation'), '2026-11-02 06:45:00'),
  ('33333333-3333-3333-3333-000000000006', (SELECT incident_type_id FROM incident_type WHERE name = 'Burglary'),  'Attempted break-in',            'Eastside', 'Pry marks on a pharmacy side door.',             (SELECT incident_status_id FROM incident_status WHERE name = 'Open'),                '2026-11-07 07:55:00'),
  ('33333333-3333-3333-3333-000000000007', (SELECT incident_type_id FROM incident_type WHERE name = 'Theft'),     'Package stolen from porch',     'Eastside', 'Delivery taken from a front porch.',             (SELECT incident_status_id FROM incident_status WHERE name = 'Open'),                '2026-11-11 15:30:00'),
  ('33333333-3333-3333-3333-000000000008', (SELECT incident_type_id FROM incident_type WHERE name = 'Traffic Accident'), 'Two-car collision',    'Westside', 'Minor collision at Elm St, no injuries.',        (SELECT incident_status_id FROM incident_status WHERE name = 'Closed'),              '2026-11-04 17:20:00'),
  ('33333333-3333-3333-3333-000000000009', (SELECT incident_type_id FROM incident_type WHERE name = 'Disturbance'), 'Noise complaint',            'Westside', 'Loud music after midnight.',                     (SELECT incident_status_id FROM incident_status WHERE name = 'Closed'),              '2026-11-08 00:40:00'),
  ('33333333-3333-3333-3333-000000000010', (SELECT incident_type_id FROM incident_type WHERE name = 'Vandalism'), 'Broken bus shelter glass',      'Uptown',   'Bus shelter panel smashed.',                     (SELECT incident_status_id FROM incident_status WHERE name = 'Open'),                '2026-11-10 21:05:00');
