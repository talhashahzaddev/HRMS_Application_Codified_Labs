-- ============================================================
-- STEP 1: CREATE BOTH TRIGGER FUNCTIONS FIRST
-- IMPORTANT: Functions must be defined BEFORE any triggers reference them.
-- ============================================================


-- ============================================================
-- FUNCTION 1: Generic UUID primary key handler
-- Used for all tables whose primary key is of type UUID.
-- ============================================================
CREATE OR REPLACE FUNCTION admin.fn_audit_trigger()
RETURNS TRIGGER AS $$
DECLARE
    v_user_id    UUID;
    v_org_id     UUID;
    v_useragent  TEXT;
    v_old_values JSONB;
    v_new_values JSONB;
    v_pk_column  TEXT;
    v_record_id  UUID;
BEGIN
    v_pk_column := TG_ARGV[0];

    -- Read User ID injected by C# DatabaseConnectionFactory
    BEGIN
        v_user_id := current_setting('hrms.current_user_id', true)::UUID;
    EXCEPTION WHEN OTHERS THEN
        v_user_id := NULL;
    END;

    -- Read Organization ID injected by C# DatabaseConnectionFactory
    BEGIN
        v_org_id := current_setting('hrms.current_org_id', true)::UUID;
    EXCEPTION WHEN OTHERS THEN
        v_org_id := NULL;
    END;

    -- Read User-Agent injected by C# DatabaseConnectionFactory
    BEGIN
        v_useragent := current_setting('hrms.current_useragent', true);
        IF v_useragent = '' THEN v_useragent := NULL; END IF;
    EXCEPTION WHEN OTHERS THEN
        v_useragent := NULL;
    END;

    IF (TG_OP = 'UPDATE') THEN
        v_old_values := to_jsonb(OLD);
        v_new_values := to_jsonb(NEW);
    ELSIF (TG_OP = 'DELETE') THEN
        v_old_values := to_jsonb(OLD);
        v_new_values := NULL;
    ELSIF (TG_OP = 'INSERT') THEN
        v_old_values := NULL;
        v_new_values := to_jsonb(NEW);
    END IF;

    IF v_new_values IS NOT NULL THEN
        v_record_id := (v_new_values ->> v_pk_column)::UUID;
    ELSE
        v_record_id := (v_old_values ->> v_pk_column)::UUID;
    END IF;

    INSERT INTO admin.auditlogs (
        organizationid,
        userid,
        tablename,
        recordid,
        action,
        oldvalues,
        newvalues,
        useragent
    )
    VALUES (
        v_org_id,
        v_user_id,
        TG_TABLE_SCHEMA || '.' || TG_TABLE_NAME,
        v_record_id,
        TG_OP,
        v_old_values,
        v_new_values,
        v_useragent
    );

    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;


-- ============================================================
-- FUNCTION 2: VARCHAR primary key handler
-- Used for tables whose primary key is varchar(36) instead of UUID.
-- Currently used by: assets.assets
-- ============================================================
CREATE OR REPLACE FUNCTION admin.fn_audit_trigger_varchar()
RETURNS TRIGGER AS $$
DECLARE
    v_user_id    UUID;
    v_org_id     UUID;
    v_useragent  TEXT;
    v_old_values JSONB;
    v_new_values JSONB;
    v_pk_column  TEXT;
    v_record_id  TEXT;
