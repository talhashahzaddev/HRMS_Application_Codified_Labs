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

                return results.FirstOrDefault(); // returns Guid.Empty if no manager found
            }
            catch (Exception ex)
            {
                // Log the exception if needed
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
            WHERE receiveruserid = @UserId
            AND organizationid = @OrganizationId
            ORDER BY createdat DESC
        ";

            // Execute query and map results to Notification objects
            var notifications = await QueryAsync<Notification>(sql, new { UserId = userid, OrganizationId = organizationId });
            return notifications.ToList();
        }

        //Get notifcation details From request id 

        public async Task<Notification> getnotificationdetailAsync(Guid requestId)
        {

            var sql = @"
            SELECT *
            FROM notification.notifications
            WHERE requestid = @requestId
            ORDER BY createdat DESC
            LIMIT 1
        ";

            // Execute query and map results to Notification objects
            var notifications = await QueryAsync<Notification>(sql, new { requestId = requestId });

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
