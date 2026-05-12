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

        // AlwaysRequired has been REMOVED.
        // All field required-status is now driven purely by Super Admin configuration.

        public OnboardingConfigService(IOnboardingConfigRepository repo)
        {
            _repo = repo;
        }

        public async Task<OnboardingFieldConfigResponse> GetConfigAsync(Guid organizationId)
        {
            var records = await _repo.GetByOrganizationAsync(organizationId);
            var lookup  = records.ToDictionary(r => $"{r.Section}:{r.FieldKey}", r => r);

            var sections = new Dictionary<string, Dictionary<string, FieldConfigDto>>();

            foreach (var (section, fields) in AllFields)
            {
                sections[section] = new Dictionary<string, FieldConfigDto>();
                foreach (var field in fields)
                {
                    var key = $"{section}:{field}";
                    if (lookup.TryGetValue(key, out var saved))
                    {
                        // Use saved config exactly as Super Admin stored it —
                        // no AlwaysRequired override.
                        sections[section][field] = new FieldConfigDto
                        {
                            Enabled  = saved.Enabled,
                            Required = saved.Required
                        };
                    }
                    else
                    {
                        // No saved config → sensible default: visible but NOT required.
                        // Super Admin must explicitly mark a field required.
                        sections[section][field] = new FieldConfigDto
                        {
                            Enabled  = true,
                            Required = false
                        };
                    }
                }
            }

            return new OnboardingFieldConfigResponse
            {
                OrganizationId = organizationId,
                Sections       = sections
            };
        }

        public async Task<bool> SaveConfigAsync(Guid organizationId, OnboardingFieldConfigRequest request)
        {
            var records = new List<FieldConfigRecord>();

            foreach (var (section, fields) in request.Sections)
            {
                foreach (var (fieldKey, config) in fields)
                {
                    // No AlwaysRequired override — honour exactly what Super Admin sent.
                    // A disabled field cannot be required (enforce consistency here).
                    records.Add(new FieldConfigRecord
                    {
                        OrganizationId = organizationId,
                        Section        = section,
                        FieldKey       = fieldKey,
                        Enabled        = config.Enabled,
                        Required       = config.Enabled && config.Required
                        // If disabled, required is forced false to stay consistent.
                    });
                }
            }

            return await _repo.BulkUpsertAsync(organizationId, records);
        }

        public Task<OnboardingFieldConfigResponse> GetDefaultConfig()
        {
            // Default: every field visible, nothing required until Super Admin configures.
            var sections = AllFields.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary(
                    f => f,
                    f => new FieldConfigDto
                    {
                        Enabled  = true,
                        Required = false
                    }
                )
            );

            return Task.FromResult(new OnboardingFieldConfigResponse
            {
                OrganizationId = Guid.Empty,
                Sections       = sections
            });
        }
    }
}