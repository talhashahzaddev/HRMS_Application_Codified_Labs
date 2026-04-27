using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services
{
  
   

    // OnboardingConfigService.cs
    public class OnboardingConfigService : IOnboardingConfigService
    {
        private readonly IOnboardingConfigRepository _repo;

        // All known fields per section — this is the master list
        private static readonly Dictionary<string, List<string>> AllFields = new()
        {
            ["personalInformation"] = new()
        {
            "fullName", "jobTitle", "email", "phone",
            "department", "gender", "nationality", "idNumber",
            "profilePhoto", "resume"
        },
            ["educationDetails"] = new()
        {
            "degreeLevel", "institutionName", "fieldOfStudy",
            "gpa", "attachDocument", "description"
        },
            ["workExperience"] = new()
        {
            "jobTitle", "company", "employmentType", "location",
            "startDate", "endDate", "currentlyWorking",
            "keyResponsibilities", "attachDocument"
        }
        };

        // Fields that are always required and cannot be disabled
        private static readonly HashSet<string> AlwaysRequired = new()
    {
        "fullName", "email", "phone"
    };

        public OnboardingConfigService(IOnboardingConfigRepository repo)
        {
            _repo = repo;
        }

        public async Task<OnboardingFieldConfigResponse> GetConfigAsync(Guid organizationId)
        {
            var records = await _repo.GetByOrganizationAsync(organizationId);
            var lookup = records.ToDictionary(r => $"{r.Section}:{r.FieldKey}", r => r);

            var sections = new Dictionary<string, Dictionary<string, FieldConfigDto>>();

            foreach (var (section, fields) in AllFields)
            {
                sections[section] = new Dictionary<string, FieldConfigDto>();
                foreach (var field in fields)
                {
                    var key = $"{section}:{field}";
                    if (lookup.TryGetValue(key, out var saved))
                    {
                        sections[section][field] = new FieldConfigDto
                        {
                            Enabled = AlwaysRequired.Contains(field) || saved.Enabled,
                            Required = AlwaysRequired.Contains(field) || saved.Required
                        };
                    }
                    else
                    {
                        // No saved config → use sensible defaults
                        sections[section][field] = new FieldConfigDto
                        {
                            Enabled = true,
                            Required = AlwaysRequired.Contains(field)
                        };
                    }
                }
            }

            return new OnboardingFieldConfigResponse
            {
                OrganizationId = organizationId,
                Sections = sections
            };
        }

        public async Task<bool> SaveConfigAsync(Guid organizationId, OnboardingFieldConfigRequest request)
        {
            var records = new List<FieldConfigRecord>();

            foreach (var (section, fields) in request.Sections)
            {
                foreach (var (fieldKey, config) in fields)
                {
                    // Protect always-required fields
                    bool enabled = AlwaysRequired.Contains(fieldKey) || config.Enabled;
                    bool required = AlwaysRequired.Contains(fieldKey) || (config.Enabled && config.Required);

                    records.Add(new FieldConfigRecord
                    {
                        OrganizationId = organizationId,
                        Section = section,
                        FieldKey = fieldKey,
                        Enabled = enabled,
                        Required = required
                    });
                }
            }

            return await _repo.BulkUpsertAsync(organizationId, records);
        }

        public Task<OnboardingFieldConfigResponse> GetDefaultConfig()
        {
            var sections = AllFields.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary(
                    f => f,
                    f => new FieldConfigDto
                    {
                        Enabled = true,
                        Required = AlwaysRequired.Contains(f)
                    }
                )
            );

            return Task.FromResult(new OnboardingFieldConfigResponse
            {
                OrganizationId = Guid.Empty,
                Sections = sections
            });
        }
    }
}