BEGIN
    v_pk_column := TG_ARGV[0];

    BEGIN
        v_user_id := current_setting('hrms.current_user_id', true)::UUID;
    EXCEPTION WHEN OTHERS THEN
        v_user_id := NULL;
    END;

    BEGIN
        v_org_id := current_setting('hrms.current_org_id', true)::UUID;
    EXCEPTION WHEN OTHERS THEN
        v_org_id := NULL;
    END;

    BEGIN
        v_useragent := current_setting('hrms.current_useragent', true);
        IF v_useragent = '' THEN v_useragent := NULL; END IF;
    EXCEPTION WHEN OTHERS THEN
        v_useragent := NULL;
    END;

    IF (TG_OP = 'UPDATE') THEN
        v_old_values := to_jsonb(OLD);
        v_new_values := to_jsonb(NEW);
    ELSIF (TG_OP = 'DELETE') THEN
        v_old_values := to_jsonb(OLD);
        v_new_values := NULL;
    ELSIF (TG_OP = 'INSERT') THEN
        v_old_values := NULL;
        v_new_values := to_jsonb(NEW);
    END IF;

    IF v_new_values IS NOT NULL THEN
        v_record_id := v_new_values ->> v_pk_column;
    ELSE
        v_record_id := v_old_values ->> v_pk_column;
    END IF;

    INSERT INTO admin.auditlogs (
        organizationid,
        userid,
        tablename,
        recordid,
        action,
        oldvalues,
        newvalues,
        useragent
    )
    VALUES (
        v_org_id,
        v_user_id,
        TG_TABLE_SCHEMA || '.' || TG_TABLE_NAME,
        CASE WHEN v_record_id ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
             THEN v_record_id::UUID
             ELSE NULL
        END,
        TG_OP,
        v_old_values,
        v_new_values,
        v_useragent
    );

    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;



-- ============================================================
-- STEP 2: ATTACH TRIGGERS TO ALL CRITICAL TABLES
-- Each trigger passes its specific Primary Key column name
-- as an argument so the function can extract the record ID.
-- ============================================================


-- ============================================================
-- SCHEMA: assets
-- ============================================================

-- assets.asset_assignments (PK: assignment_id)
DROP TRIGGER IF EXISTS trg_audit_asset_assignments ON assets.asset_assignments;
CREATE TRIGGER trg_audit_asset_assignments
    AFTER INSERT OR UPDATE OR DELETE ON assets.asset_assignments
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('assignment_id');

-- assets.assets (PK: id - varchar, so we cast carefully)
-- NOTE: assets.assets uses a varchar(36) PK, not UUID.
-- We handle this with a separate, simpler trigger function for varchar PKs.
DROP TRIGGER IF EXISTS trg_audit_assets ON assets.assets;
CREATE TRIGGER trg_audit_assets
    AFTER INSERT OR UPDATE OR DELETE ON assets.assets
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger_varchar('id');


-- ============================================================
-- SCHEMA: employee
-- ============================================================

-- employee.departments (PK: departmentid)
DROP TRIGGER IF EXISTS trg_audit_departments ON employee.departments;
CREATE TRIGGER trg_audit_departments
    AFTER INSERT OR UPDATE OR DELETE ON employee.departments
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('departmentid');

-- employee.employees (PK: employeeid)
DROP TRIGGER IF EXISTS trg_audit_employees ON employee.employees;
CREATE TRIGGER trg_audit_employees
    AFTER INSERT OR UPDATE OR DELETE ON employee.employees
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('employeeid');

-- employee.employmentdetails (PK: detailid)
DROP TRIGGER IF EXISTS trg_audit_employmentdetails ON employee.employmentdetails;
CREATE TRIGGER trg_audit_employmentdetails
    AFTER INSERT OR UPDATE OR DELETE ON employee.employmentdetails
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('detailid');

-- employee.positions (PK: positionid)
DROP TRIGGER IF EXISTS trg_audit_positions ON employee.positions;
CREATE TRIGGER trg_audit_positions
    AFTER INSERT OR UPDATE OR DELETE ON employee.positions
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('positionid');


-- ============================================================
-- SCHEMA: expense
-- ============================================================

-- expense.expenses (PK: expenseid)
DROP TRIGGER IF EXISTS trg_audit_expenses ON expense.expenses;
CREATE TRIGGER trg_audit_expenses
    AFTER INSERT OR UPDATE OR DELETE ON expense.expenses
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('expenseid');

-- expense.recurringexpenses (PK: recurringexpenseid)
DROP TRIGGER IF EXISTS trg_audit_recurringexpenses ON expense.recurringexpenses;
CREATE TRIGGER trg_audit_recurringexpenses
    AFTER INSERT OR UPDATE OR DELETE ON expense.recurringexpenses
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('recurringexpenseid');


-- ============================================================
-- SCHEMA: attendance
-- ============================================================

-- attendance.attendance (PK: attendanceid) — M21: Core attendance table was missing audit trigger
DROP TRIGGER IF EXISTS trg_audit_attendance ON attendance.attendance;
CREATE TRIGGER trg_audit_attendance
    AFTER INSERT OR UPDATE OR DELETE ON attendance.attendance
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('attendanceid');

