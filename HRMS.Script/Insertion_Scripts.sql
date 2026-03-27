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
('7f25c4f1-d4d8-4c3e-b170-3eb3bf6a7a2b', 'Finalize TimeSheet', 'timesheet_dashboard_finalize', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('7f25c4f1-d4d8-4c3e-b170-3eb3bf6a7a2b', 'View TimeSheet Details', 'timesheet_dashboard_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

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
('6f4de72f-10e0-496c-b18c-a2a34071a82b', 'Add Expense Category', 'expense_category_add', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('6f4de72f-10e0-496c-b18c-a2a34071a82b', 'Edit Expense Category', 'expense_category_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('6f4de72f-10e0-496c-b18c-a2a34071a82b', 'Delete Expense Category', 'expense_category_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Claims
('55632bd8-839d-4613-9829-58ca9c2c74de', 'Add Claim', 'claims_add_claims', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('55632bd8-839d-4613-9829-58ca9c2c74de', 'My Claims', 'my_claims', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('55632bd8-839d-4613-9829-58ca9c2c74de', 'All Claims', 'all_claims', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('55632bd8-839d-4613-9829-58ca9c2c74de', 'View Claim Details', 'claims_view_details', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('55632bd8-839d-4613-9829-58ca9c2c74de', 'Edit Claim', 'claims_edit', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('55632bd8-839d-4613-9829-58ca9c2c74de', 'Delete Claim', 'claims_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),

-- Recurring Expenses
('27e569e1-ebe2-456c-87df-b50c608a9be6', 'Add Recurring Expenses', 'add_recurring', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('27e569e1-ebe2-456c-87df-b50c608a9be6', 'My Recurring Expenses', 'my_rescurring_expenses', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('27e569e1-ebe2-456c-87df-b50c608a9be6', 'All Recurring Expenses', 'all_recurring', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
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
('857bcd02-42c2-43d4-b86a-d513dd13293a', 'Delete Role', 'role_delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);