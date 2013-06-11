using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public static class DateTimeExtensions
    {
        public static object DBNullCoalese(this DateTime? date)
        {
            if (date == null || date == default(DateTime))
                return DBNull.Value;
            else
                return date;
        }

        public static DateTime GetNearestDayOfWeek(this DateTime date, DayOfWeek targetDayOfWeek, Boolean searchForward = false)
        {
            int target = (int)targetDayOfWeek;
            int current = (int)date.DayOfWeek;
            int delta = 0;

            if (searchForward)
            {
                if (current > target)
                    delta = 7 + (target - current);
                else
                    delta = target - current;
            }
            else
            {
                if (current > target)
                    delta = target - current;
                else
                    delta = -7 + (target - current);
            }

            if (delta != 0)
                return date.AddDays(delta).Date;
            else
                return date.Date;
        }


        public static IEnumerable<DateTime> ExplodeDateRange(this DateTime start, DateTime end)
        {
            if (start == default(DateTime) || end == default(DateTime))
                throw new ArgumentException("Start date or End date is undefined");

            if (end < start)
                throw new ArgumentException("Start date is after End date!");

            start = new DateTime(start.Year, start.Month, start.Day);
            end = new DateTime(end.Year, end.Month, end.Day);

            if (start.Equals(end) || start - end == default(TimeSpan))
                throw new ArgumentException("Start date is the same as End date!");

            foreach (var value in Enumerable.Range(0, (end - start).Days + 1))
            {
                yield return start.AddDays(value);
            }
        }

        public static IEnumerable<DateTime> ExplodeHourRange(this DateTime start, DateTime end)
        {
            if (start == default(DateTime) || end == default(DateTime))
                throw new ArgumentException("Start date or End date is undefined");

            if (end < start)
                throw new ArgumentException("Start date is after End date!");

            if (start.Equals(end) || start - end == default(TimeSpan))
                throw new ArgumentException("Start date is the same as End date!");

            foreach (var value in Enumerable.Range(0, (end - start).Hours + 1))
            {
                yield return start.AddHours(value);
            }
        }

        public static long ToJSDate(this DateTime dt)
        {
            DateTime dt_1970 = new DateTime(1970, 1, 1);
            var utc_dt = TimeZoneInfo.ConvertTimeToUtc(dt);
            return (utc_dt.Ticks - dt_1970.Ticks) / 10000; //why? dunno.
        }

        public static DateTime SetFromJSDate(this DateTime dt, long jsDate)
        {
            DateTime dt_1970 = new DateTime(1970, 1, 1);
            long dt_1970_ticks = dt_1970.Ticks;
            DateTime utc_dt = new DateTime(dt_1970_ticks + (jsDate * 10000));
            return TimeZoneInfo.ConvertTimeFromUtc(utc_dt, TimeZoneInfo.Local); //does this work? ehhhhh.
        }
    }
}