-- attendance.attendancesessions (PK: sessionid)
DROP TRIGGER IF EXISTS trg_audit_attendancesessions ON attendance.attendancesessions;
CREATE TRIGGER trg_audit_attendancesessions
    AFTER INSERT OR UPDATE OR DELETE ON attendance.attendancesessions
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('sessionid');

-- attendance.attendance_update_requests (PK: request_id)
DROP TRIGGER IF EXISTS trg_audit_attendance_update_requests ON attendance.attendance_update_requests;
CREATE TRIGGER trg_audit_attendance_update_requests
    AFTER INSERT OR UPDATE OR DELETE ON attendance.attendance_update_requests
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('request_id');

-- attendance.company_holidays (PK: holidayid)
DROP TRIGGER IF EXISTS trg_audit_company_holidays ON attendance.company_holidays;
CREATE TRIGGER trg_audit_company_holidays
    AFTER INSERT OR UPDATE OR DELETE ON attendance.company_holidays
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('holidayid');

-- attendance.employee_overtime_requests (PK: requestid)
DROP TRIGGER IF EXISTS trg_audit_employee_overtime_requests ON attendance.employee_overtime_requests;
CREATE TRIGGER trg_audit_employee_overtime_requests
    AFTER INSERT OR UPDATE OR DELETE ON attendance.employee_overtime_requests
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('requestid');

-- attendance.employee_restricted_holidays (PK: id)
DROP TRIGGER IF EXISTS trg_audit_employee_restricted_holidays ON attendance.employee_restricted_holidays;
CREATE TRIGGER trg_audit_employee_restricted_holidays
    AFTER INSERT OR UPDATE OR DELETE ON attendance.employee_restricted_holidays
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('id');

-- attendance.geo_fence_violations (PK: violationid)
DROP TRIGGER IF EXISTS trg_audit_geo_fence_violations ON attendance.geo_fence_violations;
CREATE TRIGGER trg_audit_geo_fence_violations
    AFTER INSERT OR UPDATE OR DELETE ON attendance.geo_fence_violations
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('violationid');

-- attendance.geo_fences (PK: geofenceid)
DROP TRIGGER IF EXISTS trg_audit_geo_fences ON attendance.geo_fences;
CREATE TRIGGER trg_audit_geo_fences
    AFTER INSERT OR UPDATE OR DELETE ON attendance.geo_fences
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('geofenceid');

-- attendance.face_recognition_logs (PK: logid)
DROP TRIGGER IF EXISTS trg_audit_face_recognition_logs ON attendance.face_recognition_logs;
CREATE TRIGGER trg_audit_face_recognition_logs
    AFTER INSERT OR UPDATE OR DELETE ON attendance.face_recognition_logs
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('logid');

-- attendance.finalized_timesheet_records (PK: record_id)
DROP TRIGGER IF EXISTS trg_audit_finalized_timesheet_records ON attendance.finalized_timesheet_records;
CREATE TRIGGER trg_audit_finalized_timesheet_records
    AFTER INSERT OR UPDATE OR DELETE ON attendance.finalized_timesheet_records
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('record_id');

-- attendance.holiday_employee_assignments (PK: id)
DROP TRIGGER IF EXISTS trg_audit_holiday_employee_assignments ON attendance.holiday_employee_assignments;
CREATE TRIGGER trg_audit_holiday_employee_assignments
    AFTER INSERT OR UPDATE OR DELETE ON attendance.holiday_employee_assignments
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('id');

-- attendance.leaveentitlements (PK: entitlementid)
DROP TRIGGER IF EXISTS trg_audit_leaveentitlements ON attendance.leaveentitlements;
CREATE TRIGGER trg_audit_leaveentitlements
    AFTER INSERT OR UPDATE OR DELETE ON attendance.leaveentitlements
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('entitlementid');

-- attendance.leaverequests (PK: requestid)
DROP TRIGGER IF EXISTS trg_audit_leaverequests ON attendance.leaverequests;
CREATE TRIGGER trg_audit_leaverequests
    AFTER INSERT OR UPDATE OR DELETE ON attendance.leaverequests
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('requestid');

