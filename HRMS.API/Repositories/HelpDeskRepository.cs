using HRMS.API.Configuration;
using static HRMS.API.Models.DTOs.HelpDeskDto;
using HRMS.API.Repositories.Interfaces;


namespace HRMS.API.Repositories
{
    public class HelpDeskRepository : BaseRepository, IHelpDeskRepository
    {

        private readonly ILogger<HelpDeskRepository> _logger;
        public HelpDeskRepository(IDatabaseConnectionFactory connectionFactory, ILogger<HelpDeskRepository> logger) : base(connectionFactory)
        {
            _logger = logger;
        }
    public async Task<Guid> CreateTicketAsync(TicketRequest request, Guid organizationId, Guid employeeId)
        {
            try
            {
                var ticketId = await ExecuteScalarAsync<Guid>(
                    @"SELECT helpdesk.fn_create_ticket(
                @organizationId,
                @employeeId,
                @departmentId,
                @title,
                @ticketType,
                @category,
                @priority,
                @description,
                @attachment,
                @assignedGroup
            )",
                    new
                    {
                        organizationId,
                        employeeId,
                        departmentId = request.departmentId,
                        title = request.TicketTitle,
                        ticketType = request.TicketType,
                        category = request.CategoryId,
                        priority = request.Priority,
                        description = request.Description,
                        attachment = request.Attachment,
                        assignedGroup = request.assignedGroup
                    });

                return ticketId;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create ticket", ex.InnerException);
            }
        }

