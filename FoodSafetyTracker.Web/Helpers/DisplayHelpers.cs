namespace FoodSafetyTracker.Web.Helpers;

public static class DisplayHelpers
{
    public static string GetRiskDescription(string riskRating)
    {
        return riskRating switch
        {
            "Low" => " Low risk - Good hygiene standards",
            "Medium" => " Medium risk - Satisfactory, needs monitoring",
            "High" => " High risk - Poor hygiene, requires immediate action",
            _ => "Unknown"
        };
    }

    public static string GetRiskBadge(string riskRating)
    {
        return riskRating switch
        {
            "Low" => "badge bg-success",
            "Medium" => "badge bg-warning",
            "High" => "badge bg-danger",
            _ => "badge bg-secondary"
        };
    }

    public static string GetOutcomeDescription(string outcome)
    {
        return outcome switch
        {
            "Pass" => " Pass - Compliant with food safety standards",
            "Fail" => " Fail - Needs improvement, follow-up required",
            _ => "Unknown"
        };
    }

    public static string GetOutcomeBadge(string outcome)
    {
        return outcome switch
        {
            "Pass" => "badge bg-success",
            "Fail" => "badge bg-danger",
            _ => "badge bg-secondary"
        };
    }

    public static string GetFollowUpStatusDescription(string status)
    {
        return status switch
        {
            "Open" => " Open - Action required",
            "Closed" => " Closed - Resolved",
            _ => "Unknown"
        };
    }

    public static string GetFollowUpStatusBadge(string status)
    {
        return status switch
        {
            "Open" => "badge bg-warning",
            "Closed" => "badge bg-success",
            _ => "badge bg-secondary"
        };
    }

    public static string GetScoreDescription(int score)
    {
        return score switch
        {
            >= 90 => " Excellent - Outstanding hygiene",
            >= 70 => " Good - Compliant with standards",
            >= 50 => " Satisfactory - Minor improvements needed",
            >= 30 => " Poor - Significant improvements required",
            _ => " Very Poor - Urgent action required"
        };
    }

    public static string GetScoreBadge(int score)
    {
        return score switch
        {
            >= 90 => "badge bg-success",
            >= 70 => "badge bg-info",
            >= 50 => "badge bg-warning",
            _ => "badge bg-danger"
        };
    }
}