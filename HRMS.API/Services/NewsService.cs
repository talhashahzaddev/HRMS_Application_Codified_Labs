using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Org.BouncyCastle.Asn1.Ocsp;
using static HRMS.API.Models.DTOs.NewsDTOs;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Services
{
    public class NewsService : INewsService
    {
        private readonly ILogger<NewsService> _logger;
        private readonly INewsRepository _newsRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IEmailService _emailService;
        public NewsService(ILogger<NewsService> logger, INewsRepository newsRepository, IAttendanceRepository attendanceRepository, ICloudinaryService cloudinaryService,
            IEmployeeRepository employeeRepository, INotificationRepository notificationRepository, IEmailService emailService)
        {
            _logger = logger;
            _newsRepository = newsRepository;
            _attendanceRepository = attendanceRepository;
            _cloudinaryService = cloudinaryService;
            _employeeRepository = employeeRepository;
            _notificationRepository = notificationRepository;
            _emailService = emailService;
        }

        public async Task<NewsDto> CreateNewsAsync(Guid employeeId, Guid organizationId, NewsRequest request)
        {
            _logger.LogInformation("Creating news for employee {EmployeeId} in organization {OrganizationId}", employeeId, organizationId);
            var createdNews = await _newsRepository.CreateNewsAsync(employeeId, organizationId, request);
            if (createdNews != null && request.Status.Equals("Published", StringComparison.OrdinalIgnoreCase))
            {
                List<Guid> receiverIds = new List<Guid>();
                if (request.VisibleTo == "Everyone")
                {
                    var allEmployees = await _employeeRepository.GetEmployeesAsync(organizationId);
                    receiverIds = allEmployees.Data.Where(e => e.EmployeeId.HasValue).Select(e => e.EmployeeId.Value).ToList();

                }
                else if (request.VisibleTo == "Selected" &&
                         request.SelectedEmployees != null &&
                         request.SelectedEmployees.Any())
                {
                    receiverIds = request.SelectedEmployees.ToList();
                }
                if (receiverIds.Any())
                {
                    var wordLimit = 15;
                    var descriptionPreview = string.IsNullOrEmpty(request.Description)
                        ? ""
                        : string.Join(" ", request.Description
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Take(wordLimit)) + (request.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > wordLimit ? "..." : "");

                    var notification = new NotificationCreateDto
                    {
                        Receiveruserid = receiverIds,
                        Senderuserid = employeeId,
                        Title = $"{request.Title}",
                        Message = $"\"{request.Title}\" has been published. {descriptionPreview}",
                        Notificationtype = "Message",
                        Createdby = employeeId,
                        moduletype = "News",
                        Redirecturl = "/news/dashboard",
                        organizationId = organizationId,
                        RequestId = createdNews.NewsId
                    };
                    await _notificationRepository.CreateNotificationAsync(organizationId, notification);
                }
            }
            //Implemennting Email in news creation
            var ReciverEmails = new List<string>();
            if(createdNews!=null && request.Status.Equals("Published", StringComparison.OrdinalIgnoreCase)) 
            {
                if (request.VisibleTo == "Everyone")
                {
                    var allEmployees = await _employeeRepository.GetEmployeesAsync(organizationId);
                    ReciverEmails = allEmployees.Data.Where(e => e.Email != null).Select(e => e.Email).ToList();
                    await _emailService.SendNewsEmailAsync(ReciverEmails,request,organizationId,createdNews.NewsId);

                }
                else if (request.VisibleTo == "Selected")
                {
                    List<Guid> selectedEmployeeIds = request.SelectedEmployees?.ToList() ?? new List<Guid>();

                    var selectedEmployees = await _employeeRepository.GeSelectedtEmployeesByIdsAsync(selectedEmployeeIds);
                    ReciverEmails = selectedEmployees.Where(e => e.Email != null).Select(e => e.Email).ToList();
                    await _emailService.SendNewsEmailAsync(ReciverEmails, request,organizationId,createdNews.NewsId);

                }
            }
            return createdNews;
        }
        public async Task<PagedResult<NewsDto>> GetAllNewsAsync(Guid employeeId, Guid organizationId, NewsSearchViewModel searchModel)
        {
            _logger.LogInformation("Creating news for employee {EmployeeId} in organization {OrganizationId}", employeeId, organizationId);
            var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(employeeId, organizationId);
            if (isAdminOrHR)
            {
                _logger.LogInformation("User is Super Admin or HR Manager - returning all employees attendance report");
                var allNews = await _newsRepository.GetAllNewsAdminAsync(employeeId, organizationId, searchModel);
                return allNews;
            }
            else
            {
                var allnews = await _newsRepository.GetAllEmployeeAsync(employeeId, organizationId, searchModel);
                return allnews;
            }
        }
        public async Task<bool> UpdateNewsAsync(Guid newsId, Guid employeeId, Guid organizationId, NewsRequest request)
        {
            _logger.LogInformation("Updating news {NewsId} by employee {EmployeeId} in organization {OrganizationId}", newsId, employeeId, organizationId);
            var news = await _newsRepository.GetNewsByNewsIdAsync(newsId);
            var isUpdated = await _newsRepository.UpdateNewsAsync(
                newsId,
                employeeId,
                organizationId,
                request
            );

            if (!isUpdated)
                throw new KeyNotFoundException("News not found or you are not authorized to update this news");
            return true;
        }
        public async Task<NewsDto> GetNewsByNewsIdAsync(Guid newsId)
        {
            _logger.LogInformation("Updating news {NewsId} by employee {EmployeeId} in organization {OrganizationId}", newsId);
            var result = await _newsRepository.GetNewsByNewsIdAsync(newsId);

            return result;
        }

        public async Task<bool> DeleteNewsAsync(Guid newsId)
        {
            _logger.LogInformation("Deleting news with id {NewsId}", newsId);

            if (newsId == Guid.Empty)
                throw new ArgumentException("Invalid news id");
            //Getting news by news id if not news return exception
            var news = await _newsRepository.GetNewsByNewsIdAsync(newsId);
            if (news == null)
            {
                throw new KeyNotFoundException("News id does not exists");
            }

            var isDeleted = await _newsRepository.DeleteNewsAsync(newsId);

            if (!isDeleted)
                throw new KeyNotFoundException("News not found or you are not authorized to delete this news");

            return true;
        }

        public async Task<bool> AddNewsViewAsync(Guid newsId)
        {
            _logger.LogInformation("Incrementing view count for news with id {NewsId}", newsId);

            if (newsId == Guid.Empty)
                throw new ArgumentException("Invalid news id");

            // Get news by news id – if not found, throw exception
            var news = await _newsRepository.GetNewsByNewsIdAsync(newsId);
            if (news == null)
            {
                throw new KeyNotFoundException("News id does not exists");
            }

            var isUpdated = await _newsRepository.IncrementNewsViewAsync(newsId);

            if (!isUpdated)
                throw new KeyNotFoundException("Failed to update news view count");

            return true;
        }
        public async Task<bool> MarkNewsAsReadAsync(Guid newsId, Guid employeeId)
        {
            if (newsId == Guid.Empty)
                throw new ArgumentException("Invalid news ID");

            if (employeeId == Guid.Empty)
                throw new ArgumentException("Invalid employee ID");
            try
            {
                var isFirstRead = await _newsRepository.MarkNewsAsReadAsync(newsId, employeeId);
                return isFirstRead;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking news as read. NewsId: {NewsId}, EmployeeId: {EmployeeId}", newsId, employeeId);
                throw;
            }
        }
        public async Task<bool> UpdateNewsStatusAsync(Guid newsId, string status)
        {
            _logger.LogInformation("Updating status of news {NewsId} to {Status}", newsId, status);
            if (newsId == Guid.Empty)
                throw new ArgumentException("Invalid news id");
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Status cannot be empty");
            // Call repository to update status
            var isUpdated = await _newsRepository.UpdateNewsStatusAsync(newsId, status);
            var Originalnotification = await _notificationRepository.getnotificationdetailAsync(newsId);
            var News = await _newsRepository.GetNewsByNewsIdAsync(newsId);
            if (isUpdated != false && status == "Published")
            {
                List<Guid> receiverIds = new List<Guid>();
                if (News.VisibleTo == "Everyone")
                {
                    var allEmployees = await _employeeRepository.GetEmployeesAsync(News.OrganizationId);
                    receiverIds = allEmployees.Data.Where(e => e.EmployeeId.HasValue).Select(e => e.EmployeeId.Value).ToList();

                }
                else if (News.VisibleTo == "Selected" &&
                         News.SelectedEmployees != null &&
                         News.SelectedEmployees.Any())
                {
                    receiverIds = News.SelectedEmployees.ToList();
                }
                if (receiverIds.Any())
                {
                    var wordLimit = 15;
                    var descriptionPreview = string.IsNullOrEmpty(News.Description)
                        ? ""
                        : string.Join(" ", News.Description
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Take(wordLimit)) + (News.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > wordLimit ? "..." : "");

                    var notification = new NotificationCreateDto
                    {
                        Receiveruserid = receiverIds,
                        Senderuserid = News.CreatedBy,
                        Title = $"{News.Title}",
                        Message = $"\"{News.Title}\" has been published. {descriptionPreview}",
                        Notificationtype = "Message",
                        Createdby = News.CreatedBy,
                        moduletype = "News",
                        Redirecturl = "/news/dashboard",
                        organizationId = News.OrganizationId,
                        RequestId = News.NewsId
                    };
                    await _notificationRepository.CreateNotificationAsync(News.OrganizationId, notification);
                }
            }
            return isUpdated;

        }

        public async Task<Guid> GetEmployeeFromTable(Guid Userid)
        {
            _logger.LogInformation("Getting Employee Id against User id: {EmployeeId}", Userid);
            return await _attendanceRepository.GetEmployeeFromTable(Userid);
        }


    }
}
