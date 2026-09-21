namespace LibraryAPI.DTOs;

public class RecommendationDto
{
    public BookResponseDto Book { get; set; } = null!;
    public decimal Score { get; set; }
    public string Reason { get; set; } = null!;
    public List<string> MatchedGenres { get; set; } = [];
}

public class RecommendationFeedbackDto
{
    public bool IsPositive { get; set; }
}
