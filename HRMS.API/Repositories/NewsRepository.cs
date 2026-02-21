using DocumentFormat.OpenXml.Wordprocessing;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Npgsql;
using NPOI.SS.Formula;
using Org.BouncyCastle.Asn1.Ocsp;
using static HRMS.API.Models.DTOs.NewsDTOs;

namespace HRMS.API.Repositories
{
    public class NewsRepository : BaseRepository, INewsRepository
    {
        private readonly ILogger<NewsRepository> _logger;
        public NewsRepository(ILogger<NewsRepository> logger, IDatabaseConnectionFactory connectionFactory) : base(connectionFactory)
        {
            _logger = logger;
        }
        public NewsRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

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
        public async Task<NewsDto> CreateNewsAsync(Guid createdBy, Guid organizationId, NewsRequest news)
        {
        var sql = @"
        INSERT INTO news.news 
        (title, description,organizationid, createdby, category, imageurl, publishedat, status, visibleto, selectedemployees)
        VALUES 
        (@Title, @Description,@Organizationid, @CreatedBy, @Category, @ImageUrl, @PublishedAt, @Status, @VisibleTo, @SelectedEmployees)
        RETURNING newsid as  NewsId,title as Title,description as Description;
    ";
         try
            {
         var newsId = await QuerySingleAsync<NewsDto>(sql,new
                   {
                   Title = news.Title,
                   Description = news.Description,
                   CreatedBy = createdBy,
                   Organizationid = organizationId,
                   Category = news.Category ?? "Other",
                   ImageUrl = news.ImageUrl,
                   PublishedAt = DateTime.UtcNow,
                   Status = news.Status ?? "Draft",
                   VisibleTo = news.VisibleTo ?? "Everyone",
                   SelectedEmployees = news.SelectedEmployees
                   });
                   return newsId;
            }
         catch (PostgresException ex) when (ex.SqlState == "23505") // unique violation
            {
             throw new InvalidOperationException("A news item with the same identifier already exists.", ex);
            }
         catch (Exception ex)
            {
             _logger.LogError(ex, "Error creating news item");
             throw new InvalidOperationException("An error occurred while creating the news item.", ex);
            }
        }
        public async Task<PagedResult<NewsDto>> GetAllNewsAdminAsync(Guid employeeId, Guid OrganizationId, NewsSearchViewModel searchModel)
        {
         try
            {
         var sql = @"select * from news.fn_get_news_admin(@empId,@organizationId,@searchTerm,@status,@limit,@offset)";
         var results = await QueryAsync<NewsDto>(sql,new
                  {
                  empId = employeeId,
                  organizationId = OrganizationId,
                  status = searchModel.Status,
                  searchTerm = searchModel.SearchTerm,
                  limit = searchModel.PageSize,
                  offset = (searchModel.Page - 1) * searchModel.PageSize
                  });
         var news = results.Select(r => new NewsDto
                  {
                  NewsId = r.NewsId ?? Guid.Empty,
                  Title = r.Title ?? string.Empty,
                  CreatedBy = r.CreatedBy,
                  Category = r.Category ?? string.Empty,
                  ImageUrl = r.ImageUrl ?? string.Empty,
                  PublishedAt = r.PublishedAt ?? DateTime.MinValue,
                  Status = r.Status ?? string.Empty,
                  VisibleTo = r.VisibleTo,
                  SelectedEmployees = r.SelectedEmployees ?? [],
                  Views = r.Views,                 // <-- add Views
                  IsRead = r.IsRead,
                  CreatedAt = r.CreatedAt ?? null,
                  Description = r.Description,
                  OrganizationId=r.OrganizationId,
             AuthorName = r.AuthorName
         }).ToList();
         var totalCount = news.Count;
                return new PagedResult<NewsDto>
                {
                  Data = news,
                  TotalCount = (int)totalCount,
                  Page = searchModel.Page,
                  PageSize = searchModel.PageSize
                };
           }
          catch (Exception ex){throw new InvalidOperationException("An error occured while getting the News", ex);
          }
        }
        public async Task<PagedResult<NewsDto>> GetAllEmployeeAsync(Guid employeeId, Guid OrganizationId, NewsSearchViewModel searchModel)
        {
         try
            {
         var sql = @"select * from news.fn_get_news_employee(@empId,@organizationId,@searchTerm,@status,@limit,@offset)";
         var results = await QueryAsync<NewsDto>(sql, new
                  {
                  empId = employeeId,
                  organizationId = OrganizationId,
                  status = "Published",
                  searchTerm = searchModel.SearchTerm,
                  limit = searchModel.PageSize,
                  offset = (searchModel.Page - 1) * searchModel.PageSize
                  });
         var news = results.Select(r => new NewsDto
                {
                  NewsId = r.NewsId ?? Guid.Empty,
                  Title = r.Title ?? string.Empty,
                  CreatedBy = r.CreatedBy,
                  Category = r.Category ?? string.Empty,
                  ImageUrl = r.ImageUrl ?? string.Empty,
                  PublishedAt = r.PublishedAt ?? DateTime.MinValue,
                  Status = r.Status ?? string.Empty,
                  VisibleTo = r.VisibleTo,
                  SelectedEmployees = r.SelectedEmployees ?? [],
                  Views = r.Views,                 // <-- add Views
                  IsRead = r.IsRead,
                  CreatedAt = r.CreatedAt ?? null,
                  Description = r.Description,
                  OrganizationId=r.OrganizationId,
                  AuthorName=r.AuthorName
                  }).ToList();
         var totalCount = news.Count;
                return new PagedResult<NewsDto>
                {
                  Data = news,
                  TotalCount = (int)totalCount,
                  Page = searchModel.Page,
                  PageSize = searchModel.PageSize
                };
            }
         catch (Exception ex)
            {
                throw new InvalidOperationException("An error occured while getting the News", ex);
            }
        }
        public async Task<bool> UpdateNewsAsync(Guid newsId, Guid employeeId, Guid organizationId, NewsRequest request)
        {
         try
            {
         var sql = @"
           UPDATE news.news
           SET
           title=@Title,
           description=@Description,
           category=@Category,
           imageurl=@ImageUrl,
           publishedat=CURRENT_TIMESTAMP,
           status=@Status,
           visibleto=@VisibleTo,
           selectedemployees=@SelectedEmployees,
           updatedat= CURRENT_TIMESTAMP
           WHERE newsid = @NewsId
           AND organizationid = @OrganizationId
        ";

         var parameters = new{
                  NewsId = newsId,
                  OrganizationId = organizationId,
                  EmployeeId = employeeId,
                  request.Title,
                  request.Description,
                  request.Category,
                  request.ImageUrl,
                  request.Status,
                  request.VisibleTo,
                  SelectedEmployees = request.SelectedEmployees ?? Array.Empty<Guid>()
                };
         var rowsAffected = await ExecuteAsync(sql, parameters);
          return rowsAffected > 0;
            }
         catch (Exception ex)
            {
               _logger.LogError(ex,
                "Error updating news {NewsId} by employee {EmployeeId} in organization {OrganizationId}",newsId,employeeId,organizationId);
                throw;
            }
        }
      