-- attendance.leavetypes (PK: leavetypeid)
DROP TRIGGER IF EXISTS trg_audit_leavetypes ON attendance.leavetypes;
CREATE TRIGGER trg_audit_leavetypes
    AFTER INSERT OR UPDATE OR DELETE ON attendance.leavetypes
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('leavetypeid');

-- attendance.monthly_timesheets (PK: timesheet_id)
DROP TRIGGER IF EXISTS trg_audit_monthly_timesheets ON attendance.monthly_timesheets;
CREATE TRIGGER trg_audit_monthly_timesheets
    AFTER INSERT OR UPDATE OR DELETE ON attendance.monthly_timesheets
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('timesheet_id');

-- attendance.overtime (PK: overtimeid)
DROP TRIGGER IF EXISTS trg_audit_overtime ON attendance.overtime;
CREATE TRIGGER trg_audit_overtime
    AFTER INSERT OR UPDATE OR DELETE ON attendance.overtime
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('overtimeid');

-- attendance.shifts (PK: shiftid)
DROP TRIGGER IF EXISTS trg_audit_shifts ON attendance.shifts;
CREATE TRIGGER trg_audit_shifts
    AFTER INSERT OR UPDATE OR DELETE ON attendance.shifts
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('shiftid');

-- attendance.shiftswaprequests (PK: requestid)
DROP TRIGGER IF EXISTS trg_audit_shiftswaprequests ON attendance.shiftswaprequests;
CREATE TRIGGER trg_audit_shiftswaprequests
    AFTER INSERT OR UPDATE OR DELETE ON attendance.shiftswaprequests
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('requestid');

-- attendance.timesheet_payroll_summary (PK: summary_id)
DROP TRIGGER IF EXISTS trg_audit_timesheet_payroll_summary ON attendance.timesheet_payroll_summary;
CREATE TRIGGER trg_audit_timesheet_payroll_summary
    AFTER INSERT OR UPDATE OR DELETE ON attendance.timesheet_payroll_summary
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('summary_id');


-- ============================================================
-- SCHEMA: helpdesk
-- ============================================================

-- helpdesk.ticket_assignments (PK: assignmentid)
DROP TRIGGER IF EXISTS trg_audit_ticket_assignments ON helpdesk.ticket_assignments;
CREATE TRIGGER trg_audit_ticket_assignments
    AFTER INSERT OR UPDATE OR DELETE ON helpdesk.ticket_assignments
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('assignmentid');

-- helpdesk.tickets (PK: ticketid)
DROP TRIGGER IF EXISTS trg_audit_tickets ON helpdesk.tickets;
CREATE TRIGGER trg_audit_tickets
    AFTER INSERT OR UPDATE OR DELETE ON helpdesk.tickets
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('ticketid');


-- ============================================================
-- SCHEMA: jobs (ATS)
-- ============================================================

-- jobs.applicationstages (PK: applicationstageid)
DROP TRIGGER IF EXISTS trg_audit_applicationstages ON jobs.applicationstages;
CREATE TRIGGER trg_audit_applicationstages
    AFTER INSERT OR UPDATE OR DELETE ON jobs.applicationstages
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('applicationstageid');

-- jobs.jobopenings (PK: jobid)
DROP TRIGGER IF EXISTS trg_audit_jobopenings ON jobs.jobopenings;
CREATE TRIGGER trg_audit_jobopenings
    AFTER INSERT OR UPDATE OR DELETE ON jobs.jobopenings
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('jobid');


-- ============================================================
-- SCHEMA: news
-- ============================================================

-- news.news (PK: newsid)
DROP TRIGGER IF EXISTS trg_audit_news ON news.news;
CREATE TRIGGER trg_audit_news
    AFTER INSERT OR UPDATE OR DELETE ON news.news
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('newsid');


-- ============================================================
-- SCHEMA: onboarding
-- ============================================================

-- onboarding.field_configurations (PK: configid)
DROP TRIGGER IF EXISTS trg_audit_field_configurations ON onboarding.field_configurations;
CREATE TRIGGER trg_audit_field_configurations
    AFTER INSERT OR UPDATE OR DELETE ON onboarding.field_configurations
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('configid');


-- ============================================================
-- SCHEMA: payment
-- ============================================================

