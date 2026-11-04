using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HRMS.API.Configuration
{
    /// <summary>
    /// Serializes every DateTime as UTC ISO-8601 with a trailing 'Z'.
    /// All timestamps in this system are stored in UTC (DateTime.UtcNow), but values
    /// read from plain `timestamp` (without time zone) columns come back as
    /// DateTimeKind.Unspecified and would otherwise serialize without a 'Z' — causing
    /// browsers to misread them as local time. Treating Unspecified as UTC and always
    /// emitting 'Z' guarantees the frontend converts every timestamp into the
    /// company-configured timezone consistently.
    /// </summary>
    public class UtcJsonDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                // Unspecified: stored as UTC by convention, so tag it as UTC.
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Nullable counterpart of <see cref="UtcJsonDateTimeConverter"/>.</summary>
    public class UtcJsonNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var v = value.Value;
            var utc = v.Kind switch
            {
                DateTimeKind.Utc => v,
                DateTimeKind.Local => v.ToUniversalTime(),
                _ => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            };

            writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Serializes DateTimeOffset as UTC ISO-8601 with a trailing 'Z'.
    /// Converts any offset to UTC before emitting, ensuring consistent frontend handling.
    /// </summary>
    public class UtcJsonDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTimeOffset();
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            var utc = value.ToUniversalTime();
            writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Nullable counterpart of <see cref="UtcJsonDateTimeOffsetConverter"/>.</summary>
    public class UtcJsonNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            return reader.GetDateTimeOffset();
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var utc = value.Value.ToUniversalTime();
            writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }
}
