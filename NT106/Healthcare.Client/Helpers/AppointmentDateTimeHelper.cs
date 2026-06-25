using Healthcare.Client.Models.Core;
using System;

namespace Healthcare.Client.Helpers
{
    public static class AppointmentDateTimeHelper
    {
        public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

        public static DateTime NowVietnam => DateTime.UtcNow.Add(VietnamOffset);

        public static DateTime ToVietnamDate(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc || value.TimeOfDay >= TimeSpan.FromHours(17))
            {
                return value.Add(VietnamOffset).Date;
            }

            return value.Date;
        }

        public static DateTime ToVietnamDateTime(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc
                ? value.Add(VietnamOffset)
                : value;
        }

        public static DateTime DateForStorage(DateTime value)
        {
            return DateTime.SpecifyKind(ToVietnamDate(value).AddHours(12), DateTimeKind.Unspecified);
        }

        public static DateTime GetStart(Appointment appointment)
        {
            return ToVietnamDate(appointment.AppointmentDate).Add(appointment.StartTime);
        }

        public static DateTime GetEnd(Appointment appointment)
        {
            var date = ToVietnamDate(appointment.AppointmentDate);
            var end = appointment.EndTime != default
                ? appointment.EndTime
                : appointment.StartTime.Add(TimeSpan.FromMinutes(30));

            return date.Add(end);
        }
    }
}
