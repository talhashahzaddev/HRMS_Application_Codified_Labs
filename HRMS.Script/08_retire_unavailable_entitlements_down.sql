BEGIN;

UPDATE entitlement.features
SET is_active = true
WHERE key IN ('lms', 'air_ticket_management');

COMMIT;