-- payment.companysubscriptions (PK: subscriptionid)
DROP TRIGGER IF EXISTS trg_audit_companysubscriptions ON payment.companysubscriptions;
CREATE TRIGGER trg_audit_companysubscriptions
    AFTER INSERT OR UPDATE OR DELETE ON payment.companysubscriptions
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('subscriptionid');

-- payment.subscriptionplans (PK: planid)
DROP TRIGGER IF EXISTS trg_audit_subscriptionplans ON payment.subscriptionplans;
CREATE TRIGGER trg_audit_subscriptionplans
    AFTER INSERT OR UPDATE OR DELETE ON payment.subscriptionplans
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('planid');

-- payment.transactions (PK: transaction_id)
DROP TRIGGER IF EXISTS trg_audit_transactions ON payment.transactions;
CREATE TRIGGER trg_audit_transactions
    AFTER INSERT OR UPDATE OR DELETE ON payment.transactions
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('transaction_id');


-- ============================================================
-- SCHEMA: performance
-- ============================================================

-- performance.appraisal_cycle (PK: cycleid)
DROP TRIGGER IF EXISTS trg_audit_appraisal_cycle ON performance.appraisal_cycle;
CREATE TRIGGER trg_audit_appraisal_cycle
    AFTER INSERT OR UPDATE OR DELETE ON performance.appraisal_cycle
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('cycleid');

-- performance.employeeappraisals (PK: appraisalid)
DROP TRIGGER IF EXISTS trg_audit_employeeappraisals ON performance.employeeappraisals;
CREATE TRIGGER trg_audit_employeeappraisals
    AFTER INSERT OR UPDATE OR DELETE ON performance.employeeappraisals
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('appraisalid');

-- performance.employeeskills (PK: employeeskillid)
DROP TRIGGER IF EXISTS trg_audit_employeeskills ON performance.employeeskills;
CREATE TRIGGER trg_audit_employeeskills
    AFTER INSERT OR UPDATE OR DELETE ON performance.employeeskills
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('employeeskillid');

-- performance.hrappraisals (PK: hrreviewid)
DROP TRIGGER IF EXISTS trg_audit_hrappraisals ON performance.hrappraisals;
CREATE TRIGGER trg_audit_hrappraisals
    AFTER INSERT OR UPDATE OR DELETE ON performance.hrappraisals
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('hrreviewid');

-- performance.managerreviews (PK: managerreviewid)
DROP TRIGGER IF EXISTS trg_audit_managerreviews ON performance.managerreviews;
CREATE TRIGGER trg_audit_managerreviews
    AFTER INSERT OR UPDATE OR DELETE ON performance.managerreviews
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('managerreviewid');


-- ============================================================
-- SCHEMA: payroll — Rule & Policy Tables
-- ============================================================

-- payroll.overtimerules (PK: ruleid)
DROP TRIGGER IF EXISTS trg_audit_overtimerules ON payroll.overtimerules;
CREATE TRIGGER trg_audit_overtimerules
    AFTER INSERT OR UPDATE OR DELETE ON payroll.overtimerules
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('ruleid');

-- payroll.attendancedeductionrules (PK: ruleid)
DROP TRIGGER IF EXISTS trg_audit_attendancedeductionrules ON payroll.attendancedeductionrules;
CREATE TRIGGER trg_audit_attendancedeductionrules
    AFTER INSERT OR UPDATE OR DELETE ON payroll.attendancedeductionrules
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('ruleid');

-- payroll.lateattendancerules (PK: ruleid)
DROP TRIGGER IF EXISTS trg_audit_lateattendancerules ON payroll.lateattendancerules;
CREATE TRIGGER trg_audit_lateattendancerules
    AFTER INSERT OR UPDATE OR DELETE ON payroll.lateattendancerules
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('ruleid');

-- payroll.leaverules (PK: ruleid)
DROP TRIGGER IF EXISTS trg_audit_leaverules ON payroll.leaverules;
CREATE TRIGGER trg_audit_leaverules
    AFTER INSERT OR UPDATE OR DELETE ON payroll.leaverules
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('ruleid');

