using System;

namespace FoodTour_WebAdmin.Api.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToVietnamTime(this DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        else if (utcDateTime.Kind == DateTimeKind.Local)
        {
            utcDateTime = utcDateTime.ToUniversalTime();
        }

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, tz);
        }
    }
}
