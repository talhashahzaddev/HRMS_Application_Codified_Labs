using System.Collections.Generic;
using System.Text;

namespace HRMS.AI.Services
{
    // Simple static provider for HRMS context by module.
    // Keeps module-specific guidance text centralized and easy to maintain.
    public static class HrmsContext
    {
        private static readonly Dictionary<string, string> _moduleContexts = new()
        {
            ["Greeting"] =
        "Greeting Context: The user has started the conversation with a greeting such as hi, hello, hey, good morning, or similar. " +
        "The assistant should respond politely, introduce itself as an HR assistant, and guide the user to ask HR-related questions " +
        "such as attendance, leave management, payroll, performance, or HR policies.",

            ["EmployeeManagement"] =
        "Employee Management Access Rules: The Employee Management module is accessible only to Super Admin and HR Manager. Employees and Managers cannot access this module." +
        "\nEmployee Management Guidance: When a new organization is created, the Super Admin performs the initial setup by creating departments, defining all required positions, and then adding employees by assigning them to departments and positions. All employee records are managed only from the Employee Management module." +
        "\n When a manager adds a new employee, an email is sent to the employee containing their login credentials (email and password)." +
        "\nEmployee Management Steps: To add an employee, the Super Admin navigates to Employee Management, creates departments first, then creates positions, and finally adds employees by selecting the relevant department and position during employee creation." +
        "\nWhen user asks about one thing, only answer about that thing." +
        "\nURLs:" +
        "\n- Employees: /employees/list" +
        "\n- Departments: /employees/departments" +
        "\n- Positions: /employees/positions",

            ["Attendance"] =
            "Attendance & Timesheet Access Rules: Team Attendance and Attendance Reports are accessible to Super Admin, HR Manager, and Managers. Employees can view only their own attendance and submitted timesheets." +
            "\nAttendance Rules: Employees must clock in by 9:30 AM. Attendance is marked late after 9:45 AM. Attendance is calculated based on clock-in and clock-out sessions." +
            "\nTimesheet Rules: Managers create timesheets for employees. Employees can edit and submit their timesheets. Submitted timesheets trigger notifications to the manager, who can approve or reject them. Once approved, timesheets are locked and the status is shown as Approved to the employee." +
            "\nAttendance & Timesheet Guidance: Employees can clock in/out via Time Tracker and view attendance summaries. Timesheets are visible in the Timesheet page, allowing edits before submission. Managers and higher roles can view and manage both attendance and timesheets." +
            "\nAttendance & Timesheet Steps: " +
            "1. Employee clocks in/out from Time Tracker or via calendar. " +
            "2. Managers create timesheets for employees. Employees edit and submit timesheets. " +
            "3. Manager receives notification on dashboard for pending timesheets. " +
            "4. Manager approves or rejects; once approved, timesheet locks and status updates to Approved for employee." +
            "\nAttach URL with guidance." +
            "\nURLs:" +
            "\n- My Attendance: /attendance/dashboard" +
            "\n- Time Tracker: /attendance/time-tracker" +
            "\n- Team Attendance: /attendance/team-attendance" +
            "\n- Attendance Report: /attendance/reports" +
            "\n- Timesheet: /attendance/timesheet" +
            "\n- Timesheets Dashboard: /attendance/approvals",

            ["Leave"] =
        "Leave Management Rules: Employees must apply for leave at least one week in advance. Leave requests require manager approval. Payroll calculations consider only approved leave records." +
        "\nLeave Management Guidance: The Super Admin can create and customize leave types from Leave Module under Leave Types. Leave configuration includes allowed days per year, paid or unpaid status, carry-forward rules, and calendar color. Managers receive notifications for leave approvals or rejections." +
        "\nLeave Management Steps: To apply for leave, an employee goes to Leave Management, clicks Apply Leave, selects start and end dates, adds a reason, and submits the request." +
        "\nAttach URL with guidance." +
        "\nURLs:" +
        "\n- My Leave: /leave/dashboard" +
        "\n- Apply for Leave: /leave/apply" +
        "\n- Team Leaves: /leave/team" +
        "\n- Leave Types: /leave/types",

            ["Payroll"] =
        "Payroll Access Rules: The Payroll module is accessible only to Super Admin and HR Manager." +
        "\nPayroll Guidance: Salary components and payroll rules are defined by the Super Admin. Payroll is processed based on approved attendance and leave data." +
        "\nPayroll Steps: To process payroll, the Super Admin or HR Manager creates a payroll cycle, navigates to Process Payroll, selects the cycle, chooses departments, defines rules and applicable positions, and processes payroll. Employees can view and download payslips from the Salary Slip section." +
        "\nAttach URL with guidance." +
        "\nURLs:" +
        "\n- Payroll Periods: /payroll/periods" +
        "\n- Process Payroll: /payroll/process" +
        "\n- Salary Components: /payroll/salary-component" +
        "\n- Payroll Report: /payroll/reports" +
        "\n- Salary Slips: /payroll/slips",

            ["Performance"] =
        "Performance Management Rules: Performance cycles can be created for six months or one year.Only Super Admin or HR Manager can create the Appraisal Cycles." +
        "\nPerformance Management Guidance: Managers create appraisal cycles. Employees submit self-assessments which are reviewed by managers to create appraisals." +
        "\nPerformance Management Steps: To submit a self-assessment, an employee navigates to Appraisals, selects the active cycle, rates their performance, and submits the assessment. Employees can view appraisal results from the Performance Dashboard." +
        "\nAttach URL with guidance." +
        "\nURLs:" +
        "\n- Performance: /performance/dashboard" +
        "\n- Appraisals: /performance/appraisals" +
        "\n- Goals & KRAs: /performance/goals" +
        "\n- Performance Reports: /performance/reports",

            ["SkillMatrix"] =
        "Skill Matrix Rules: Skills are defined only by the organization." +
        "\nSkill Matrix Guidance: Employees can select skills only from the organization-defined skills list. Managers review and rate employee skills." +
        "\nSkill Matrix Steps: To manage skills, an employee navigates to Skill Matrix, selects relevant skills, and submits them for review. Managers rate the submitted skills, which employees can later view in the Skill Matrix section." +
        "\nAttach URL with guidance." +
        "\nURLs:" +
        "\n- Skills Matrix: /performance/skills",

            ["ShiftManagement"] =
        "Shift Management Rules: Shift Management is part of the Attendance module. Only Managers can create and assign shifts." +
        "\nShift Management Guidance: Managers assign shifts to employees. Employees can request shift swaps if they face issues with assigned shifts." +
        "\nShift Management Steps: Employees can view assigned shifts from Attendance. To request a swap, the employee submits a shift swap request, which the manager can approve or reject with a reason." +
        "\nAttach URL with guidance." +
        "\nURLs:" +
        "\n- Shifts: /attendance/shift",

            ["Profile"] =
        "Employee Profile and Security Rules: Employees can edit only their own profile and change their password." +
        "\nEmployee Profile and Security Steps: To update profile or change password, an employee navigates to Settings, edits profile details, and updates the password. Employees are required to change the default password assigned during account creation." +
        "\nAttach URL with guidance." +
        "\nURLs:" +
        "\n- Profile: /profile",

            ["OrganizationSettings"] =
        "Organization Settings Rules: Only the Super Admin can manage organization-level settings including system currency." +
        "\nOrganization Settings Steps: To update currency, the Super Admin navigates to Settings, selects the preferred currency, and saves changes. The selected currency is applied across the entire system." +
        "\nAttach URL with guidance." +
        "\nURLs:" +
        "\n- Profile: /profile",

            ["HRPolicies"] =
        "HR Policies Rules: All employees must follow organization-defined HR policies." +
        "\nAttendance must be marked daily; late check-ins and absences are tracked based on defined working hours." +
        "\nLeave requests require prior submission and manager approval; only approved leaves are considered for payroll." +
        "\nPerformance is evaluated in defined cycles based on ratings and appraisals." +
        "\nPayroll is processed according to approved attendance, leave, and applicable salary rules.",

            ["AssetsManagment"] =
                "Assets Management Access Rules: The Assets Management module is accessible to Super Admin, HR Manager, and Managers. " +
                "Only Super Admin can create and manage asset types. HR Managers and Managers can create assets, assign them to employees, and view assigned assets. Employees have no access to this module." +
                "\nAssets Management Guidance: Super Admin first creates Asset Types under Assets Types. HR Managers and Managers can then create individual assets, assign them to employees, update asset status, or reassign assets. All actions including edit, delete, and assignment are recorded." +
                "\nAssets Management Steps: " +
                "1. To create Asset Types, Super Admin navigates to /assets/types and adds new types. " +
                "2. To create an Asset, HR Manager or Manager navigates to /assets/create, selects an Asset Type, fills in asset details, and assigns to an employee. " +
                "3. To view assigned assets, navigate to the Assets List; you can edit, delete, or reassign assets using the actions menu for each record. " +
                "4. All asset assignments include employee details, asset status, and assignment history." +
                "\nAttach URL with guidance." +
                "\nURLs:" +
                "\n- Asset Types: /assets/types" +
                "\n- Create Asset: /assets/create" +
                "\n- Assigned Assets List (with actions): /assets/list",

            ["AccountCreation"] =
            "Account Creation Rules: After organization registration, an account activation email is sent to the registered email. " +
            "Users must activate their account to log in as Super Admin. During registration, you can add your custom domain name." +
            "\nAccount Creation Steps: Activate account from email, log in as Super Admin, complete organization setup including domain name and initial configurations." +
            "\nAttach URL with guidance." +
            "\n BaseUrl : https://www.briskpeople.com/" +
            "\nURLs:" +
            "\n- Organization Registration: /sign-up",

            ["GlobalCalendar"] =
            "Global Calendar Rules: The calendar displays holidays, leave records, attendance, and user presence." +
            "\nGlobal Calendar Guidance: Users can view holidays, leave, present/absent status, and can also apply for leave or clock in/out directly from the calendar." +
            "\nGlobal Calendar Steps: Navigate to the Global Calendar section, view schedules and statuses, and use action buttons to mark attendance or apply leave." +
            "\nAttach URL with guidance." +
            "\nURLs:" +
            "\n- Calendar View: /calendar",

            ["NewsManagement"] =
            "News Rules: Only Super Admin and HR Manager can create news items. Employees can view news in their dashboard." +
            "\nNews Guidance: Employees are notified of new news items on their dashboard. Super Admin and HR Manager manage news publication and visibility." +
            "\nNews Steps: Super Admin/HR Manager creates news under News Management. Employees access news from their dashboard." +
            "\nAttach URL with guidance." +
            "\nURLs:" +
            "\n- News Dashboard: /news/dashboard" +
            "\n- Create News: /news/create-news" ,

            ["ExpensesManagement"] =
            "Expenses Rules: Expense categories and recurring expenses are created by Super Admin and HR Manager. Employees can submit expense claims which are sent as notifications to the assigned manager for approval." +
            "\nExpenses Guidance: Employees must provide justification and attach proof/screenshots for claims. Approved claims are reimbursed. Reports are available for recurring and one-time expenses on a monthly, weekly, and yearly basis." +
            "\nExpenses Steps: Employee submits claim → Manager approves/rejects → Reimbursement processed. View reports from the Expense Reports section." +
            "\nAttach URL with guidance." +
            "\nURLs:" +
            "\n- Expense Categories: /expense/categories" +
            "\n- Expense Claim: /expense/claims" +
            "\n- Expense Reports: /expense/expense-report" +
            "\n- Recurring Expenses: /expense/recurring",

            ["JobsManagement"] =
            "Jobs Rules: The Jobs module has three sections: Openings, Job Applications, and Stages. Super Admin and HR Manager can create job openings and manage stages. Employees can only view job openings and their submitted applications." +
            "\nJobs Guidance: Employees can apply for jobs and refer candidates. Managers, Super Admin, and employees can track application status and referrals." +
            "\nJobs Steps: Super Admin/HR Manager creates job openings and manages stages. Employee navigates to Job Openings, applies, or refers candidates. Track applications and status in Job Applications section." +
            "\nAttach URL with guidance." +
            "\nURLs:" +
            "\n- Job Openings: /jobs/openings" +
            "\n- Job Applications: /jobs/applied" +
            "\n- Job Stages: /jobs/stage" 

        };

        private const string _generalContext = "Use only the provided module context when answering. Do not assume any behavior or permission that is not defined in the context.On the base of role answer to the user if the module is not accessible to that role just reply you have the permission to access these information.";

        public static string GetContext(string module)
        {
            if (string.IsNullOrWhiteSpace(module))
            {
                // return concatenated context for all modules
                var sb = new StringBuilder();
                foreach (var kv in _moduleContexts)
                {
                    sb.AppendLine(kv.Value);
                    sb.AppendLine();
                }
                sb.AppendLine(_generalContext);
                return sb.ToString();
            }

            // normalize keys: allow different casings and common names
            var key = module.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);
            foreach (var entry in _moduleContexts)
            {
                var normalized = entry.Key.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);
                if (string.Equals(normalized, key, System.StringComparison.OrdinalIgnoreCase))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(entry.Value);
                    sb.AppendLine();
                    sb.AppendLine(_generalContext);
                    return sb.ToString();
                }
            }

            // unknown module -> return all context as fallback
            var fallback = new StringBuilder();
            foreach (var kv in _moduleContexts)
            {
                fallback.AppendLine(kv.Value);
                fallback.AppendLine();
            }
            fallback.AppendLine(_generalContext);
            return fallback.ToString();
        }
    }
}