        public async Task<List<TicketDto>> GetAllTicketsAsync(Guid organizationId, TicketSearchModel search)
        {
            try
            {
                var sql = @"
        SELECT 
        t.ticketid              AS TicketId,
        t.departmentid          AS DepartmentId,
        COALESCE(d.departmentname, 'N/A') AS DepartmentName,

        t.title                 AS TicketTitle,
        t.tickettype            AS TicketType,

        -- ✅ Category (NEW)
        t.categoryid            AS CategoryId,
        COALESCE(c.name, 'N/A') AS Category,

        t.priority              AS Priority,
        t.description           AS Description,
        t.attachment            AS Attachment,
        t.status                AS Status,
        t.createdat             AS CreatedAt,

        tg.grouptitle           AS GroupName,
        ta.groupid              AS GroupId,   -- ✅ added

        COALESCE(emp_list.employeeids, '{}') AS AssignedEmployees
    FROM helpdesk.tickets t

    -- Department join
    LEFT JOIN employee.departments d
        ON t.departmentid = d.departmentid
        AND d.isactive = true

    -- ✅ Category join (NEW)
    LEFT JOIN helpdesk.categories c
        ON t.categoryid = c.categoryid

    LEFT JOIN helpdesk.ticket_assignments ta
        ON t.ticketid = ta.ticketid

    LEFT JOIN helpdesk.ticket_group tg
        ON ta.groupid = tg.groupid

    -- Aggregate assigned employees
    LEFT JOIN LATERAL (
        SELECT ARRAY(
            SELECT DISTINCT emp_id
            FROM (
                SELECT tae.employeeid AS emp_id
                FROM helpdesk.ticket_assigned_employees tae
                WHERE tae.ticketid = t.ticketid

                UNION

                SELECT unnest(tg2.employeeids)
                FROM helpdesk.ticket_group tg2
                WHERE tg2.groupid = ta.groupid

                UNION

                SELECT unnest(ta.assigned_employees)
            ) AS all_employees
        ) AS employeeids
    ) emp_list ON TRUE

    WHERE t.organizationid = @OrganizationId
      AND t.isactive = true

      -- Search filter
      AND (
            @SearchTerm IS NULL
            OR t.title ILIKE '%' || @SearchTerm || '%'
            OR t.description ILIKE '%' || @SearchTerm || '%'
      )

      -- Department filter
      AND (
            @DepartmentId IS NULL
            OR t.departmentid = @DepartmentId
      )

      -- Status filter
      AND (
            @Status IS NULL
            OR t.status ILIKE @Status
      )

    ORDER BY
        CASE WHEN @SortBy = 'title' AND @SortOrder = 'asc' THEN t.title END ASC,
        CASE WHEN @SortBy = 'title' AND @SortOrder = 'desc' THEN t.title END DESC,
        t.createdat DESC

    LIMIT @PageSize
    OFFSET (@Page - 1) * @PageSize;
    ";

                var parameters = new
                {
                    OrganizationId = organizationId,
                    SearchTerm = string.IsNullOrWhiteSpace(search.SearchTerm) ? null : search.SearchTerm,
                    DepartmentId = search.DepartmentId,
                    Page = search.Page <= 0 ? 1 : search.Page,
                    PageSize = search.PageSize <= 0 ? 10 : search.PageSize,
                    SortBy = search.SortBy?.ToLower(),
                    SortOrder = search.SortOrder?.ToLower(),
                    Status = search.Status
                };

                var result = await QueryAsync<TicketDto>(sql, parameters);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all tickets");
                throw;
            }
        }
    public async Task<List<TicketDto>> GetAllTicketsCreatedByEmployeeAsync(Guid organizationId, Guid employeeId, TicketSearchModel search)
        {
            try
            {
                var sql = @"
        SELECT 
        t.ticketid        AS TicketId,
        t.departmentid    AS DepartmentId,
        d.departmentname  AS DepartmentName,
        t.title           AS TicketTitle,
        t.tickettype      AS TicketType,
        -- ✅ Category (UPDATED)
        t.categoryid      AS CategoryId,
        COALESCE(c.name, 'N/A') AS Category,
        t.priority        AS Priority,
        t.description     AS Description,
        t.attachment      AS Attachment,
        t.status          AS Status,
        t.createdat       AS CreatedAt,
        tg.grouptitle     AS GroupName,
        ta.groupid        AS GroupId,   -- (optional but good, matches DTO)

        COALESCE(emp_list.employeeids, '{}') AS AssignedEmployees

    FROM helpdesk.tickets t

    -- ✅ Department join
    LEFT JOIN employee.departments d
        ON t.departmentid = d.departmentid
        AND d.isactive = true

    -- ✅ Category join (NEW)
    LEFT JOIN helpdesk.categories c
        ON t.categoryid = c.categoryid

    LEFT JOIN helpdesk.ticket_assignments ta 
        ON t.ticketid = ta.ticketid

    LEFT JOIN helpdesk.ticket_group tg 
        ON ta.groupid = tg.groupid

    -- ✅ Aggregate employees
    LEFT JOIN LATERAL (
        SELECT ARRAY(
            SELECT DISTINCT emp_id
            FROM (
                SELECT tae.employeeid AS emp_id
                FROM helpdesk.ticket_assigned_employees tae
                WHERE tae.ticketid = t.ticketid

                UNION

                SELECT UNNEST(tg2.employeeids)
                FROM helpdesk.ticket_group tg2
                WHERE tg2.groupid = ta.groupid

                UNION

                SELECT UNNEST(ta.assigned_employees)
            ) AS all_employees
        ) AS employeeids
    ) emp_list ON TRUE

    WHERE t.organizationid = @OrganizationId
      AND t.isactive = true
      AND t.employeeid = @EmployeeId

      -- ✅ Search filter
      AND (
            @SearchTerm IS NULL
            OR t.title ILIKE '%' || @SearchTerm || '%'
            OR t.description ILIKE '%' || @SearchTerm || '%'
      )

      -- ✅ Department filter
      AND (
            @DepartmentId IS NULL
            OR t.departmentid = @DepartmentId
      )

      -- ✅ Status filter
      AND (
            @Status IS NULL
            OR t.status ILIKE @Status
      )

    ORDER BY
        CASE WHEN @SortBy = 'title' AND @SortOrder = 'asc' THEN t.title END ASC,
        CASE WHEN @SortBy = 'title' AND @SortOrder = 'desc' THEN t.title END DESC,
        t.createdat DESC

    LIMIT @PageSize
    OFFSET (@Page - 1) * @PageSize;
    ";
                var parameters = new
                {
                    OrganizationId = organizationId,
                    EmployeeId = employeeId,
                    SearchTerm = string.IsNullOrWhiteSpace(search.SearchTerm) ? null : search.SearchTerm,
                    DepartmentId = search.DepartmentId,
                    Page = search.Page <= 0 ? 1 : search.Page,
                    PageSize = search.PageSize <= 0 ? 10 : search.PageSize,
                    SortBy = search.SortBy?.ToLower(),
                    SortOrder = search.SortOrder?.ToLower(),
                    Status = search.Status
                };

                var tickets = await QueryAsync<TicketDto>(sql, parameters);

                return tickets.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tickets created by Employee {EmployeeId}", employeeId);
                throw;
            }
        }

