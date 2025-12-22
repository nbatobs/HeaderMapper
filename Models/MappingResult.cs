namespace HeaderMapper.Models;

public class MappingResult
{
    public string UserColumn { get; set; } = string.Empty;
    public string CanonicalColumn { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public HeaderMatchType MatchType { get; set; }
    public string MatchDetails { get; set; } = string.Empty;
    public MappingAction RecommendedAction { get; set; }
}
