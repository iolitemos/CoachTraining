namespace CoachTraining.Api.Helpers;

public static class ThaiDateHelper
{
    public static string DayName(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "จันทร์",
        DayOfWeek.Tuesday => "อังคาร",
        DayOfWeek.Wednesday => "พุธ",
        DayOfWeek.Thursday => "พฤหัสบดี",
        DayOfWeek.Friday => "ศุกร์",
        DayOfWeek.Saturday => "เสาร์",
        DayOfWeek.Sunday => "อาทิตย์",
        _ => dayOfWeek.ToString(),
    };
}
