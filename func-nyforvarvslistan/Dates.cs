using func_nyforvarvslistan.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace func_nyforvarvslistan
{
    public static class Dates
    {
        public static DateTime StartOfPreviousMonth
        {
            get
            {
                DateTime now = DateTime.UtcNow;
                DateTime firstOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                return firstOfThisMonth.AddMonths(-1);
            }
        }

        public static DateTime EndOfPreviousMonth
        {
            get
            {
                DateTime now = DateTime.UtcNow;
                DateTime firstOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                return firstOfThisMonth.AddTicks(-1);
            }
        }

        public static string GetMonthNameInSwedish(DateTime date)
        {
            return date.ToString("MMMM", new CultureInfo("sv-SE"));
        }

        public static string GetFormattedBookTitle(string libraryId, DateTime referenceDate)
        {
            string monthName = GetMonthNameInSwedish(referenceDate).ToLower();
            string year = referenceDate.Year.ToString();

            string title;
            if (libraryId.StartsWith("C"))
            {
                title = $"Nya talböcker {monthName} {year}";
            }
            else if (libraryId.StartsWith("P"))
            {
                title = $"Nya punktskriftsböcker {monthName} {year}";
            }
            else
            {
                title = $"Nya böcker {monthName} {year}";
            }

            return title;
        }

        public static string GetCurrentYear(DateTime referenceDate)
        {
            return referenceDate.Year.ToString();
        }
    }
}
