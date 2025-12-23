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
                "\nEmployee Management Steps: To add an employee, the Super Admin navigates to Employee Management, creates departments first, then creates positions, and finally adds employees by selecting the relevant department and position during employee creation.",

            ["Attendance"] =
                "Attendance Access Rules: Team Attendance and Attendance Reports are accessible only to Super Admin and Team Managers. Employees do not have access to team attendance or reports." +
                "\nAttendance Rules: Employees must clock in by 9:30 AM. Attendance is marked late after 9:45 AM. Attendance is calculated based on clock-in and clock-out sessions." +
                "\nAttendance Guidance: Employees can clock in and clock out from Attendance under the Time Tracker option. Multiple clock-in and clock-out sessions are allowed in a single day. Total working hours are calculated by summing all sessions." +
                "\nAttendance Steps: To mark attendance, an employee goes to Attendance, opens Time Tracker, clicks Clock In at the start of work, and Clock Out when leaving. To view attendance summary or calendar view, the employee navigates to My Attendance.",

            ["Leave"] =
                "Leave Management Rules: Employees must apply for leave at least one week in advance. Leave requests require manager approval. Payroll calculations consider only approved leave records." +
                "\nLeave Management Guidance: The Super Admin can create and customize leave types from Leave Module under Leave Types. Leave configuration includes allowed days per year, paid or unpaid status, carry-forward rules, and calendar color. Managers receive notifications for leave approvals or rejections." +
                "\nLeave Management Steps: To apply for leave, an employee goes to Leave Management, clicks Apply Leave, selects start and end dates, adds a reason, and submits the request. Employees can view leave balance and status from My Leaves and calendar view.",

            ["Payroll"] =
                "Payroll Access Rules: The Payroll module is accessible only to Super Admin and HR Manager." +
                "\nPayroll Guidance: Salary components and payroll rules are defined by the Super Admin. Payroll is processed based on approved attendance and leave data." +
                "\nPayroll Steps: To process payroll, the Super Admin or HR Manager creates a payroll cycle, navigates to Process Payroll, selects the cycle, chooses departments, defines rules and applicable positions, and processes payroll. Employees can view and download payslips from the Salary Slip section.",

            ["Performance"] =
                "Performance Management Rules: Performance cycles can be created for six months or one year." +
                "\nPerformance Management Guidance: Managers create appraisal cycles. Employees submit self-assessments which are reviewed by managers to create appraisals." +
                "\nPerformance Management Steps: To submit a self-assessment, an employee navigates to Appraisals, selects the active cycle, rates their performance, and submits the assessment. Employees can view appraisal results from the Performance Dashboard.",

            ["SkillMatrix"] =
                "Skill Matrix Rules: Skills are defined only by the organization." +
                "\nSkill Matrix Guidance: Employees can select skills only from the organization-defined skills list. Managers review and rate employee skills." +
                "\nSkill Matrix Steps: To manage skills, an employee navigates to Skill Matrix, selects relevant skills, and submits them for review. Managers rate the submitted skills, which employees can later view in the Skill Matrix section.",

            ["ShiftManagement"] =
                "Shift Management Rules: Shift Management is part of the Attendance module. Only Managers can create and assign shifts." +
                "\nShift Management Guidance: Managers assign shifts to employees. Employees can request shift swaps if they face issues with assigned shifts." +
                "\nShift Management Steps: Employees can view assigned shifts from Attendance. To request a swap, the employee submits a shift swap request, which the manager can approve or reject with a reason.",

            ["Profile"] =
                "Employee Profile and Security Rules: Employees can edit only their own profile and change their password." +
                "\nEmployee Profile and Security Steps: To update profile or change password, an employee navigates to Settings, edits profile details, and updates the password. Employees are required to change the default password assigned during account creation.",

            ["OrganizationSettings"] =
                "Organization Settings Rules: Only the Super Admin can manage organization-level settings including system currency." +
                "\nOrganization Settings Steps: To update currency, the Super Admin navigates to Settings, selects the preferred currency, and saves changes. The selected currency is applied across the entire system.",
            ["HRPolicies"] =
                "HR Policies Rules: All employees must follow organization-defined HR policies." +
                "\nAttendance must be marked daily; late check-ins and absences are tracked based on defined working hours." +
                "\nLeave requests require prior submission and manager approval; only approved leaves are considered for payroll." +
                "\nPerformance is evaluated in defined cycles based on ratings and appraisals." +
                "\nPayroll is processed according to approved attendance, leave, and applicable salary rules.",
        };

        private const string _generalContext = "Use only the provided module context when answering. Do not assume any behavior or permission that is not defined in the context.";

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
