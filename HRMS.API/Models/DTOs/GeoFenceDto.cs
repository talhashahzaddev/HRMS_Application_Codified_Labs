namespace HRMS.API.Models.DTOs;

// ── Geo-Fence DTOs ─────────────────────────────────────────

public class GeoFenceDto
{
    public Guid GeoFenceId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public decimal? CenterLatitude { get; set; }
    public decimal? CenterLongitude { get; set; }
    public int? RadiusMeters { get; set; }
    public string? PolygonCoords { get; set; }
    public string Shape { get; set; } = "circle";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateGeoFenceDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public decimal? CenterLatitude { get; set; }
    public decimal? CenterLongitude { get; set; }
    public int? RadiusMeters { get; set; }
    public string? PolygonCoords { get; set; }
    public string Shape { get; set; } = "circle";
}

public class UpdateGeoFenceDto : CreateGeoFenceDto
{
    public bool IsActive { get; set; } = true;
}

// ── Face Recognition DTOs ──────────────────────────────────

public class FaceRecognitionLogDto
{
    public Guid LogId { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime AttemptTime { get; set; }
    public string MatchResult { get; set; } = "error";
    public decimal? ConfidenceScore { get; set; }
    public decimal? DistanceScore { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool ManuallyOverridden { get; set; }
    public Guid? AttendanceId { get; set; }
}

public class SubmitFaceLogRequest
{
    public string? FaceDescriptor { get; set; }     // JSON 128-float array from face-api.js
    public string? ReferencePhotoUrl { get; set; }
    public string MatchResult { get; set; } = "error";
    public decimal? ConfidenceScore { get; set; }
    public decimal? DistanceScore { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public Guid? GeoFenceId { get; set; }
    public string? DeviceInfo { get; set; }
}

public class SaveFaceDescriptorRequest
{
    public string DescriptorVector { get; set; } = string.Empty; // JSON 128-float array
    public string SourcePhotoUrl { get; set; } = string.Empty;
}

// ── Geo-aware Clock-In DTOs ────────────────────────────────

public class GeoClockInRequest
{
    public string Action { get; set; } = "in";     // "in" or "out"
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public Guid? GeoFenceId { get; set; }
    public string? FaceDescriptor { get; set; }    // JSON 128-float array
    public decimal? ConfidenceScore { get; set; }
    public decimal? DistanceScore { get; set; }
    public string MatchResult { get; set; } = "match";
    public string? Notes { get; set; }
    public string? DeviceInfo { get; set; }
}

public class GeoClockInResponse
{
    public bool Success { get; set; }
    public Guid? AttendanceId { get; set; }
    public Guid? SessionId { get; set; }
    public string LocationStatus { get; set; } = "unknown";
    public decimal? DistanceMeters { get; set; }
    public Guid? ViolationId { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ── Violations Report DTOs ─────────────────────────────────

public class GeoFenceViolationReportDto
{
    public Guid ViolationId { get; set; }
    public DateTime ViolatedAt { get; set; }
    public string ActionTaken { get; set; } = string.Empty;
    public decimal DistanceOverMeters { get; set; }
    public decimal ActualLatitude { get; set; }
    public decimal ActualLongitude { get; set; }
    public bool Resolved { get; set; }

    // Employee
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }

    // Fence
    public Guid? GeoFenceId { get; set; }
    public string? GeoFenceName { get; set; }
    public decimal? CenterLatitude { get; set; }
    public decimal? CenterLongitude { get; set; }
    public int? RadiusMeters { get; set; }

    // Shift
    public Guid? ShiftId { get; set; }
    public string? ShiftName { get; set; }

    // Face recognition
    public string? FaceMatchResult { get; set; }
    public decimal? FaceConfidenceScore { get; set; }
    public string? FaceReferencePhotoUrl { get; set; }
}

// ── Team Live Stream DTO ───────────────────────────────────

public class TeamLiveStreamDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime? LastCheckIn { get; set; }
    public string LocationStatus { get; set; } = "unknown";
    public decimal? DistanceFromFenceM { get; set; }
    public string? GeoFenceName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? DeviceIp { get; set; }
    public decimal? TotalHours { get; set; }
    public decimal? CompliancePct { get; set; }
    public string? ShiftName { get; set; }
    public string? AttendanceStatus { get; set; }
}

// ── System Health DTO ──────────────────────────────────────

public class SystemHealthDto
{
    public decimal AttendanceHealthPct { get; set; }
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int PresentToday { get; set; }
    public int GeoFenceViolationsToday { get; set; }
    public int TotalDepartments { get; set; }
    public int EmployeesInZone { get; set; }
}

// ── Shift Geo-Fence link ──────────────────────────────────

public class ShiftGeoFenceDto
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public Guid GeoFenceId { get; set; }
    public string? GeoFenceName { get; set; }
    public int? RadiusMeters { get; set; }
    public bool IsActive { get; set; }
}

public class EmployeeFaceDescriptor
{
    public Guid DescriptorId { get; set; }
    public Guid EmployeeId { get; set; }
    public string DescriptorVector { get; set; } = string.Empty; // JSON 128-float array
    public string SourcePhotoUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime GeneratedAt { get; set; }
    public Guid? GeneratedBy { get; set; }
    public DateTime? SupersededAt { get; set; }
}