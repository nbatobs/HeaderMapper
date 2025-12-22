using FuzzySharp;
using HeaderMapper.Models;

namespace HeaderMapper.Services;

public class HeaderMatcher
{
    private readonly Dictionary<string, ColumnSchema> _schema;
    private readonly MatchingConfig _config;

    public HeaderMatcher(Dictionary<string, ColumnSchema> schema, MatchingConfig? config = null)
    {
        _schema = schema;
        _config = config ?? new MatchingConfig();
    }

    public List<MappingResult> MapHeaders(List<string> userHeaders)
    {
        var results = new List<MappingResult>();

        foreach (var userHeader in userHeaders)
        {
            var result = MapSingleHeader(userHeader);
            results.Add(result);
        }

        return results;
    }

    public MappingResult MapSingleHeader(string userHeader)
    {
        var normalizedUserHeader = NormalizeHeader(userHeader);
        
        // Layer 1: Exact match
        foreach (var (key, schema) in _schema)
        {
            if (NormalizeHeader(schema.CanonicalName) == normalizedUserHeader)
            {
                return new MappingResult
                {
                    UserColumn = userHeader,
                    CanonicalColumn = schema.CanonicalName,
                    Confidence = 1.0,
                    MatchType = HeaderMatchType.ExactMatch,
                    MatchDetails = "Exact match to canonical name",
                    RecommendedAction = MappingAction.AutoMap
                };
            }
        }

        // Layer 2: Alias match
        foreach (var (key, schema) in _schema)
        {
            foreach (var alias in schema.Aliases)
            {
                if (NormalizeHeader(alias) == normalizedUserHeader)
                {
                    return new MappingResult
                    {
                        UserColumn = userHeader,
                        CanonicalColumn = schema.CanonicalName,
                        Confidence = 0.95,
                        MatchType = HeaderMatchType.AliasMatch,
                        MatchDetails = $"Matched alias: '{alias}'",
                        RecommendedAction = MappingAction.AutoMap
                    };
                }
            }
        }

        // Layer 3: Fuzzy matching
        var bestMatch = FindBestFuzzyMatch(userHeader, normalizedUserHeader);
        
        if (bestMatch != null)
        {
            return bestMatch;
        }

        // No match found
        return new MappingResult
        {
            UserColumn = userHeader,
            CanonicalColumn = string.Empty,
            Confidence = 0.0,
            MatchType = HeaderMatchType.NoMatch,
            MatchDetails = "No suitable match found",
            RecommendedAction = MappingAction.ManualMap
        };
    }

    private MappingResult? FindBestFuzzyMatch(string userHeader, string normalizedUserHeader)
    {
        var candidates = new List<(string canonical, string target, int score)>();

        foreach (var (key, schema) in _schema)
        {
            // Compare against canonical name
            var canonicalScore = Fuzz.Ratio(normalizedUserHeader, NormalizeHeader(schema.CanonicalName));
            candidates.Add((schema.CanonicalName, schema.CanonicalName, canonicalScore));

            // Compare against aliases
            foreach (var alias in schema.Aliases)
            {
                var aliasScore = Fuzz.Ratio(normalizedUserHeader, NormalizeHeader(alias));
                candidates.Add((schema.CanonicalName, alias, aliasScore));
            }

            // Compare against description (token-based for semantic similarity)
            var descriptionScore = Fuzz.TokenSetRatio(normalizedUserHeader, NormalizeHeader(schema.Description));
            candidates.Add((schema.CanonicalName, $"description: {schema.Description}", descriptionScore / 2)); // Lower weight
        }

        // Find best match
        var best = candidates.OrderByDescending(c => c.score).FirstOrDefault();
        
        if (best.score >= _config.FuzzyMinThreshold)
        {
            var confidence = best.score / 100.0;
            var action = DetermineAction(confidence, _schema[best.canonical].Required);

            return new MappingResult
            {
                UserColumn = userHeader,
                CanonicalColumn = best.canonical,
                Confidence = confidence,
                MatchType = HeaderMatchType.FuzzyMatch,
                MatchDetails = $"Fuzzy match against '{best.target}' (score: {best.score})",
                RecommendedAction = action
            };
        }

        return null;
    }

    private string NormalizeHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return string.Empty;

        return header
            .ToLowerInvariant()
            .Replace("_", " ")
            .Replace("-", " ")
            .Replace(".", " ")
            .Replace("(", " ")
            .Replace(")", " ")
            .Replace("/", " ")
            .Trim()
            .Replace("  ", " ");
    }

    private MappingAction DetermineAction(double confidence, bool isRequired)
    {
        var thresholds = isRequired ? _config.RequiredThresholds : _config.OptionalThresholds;

        if (confidence >= thresholds.AutoMapThreshold)
            return MappingAction.AutoMap;
        
        if (confidence >= thresholds.ReviewThreshold)
            return MappingAction.Review;
        
        return MappingAction.ManualMap;
    }

    public List<MappingResult> GetTopMatches(string userHeader, int topN = 3)
    {
        var normalizedUserHeader = NormalizeHeader(userHeader);
        var candidates = new List<MappingResult>();

        foreach (var (key, schema) in _schema)
        {
            var canonicalScore = Fuzz.Ratio(normalizedUserHeader, NormalizeHeader(schema.CanonicalName));
            
            var maxAliasScore = 0;
            var bestAlias = string.Empty;
            foreach (var alias in schema.Aliases)
            {
                var aliasScore = Fuzz.Ratio(normalizedUserHeader, NormalizeHeader(alias));
                if (aliasScore > maxAliasScore)
                {
                    maxAliasScore = aliasScore;
                    bestAlias = alias;
                }
            }

            var bestScore = Math.Max(canonicalScore, maxAliasScore);
            var matchTarget = maxAliasScore > canonicalScore ? bestAlias : schema.CanonicalName;

            if (bestScore >= _config.FuzzyMinThreshold)
            {
                candidates.Add(new MappingResult
                {
                    UserColumn = userHeader,
                    CanonicalColumn = schema.CanonicalName,
                    Confidence = bestScore / 100.0,
                    MatchType = HeaderMatchType.FuzzyMatch,
                    MatchDetails = $"Matched against '{matchTarget}'",
                    RecommendedAction = DetermineAction(bestScore / 100.0, schema.Required)
                });
            }
        }

        return candidates
            .OrderByDescending(c => c.Confidence)
            .Take(topN)
            .ToList();
    }
}
