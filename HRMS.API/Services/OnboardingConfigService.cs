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
            ["Personal Information"] = new()
            {
                "Profile Photo", "Full Name", "Job Title", "Email Address", "Phone Number",
                "Department", "Gender", "Nationality", "National ID/CNIC", "Resume/CV"
            },
            ["Qualification Details"] = new()
            {
                "Degree Level", "Institute Name", "Field of Study", "GPA", "ATTACH DOCOMENT", "Discription"
            },
            ["Work Experience"] = new()
            {
                "JOB TITTLE", "COMPANY", "EMPLOYMENT TYPE", "LOCATION", "SATRT DATE", "END DATE",
                "Currently working here", "KEY RESPONSIBILTIES", "ATTACH DOCOMENT"
            },
            ["BANK DETAILS"] = new()
            {
                "ACCOUNT HOLDER NAME", "PAYMENT METHOD", "ACCOUNT NUMBER", "CONFIRM ACCOUNT NUMBER",
                "BANK NAME", "IBN/SWIFT CODE", "BRANCH NAME", "BRANCH CODE", "BANK & BRANCH"
            }
        };

        // AlwaysRequired has been REMOVED.
        // All field required-status is now driven purely by Super Admin configuration.

        public OnboardingConfigService(IOnboardingConfigRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<FieldConfigRecord>> GetConfigAsync(Guid organizationId)
        {
            var records = await _repo.GetByOrganizationAsync(organizationId);
            return records;
        }

        //public async Task<bool> SaveConfigAsync(Guid organizationId, OnboardingFieldConfigRequest request)
        //{
        //    var records = new List<FieldConfigRecord>();

        //    foreach (var (section, fields) in request.Sections)
        //    {
        //        foreach (var (fieldKey, config) in fields)
        //        {
        //            // No AlwaysRequired override — honour exactly what Super Admin sent.
        //            // A disabled field cannot be required (enforce consistency here).
        //            records.Add(new FieldConfigRecord
        //            {
        //                OrganizationId = organizationId,
        //                Section        = section,
        //                FieldKey       = fieldKey,
        //                Enabled        = config.Enabled,
        //                Required       = config.Enabled && config.Required
        //                // If disabled, required is forced false to stay consistent.
        //            });
        //        }
        //    }

        //    return await _repo.BulkUpsertAsync(organizationId, records);
        //}

        public async Task<bool> UpdateConfigAsync(Guid organizationId,UpdateOnBoardingConfigurationRequest request)
        {
            return await _repo.UpdateConfigAsync(organizationId, request);
        }

        public async Task<bool> SaveConfigAsync(Guid organizationId,UpdateOnBoardingConfigurationRequest request)
        {
            var records = new List<FieldConfigRecord>();

            foreach (var field in request.Fields)
            {
                records.Add(new FieldConfigRecord
                {
                    OrganizationId = organizationId,
                    Section = field.Section,
                    FieldKey = field.FieldKey,

                    // keep consistency rule
                    Enabled = field.Enabled,
                    Required = field.Enabled && field.Required
                });
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