namespace HRMS.API.Models.DTOs
{
    public class AiChatRequestDto
    {
        public Guid employeeid { get; set; }
        public Guid organizationid { get; set; }
        public string rolename { get; set; } = string.Empty;
        public string usermessage { get; set; } = string.Empty;
        public string intent { get; set; } = string.Empty;
        public string airesponse { get; set; } = string.Empty;
        public string modulename { get; set; }
        public DateTime createdat { get; set; }
    }
}