        public async Task<NewsDto> GetNewsByNewsIdAsync(Guid newsId)
        {
            try
            {
                var sql = @"select  
            n.newsid            as NewsId,
            n.organizationid   as OrganizationId,
            n.title             as Title,
            n.description       as Description,
            n.createdby         as CreatedBy,
            n.category          as Category,
            n.imageurl          as ImageUrl,
            n.publishedat       as PublishedAt,
            n.status            as Status,
            n.visibleto         as VisibleTo,
            n.selectedemployees as SelectedEmployees,
            n.createdat         as CreatedAt,
            concat(e.firstname, ' ', e.lastname) as AuthorName
        from news.news n
        left join employee.employees e 
            on e.employeeid = n.createdby
        where n.newsid = @newsId";

                var result = await QuerySingleAsync<NewsDto>(sql, new { newsId });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting news {NewsId}",
                    newsId);

                throw;
            }
        }

        public async Task<bool> DeleteNewsAsync(Guid newsId)
        {
         try
            {
             var sql = @"
            DELETE FROM news.news WHERE newsid = @NewsId ";
            var rowsAffected = await ExecuteAsync(sql, new{NewsId = newsId});
                return rowsAffected > 0;
            }
         catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting news with id {NewsId}", newsId);
                throw;
            }
        }
        public async Task<bool> MarkNewsAsReadAsync(Guid newsId, Guid employeeId)
        {
         try
            {
            var sql = @"
             WITH ins AS (
             INSERT INTO news.news_reads_count (newsid, employeeid)
             VALUES (@NewsId, @EmployeeId)
             ON CONFLICT DO NOTHING
             RETURNING *)
             UPDATE news.news
             SET views = views + 1
             WHERE newsid = @NewsId
             AND EXISTS (SELECT 1 FROM ins);

";
            var rowsAffected = await ExecuteAsync(sql, new{
                NewsId = newsId,
                EmployeeId = employeeId
                });
                return rowsAffected > 0;
            }
         catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking news as read. NewsId: {NewsId}, EmployeeId: {EmployeeId}", newsId, employeeId);
                throw;
            }
        }
        public async Task<bool> IncrementNewsViewAsync(Guid newsId)
        {
          try
            {
            var sql = @"
            UPDATE news.news
            SET views = views + 1
            WHERE newsid = @NewsId;
        ";
            var rowsAffected = await ExecuteAsync(sql, new
                {NewsId = newsId});
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing view count for news with id {NewsId}", newsId); throw;
            }
        }
        public async Task<bool> UpdateNewsStatusAsync(Guid newsId, string status)
        {
          try
            {
            var sql = @"
            UPDATE news.news
            SET
            status = @Status,
            updatedat = CURRENT_TIMESTAMP,
            publishedat = CASE 
            WHEN @Status = 'Published' AND publishedat IS NULL THEN CURRENT_TIMESTAMP
            ELSE publishedat
            END
            WHERE newsid = @NewsId;
        ";
            var rowsAffected = await ExecuteAsync(sql, new
                {NewsId = newsId,
                 Status = status
                 });
                return rowsAffected > 0;
            }
          catch (Exception ex)
            {_logger.LogError(ex, "Error updating status for news {NewsId}", newsId);throw;}
        }

    }
}
