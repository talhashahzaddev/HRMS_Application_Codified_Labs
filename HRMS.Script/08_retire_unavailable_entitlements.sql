BEGIN;

UPDATE entitlement.features
SET is_active = false
WHERE key IN ('lms', 'air_ticket_management');

COMMIT;
