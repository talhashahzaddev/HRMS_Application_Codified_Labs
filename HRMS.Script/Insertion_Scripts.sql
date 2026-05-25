INSERT INTO auth.actions (submenuid, actionname, actionkey, created_date, updated_date) VALUES

-- Employee Management (New)
('6d880c64-e0b6-4634-b3eb-bb549cce65bf', 'Add Employee', 'add_employee', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('6d880c64-e0b6-4634-b3eb-bb549cce65bf', 'View Employee Details', 'view_employee_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('6d880c64-e0b6-4634-b3eb-bb549cce65bf', 'Edit Employee', 'edit_employee', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('6d880c64-e0b6-4634-b3eb-bb549cce65bf', 'Activate/DeActivate Employee', 'activate/deactivate_employee', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('6d880c64-e0b6-4634-b3eb-bb549cce65bf', 'Delete Employee', 'delete_employee', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Department Management (New)
('49d59538-3e60-4716-9ecf-59103735f7a3', 'Add Department', 'add_department', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('49d59538-3e60-4716-9ecf-59103735f7a3', 'View Department Details', 'view_department_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('49d59538-3e60-4716-9ecf-59103735f7a3', 'Edit Department', 'edit_department', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('49d59538-3e60-4716-9ecf-59103735f7a3', 'View Department Employees', 'view_department_emloyees', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('49d59538-3e60-4716-9ecf-59103735f7a3', 'Deactivate/Activate Department', 'deactivat/activate_department', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('49d59538-3e60-4716-9ecf-59103735f7a3', 'Delete Department', 'delete_department', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Position Management (New)
('5f19ffa6-cc28-4c3e-b73a-b99ba640431f', 'Add Position', 'add_position', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('5f19ffa6-cc28-4c3e-b73a-b99ba640431f', 'View Position Details', 'view_position_detaisl', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('5f19ffa6-cc28-4c3e-b73a-b99ba640431f', 'View Position Employees', 'view_position_employees', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('5f19ffa6-cc28-4c3e-b73a-b99ba640431f', 'Edit Position', 'edit_position', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('5f19ffa6-cc28-4c3e-b73a-b99ba640431f', 'Deactivate/Activate Position', 'deactivat/activate_position', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Attendance & Time Tracking
('860e3439-142f-445b-83f7-c894dc82e5dc', 'Attendance Summary', 'my_attendance_summary', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('fd7e332b-43c3-431e-8734-03b6a36c0469', 'Clock In', 'time_tracket_clock_in', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('fd7e332b-43c3-431e-8734-03b6a36c0469', 'Clock Out', 'time_tracket_clock_out', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('12fa1694-ab9a-4ed1-b57c-d88236c38969', 'View Attendance Details', 'team_attendance_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('12fa1694-ab9a-4ed1-b57c-d88236c38969', 'Edit Attendance Record', 'team_attendance_edit_record', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('12fa1694-ab9a-4ed1-b57c-d88236c38969', 'Export Attendance Records', 'team_attendance_export', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Timesheet
('ca06162c-1a55-45fe-bd96-fa7306368fae', 'Create New Snapshot', 'timesheet_create_new_snapshot', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ca06162c-1a55-45fe-bd96-fa7306368fae', 'View TimeSheet Details', 'timesheet_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ca06162c-1a55-45fe-bd96-fa7306368fae', 'Edit Or Over Ride Attendance Recored', 'edit_or_override_attendace_recored', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ca06162c-1a55-45fe-bd96-fa7306368fae', 'Finalize Timesheet', 'finalize_timesheet', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Timesheet Dashboard
('7f25c4f1-d4d8-4c3e-b170-3eb3bf6a7a2b', 'Finalize TimeSheet', 'timesheet_dashboard_finalize', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('7f25c4f1-d4d8-4c3e-b170-3eb3bf6a7a2b', 'View TimeSheet Details', 'timesheet_dashboard_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('7f25c4f1-d4d8-4c3e-b170-3eb3bf6a7a2b', 'Edit Timesheet Records', 'edit_timesheet_records_in_dashboard', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('7f25c4f1-d4d8-4c3e-b170-3eb3bf6a7a2b', 'Approve Updated Time Sheet', 'approve_updated_timesheet', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Reports
('08e17d63-4516-44a6-acb4-617502fa09a1', 'Genrate Attendance Report', 'reports_genrate_report', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('08e17d63-4516-44a6-acb4-617502fa09a1', 'Export Attendance Report', 'reports_export', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Shifts
('1044298b-9f4e-49ec-b027-086e0a97fd5d', 'Create Shift', 'shifts_create_shift', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('1044298b-9f4e-49ec-b027-086e0a97fd5d', 'Assign Shift', 'shifts_assign_shift', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('1044298b-9f4e-49ec-b027-086e0a97fd5d', 'Swap Shift', 'shifts_swap_shift', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('1044298b-9f4e-49ec-b027-086e0a97fd5d', 'Edit Shift Details', 'shifts_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('1044298b-9f4e-49ec-b027-086e0a97fd5d', 'Delete Shift', 'shifts_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('1044298b-9f4e-49ec-b027-086e0a97fd5d', 'All Shifts', 'all_shifts', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('1044298b-9f4e-49ec-b027-086e0a97fd5d', 'Team Swap Requests', 'shift_team_swap_request', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Leave Management - My Leave
('ecdaac92-0b77-4ece-b852-a0d4494b14eb', 'Request Leave', 'my_leave_request_leave', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ecdaac92-0b77-4ece-b852-a0d4494b14eb', 'Edit Leave Request', 'my_leave_edit_request', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ecdaac92-0b77-4ece-b852-a0d4494b14eb', 'Cancel Leave Request', 'my_leave_cancel_request', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ecdaac92-0b77-4ece-b852-a0d4494b14eb', 'Leave Request Details', 'my_leave_request_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Team Leave
('76443fd8-9b25-40fe-856e-7c8c3c65ca32', 'Approve Leave Request', 'team_leave_approve', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('76443fd8-9b25-40fe-856e-7c8c3c65ca32', 'Reject Leave Request', 'team_leave_reject', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('76443fd8-9b25-40fe-856e-7c8c3c65ca32', 'View Leave Request Details', 'team_leave_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Team Requests
('484c6137-bb81-4fa7-bf9f-06bf74c95c50', 'Pending Approvals', 'team_request_pending_approvals', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('484c6137-bb81-4fa7-bf9f-06bf74c95c50', 'Team Leave Balance', 'team_request_team_leave_balance', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Leave Type
('2d841995-f507-44d8-967c-5e34a15af082', 'Add Leave Type', 'leave_type_add_leave_type', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2d841995-f507-44d8-967c-5e34a15af082', 'Edit Leave Type', 'leave_type_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Payroll Period
('bb68cf35-cc3b-4c78-90e0-84731598ad3a', 'New Payroll Period', 'payroll_period_new', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('bb68cf35-cc3b-4c78-90e0-84731598ad3a', 'Edit Payroll Period', 'payroll_period_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('bb68cf35-cc3b-4c78-90e0-84731598ad3a', 'Process Payroll', 'payroll_period_process_payroll', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('bb68cf35-cc3b-4c78-90e0-84731598ad3a', 'Export Payroll Report', 'payroll_period_export_report', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('bb68cf35-cc3b-4c78-90e0-84731598ad3a', 'Delete Payroll Period', 'payroll_period_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Process Payroll
('ff078600-5218-4362-ae40-ddfa54b215d7', 'Peocess Payroll', 'process_payroll', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Salary Component
('9014b4cf-c3f3-4cd0-8cef-74b12245a050', 'Create Salary Component', 'salary_component_create', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('9014b4cf-c3f3-4cd0-8cef-74b12245a050', 'Edit Salary Component', 'salary_component_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('9014b4cf-c3f3-4cd0-8cef-74b12245a050', 'Delete Salary Component', 'salary_component_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('9014b4cf-c3f3-4cd0-8cef-74b12245a050', 'Create Salary Rule', 'salary_component_create_rule', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('9014b4cf-c3f3-4cd0-8cef-74b12245a050', 'Edit Salary Rule', 'salary_component_edit_rule', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('9014b4cf-c3f3-4cd0-8cef-74b12245a050', 'Delete Salary Rule', 'salary_component_delete_rule', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Payroll Report
('2ceab223-2793-4317-9573-3e8060a00296', 'Genrate Payroll Report', 'payroll_report_genrate', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2ceab223-2793-4317-9573-3e8060a00296', 'Export Payroll Report', 'payroll_report_export', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Salary Slip
('c2546b28-1869-4561-b7b5-5da9634722ec', 'Perview Payslip', 'salary_slip_perview_payslip', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c2546b28-1869-4561-b7b5-5da9634722ec', 'Download Payslip', 'salary_slip_download_pdf', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c2546b28-1869-4561-b7b5-5da9634722ec', 'Email Payslip', 'salary_slip_emial_payslip', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c2546b28-1869-4561-b7b5-5da9634722ec', 'Regenerate Payslip', 'salary_slip_regenerate', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Asset Type
('87b82790-acc6-480c-bbb7-751de9173ad5', 'Create Asset Type', 'type_of_asset_Craete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('87b82790-acc6-480c-bbb7-751de9173ad5', 'Edit Asset Type', 'type_of_asset_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('87b82790-acc6-480c-bbb7-751de9173ad5', 'Delete Asset Type', 'type_of_asset_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Assets
('7912df0b-2767-45f4-81fe-a36ffd321bd4', 'Create Asset', 'assets_create', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('7912df0b-2767-45f4-81fe-a36ffd321bd4', 'Asset History', 'assets_history', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('7912df0b-2767-45f4-81fe-a36ffd321bd4', 'View Asset Details', 'assets_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('7912df0b-2767-45f4-81fe-a36ffd321bd4', 'Edit Asset', 'assets_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('7912df0b-2767-45f4-81fe-a36ffd321bd4', 'Return Asset', 'assets_return', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('7912df0b-2767-45f4-81fe-a36ffd321bd4', 'Delete Asset', 'assets_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('7912df0b-2767-45f4-81fe-a36ffd321bd4', 'Assign Asset', 'assets_assign', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Appraisal
('df10d47b-3c24-4360-856e-02ea04120e39', 'New Appraisal Cycle', 'appraisal_cycle_new', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c05fb051-5398-4b48-96e6-f5c110f77731', 'Create Appriasal', 'appraisal_create_appriasal', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c05fb051-5398-4b48-96e6-f5c110f77731', 'Create Self Assessment', 'appraisal_self_assessment', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c05fb051-5398-4b48-96e6-f5c110f77731', 'View Appriasal Details', 'view_appraisal_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c05fb051-5398-4b48-96e6-f5c110f77731', 'Delete Appriasal', 'appraisal_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Skill Matrix
('c3921870-089a-4dfc-ba7d-0c2096e9d30c', 'Create Skill', 'skill_matrix_create', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c3921870-089a-4dfc-ba7d-0c2096e9d30c', 'Edit Skill', 'skill_matrix_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c3921870-089a-4dfc-ba7d-0c2096e9d30c', 'Activate/DeActivate Skill', 'skill_matrix_activate-deactivate', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Goals & KRAs
('e9745efe-8e5d-431e-9787-980703d80abb', 'Create New KRA', 'goals&kras_new', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('e9745efe-8e5d-431e-9787-980703d80abb', 'View KRA Details', 'goals&kras_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('e9745efe-8e5d-431e-9787-980703d80abb', 'Edit KRA', 'goals&kras_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('e9745efe-8e5d-431e-9787-980703d80abb', 'Activate/DeActivate KRA', 'goals&kras_activate_deactivate', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('e9745efe-8e5d-431e-9787-980703d80abb', 'Delete KRA', 'goals&kras_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Performance
('72abd7d9-1574-4fe3-a0d4-28e4757f15ed', 'Performance Reports', 'perfomance_reports', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Calendar
('84301bc7-83a1-4848-b4ea-57e7c2a6ccbd', 'Apply for Leave', 'calendar_apply_for_leave', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('84301bc7-83a1-4848-b4ea-57e7c2a6ccbd', 'Clock In', 'calendar_clock_in', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('84301bc7-83a1-4848-b4ea-57e7c2a6ccbd', 'Clock Out', 'calendar_clock_out', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('84301bc7-83a1-4848-b4ea-57e7c2a6ccbd', 'View Day Details', 'calandar_view_day_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- AI Assistant
('139a49b5-33c3-48eb-b075-80ec974c10d3', 'Ai Assistant', 'ai_assistant', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Billing
('53fbfdc0-cda9-4a1d-980d-02a7c834658a', 'Select Plan', 'select_plan', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('bb90c292-22c0-4d21-ab6f-d2bb021f9a81', 'Bill Transactions', 'billing_transactions', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('bb90c292-22c0-4d21-ab6f-d2bb021f9a81', 'Invoices', 'billing_invoices', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Expense Category
('6f4de72f-10e0-496c-b18c-a2a34071a82b', 'View Expense Categories', 'expense_category_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('6f4de72f-10e0-496c-b18c-a2a34071a82b', 'View Expense Category Details', 'expense_category_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('6f4de72f-10e0-496c-b18c-a2a34071a82b', 'Add Expense Category', 'expense_category_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('6f4de72f-10e0-496c-b18c-a2a34071a82b', 'Edit Expense Category', 'expense_category_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('6f4de72f-10e0-496c-b18c-a2a34071a82b', 'Delete Expense Category', 'expense_category_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Claims
('55632bd8-839d-4613-9829-58ca9c2c74de', 'Add Claim', 'claims_add_claims', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('55632bd8-839d-4613-9829-58ca9c2c74de', 'Upload Claim Receipt', 'claims_upload_receipt', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('55632bd8-839d-4613-9829-58ca9c2c74de', 'My Claims', 'my_claims', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('55632bd8-839d-4613-9829-58ca9c2c74de', 'All Claims', 'all_claims', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('55632bd8-839d-4613-9829-58ca9c2c74de', 'Claim Request Action', 'claims_request_action', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('55632bd8-839d-4613-9829-58ca9c2c74de', 'View Claim Details', 'claims_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('55632bd8-839d-4613-9829-58ca9c2c74de', 'Edit Claim', 'claims_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('55632bd8-839d-4613-9829-58ca9c2c74de', 'Delete Claim', 'claims_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Recurring Expenses
('27e569e1-ebe2-456c-87df-b50c608a9be6', 'Add Recurring Expenses', 'add_recurring', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('27e569e1-ebe2-456c-87df-b50c608a9be6', 'My Recurring Expenses', 'my_rescurring_expenses', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('27e569e1-ebe2-456c-87df-b50c608a9be6', 'All Recurring Expenses', 'all_recurring', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('27e569e1-ebe2-456c-87df-b50c608a9be6', 'Recurring Request Action', 'recurring_request_action', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('27e569e1-ebe2-456c-87df-b50c608a9be6', 'View Recurring Expense Details', 'recurring_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('27e569e1-ebe2-456c-87df-b50c608a9be6', 'Edit Recurring Expense', 'recurring_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('27e569e1-ebe2-456c-87df-b50c608a9be6', 'Delete Recurring Expense', 'recurring_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Expense Reports
('aab7ce10-8c22-4a68-a0d9-d5f290c7652e', 'Expense By Category', 'report_expense_by_category', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('aab7ce10-8c22-4a68-a0d9-d5f290c7652e', 'Expense by Month', 'report_expense_by_month', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('aab7ce10-8c22-4a68-a0d9-d5f290c7652e', 'Expense By Year', 'report_expense_by_year', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- News
('c2525de0-5c02-4902-aaaf-fa7541c44648', 'Create News', 'news_dashboard_create_news', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c2525de0-5c02-4902-aaaf-fa7541c44648', 'Edit News', 'news_dashboard_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c2525de0-5c02-4902-aaaf-fa7541c44648', 'Delete News', 'news_dashboard_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Job Openings
('ae4a8ee3-158c-4815-a771-e443923a7593', 'Create Job', 'opnings_create_job', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ae4a8ee3-158c-4815-a771-e443923a7593', 'View Job Details', 'opnings_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ae4a8ee3-158c-4815-a771-e443923a7593', 'Refer a Candidate', 'opnongs_refer', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ae4a8ee3-158c-4815-a771-e443923a7593', 'Edit Job', 'opnongs_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ae4a8ee3-158c-4815-a771-e443923a7593', 'Close Job', 'opnongs_close', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Job Applications
('ced9a8e1-b02b-4ca3-a3e7-dc48c2e477ed', 'My Referenced Application', 'my_referenced_application', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ced9a8e1-b02b-4ca3-a3e7-dc48c2e477ed', 'Received Application By My Job Post', 'received_application_by_my_job_post', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ced9a8e1-b02b-4ca3-a3e7-dc48c2e477ed', 'All Job Application', 'all_job_application', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ced9a8e1-b02b-4ca3-a3e7-dc48c2e477ed', 'View Refered Job Details', 'job_application_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ced9a8e1-b02b-4ca3-a3e7-dc48c2e477ed', 'Edit Refered Job', 'job_application_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ced9a8e1-b02b-4ca3-a3e7-dc48c2e477ed', 'Delete Refered Job', 'job_application_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Stages
('f4ad7d31-0118-4e55-a17e-f3d6e1d9fe8e', 'Add Stage', 'add_stage', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('f4ad7d31-0118-4e55-a17e-f3d6e1d9fe8e', 'Edit Stage', 'stage_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('f4ad7d31-0118-4e55-a17e-f3d6e1d9fe8e', 'Delete Stage', 'stage_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Company Settings
('9154a702-1fa5-4f83-936c-b9ba18360511', 'Localization Settings', 'company_settings_save_changes', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- IP Management
('70173e77-ddc5-45cf-8eba-58b53eb6ac5e', 'Add IP', 'manage_ip_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('70173e77-ddc5-45cf-8eba-58b53eb6ac5e', 'Edit IP', 'manage_ip_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('70173e77-ddc5-45cf-8eba-58b53eb6ac5e', 'Delete IP', 'manage_ip_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Career Management
('8504c4bd-38e2-4f65-bc3e-eccf0047e9e1', 'Career Mangment Page Edit', 'career_management_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Role Management
('857bcd02-42c2-43d4-b86a-d513dd13293a', 'Add New Role', 'add_new_role', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('857bcd02-42c2-43d4-b86a-d513dd13293a', 'View Role Details', 'role_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('857bcd02-42c2-43d4-b86a-d513dd13293a', 'Edit Role', 'role_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('857bcd02-42c2-43d4-b86a-d513dd13293a', 'Delete Role', 'role_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Payroll
-- Payroll Period
('19c46f08-8a36-4700-8df4-994ee3cc8a56', 'View Payroll Periods', 'payroll_period_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('19c46f08-8a36-4700-8df4-994ee3cc8a56', 'View Payroll Period Details', 'payroll_period_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('19c46f08-8a36-4700-8df4-994ee3cc8a56', 'Add Payroll Period', 'payroll_period_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('19c46f08-8a36-4700-8df4-994ee3cc8a56', 'Edit Payroll Period', 'payroll_period_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('19c46f08-8a36-4700-8df4-994ee3cc8a56', 'Delete Payroll Period', 'payroll_period_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Policies (SubMenuId: 2b3df626-459b-4435-acf8-a85ba23f6b7b)
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Payroll Rules', 'payroll_rules_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Overtime Rules
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Overtime Rules', 'overtime_rule_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Overtime Rule', 'overtime_rule_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Overtime Rule', 'overtime_rule_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Overtime Rule', 'overtime_rule_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Attendance Deduction Rules
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Attendance Deduction Rules', 'attendance_deduction_rule_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Attendance Deduction Rule', 'attendance_deduction_rule_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Attendance Deduction Rule', 'attendance_deduction_rule_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Attendance Deduction Rule', 'attendance_deduction_rule_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Late Arrival Rules
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Late Arrival Rules', 'late_arrival_rule_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Late Arrival Rule', 'late_arrival_rule_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Late Arrival Rule', 'late_arrival_rule_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Late Arrival Rule', 'late_arrival_rule_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Leave Deduction Rules
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Leave Deduction Rules', 'leave_deduction_rule_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Leave Deduction Rule', 'leave_deduction_rule_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Leave Deduction Rule', 'leave_deduction_rule_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Leave Deduction Rule', 'leave_deduction_rule_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Performance Rules
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Performance Rules', 'performance_rule_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Performance Rule', 'performance_rule_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Performance Rule', 'performance_rule_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Performance Rule', 'performance_rule_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Bonus Rules
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Bonus Rules', 'bonus_rule_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Bonus Rule', 'bonus_rule_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Bonus Rule', 'bonus_rule_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Bonus Rule', 'bonus_rule_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Loan Rules
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Loan Rules', 'loan_rule_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Loan Rule', 'loan_rule_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Loan Rule', 'loan_rule_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Loan Rule', 'loan_rule_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Salary Advance Rules
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Salary Advance Rules', 'salary_advance_rule_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Salary Advance Rule', 'salary_advance_rule_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Salary Advance Rule', 'salary_advance_rule_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Salary Advance Rule', 'salary_advance_rule_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Provident Fund Rules
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Provident Fund Rules', 'provident_fund_rule_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Provident Fund Rule', 'provident_fund_rule_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Provident Fund Rule', 'provident_fund_rule_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Provident Fund Rule', 'provident_fund_rule_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Tax Regimes
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Tax Regimes', 'tax_regime_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Tax Regime', 'tax_regime_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Tax Regime', 'tax_regime_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Tax Regime', 'tax_regime_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Tax Categories
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Tax Categories', 'tax_category_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Tax Category', 'tax_category_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Tax Category', 'tax_category_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Tax Category', 'tax_category_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Tax Slabs
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Tax Slabs', 'tax_slab_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Tax Slab', 'tax_slab_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Tax Slab', 'tax_slab_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Tax Slab', 'tax_slab_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Tax Component Rules / Tax Rules
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Tax Component Rules', 'tax_component_rule_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Tax Component Rule', 'tax_component_rule_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Tax Component Rule', 'tax_component_rule_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Tax Component Rule', 'tax_component_rule_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Gratuity Rules
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'View Gratuity Rules', 'gratuity_rule_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Add Gratuity Rule', 'gratuity_rule_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Edit Gratuity Rule', 'gratuity_rule_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('2b3df626-459b-4435-acf8-a85ba23f6b7b', 'Delete Gratuity Rule', 'gratuity_rule_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Bonus Pay (entries)
('4bbf6fcb-c7be-40ba-bb48-c208074d5b87', 'View Bonus Pay', 'bonus_entry_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('4bbf6fcb-c7be-40ba-bb48-c208074d5b87', 'Add Bonus Pay', 'bonus_entry_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('4bbf6fcb-c7be-40ba-bb48-c208074d5b87', 'Edit Bonus Pay', 'bonus_entry_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('4bbf6fcb-c7be-40ba-bb48-c208074d5b87', 'Delete Bonus Pay', 'bonus_entry_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Performance Pay (entries)
('caef7fa3-2ede-4bf2-85a1-72d1d8fc001f', 'View Performance Pay', 'performance_pay_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('caef7fa3-2ede-4bf2-85a1-72d1d8fc001f', 'Add Performance Pay', 'performance_pay_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('caef7fa3-2ede-4bf2-85a1-72d1d8fc001f', 'Edit Performance Pay', 'performance_pay_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('caef7fa3-2ede-4bf2-85a1-72d1d8fc001f', 'Delete Performance Pay', 'performance_pay_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Overtime (entries)
('c65b2d29-0090-4810-ac4f-6e38273532c4', 'View Overtime', 'overtime_entry_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c65b2d29-0090-4810-ac4f-6e38273532c4', 'Add Overtime', 'overtime_entry_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c65b2d29-0090-4810-ac4f-6e38273532c4', 'Edit Overtime', 'overtime_entry_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('c65b2d29-0090-4810-ac4f-6e38273532c4', 'Delete Overtime', 'overtime_entry_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Attendance Summary / Absences (entries)
('87327a28-6212-4467-815e-c9c3023fc0ac', 'View Absences', 'attendance_summary_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('87327a28-6212-4467-815e-c9c3023fc0ac', 'Add Absence', 'attendance_summary_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('87327a28-6212-4467-815e-c9c3023fc0ac', 'Edit Absence', 'attendance_summary_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('87327a28-6212-4467-815e-c9c3023fc0ac', 'Delete Absence', 'attendance_summary_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Late / Half-day Attendance (entries)
('22032ca4-cb23-40ce-bf58-9fe5a7c8f01d', 'View Late / Half-day', 'late_attendance_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('22032ca4-cb23-40ce-bf58-9fe5a7c8f01d', 'Add Late / Half-day', 'late_attendance_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('22032ca4-cb23-40ce-bf58-9fe5a7c8f01d', 'Edit Late / Half-day', 'late_attendance_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('22032ca4-cb23-40ce-bf58-9fe5a7c8f01d', 'Delete Late / Half-day', 'late_attendance_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Leave Summary (entries)
('b7bc02f5-bbfa-49bb-b7a8-2252b0a26785', 'View Leave Summary', 'leave_summary_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('b7bc02f5-bbfa-49bb-b7a8-2252b0a26785', 'Add Leave Summary', 'leave_summary_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('b7bc02f5-bbfa-49bb-b7a8-2252b0a26785', 'Edit Leave Summary', 'leave_summary_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('b7bc02f5-bbfa-49bb-b7a8-2252b0a26785', 'Delete Leave Summary', 'leave_summary_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Loan Admin
('4672c999-66a9-4920-8849-ab6b117d5104', 'View Loans Admin', 'loan_admin_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('4672c999-66a9-4920-8849-ab6b117d5104', 'Add Loan Admin', 'loan_admin_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('4672c999-66a9-4920-8849-ab6b117d5104', 'Edit Loan Admin', 'loan_admin_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('4672c999-66a9-4920-8849-ab6b117d5104', 'Delete Loan Admin', 'loan_admin_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Provident Fund Admin
('d6dad615-5f4b-40c1-9756-7610b9380efc', 'View Provident Fund Admin', 'pf_admin_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('d6dad615-5f4b-40c1-9756-7610b9380efc', 'Add Provident Fund Admin', 'pf_admin_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('d6dad615-5f4b-40c1-9756-7610b9380efc', 'Edit Provident Fund Admin', 'pf_admin_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('d6dad615-5f4b-40c1-9756-7610b9380efc', 'Delete Provident Fund Admin', 'pf_admin_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- My Payslips (employee self-service tab — single permission)
('257937b5-ee02-41cc-8ae3-2136447d54f1', 'My Payslips', 'my_payslip', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Compliance Payslips (admin management tab)
('257937b5-ee02-41cc-8ae3-2136447d54f1', 'View Compliance Payslips', 'compliance_payslip_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('257937b5-ee02-41cc-8ae3-2136447d54f1', 'Generate Payslips', 'payslip_generation', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('257937b5-ee02-41cc-8ae3-2136447d54f1', 'Mail Upload Payslips', 'mail_upload_payslip', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Gratuity Admin
('b9f1b555-f313-40b5-9773-13fd3b07d8c8', 'View Gratuity Admin', 'gratuity_admin_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('b9f1b555-f313-40b5-9773-13fd3b07d8c8', 'Add Gratuity Admin', 'gratuity_admin_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('b9f1b555-f313-40b5-9773-13fd3b07d8c8', 'Edit Gratuity Admin', 'gratuity_admin_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('b9f1b555-f313-40b5-9773-13fd3b07d8c8', 'Delete Gratuity Admin', 'gratuity_admin_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Payroll Calculation tab & Payroll Results tab
('dd68096e-c54f-454c-9bc0-ee65f3628460', 'Payroll Calculation', 'payroll_calculation', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('dd68096e-c54f-454c-9bc0-ee65f3628460', 'Payroll Results', 'payroll_result', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Salary Advance Admin
('05c4c176-1feb-45d6-a662-cace9283001a', 'List Salary Advance Admin', 'salary_advance_admin_list', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('05c4c176-1feb-45d6-a662-cace9283001a', 'View Salary Advance Admin', 'salary_advance_admin_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('05c4c176-1feb-45d6-a662-cace9283001a', 'Approve Salary Advance Admin', 'salary_advance_admin_approve', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('05c4c176-1feb-45d6-a662-cace9283001a', 'Reject Salary Advance Admin', 'salary_advance_admin_reject', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('05c4c176-1feb-45d6-a662-cace9283001a', 'Disburse Salary Advance Admin', 'salary_advance_admin_disburse', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- My Benefits (employee overview)
('0af0cd0c-c490-4389-bbfe-27e4b2cfb280', 'My Benefits', 'my_benefits', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Salary Advance Employee
('238f623e-c09e-4a66-b1c7-472485750aa0', 'Request Salary Advance', 'salary_advance_employee_request', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('238f623e-c09e-4a66-b1c7-472485750aa0', 'View Salary Advance', 'salary_advance_employee_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('238f623e-c09e-4a66-b1c7-472485750aa0', 'Edit Salary Advance', 'salary_advance_employee_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('238f623e-c09e-4a66-b1c7-472485750aa0', 'Cancel Salary Advance', 'salary_advance_employee_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('238f623e-c09e-4a66-b1c7-472485750aa0', 'List Salary Advances', 'salary_advance_employee_list', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('238f623e-c09e-4a66-b1c7-472485750aa0', 'Salary Advance Summary', 'salary_advance_employee_summary', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Provident Fund Employee
('8cd4e441-a4b5-4fa4-bcd1-f380f7d43b98', 'Enroll Provident Fund', 'pf_employee_enroll', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('8cd4e441-a4b5-4fa4-bcd1-f380f7d43b98', 'Edit PF Enrollment', 'pf_employee_edit_enroll', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('8cd4e441-a4b5-4fa4-bcd1-f380f7d43b98', 'Update PF Percentage', 'pf_employee_update_percentage', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('8cd4e441-a4b5-4fa4-bcd1-f380f7d43b98', 'Withdraw Provident Fund', 'pf_employee_withdraw', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('8cd4e441-a4b5-4fa4-bcd1-f380f7d43b98', 'Delete PF Request', 'pf_employee_delete_request', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('8cd4e441-a4b5-4fa4-bcd1-f380f7d43b98', 'List PF Pending Requests', 'pf_employee_pending_requests', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('8cd4e441-a4b5-4fa4-bcd1-f380f7d43b98', 'View PF Active Account', 'pf_employee_active', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('8cd4e441-a4b5-4fa4-bcd1-f380f7d43b98', 'PF Transaction History', 'pf_employee_transactions', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Gratuity Employee
('eb15b713-9c65-4751-a8fe-61d5aea10747', 'My Gratuity', 'gratuity_employee', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Loan Employee
('ebe95bf5-b19c-4d39-87b6-fd01ae2ddaaf', 'Request Loan', 'loan_employee_request', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ebe95bf5-b19c-4d39-87b6-fd01ae2ddaaf', 'View Loan', 'loan_employee_view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ebe95bf5-b19c-4d39-87b6-fd01ae2ddaaf', 'Edit Loan Request', 'loan_employee_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ebe95bf5-b19c-4d39-87b6-fd01ae2ddaaf', 'Cancel Loan Request', 'loan_employee_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ebe95bf5-b19c-4d39-87b6-fd01ae2ddaaf', 'My Active Loans', 'loan_employee_active', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ebe95bf5-b19c-4d39-87b6-fd01ae2ddaaf', 'My Pending Loans', 'loan_employee_pending', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ebe95bf5-b19c-4d39-87b6-fd01ae2ddaaf', 'Loan Payment History', 'loan_employee_history', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ebe95bf5-b19c-4d39-87b6-fd01ae2ddaaf', 'Loan References', 'loan_employee_references', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ebe95bf5-b19c-4d39-87b6-fd01ae2ddaaf', 'List My Loans', 'loan_employee_list', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- ============================================================
-- SEED DATA: Country Holiday Catalogs
-- ============================================================

-- India Public Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('IN', 'India', 'Republic Day',            '2025-01-26', true,  'Celebrates the adoption of the Constitution of India'),
('IN', 'India', 'Maha Shivaratri',         '2025-02-26', true,  'Hindu festival dedicated to Lord Shiva'),
('IN', 'India', 'Holi',                    '2025-03-14', true,  'Festival of colors'),
('IN', 'India', 'Good Friday',             '2025-04-18', true,  'Christian observance of the crucifixion of Jesus'),
('IN', 'India', 'Eid ul-Fitr',             '2025-03-31', false, 'Marks the end of Ramadan'),
('IN', 'India', 'Dr. Ambedkar Jayanti',    '2025-04-14', true,  'Birth anniversary of Dr. B.R. Ambedkar'),
('IN', 'India', 'Ram Navami',              '2025-04-06', true,  'Celebrates the birth of Lord Rama'),
('IN', 'India', 'Mahavir Jayanti',         '2025-04-10', true,  'Birth anniversary of Lord Mahavir'),
('IN', 'India', 'May Day',                 '2025-05-01', true,  'International Workers Day'),
('IN', 'India', 'Buddha Purnima',          '2025-05-12', true,  'Celebrates the birth of Gautama Buddha'),
('IN', 'India', 'Eid ul-Adha',             '2025-06-07', false, 'Festival of Sacrifice'),
('IN', 'India', 'Muharram',                '2025-07-06', false, 'Islamic New Year'),
('IN', 'India', 'Independence Day',        '2025-08-15', true,  'Celebrates India''s independence from British rule'),
('IN', 'India', 'Janmashtami',             '2025-08-16', true,  'Celebrates the birth of Lord Krishna'),
('IN', 'India', 'Milad un-Nabi',           '2025-09-05', false, 'Birthday of Prophet Muhammad'),
('IN', 'India', 'Mahatma Gandhi Jayanti',  '2025-10-02', true,  'Birth anniversary of Mahatma Gandhi'),
('IN', 'India', 'Dussehra',                '2025-10-02', true,  'Victory of good over evil'),
('IN', 'India', 'Diwali',                  '2025-10-20', true,  'Festival of lights'),
('IN', 'India', 'Guru Nanak Jayanti',      '2025-11-05', true,  'Birth anniversary of Guru Nanak'),
('IN', 'India', 'Christmas Day',           '2025-12-25', true,  'Celebrates the birth of Jesus Christ')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;

-- United States Federal Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('US', 'United States', 'New Year''s Day',              '2025-01-01', true, 'Celebrates the start of the new year'),
('US', 'United States', 'Martin Luther King Jr. Day',   '2025-01-20', true, 'Honors civil rights leader Martin Luther King Jr.'),
('US', 'United States', 'Presidents'' Day',             '2025-02-17', true, 'Honors all U.S. presidents'),
('US', 'United States', 'Memorial Day',                 '2025-05-26', true, 'Honors military personnel who have died in service'),
('US', 'United States', 'Juneteenth',                   '2025-06-19', true, 'Commemorates the emancipation of enslaved people'),
('US', 'United States', 'Independence Day',             '2025-07-04', true, 'Celebrates the Declaration of Independence'),
('US', 'United States', 'Labor Day',                    '2025-09-01', true, 'Honors the American labor movement'),
('US', 'United States', 'Columbus Day',                 '2025-10-13', true, 'Commemorates the arrival of Columbus in the Americas'),
('US', 'United States', 'Veterans Day',                 '2025-11-11', true, 'Honors military veterans'),
('US', 'United States', 'Thanksgiving Day',             '2025-11-27', true, 'A day of giving thanks'),
('US', 'United States', 'Christmas Day',                '2025-12-25', true, 'Celebrates the birth of Jesus Christ')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;

-- United Kingdom Public Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('GB', 'United Kingdom', 'New Year''s Day',          '2025-01-01', true, 'Celebrates the start of the new year'),
('GB', 'United Kingdom', 'Good Friday',              '2025-04-18', true, 'Christian observance'),
('GB', 'United Kingdom', 'Easter Monday',            '2025-04-21', true, 'Day after Easter Sunday'),
('GB', 'United Kingdom', 'Early May Bank Holiday',   '2025-05-05', true, 'First Monday of May'),
('GB', 'United Kingdom', 'Spring Bank Holiday',      '2025-05-26', true, 'Last Monday of May'),
('GB', 'United Kingdom', 'Summer Bank Holiday',      '2025-08-25', true, 'Last Monday of August'),
('GB', 'United Kingdom', 'Christmas Day',            '2025-12-25', true, 'Celebrates the birth of Jesus Christ'),
('GB', 'United Kingdom', 'Boxing Day',               '2025-12-26', true, 'Day after Christmas')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;

-- Canada Public Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('CA', 'Canada', 'New Year''s Day',     '2025-01-01', true, 'Celebrates the start of the new year'),
('CA', 'Canada', 'Family Day',          '2025-02-17', true, 'Provincial family holiday'),
('CA', 'Canada', 'Good Friday',         '2025-04-18', true, 'Christian observance'),
('CA', 'Canada', 'Victoria Day',        '2025-05-19', true, 'Honours Queen Victoria'),
('CA', 'Canada', 'Canada Day',          '2025-07-01', true, 'Celebrates the anniversary of Canadian Confederation'),
('CA', 'Canada', 'Labour Day',          '2025-09-01', true, 'Celebrates the labour movement'),
('CA', 'Canada', 'Thanksgiving',        '2025-10-13', true, 'A day of giving thanks'),
('CA', 'Canada', 'Remembrance Day',     '2025-11-11', true, 'Honours military veterans'),
('CA', 'Canada', 'Christmas Day',       '2025-12-25', true, 'Celebrates the birth of Jesus Christ'),
('CA', 'Canada', 'Boxing Day',          '2025-12-26', true, 'Day after Christmas')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;

-- Australia Public Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('AU', 'Australia', 'New Year''s Day',   '2025-01-01', true, 'Celebrates the start of the new year'),
('AU', 'Australia', 'Australia Day',     '2025-01-27', true, 'Commemorates the foundation of Australia'),
('AU', 'Australia', 'Good Friday',       '2025-04-18', true, 'Christian observance'),
('AU', 'Australia', 'Easter Saturday',   '2025-04-19', true, 'Day before Easter Sunday'),
('AU', 'Australia', 'Easter Monday',     '2025-04-21', true, 'Day after Easter Sunday'),
('AU', 'Australia', 'Anzac Day',         '2025-04-25', true, 'Commemorates Australian and New Zealand war veterans'),
('AU', 'Australia', 'Queen''s Birthday', '2025-06-09', true, 'Honours the reigning British Monarch'),
('AU', 'Australia', 'Christmas Day',     '2025-12-25', true, 'Celebrates the birth of Jesus Christ'),
('AU', 'Australia', 'Boxing Day',        '2025-12-26', true, 'Day after Christmas')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;

-- UAE Public Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('AE', 'United Arab Emirates', 'New Year''s Day',               '2025-01-01', true,  'Celebrates the start of the new year'),
('AE', 'United Arab Emirates', 'Eid al-Fitr',                   '2025-03-30', false, 'Marks the end of Ramadan'),
('AE', 'United Arab Emirates', 'Eid al-Fitr Holiday',           '2025-03-31', false, 'Extended Eid al-Fitr holiday'),
('AE', 'United Arab Emirates', 'Arafat Day',                    '2025-06-06', false, 'Day before Eid al-Adha'),
('AE', 'United Arab Emirates', 'Eid al-Adha',                   '2025-06-07', false, 'Festival of Sacrifice'),
('AE', 'United Arab Emirates', 'Islamic New Year',              '2025-06-27', false, 'Start of Islamic calendar year'),
('AE', 'United Arab Emirates', 'Prophet Muhammad''s Birthday',  '2025-09-05', false, 'Birthday of Prophet Muhammad'),
('AE', 'United Arab Emirates', 'Commemoration Day',             '2025-11-30', true,  'Honours UAE martyrs'),
('AE', 'United Arab Emirates', 'UAE National Day',              '2025-12-02', true,  'Celebrates the formation of the UAE'),
('AE', 'United Arab Emirates', 'UAE National Day Holiday',      '2025-12-03', true,  'Extended National Day holiday')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;

-- Singapore Public Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('SG', 'Singapore', 'New Year''s Day',          '2025-01-01', true,  'Celebrates the start of the new year'),
('SG', 'Singapore', 'Chinese New Year',         '2025-01-29', true,  'Lunar New Year celebration'),
('SG', 'Singapore', 'Chinese New Year Day 2',   '2025-01-30', true,  'Second day of Lunar New Year'),
('SG', 'Singapore', 'Good Friday',              '2025-04-18', true,  'Christian observance'),
('SG', 'Singapore', 'Hari Raya Puasa',          '2025-03-31', false, 'Marks the end of Ramadan'),
('SG', 'Singapore', 'Labour Day',               '2025-05-01', true,  'International Workers Day'),
('SG', 'Singapore', 'Vesak Day',                '2025-05-12', true,  'Celebrates the birth of Buddha'),
('SG', 'Singapore', 'Hari Raya Haji',           '2025-06-07', false, 'Festival of Sacrifice'),
('SG', 'Singapore', 'National Day',             '2025-08-09', true,  'Celebrates Singapore''s independence'),
('SG', 'Singapore', 'Deepavali',                '2025-10-20', true,  'Festival of lights'),
('SG', 'Singapore', 'Christmas Day',            '2025-12-25', true,  'Celebrates the birth of Jesus Christ')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;