        //Get ticket by id
    public async Task<TicketDto?> GetTicketByIdAsync(Guid organizationId, Guid ticketId)
        {
            var sql = @"
    SELECT 
    t.ticketid        AS TicketId,
    t.departmentid    AS DepartmentId,
    COALESCE(d.departmentname, 'N/A') AS DepartmentName,

    t.title           AS TicketTitle,
    t.tickettype      AS TicketType,

    -- ✅ Category (UPDATED)
    t.categoryid      AS CategoryId,
    COALESCE(c.name, 'N/A') AS Category,

    t.priority        AS Priority,
    t.description     AS Description,
    t.status          AS status,
    t.attachment      AS Attachment,

    tg.groupid        AS GroupId,
    tg.grouptitle     AS GroupName,

    -- ✅ Manual Assigned Employees
    COALESCE(manual_emp.employeeids, '{}') AS AssignedEmployees,
    COALESCE(manual_emp.employeenames, '{}') AS AssignedEmployeeNames,

    -- ✅ Group Employees
    COALESCE(group_emp.employeenames, '{}') AS GroupEmployeeNames,

    creator.firstname || ' ' || creator.lastname AS CreatedByName

    FROM helpdesk.tickets t
    LEFT JOIN employee.departments d
    ON d.departmentid = t.departmentid
    -- ✅ Category join (NEW)
   LEFT JOIN helpdesk.categories c
    ON t.categoryid = c.categoryid
    LEFT JOIN helpdesk.ticket_assignments ta 
    ON t.ticketid = ta.ticketid
    LEFT JOIN helpdesk.ticket_group tg 
    ON ta.groupid = tg.groupid

    -- ✅ Manual Assigned Employees
    LEFT JOIN LATERAL (
    SELECT 
        ARRAY_AGG(DISTINCT e.employeeid) AS employeeids,
        ARRAY_AGG(DISTINCT e.firstname || ' ' || e.lastname) AS employeenames
     FROM employee.employees e
     WHERE e.employeeid = ANY(ta.assigned_employees)
     ) manual_emp ON TRUE

-- ✅ Group Employees
LEFT JOIN LATERAL (
    SELECT 
        ARRAY_AGG(DISTINCT e.firstname || ' ' || e.lastname) AS employeenames
    FROM employee.employees e
    WHERE e.employeeid = ANY(tg.employeeids)
) group_emp ON TRUE

-- ✅ Creator Info
LEFT JOIN employee.employees creator
    ON creator.employeeid = t.employeeid

WHERE t.organizationid = @OrganizationId
  AND t.ticketid = @TicketId
  AND t.isactive = true;
";
            var parameters = new
            {
                OrganizationId = organizationId,
                TicketId = ticketId
            };
            return (await QueryAsync<TicketDto>(sql, parameters)).FirstOrDefault();
        }

