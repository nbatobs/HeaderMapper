namespace HeaderMapper.Services;

public class MatchingConfig
{
    public int FuzzyMinThreshold { get; set; } = 60;
    
    public ThresholdConfig RequiredThresholds { get; set; } = new()
    {
        AutoMapThreshold = 0.90,
        ReviewThreshold = 0.75
    };
    
    public ThresholdConfig OptionalThresholds { get; set; } = new()
    {
        AutoMapThreshold = 0.85,
        ReviewThreshold = 0.70
    };
}