-- payroll.performancerules (PK: ruleid)
DROP TRIGGER IF EXISTS trg_audit_performancerules ON payroll.performancerules;
CREATE TRIGGER trg_audit_performancerules
    AFTER INSERT OR UPDATE OR DELETE ON payroll.performancerules
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('ruleid');

-- payroll.bonusrules (PK: ruleid)
DROP TRIGGER IF EXISTS trg_audit_bonusrules ON payroll.bonusrules;
CREATE TRIGGER trg_audit_bonusrules
    AFTER INSERT OR UPDATE OR DELETE ON payroll.bonusrules
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('ruleid');

-- payroll.loanrules (PK: ruleid)
DROP TRIGGER IF EXISTS trg_audit_loanrules ON payroll.loanrules;
CREATE TRIGGER trg_audit_loanrules
    AFTER INSERT OR UPDATE OR DELETE ON payroll.loanrules
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('ruleid');

-- payroll.salaryadvancerules (PK: ruleid)
DROP TRIGGER IF EXISTS trg_audit_salaryadvancerules ON payroll.salaryadvancerules;
CREATE TRIGGER trg_audit_salaryadvancerules
    AFTER INSERT OR UPDATE OR DELETE ON payroll.salaryadvancerules
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('ruleid');

-- payroll.pfrules (PK: ruleid)
DROP TRIGGER IF EXISTS trg_audit_pfrules ON payroll.pfrules;
CREATE TRIGGER trg_audit_pfrules
    AFTER INSERT OR UPDATE OR DELETE ON payroll.pfrules
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('ruleid');

-- payroll.gratuityconfig (PK: id)
DROP TRIGGER IF EXISTS trg_audit_gratuityconfig ON payroll.gratuityconfig;
CREATE TRIGGER trg_audit_gratuityconfig
    AFTER INSERT OR UPDATE OR DELETE ON payroll.gratuityconfig
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('id');

-- payroll.taxregimes (PK: regimeid)
DROP TRIGGER IF EXISTS trg_audit_taxregimes ON payroll.taxregimes;
CREATE TRIGGER trg_audit_taxregimes
    AFTER INSERT OR UPDATE OR DELETE ON payroll.taxregimes
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('regimeid');

-- payroll.taxcategories (PK: categoryid)
DROP TRIGGER IF EXISTS trg_audit_taxcategories ON payroll.taxcategories;
CREATE TRIGGER trg_audit_taxcategories
    AFTER INSERT OR UPDATE OR DELETE ON payroll.taxcategories
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('categoryid');

-- payroll.taxslabs (PK: slabid)
DROP TRIGGER IF EXISTS trg_audit_taxslabs ON payroll.taxslabs;
CREATE TRIGGER trg_audit_taxslabs
    AFTER INSERT OR UPDATE OR DELETE ON payroll.taxslabs
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('slabid');

-- payroll.taxcomponentrules (PK: componentruleid)
DROP TRIGGER IF EXISTS trg_audit_taxcomponentrules ON payroll.taxcomponentrules;
CREATE TRIGGER trg_audit_taxcomponentrules
    AFTER INSERT OR UPDATE OR DELETE ON payroll.taxcomponentrules
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('componentruleid');

-- payroll.socialsecurityrules (PK: ruleid)
DROP TRIGGER IF EXISTS trg_audit_socialsecurityrules ON payroll.socialsecurityrules;
CREATE TRIGGER trg_audit_socialsecurityrules
    AFTER INSERT OR UPDATE OR DELETE ON payroll.socialsecurityrules
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('ruleid');


-- ============================================================
-- SCHEMA: payroll — Requests & Approval Workflows
-- ============================================================

-- payroll.loans (PK: loanid)
DROP TRIGGER IF EXISTS trg_audit_loans ON payroll.loans;
CREATE TRIGGER trg_audit_loans
    AFTER INSERT OR UPDATE OR DELETE ON payroll.loans
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('loanid');

-- payroll.salaryadvance (PK: advanceid)
DROP TRIGGER IF EXISTS trg_audit_salaryadvance ON payroll.salaryadvance;
CREATE TRIGGER trg_audit_salaryadvance
    AFTER INSERT OR UPDATE OR DELETE ON payroll.salaryadvance
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('advanceid');

