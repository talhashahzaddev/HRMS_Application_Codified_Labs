BEGIN;

DROP TRIGGER IF EXISTS trg_assign_starter_plan_to_new_tenant ON auth.organizations;
DROP FUNCTION IF EXISTS entitlement.assign_starter_plan_to_new_tenant();
DROP TRIGGER IF EXISTS trg_enforce_tenant_user_limit ON employee.employees;
DROP FUNCTION IF EXISTS entitlement.enforce_tenant_user_limit();

DO $rollback$
BEGIN
    IF to_regclass('entitlement.migration_plan_backup') IS NULL THEN
        RAISE WARNING
            'entitlement.migration_plan_backup is missing; preserving subscription plans because their pre-migration state cannot be restored safely.';
    ELSE
        IF to_regclass('payment.transactions') IS NOT NULL THEN
            EXECUTE $sql$
                DELETE FROM payment.subscriptionplans p
                WHERE p.code IN ('starter', 'premium', 'business', 'pro', 'custom')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM entitlement.migration_plan_backup b
                      WHERE b.planid = p.planid
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM payment.companysubscriptions s
                      WHERE s.planid = p.planid
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM payment.transactions t
                      WHERE t.plan_id = p.planid
                  );
            $sql$;
        ELSE
            EXECUTE $sql$
                DELETE FROM payment.subscriptionplans p
                WHERE p.code IN ('starter', 'premium', 'business', 'pro', 'custom')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM entitlement.migration_plan_backup b
                      WHERE b.planid = p.planid
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM payment.companysubscriptions s
                      WHERE s.planid = p.planid
                  );
            $sql$;
        END IF;

        EXECUTE $sql$
            UPDATE payment.subscriptionplans p
            SET name = b.name,
                monthlyprice = b.monthlyprice,
                annualprice = b.annualprice,
                maxusers = b.maxusers,
                features = b.features,
                iscustom = b.iscustom,
                isactive = b.isactive
            FROM entitlement.migration_plan_backup b
            WHERE p.planid = b.planid;
        $sql$;
    END IF;
END;
$rollback$;

DROP TABLE IF EXISTS entitlement.audit_logs;
DROP TABLE IF EXISTS entitlement.ai_query_usage;
DROP TABLE IF EXISTS entitlement.default_role_permissions;
DROP TABLE IF EXISTS entitlement.tenant_feature_overrides;
DROP TABLE IF EXISTS entitlement.plan_features;
DROP TABLE IF EXISTS entitlement.features;
DROP SCHEMA IF EXISTS entitlement;

DROP INDEX IF EXISTS idx_companysubscriptions_tenant_status;
ALTER TABLE payment.companysubscriptions
    DROP CONSTRAINT IF EXISTS ck_companysubscriptions_custom_max_users,
    DROP CONSTRAINT IF EXISTS ck_companysubscriptions_custom_price,
    DROP COLUMN IF EXISTS trial_ends_at,
    DROP COLUMN IF EXISTS custom_price,
    DROP COLUMN IF EXISTS custom_max_users,
    DROP COLUMN IF EXISTS custom_currency;

DROP INDEX IF EXISTS uq_subscriptionplans_code;
ALTER TABLE payment.subscriptionplans
    DROP CONSTRAINT IF EXISTS ck_subscriptionplans_user_count_type,
    DROP CONSTRAINT IF EXISTS ck_subscriptionplans_status,
    DROP CONSTRAINT IF EXISTS ck_subscriptionplans_version,
    DROP COLUMN IF EXISTS code,
    DROP COLUMN IF EXISTS currency,
    DROP COLUMN IF EXISTS user_count_type,
    DROP COLUMN IF EXISTS is_public,
    DROP COLUMN IF EXISTS is_recommended,
    DROP COLUMN IF EXISTS status,
    DROP COLUMN IF EXISTS version;

COMMIT;