    public async Task<bool> UpdateTicketDataAsync(Guid organizationId, UpdateTicketRequest request)
        {
            try
            {
                var sql = @"
        UPDATE helpdesk.tickets
        SET
            departmentid = @DepartmentId,
            title = @Title,
            tickettype = @TicketType,
            categoryid = @Category,
            priority = @Priority,
            description = @Description,
            attachment = @Attachment,
            status = @Status,
            updatedat = CURRENT_TIMESTAMP
        WHERE ticketid = @TicketId
          AND organizationid = @OrganizationId
          AND isactive = true;
        ";

                var parameters = new
                {
                    TicketId = request.TicketId,
                    OrganizationId = organizationId,
                    DepartmentId = request.DepartmentId,
                    Title = request.TicketTitle,
                    TicketType = request.TicketType,
                    Category = request.CategoryId,
                    Priority = request.Priority,
                    Description = request.Description,
                    Attachment = request.Attachment,
                    Status = request.Status
                };

                var rowsAffected = await ExecuteAsync(sql, parameters);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket");
                throw;
            }
        }
        public async Task<bool> UpdateTicketAssignmentsAsync(UpdateTicketRequest request, Guid currentEmployeeId)
        {
            try
            {
                // 🔹 STEP 1: Get existing assignment
                var assignmentId = await ExecuteScalarAsync<Guid?>(
                    @"SELECT assignmentid 
      FROM helpdesk.ticket_assignments 
      WHERE ticketid = @TicketId 
      LIMIT 1;",
                    new { request.TicketId });

                // 🔹 STEP 2: INSERT or UPDATE assignment
                if (assignmentId != null && assignmentId != Guid.Empty)
                {
                    await ExecuteAsync(@"
        UPDATE helpdesk.ticket_assignments
        SET
            groupid = @GroupId,
            assigned_employees = @AssignedEmployees,
            assignedby = @AssignedBy,
            createdat = CURRENT_TIMESTAMP
        WHERE assignmentid = @AssignmentId;",
                        new
                        {
                            AssignmentId = assignmentId,
                            GroupId = request.GroupId,
                            AssignedEmployees = request.AssignedEmployees, // ✅ ONLY manual employees
                            AssignedBy = currentEmployeeId
                        });
                }
                else
                {
                    assignmentId = Guid.NewGuid();

                    await ExecuteAsync(@"
        INSERT INTO helpdesk.ticket_assignments
        (assignmentid, ticketid, groupid, assigned_employees, assignedby)
        VALUES
        (@AssignmentId, @TicketId, @GroupId, @AssignedEmployees, @AssignedBy);",
                        new
                        {
                            AssignmentId = assignmentId,
                            TicketId = request.TicketId,
                            GroupId = request.GroupId,
                            AssignedEmployees = request.AssignedEmployees, // ✅ ONLY manual employees
                            AssignedBy = currentEmployeeId
                        });
                }

                // 🔹 STEP 3: DELETE OLD EMPLOYEES (IMPORTANT)
                await ExecuteAsync(@"
    DELETE FROM helpdesk.ticket_assigned_employees
    WHERE ticketid = @TicketId;",
                    new { request.TicketId });

                // 🔥 STEP 4: INSERT NEW EMPLOYEES (BASED ON RULES)

                var employeesToInsert = new List<Guid>();

                // ✅ CASE 1: If GROUP is selected → take ONLY group employees
                if (request.GroupId != null && request.GroupId != Guid.Empty)
                {
                    var groupEmployees = await ExecuteScalarAsync<Guid[]>(@"
        SELECT employeeids
        FROM helpdesk.ticket_group
        WHERE groupid = @GroupId
          AND isactive = true;",
                        new { request.GroupId });

                    if (groupEmployees != null && groupEmployees.Any())
                    {
                        employeesToInsert.AddRange(groupEmployees);
                    }
                }

                // ✅ CASE 2: If manual employees exist → ADD them ALSO
                if (request.AssignedEmployees != null && request.AssignedEmployees.Any())
                {
                    employeesToInsert.AddRange(request.AssignedEmployees);
                }

                // ✅ Remove duplicates
                var finalEmployees = employeesToInsert.Distinct().ToList();

                // 🔹 STEP 5: INSERT into normalized table
                if (finalEmployees.Any())
                {
                    var insertSql = @"
        INSERT INTO helpdesk.ticket_assigned_employees
        (id, ticketid, employeeid, assignmentid)
        VALUES
        (@Id, @TicketId, @EmployeeId, @AssignmentId);";

                    foreach (var empId in finalEmployees)
                    {
                        await ExecuteAsync(insertSql, new
                        {
                            Id = Guid.NewGuid(),
                            TicketId = request.TicketId,
                            EmployeeId = empId,
                            AssignmentId = assignmentId
                        });
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket assignments");
                throw;
            }
        }

    public async Task<bool> DeleteTicketByIdAsync(Guid organizationId, Guid ticketId)
        {
            var sql = @"
            DELETE FROM helpdesk.tickets
            WHERE organizationid = @OrganizationId
            AND ticketid = @TicketId
            ";

            var parameters = new
            {
                OrganizationId = organizationId,
                TicketId = ticketId
            };

            var rowsAffected = await ExecuteAsync(sql, parameters);

            // Return true if a row was deleted, false otherwise
            return rowsAffected > 0;
        }



    public async Task<bool> UpdateTicketGroupAsync(Guid organizationId, UpdateTicketGroupRequest request)
        {
            try
            {
                var sql = @"
            UPDATE helpdesk.ticket_group
            SET
            grouptitle = @GroupTitle,
            departmentid = @DepartmentId,
            employeeids=@EmployeeIds::uuid[],
            categoryid = @CategoryId,
            updatedat = CURRENT_TIMESTAMP
            WHERE groupid = @GroupId
            AND organizationid = @OrganizationId
            AND isactive = true;
            ";

                var parameters = new
                {
                    GroupId = request.GroupId,
                    OrganizationId = organizationId,
                    GroupTitle = request.GroupTitle,
                    employeeIds= request.EmployeeIds.ToArray(),
                    DepartmentId = request.DepartmentId,
                    CategoryId = request.CategoryId
                };

                var rowsAffected = await ExecuteAsync(sql, parameters);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket group");
                throw;
            }
        }
    public async Task<bool> UpdateGroupEmployeesAsync(Guid groupId, List<Guid> newEmployeeIds, List<Guid> oldGroupEmpIds)
        {
            try
            {
                if (groupId == Guid.Empty)
                    throw new ArgumentException("GroupId is required");

                newEmployeeIds = newEmployeeIds ?? new List<Guid>();
                var distinctEmployees = newEmployeeIds.Distinct().ToList();

                oldGroupEmpIds = oldGroupEmpIds ?? new List<Guid>();

                // 🔹 STEP 1: Get all assignments linked to this group
                var assignmentIds = (await QueryAsync<Guid>(
                    @"SELECT assignmentid
              FROM helpdesk.ticket_assignments
              WHERE groupid = @GroupId;",
                    new { GroupId = groupId }
                )).ToList();

                // 🔹 STEP 2: Update normalized employees for each assignment
                foreach (var assignmentId in assignmentIds)
                {
                    // Get ticketid for this assignment
                    var ticketId = await ExecuteScalarAsync<Guid>(
                        @"SELECT ticketid 
                  FROM helpdesk.ticket_assignments 
                  WHERE assignmentid = @AssignmentId;",
                        new { AssignmentId = assignmentId });

                    // Delete only old group employees from assignment (manual employees stay untouched)
                    if (oldGroupEmpIds.Any())
                    {
                        await ExecuteAsync(@"
                    DELETE FROM helpdesk.ticket_assigned_employees
                    WHERE assignmentid = @AssignmentId
                    AND employeeid = ANY(@OldEmployeeIds);",
                            new { AssignmentId = assignmentId, OldEmployeeIds = oldGroupEmpIds });
                    }

                    // Insert updated group employees into normalized table if not already present
                    if (distinctEmployees.Any())
                    {
                        var insertSql = @"
                    INSERT INTO helpdesk.ticket_assigned_employees 
                    (id, ticketid, employeeid, assignmentid)
                    VALUES (@Id, @TicketId, @EmployeeId, @AssignmentId)
                    ON CONFLICT (ticketid, employeeid) DO NOTHING;"; // Prevent duplicates

                        foreach (var empId in distinctEmployees)
                        {
                            await ExecuteAsync(insertSql, new
                            {
                                Id = Guid.NewGuid(),
                                TicketId = ticketId,
                                EmployeeId = empId,
                                AssignmentId = assignmentId
                            });
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket assignment employees for group");
                throw;
            }
        }


    public async Task<List<TicketDto>> GetAllEmployeeAssignedTickets(Guid organizationId,Guid employeeId,TicketSearchModel search)
        {
            try
            {
                var sql = @"
                SELECT 
    t.ticketid              AS TicketId,
    t.departmentid          AS DepartmentId,
    COALESCE(d.departmentname, 'N/A') AS DepartmentName,

    t.title                 AS TicketTitle,
    t.tickettype            AS TicketType,

    -- ✅ Category (UPDATED)
    t.categoryid            AS CategoryId,
    COALESCE(c.name, 'N/A') AS Category,

    t.priority              AS Priority,
    t.description           AS Description,
    t.attachment            AS Attachment,
    t.status                AS Status,

    tg.grouptitle           AS GroupName,

    COALESCE(emp_list.employeeids, '{}') AS AssignedEmployees

FROM helpdesk.tickets t

-- Department join
LEFT JOIN employee.departments d
    ON t.departmentid = d.departmentid
    AND d.isactive = true

-- ✅ Category join (NEW)
LEFT JOIN helpdesk.categories c
    ON t.categoryid = c.categoryid

LEFT JOIN helpdesk.ticket_assignments ta 
    ON t.ticketid = ta.ticketid

LEFT JOIN helpdesk.ticket_group tg 
    ON ta.groupid = tg.groupid

-- Aggregate employees
LEFT JOIN LATERAL (
    SELECT ARRAY(
        SELECT DISTINCT emp_id
        FROM (
            SELECT tae.employeeid AS emp_id
            FROM helpdesk.ticket_assigned_employees tae
            WHERE tae.ticketid = t.ticketid

            UNION

            SELECT UNNEST(tg2.employeeids)
            FROM helpdesk.ticket_group tg2
            WHERE tg2.groupid = ta.groupid

            UNION

            SELECT UNNEST(ta.assigned_employees)
        ) AS all_employees
    ) AS employeeids
) emp_list ON TRUE

WHERE t.organizationid = @OrganizationId
  AND t.isactive = true

  AND @EmployeeId = ANY(emp_list.employeeids)

  AND (
        @SearchTerm IS NULL
        OR t.title ILIKE '%' || @SearchTerm || '%'
        OR t.description ILIKE '%' || @SearchTerm || '%'
  )

  AND (
        @DepartmentId IS NULL
        OR t.departmentid = @DepartmentId
  )

  AND (
        @Status IS NULL
        OR t.status ILIKE @Status
  )

ORDER BY
    CASE WHEN @SortBy = 'title' AND @SortOrder = 'asc' THEN t.title END ASC,
    CASE WHEN @SortBy = 'title' AND @SortOrder = 'desc' THEN t.title END DESC,
    t.createdat DESC

LIMIT @PageSize
OFFSET (@Page - 1) * @PageSize;
";

                var parameters = new
                {
                    OrganizationId = organizationId,
                    EmployeeId = employeeId,
                    SearchTerm = string.IsNullOrWhiteSpace(search.SearchTerm) ? null : search.SearchTerm,
                    DepartmentId = search.DepartmentId,
                    Status = search.Status,
                    Page = search.Page <= 0 ? 1 : search.Page,
                    PageSize = search.PageSize <= 0 ? 10 : search.PageSize,
                    SortBy = search.SortBy?.ToLower(),
                    SortOrder = search.SortOrder?.ToLower()
                };

                var tickets = await QueryAsync<TicketDto>(sql, parameters);
                return tickets.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching assigned tickets for Employee {EmployeeId}", employeeId);
                throw;
            }
        }





        //Create Ticket Group

        public async Task<TicketGroupDto> CreateTicketGroupAsync(Guid organizationId, CreateTicketGroupRequest request)
        {
            try
            {
                var sql = @"
        INSERT INTO helpdesk.ticket_group (
            organizationid,
            grouptitle,
            employeeids,
            departmentid,
            categoryid,
            createdat,
            updatedat,
            isactive
        )
        VALUES (
            @OrganizationId,
            @GroupTitle,
            @EmployeeIds,
            @DepartmentId,
            @categoryId,
            NOW(),
            NOW(),
            TRUE
        )
        RETURNING 
            groupid        AS GroupId,
            grouptitle     AS GroupTitle,
            employeeids    AS EmployeeIds,
            createdat      AS CreatedAt;
        ";

                var result = await QuerySingleAsync<TicketGroupDto>(
                    sql,
                    new
                    {
                        OrganizationId = organizationId,
                        GroupTitle = request.GroupTitle,
                        categoryId=request.categoryId,
                        EmployeeIds = request.EmployeeIds.ToArray(),// ✅ important for PostgreSQL array
                        DepartmentId = request.departmentId
                    });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating ticket group for Organization {OrganizationId}",
                    organizationId
                );
                throw;
            }
        }

        public async Task<List<TicketGroupDto>> GetAllTicketGroupsAsync(Guid organizationId, string? search, Guid? departmentId, Guid? categoryId)
        {
            var sql = @"
SELECT 
    tg.groupid        AS GroupId,
    tg.grouptitle     AS GroupTitle,
    tg.employeeids    AS EmployeeIds,
    d.departmentid    AS DepartmentId,
    d.departmentname  AS DepartmentName,
    tg.categoryid     AS CategoryId,
    c.name            AS CategoryName,
    tg.createdat      AS CreatedAt,
    -- Aggregate employee names as comma-separated string
    COALESCE(
        STRING_AGG(e.firstname || ' ' || e.lastname, ', ' ORDER BY e.firstname, e.lastname), ''
    ) AS EmployeeNames
FROM helpdesk.ticket_group tg
LEFT JOIN employee.departments d
    ON tg.departmentid = d.departmentid
LEFT JOIN helpdesk.categories c
    ON tg.categoryid = c.categoryid
LEFT JOIN LATERAL (
    SELECT e.firstname, e.lastname
    FROM employee.employees e
    WHERE e.employeeid = ANY(tg.employeeids)
) e ON TRUE
WHERE tg.organizationid = @OrganizationId
  AND tg.isactive = TRUE
  AND (@DepartmentId IS NULL OR tg.departmentid = @DepartmentId)
  AND (@CategoryId IS NULL OR tg.categoryid = @CategoryId)
  AND (@Search IS NULL OR LOWER(tg.grouptitle) LIKE '%' || LOWER(@Search) || '%')
GROUP BY tg.groupid, tg.grouptitle, tg.employeeids, d.departmentid, d.departmentname, tg.categoryid, c.name, tg.createdat
ORDER BY tg.createdat DESC;
";

            var parameters = new
            {
                OrganizationId = organizationId,
                DepartmentId = departmentId,
                CategoryId = categoryId,
                Search = search
            };

            var groups = await QueryAsync<TicketGroupDto>(sql, parameters);
            return groups.ToList();
        }

        //Get Group by id
        public async Task<TicketGroupDto?> GetTicketGroupByIdAsync(Guid organizationId, Guid groupId)
        {
            var sql = @"
SELECT 
    groupid       AS GroupId,
    grouptitle    AS GroupTitle,
    employeeids   AS EmployeeIds,
    createdat     AS CreatedAt
FROM helpdesk.ticket_group
WHERE organizationid = @OrganizationId
  AND groupid = @GroupId
  AND isactive = true;";

            var parameters = new
            {
                OrganizationId = organizationId,
                GroupId = groupId
            };

            // Use your QueryAsync helper (e.g., Dapper)
            return (await QueryAsync<TicketGroupDto>(sql, parameters)).FirstOrDefault();
        }
        public async Task<bool> DeleteTicketGroupAsync(Guid organizationId, Guid groupId)
        {
            var sql = @"
delete FROM helpdesk.ticket_group
WHERE organizationid = @OrganizationId
  AND groupid = @GroupId
";

            var parameters = new
            {
                OrganizationId = organizationId,
                GroupId = groupId
            };

            // Execute the update
            var rowsAffected = await ExecuteAsync(sql, parameters);

            // Return true if a row was updated (group deleted), false otherwise
            return rowsAffected > 0;
        }

        public async Task<bool> AssignTicketAsync(AssignTicketRequest request, Guid assignedBy)
        {
            try
            {
                var sql = @"

-- 1️⃣ Ensure assignment exists
INSERT INTO helpdesk.ticket_assignments (
    ticketid,
    groupid,
    assigned_employees,
    assignedby,
    reason,
    createdat
)
SELECT
    @TicketId,
    @GroupId,
    COALESCE(@AssignedEmployees::uuid[], '{}'::uuid[]), -- ONLY manual here
    @AssignedBy,
    @Reason,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 
    FROM helpdesk.ticket_assignments 
    WHERE ticketid = @TicketId
);

-- 2️⃣ Insert employees into mapping table (manual + group)
WITH incoming_employees AS (
    -- Manual employees
    SELECT UNNEST(COALESCE(@AssignedEmployees::uuid[], '{}')) AS emp
    
    UNION
    
    -- Group employees
    SELECT UNNEST(COALESCE(tg.employeeids, '{}'))
    FROM helpdesk.ticket_group tg
    WHERE tg.groupid = @GroupId
)

INSERT INTO helpdesk.ticket_assigned_employees (
    ticketid,
    employeeid,
    assignmentid,
    createdat
)
SELECT 
    @TicketId,
    ie.emp,
    ta.assignmentid,
    NOW()
FROM incoming_employees ie
JOIN helpdesk.ticket_assignments ta 
    ON ta.ticketid = @TicketId
WHERE NOT EXISTS (
    SELECT 1 
    FROM helpdesk.ticket_assigned_employees t
    WHERE t.ticketid = @TicketId
      AND t.employeeid = ie.emp
);

-- 3️⃣ UPDATE assignment table (ONLY manual employees in array)
UPDATE helpdesk.ticket_assignments
SET 
    assigned_employees = (
        SELECT ARRAY(
            SELECT DISTINCT emp
            FROM (
                -- existing manual employees
                SELECT UNNEST(COALESCE(assigned_employees, '{}')) AS emp
                
                UNION
                
                -- new manual employees ONLY
                SELECT UNNEST(COALESCE(@AssignedEmployees::uuid[], '{}')) AS emp
            ) x
            WHERE emp IS NOT NULL
        )
    ),
    groupid = COALESCE(@GroupId, groupid)
WHERE ticketid = @TicketId;

";

                var rows = await ExecuteAsync(sql, new
                {
                    TicketId = request.TicketId,
                    GroupId = request.GroupId,
                    AssignedEmployees = request.AssignedEmployees,
                    AssignedBy = assignedBy,
                    Reason = request.Reason
                });

                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning ticket {TicketId}", request.TicketId);
                throw;
            }
        }
        public async Task<List<Guid>> GetAssignmentsByGroupIdAsync(Guid groupId)
        {
            if (groupId == Guid.Empty)
                throw new ArgumentException("GroupId is required");

            var sql = @"
        SELECT assignmentid
        FROM helpdesk.ticket_assignments
        WHERE groupid = @GroupId;
    ";

            var assignmentIds = (await QueryAsync<Guid>(sql, new { GroupId = groupId })).ToList();
            return assignmentIds;
        }

        public async Task<bool> DeleteOldGroupEmployeesFromAssignmentsAsync(List<Guid> oldGroupEmployeeIds, List<Guid> assignmentIds)
        {
            try
            {
                if (oldGroupEmployeeIds == null || !oldGroupEmployeeIds.Any())
                    return false;

                if (assignmentIds == null || !assignmentIds.Any())
                    return false;

                // 🔹 Delete old group employees from the provided assignments
                var sql = @"
DELETE FROM helpdesk.ticket_assigned_employees
WHERE assignmentid = ANY(@AssignmentIds)
  AND employeeid = ANY(@OldEmployeeIds);
";

                var parameters = new
                {
                    AssignmentIds = assignmentIds.ToArray(),
                    OldEmployeeIds = oldGroupEmployeeIds.ToArray()
                };

                var rowsAffected = await ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old group employees from ticket assignments");
                throw;
            }
        }

        public async Task<bool> CreateMessageAsync(Guid organizationId, TicketMessageRequest request)
        {
            // SQL to insert into ticket_messages
            var sql = @"
    INSERT INTO helpdesk.ticket_messages
    (
        ticket_id,
        sender_id,
        message,
        message_type,
        subject,
        organizationid,
        attachment_url,
        created_at
    )
    VALUES
    (
        @TicketId,
        @SenderId,
        @Message,
        @MessageType,
        @Subject,
        @OrganizationId,
        @attachmenturl,
        CURRENT_TIMESTAMP
    )";

            // Execute insert and get affected rows
            var affectedRows = await ExecuteAsync(sql, new
            {
                TicketId = request.TicketId,
                SenderId = request.SenderId,
                Message = request.Message,
                MessageType = request.MessageType,
                Subject = request.Subject,
                OrganizationId = organizationId,
                attachmenturl=request.attachment
            });

            // If message was inserted and recipients exist, insert into ticket_message_recipients
            if (affectedRows > 0 && request.RecipientIds != null && request.RecipientIds.Any())
            {
                // Optionally, get the last inserted message_id
                var messageIdSql = "SELECT message_id FROM helpdesk.ticket_messages WHERE ticket_id = @TicketId AND sender_id = @SenderId ORDER BY created_at DESC LIMIT 1";
                var messageId = await QuerySingleAsync<Guid>(messageIdSql, new { TicketId = request.TicketId, SenderId = request.SenderId });

                var recipientSql = @"
        INSERT INTO helpdesk.ticket_message_recipients
        (
            message_id,
            recipient_id,
            is_read
        )
        VALUES
        (
            @MessageId,
            @RecipientId,
            false
        )";

                foreach (var recipientId in request.RecipientIds)
                {
                    await ExecuteAsync(recipientSql, new
                    {
                        MessageId = messageId,
                        RecipientId = recipientId
                    });
                }
            }

            return affectedRows > 0;
        }
        public async Task<List<TicketMessageDto>> GetMessagesByTicketIdAsync(
    Guid organizationId,
    Guid ticketId)
        {
            try
            {
                var sql = @"
SELECT
    m.message_id AS MessageId,
    m.ticket_id AS TicketId,
    m.sender_id AS SenderId,
    m.message AS Message,
    m.message_type AS MessageType,
    m.subject AS Subject,
    m.attachment_url  AS  attachmentUrl,
    m.created_at AS CreatedAt,

    ARRAY(
        SELECT r.recipient_id
        FROM helpdesk.ticket_message_recipients r
        WHERE r.message_id = m.message_id
    ) AS RecipientIds,

    -- ✅ Sender Full Name
    COALESCE(
        CONCAT(e.firstname, ' ', e.lastname),
        'Unknown Sender'
    ) AS SenderFullName,

    -- ✅ Sender Email
    COALESCE(e.email, '') AS SenderEmail

FROM helpdesk.ticket_messages m

LEFT JOIN employee.employees e
    ON e.employeeid = m.sender_id
   AND e.organizationid = m.organizationid   -- 🔥 IMPORTANT FIX

WHERE m.organizationid = @OrganizationId
  AND m.ticket_id = @TicketId

ORDER BY m.created_at ASC;
";

                var result = await QueryAsync<TicketMessageDto>(sql, new
                {
                    OrganizationId = organizationId,
                    TicketId = ticketId
                });

                return result?.ToList() ?? new List<TicketMessageDto>();
            }
            catch (Exception ex)
            {
                // optional: log error here
                throw;
            }
        }
        public async Task<bool> CreateCategoriesAsync(Guid organizationId, Guid createdBy, CreateCategoryRequest request)
        {
            try
            {
                var sql = @"
INSERT INTO helpdesk.categories
(
    organizationid,
    departmentid,
    employeeid,
    name,
    status,
    createdat,
    updatedat
)
VALUES
(
    @OrganizationId,
    @DepartmentId,
    @empId,
    @Name,
    @Status,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
);
";

                var parameters = new
                {
                    OrganizationId = organizationId,
                    DepartmentId = request.DepartmentId,
                    empId=createdBy,
                    Name = request.Name,
                    Status = request.Status
                    // createdBy can be added if you have column later
                };

                var rowsAffected = await ExecuteAsync(sql, parameters);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                throw;
            }
        }

        public async Task<List<CategoryDto>> GetCategoriesAsync(Guid organizationId, searchCateogry? search)
        {
            try
            {
                // 🔹 Ensure search is not null
                search ??= new searchCateogry(); // default values will be used

                // 🔹 SQL query with department name
                var sql = @"
SELECT 
    c.categoryid AS CategoryId,
    c.organizationid AS OrganizationId,
    c.departmentid AS DepartmentId,
    d.departmentname AS DepartmentName,
    c.name AS CategoryName,
    c.status AS Status,
    c.createdat AS CreatedAt
FROM helpdesk.categories c
LEFT JOIN employee.departments d
    ON c.departmentid = d.departmentid
    AND d.isactive = true
WHERE c.organizationid = @OrganizationId
  AND (@DepartmentId IS NULL OR c.departmentid = @DepartmentId)
  AND (@SearchTerm IS NULL OR LOWER(c.name) LIKE '%' || LOWER(@SearchTerm) || '%')
ORDER BY c.createdat DESC
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;
";

                // 🔹 Calculate offset for pagination
                var offset = (search.Page - 1) * search.PageSize;

                var parameters = new
                {
                    OrganizationId = organizationId,
                    DepartmentId = search.departmentId,
                    SearchTerm = search.SearchTerm,
                    Offset = offset,
                    PageSize = search.PageSize
                };

                var result = await QueryAsync<CategoryDto>(sql, parameters);

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories");
                throw;
            }
        }


        public async Task<bool> UpdateCategoryAsync(Guid organizationId, UpdateCategoryRequest request, Guid empId)
        {
            try
            {
                var sql = @"
UPDATE helpdesk.categories
SET
    departmentid = @DepartmentId,
    employeeid = @EmployeeId,
    name = @CategoryName,
    status = @Status,
    updatedat = CURRENT_TIMESTAMP
WHERE categoryid = @CategoryId
  AND organizationid = @OrganizationId;
";

                var parameters = new
                {
                    CategoryId = request.CategoryId,
                    OrganizationId = organizationId,
                    DepartmentId = request.DepartmentId,
                    EmployeeId = empId, // ✅ now used
                    CategoryName = request.CategoryName,
                    Status = request.Status
                };

                var rowsAffected = await ExecuteAsync(sql, parameters);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(Guid organizationId, Guid categoryId)
        {
            try
            {
                var sql = @"
DELETE FROM helpdesk.categories
WHERE categoryid = @CategoryId
  AND organizationid = @OrganizationId;
";

                var parameters = new
                {
                    CategoryId = categoryId,
                    OrganizationId = organizationId
                };

                var rowsAffected = await ExecuteAsync(sql, parameters);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                throw;
            }
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(Guid organizationId, Guid categoryId)
        {
            try
            {
                var sql = @"
SELECT 
    categoryid AS CategoryId,
    organizationid AS OrganizationId,
    departmentid AS DepartmentId,
    name AS CategoryName,
    status AS Status,
    createdat AS CreatedAt
FROM helpdesk.categories
WHERE categoryid = @CategoryId
  AND organizationid = @OrganizationId
  AND isactive = true;
";

                var parameters = new
                {
                    CategoryId = categoryId,
                    OrganizationId = organizationId
                };

                var result = await QuerySingleOrDefaultAsync<CategoryDto>(sql, parameters);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category by ID");
                throw;
            }
        }




    }
}