-- payroll.pfrequests (PK: requestid)
DROP TRIGGER IF EXISTS trg_audit_pfrequests ON payroll.pfrequests;
CREATE TRIGGER trg_audit_pfrequests
    AFTER INSERT OR UPDATE OR DELETE ON payroll.pfrequests
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('requestid');

-- payroll.socialsecurityrequests (PK: requestid)
DROP TRIGGER IF EXISTS trg_audit_socialsecurityrequests ON payroll.socialsecurityrequests;
CREATE TRIGGER trg_audit_socialsecurityrequests
    AFTER INSERT OR UPDATE OR DELETE ON payroll.socialsecurityrequests
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('requestid');

-- payroll.socialsecurityclaims (PK: claimid)
DROP TRIGGER IF EXISTS trg_audit_socialsecurityclaims ON payroll.socialsecurityclaims;
CREATE TRIGGER trg_audit_socialsecurityclaims
    AFTER INSERT OR UPDATE OR DELETE ON payroll.socialsecurityclaims
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('claimid');


-- ============================================================
-- SCHEMA: payroll — Financial Balances & Transactions
-- ============================================================

-- payroll.providentfund (PK: pfid)
DROP TRIGGER IF EXISTS trg_audit_providentfund ON payroll.providentfund;
CREATE TRIGGER trg_audit_providentfund
    AFTER INSERT OR UPDATE OR DELETE ON payroll.providentfund
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('pfid');

-- payroll.pftransactions (PK: transactionid)
DROP TRIGGER IF EXISTS trg_audit_pftransactions ON payroll.pftransactions;
CREATE TRIGGER trg_audit_pftransactions
    AFTER INSERT OR UPDATE OR DELETE ON payroll.pftransactions
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('transactionid');

-- payroll.loanpayments (PK: id)
DROP TRIGGER IF EXISTS trg_audit_loanpayments ON payroll.loanpayments;
CREATE TRIGGER trg_audit_loanpayments
    AFTER INSERT OR UPDATE OR DELETE ON payroll.loanpayments
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('id');

-- payroll.gratuitytransactions (PK: id)
DROP TRIGGER IF EXISTS trg_audit_gratuitytransactions ON payroll.gratuitytransactions;
CREATE TRIGGER trg_audit_gratuitytransactions
    AFTER INSERT OR UPDATE OR DELETE ON payroll.gratuitytransactions
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('id');

-- payroll.bonusentries (PK: bonusid)
DROP TRIGGER IF EXISTS trg_audit_bonusentries ON payroll.bonusentries;
CREATE TRIGGER trg_audit_bonusentries
    AFTER INSERT OR UPDATE OR DELETE ON payroll.bonusentries
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('bonusid');

-- payroll.performancepay (PK: performancepayid)
DROP TRIGGER IF EXISTS trg_audit_performancepay ON payroll.performancepay;
CREATE TRIGGER trg_audit_performancepay
    AFTER INSERT OR UPDATE OR DELETE ON payroll.performancepay
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('performancepayid');

-- payroll.payrollresults (PK: resultid)
DROP TRIGGER IF EXISTS trg_audit_payrollresults ON payroll.payrollresults;
CREATE TRIGGER trg_audit_payrollresults
    AFTER INSERT OR UPDATE OR DELETE ON payroll.payrollresults
    FOR EACH ROW EXECUTE FUNCTION admin.fn_audit_trigger('resultid');


-- ============================================================
-- SCHEMA: admin — M22: Make auditlogs immutable
-- UPDATE and DELETE on auditlogs are blocked to preserve audit trail integrity.
-- ============================================================

-- Block UPDATE on admin.auditlogs
CREATE OR REPLACE FUNCTION admin.fn_auditlogs_immutable()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'auditlogs table is immutable: % operations are not allowed', TG_OP
        USING ERRCODE = 'P0001';
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_auditlogs_immutable ON admin.auditlogs;
CREATE TRIGGER trg_auditlogs_immutable
    BEFORE UPDATE OR DELETE ON admin.auditlogs
    FOR EACH ROW EXECUTE FUNCTION admin.fn_auditlogs_immutable();


-- ============================================================
-- DONE.
-- Both functions are now defined before all triggers.
-- To verify, run: SELECT * FROM admin.auditlogs ORDER BY createdat DESC LIMIT 20;
-- ============================================================
