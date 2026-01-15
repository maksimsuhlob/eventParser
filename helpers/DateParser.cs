using System;

namespace eventParser;
public static class DateParser
{
    public static (DateTime? StartDate, DateTime? EndDate) ParseDateRange(string dateString, int year = 0)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return (null, null);

        // Используем текущий год, если не указан
        if (year == 0)
            year = DateTime.Now.Year;

        dateString = dateString.Trim();

        // Проверяем, есть ли диапазон дат (содержит " - ")
        if (dateString.Contains(" - "))
        {
            var parts = dateString.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var startDate = ParseSingleDate(parts[0].Trim(), year);
                var endDate = ParseSingleDate(parts[1].Trim(), year);
                return (startDate, endDate);
            }
        }
        else
        {
            // Одиночная дата
            var singleDate = ParseSingleDate(dateString, year);
            return (singleDate, singleDate);
        }

        return (null, null);
    }

    private static DateTime? ParseSingleDate(string dateString, int year)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        // Парсим формат dd.mm
        var parts = dateString.Split('.');
        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0], out int day) && int.TryParse(parts[1], out int month))
            {
                try
                {
                    return new DateTime(year, month, day);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Неверная дата
                    return null;
                }
            }
        }

        return null;
    }

    // Дополнительные полезные методы
    public static bool IsValidDateFormat(string dateString)
    {
        var (start, end) = ParseDateRange(dateString);
        return start.HasValue;
    }

    public static bool IsCurrentDateInRange(string dateString, int year = 0)
    {
        var (start, end) = ParseDateRange(dateString, year);
        if (!start.HasValue) return false;

        var today = DateTime.Today;
        var startDate = start.Value.Date;
        var endDate = end?.Date ?? startDate;

        return today >= startDate && today <= endDate;
    }
    public static bool IsCurrentDateInRange((DateTime? StartDate, DateTime? EndDate) date)
    {
        var (start, end) = date;
        if (!start.HasValue) return false;

        var today = DateTime.Today;
        var startDate = start.Value.Date;
        var endDate = end?.Date ?? startDate;

        return today >= startDate && today <= endDate;
    }

    public static string FormatDateRange(DateTime? startDate, DateTime? endDate)
    {
        if (!startDate.HasValue) return "";

        if (!endDate.HasValue || startDate.Value.Date == endDate.Value.Date)
        {
            return startDate.Value.ToString("dd.MM");
        }

        return $"{startDate.Value:dd.MM} - {endDate.Value:dd.MM}";
    }
    
    public static bool IsCurrentWeekInRange((DateTime? StartDate, DateTime? EndDate) date)
    {
        var (start, end) = date;
        if (!start.HasValue) return false;

        var today = DateTime.Today;
        var startOfWeek = GetStartOfWeek(today);
        var endOfWeek = startOfWeek.AddDays(6);

        var eventStartDate = start.Value.Date;
        var eventEndDate = end?.Date ?? eventStartDate;

        // Проверяем, пересекается ли событие с текущей неделей
        return eventStartDate <= endOfWeek && eventEndDate >= startOfWeek;
    }

    public static bool IsCurrentWeekInRange(string dateString, int year = 0)
    {
        var date = ParseDateRange(dateString, year);
        return IsCurrentWeekInRange(date);
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        // Понедельник как начало недели (европейский стандарт)
        var dayOfWeek = (int)date.DayOfWeek;
        if (dayOfWeek == 0) dayOfWeek = 7; // Воскресенье = 7
        return date.AddDays(1 - dayOfWeek);
    }

}