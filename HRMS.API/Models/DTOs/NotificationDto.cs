namespace HRMS.API.Models.DTOs
{
    public class NotificationDto
    {

        public class NotificationCreateDto
        {
            public List<Guid>? Receiveruserid { get; set; }
            public Guid? Senderuserid { get; set; }  // Who triggered it
            public string Title { get; set; }       // Leave, Attendance, Employee
            public string Notificationtype { get; set; } // RequestCreated, Approved, etc.
            public Guid? RequestId { get; set; }  //For getting request id 
            public string Message { get; set; }
            public string Redirecturl { get; set; }
            public string moduletype { get; set; }
            public Guid? Createdby { get; set; }
            public Guid? organizationId { get; set; }
        }

        public class Notification
        {
            public Guid notificationid { get; set; }
            public Guid[] ReceiverUserIds { get; set; }
            public Guid? SenderUserId { get; set; }
            public Guid? organizationId { get; set; }
            public string title { get; set; }
            public string NotificationType { get; set; }
            public Guid requestid { get; set; }
            public string Message { get; set; }
            public string RedirectUrl { get; set; }
            public string moduletype { get; set; }
            public bool IsRead { get; set; }
            public DateTime CreatedAt { get; set; }
            public string requestStatus { get; set; }
        }

        public class leaveNotification
        {
            public Guid reportingmanagerid { get; set; }
            public string email { get; set; }
            public string receivername { get; set; }
        }



    }
}
