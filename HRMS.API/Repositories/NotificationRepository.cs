using DocumentFormat.OpenXml.InkML;
using HRMS.API.Configuration;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Org.BouncyCastle.Asn1.Ocsp;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Repositories
{
    public class NotificationRepository : BaseRepository, INotificationRepository
    {
        public NotificationRepository(IDatabaseConnectionFactory connectionFactory)
        : base(connectionFactory) { }


        public async Task<Guid> GetEmpId(Guid? employeeId)
        {
            try
            {
                var sql = @"
            SELECT em.employeeid
            FROM employee.employees em
            WHERE ( em.userid = @employeeId)
            LIMIT 1";

                var results = await QueryAsync<Guid>(sql, new { employeeId });

                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                return Guid.Empty;
            }
        }

        public async Task<Guid> GetReciveridAsync(Guid employeeId)
        {
            try
            {
                var sql = @"
            SELECT ed.reportingmanagerid
            FROM employee.employmentdetails ed
            WHERE ed.employeeid = @EmployeeId
              AND ed.iscurrent = true
            LIMIT 1";

                var results = await QueryAsync<Guid>(sql, new { EmployeeId = employeeId });

                return results.FirstOrDefault(); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching reporting manager: {ex.Message}");
                return Guid.Empty;
            }
        }

        public async Task<Guid> GetNoManagerReciveridAsync(Guid employeeId)
        {
            try
            {
                var sql = @"
            SELECT e.employeeid
            FROM employee.employees e
            JOIN auth.users u ON u.userid = e.userid
            WHERE e.organizationid = (
                    SELECT organizationid
                    FROM employee.employees
                    WHERE employeeid = 'fd0db15e-8f0c-4832-89b8-ae052b02d30e'
                    LIMIT 1
                )
            AND u.roleid = (
                    SELECT roleid
                    FROM auth.roles
                    WHERE rolename = 'Super Admin'
                    AND organizationid = e.organizationid
                    LIMIT 1
                )
            LIMIT 1";

                var results = await QueryAsync<Guid>(sql, new { EmployeeId = employeeId });

                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching reporting manager: {ex.Message}");
                return Guid.Empty;
            }
        }




        public async Task<bool> CreateNotificationAsync(Guid? organizationId, NotificationCreateDto request)
        {


            var sql = @"
            INSERT INTO notification.notifications
            (
                organizationid,
                receiveruserid,
                senderuserid,
                title,
                notificationtype,
                message,
                redirecturl, 
                requestid,
                isread,
                moduletype,
                createdby
            )
            VALUES
            (
                @organizationId,
                @receiverUserId,
                @senderUserId,
                @title,
                @notificationType,
                @message,
                @redirectUrl,
                @requestId,
                false,
                @Moduletype,
                @senderUserId
            )";

            var affectedRows = await ExecuteAsync(sql, new
            {
                organizationId,
                receiverUserId = request.Receiveruserid,
                senderUserId = request.Senderuserid,
                title = request.Title,
                notificationType = request.Notificationtype,
                message = request.Message,
                requestId=request.RequestId,
                redirectUrl = request.Redirecturl,
                Moduletype=request.moduletype,
            });

            return affectedRows > 0;
        }


        // Get notifications for a specific user
        public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId, Guid organizationId)
        {
            var userid = await GetEmpId(userId);
            var sql = @"
        SELECT *
        FROM notification.notifications
        WHERE @UserId = ANY(receiveruserid)
        AND organizationid = @OrganizationId
        ORDER BY createdat DESC
    ";

            // Execute query and map results to Notification objects
            var notifications = await QueryAsync<Notification>(sql, new { UserId = userid, OrganizationId = organizationId });
            return notifications.ToList();
        }

        //Get notifcation details From request id 

        //public async Task<Notification> getnotificationdetailAsync(Guid requestId)
        //{

        //    var sql = @"
        //    SELECT *
        //    FROM notification.notifications
        //    WHERE requestid = @requestId
        //    ORDER BY createdat DESC
        //    LIMIT 1
        //";

        //    // Execute query and map results to Notification objects
        //    var notifications = await QueryAsync<Notification>(sql, new { requestId = requestId });

        //    return notifications.FirstOrDefault();

        //}



        public async Task<Notification> getnotificationdetailAsync(Guid requestId)
        {
            var sql = @"
      SELECT notificationid,
             receiveruserid AS ""ReceiverUserIds"",
             senderuserid AS ""SenderUserId"",
             organizationid AS ""organizationId"",
             title,
             notificationtype AS ""NotificationType"",
             requestid,
             message AS ""Message"",
             redirecturl AS ""RedirectUrl"",
             moduletype AS ""moduletype"",
             isread AS ""IsRead"",
             createdat AS ""CreatedAt""
      FROM notification.notifications
      WHERE requestid = @requestId
      ORDER BY createdat DESC
      LIMIT 1
  ";

            var notifications = await QueryAsync<Notification>(sql, new { requestId });
            return notifications.FirstOrDefault();
        }


        //Delete Noitfication

        public async Task<bool> DeleteNotificationAsync(Guid id)
        {
            
                var sql = @"delete from notification.notifications where requestid=@id ";

                var results = await ExecuteAsync(sql, new{id});
                return results>0;

        }



        // Get userId and organizationId from employeeId
        public async Task<(Guid userId, Guid organizationId)?> GetEmployeeUserInfoAsync(Guid employeeId)
        {
            try
            {
                var sql = @"
                    SELECT userid, organizationid
                    FROM employee.employees
                    WHERE employeeid = @employeeId
                    LIMIT 1";

                var result = await QuerySingleOrDefaultAsync<(Guid userId, Guid organizationId)?>(sql, new { employeeId });
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }



        public async Task<bool> MarkNotificationAsReadAsync(Guid notificationId)
        {
            var sql = @"
        UPDATE notification.notifications
        SET isread = TRUE
        WHERE notificationid = @notificationId";

            var affectedRows = await ExecuteAsync(sql, new { notificationId });
            return affectedRows > 0;
        }




    }
}
