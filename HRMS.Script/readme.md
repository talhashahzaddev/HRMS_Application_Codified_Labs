# Plan feature entitlements

The entitlement migration is `07_plan_feature_entitlements.sql`; its rollback is
`07_plan_feature_entitlements_down.sql`. After applying migration 07, apply
`08_retire_unavailable_entitlements.sql` to deactivate unsupported LMS and air
ticket features; its rollback is `08_retire_unavailable_entitlements_down.sql`.
Apply `09_sync_email_verification_state.sql` as well to synchronize legacy
verified accounts and keep both verification flags in sync. Apply the migrations to the target
PostgreSQL database before deploying API code that reads the `entitlement`
schema. The migration is transactional and is intentionally not run by the
application at startup.

## Adding a feature key

1. Add the feature row to the `INSERT INTO entitlement.features` seed in the
   migration, using a stable key and the appropriate module key and type.
2. Add the feature to each plan's `plan_features` seed where it should be
   enabled. Add limit values only for `type = 'limit'`.
3. If the feature maps to an API controller, add its controller and any
   separately gated sub-feature mapping to
   `HRMS.API/Middleware/TenantEntitlementMiddleware.cs`.
4. If the tenant UI has a route or navigation item for the feature, add the
   feature key to its route metadata and sidebar mapping.
5. Add or update tests for base-plan access, tenant overrides, and denied
   requests before deploying the migration.

Feature keys are the contract shared by the database catalog, entitlement API,
route guards, and server-side enforcement. Do not hardcode plan names in
feature checks; use feature keys and the effective entitlement response.

Subscription states `expired` and `cancelled` are read-only. `past_due` remains
writable because the current deployment has no agreed grace-period policy;
define that policy before changing this behavior.

/api/dashboard/summary
Referrer

/api/dashboard/attendance-stats?period=month

/api/dashboard/leave-stats?period=month

/api/dashboard/payroll-stats?period=month

/api/dashboard/performance-stats

/api/dashboard/recent-activities?limit=10

/api/dashboard/employee-growth?period=12m

api/dashboard/department-stats

/api/dashboard/upcoming-events?limit=5

/api/dashboard/summary

/api/dashboard/attendance-stats?period=month

/api/dashboard/leave-stats?period=month

/api/dashboard/payroll-stats?period=month

/api/dashboard/performance-stats

/api/dashboard/recent-activities?limit=10

/api/dashboard/employee-growth?period=12m

/api/dashboard/department-stats

/api/dashboard/upcoming-events?limit=5

/api/dashboard/attendance-stats?period=month

/api/dashboard/leave-stats?period=month

/api/dashboard/payroll-stats?period=month

/api/dashboard/performance-stats

/api/dashboard/recent-activities?limit=10

/api/dashboard/employee-growth?period=12m

/api/dashboard/department-stats

/api/dashboard/upcoming-events?limit=5