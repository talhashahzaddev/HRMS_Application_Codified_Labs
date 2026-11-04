-- ============================================================
-- FILE: fix_permissions_backfill.sql
-- PURPOSE: One-time retroactive fix for all existing organizations.
--
-- PROBLEM: auth.permissions was never populated for org-specific
-- system roles, so newly registered organizations (and existing ones)
-- had no permissions in auth.permissions, causing the frontend to
-- deny access to most/all modules.
--
-- WHAT THIS SCRIPT DOES:
--   1. Backfills auth.permissions for ALL active organizations from
--      auth.global_role_permissions (the source of truth defined by
--      the Platform Admin).
--   2. Patches fn_create_organization in the live database so that
--      all future new organizations automatically get their permissions.
--
-- RUN ONCE against your PostgreSQL database.
-- ============================================================

BEGIN;

-- ============================================================
-- STEP 1: Backfill auth.permissions for all existing organizations.
--
-- For every active org, for every system role they have in auth.roles,
-- copy the corresponding permission rows from auth.global_role_permissions.
-- Skip if the permission row already exists (ON CONFLICT DO NOTHING).
-- ============================================================
INSERT INTO auth.permissions (
    permissionid,
    roleid,
    actionid,
    organizationid,
    has_permission,
    created_date,
    updated_date
)
SELECT
    public.uuid_generate_v4(),
    grp.roleid,
    grp.actionid,
    o.organizationid,
    grp.has_permission,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
FROM auth.organizations o
INNER JOIN auth.roles r
    ON r.organizationid = o.organizationid
    AND r.issystemrole = true
INNER JOIN auth.global_role_permissions grp
    ON grp.roleid = r.roleid
WHERE o.status = 'active'
ON CONFLICT DO NOTHING;

DO $$
DECLARE
    v_inserted INTEGER;
BEGIN
    GET DIAGNOSTICS v_inserted = ROW_COUNT;
    RAISE NOTICE 'STEP 1 COMPLETE: Inserted % permission rows into auth.permissions.', v_inserted;
END $$;

-- ============================================================
-- STEP 2: Patch the live fn_create_organization function so that
-- all future new organization registrations automatically get their
-- permissions populated.
-- ============================================================
CREATE OR REPLACE FUNCTION auth.fn_create_organization(
    p_name character varying,
    p_domain character varying DEFAULT NULL::character varying,
    p_subscription_type character varying DEFAULT 'basic'::character varying,
    p_currency character varying DEFAULT NULL::character varying
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    newOrgId UUID;
    insertedRolesCount INTEGER;
    insertedPermissionsCount INTEGER;
BEGIN
    -- Insert new organization
    INSERT INTO auth.organizations (name, domain, subscriptionType, currency)
    VALUES (p_name, p_domain, p_subscription_type, p_currency)
    RETURNING organizationId INTO newOrgId;

    -- Copy all active global roles into org-specific auth.roles
    INSERT INTO auth.roles (
        roleid,
        organizationid,
        rolename,
        description,
        issystemrole,
        role_version,
        createdat
    )
    SELECT
        gr.roleid,
        newOrgId,
        gr.rolename,
        gr.description,
        true,
        gr.version,
        CURRENT_TIMESTAMP
    FROM auth.global_roles gr
    WHERE gr.isactive = true;

    GET DIAGNOSTICS insertedRolesCount = ROW_COUNT;

    -- Copy all global role permissions into org-specific auth.permissions.
    -- This is the fix: every new org immediately gets the full permission set
    -- as defined by the Platform Admin in auth.global_role_permissions.
    INSERT INTO auth.permissions (
        permissionid,
        roleid,
        actionid,
        organizationid,
        has_permission,
        created_date,
        updated_date
    )
    SELECT
        public.uuid_generate_v4(),
        grp.roleid,
        grp.actionid,
        newOrgId,
        grp.has_permission,
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    FROM auth.global_role_permissions grp
    INNER JOIN auth.roles r
        ON grp.roleid = r.roleid
    WHERE r.organizationid = newOrgId
    ON CONFLICT DO NOTHING;

    GET DIAGNOSTICS insertedPermissionsCount = ROW_COUNT;

    -- Create default payment subscription
    INSERT INTO payment.companysubscriptions (
        companyid,
        planid,
        status,
        billingcycle,
        startdate,
        enddate
    )
    VALUES (
        newOrgId,
        '620212ec-cf42-4365-a4c1-f9c92c4250b9',
        'active',
        'monthly',
        now(),
        now() + interval '1 month'
    );

    RAISE NOTICE 'Organization % created. Roles: %. Permissions: %.', newOrgId, insertedRolesCount, insertedPermissionsCount;

    RETURN newOrgId;
END;
$$;

COMMIT;

-- ============================================================
-- VERIFICATION QUERIES (uncomment and run to verify)
-- ============================================================

-- Check permissions per organization
-- SELECT o.name, COUNT(p.permissionid) AS permission_count
-- FROM auth.organizations o
-- LEFT JOIN auth.permissions p ON p.organizationid = o.organizationid
-- GROUP BY o.name ORDER BY o.name;

-- Check permissions per role per org
-- SELECT r.rolename, COUNT(p.permissionid) AS total, SUM(CASE WHEN p.has_permission THEN 1 ELSE 0 END) AS granted
-- FROM auth.roles r
-- LEFT JOIN auth.permissions p ON p.roleid = r.roleid AND p.organizationid = r.organizationid
-- WHERE r.organizationid = '<your-org-id-here>'
-- GROUP BY r.rolename ORDER BY r.rolename;
