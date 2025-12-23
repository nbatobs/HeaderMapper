using HeaderMapper.Models;
using HeaderMapper.Services;

namespace HeaderMapper;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  Excel Header Extractor Demo");
        Console.WriteLine("═══════════════════════════════════════════════════════\n");

        // Get Excel file path from arguments or prompt user
        string excelFilePath;
        
        if (args.Length > 0)
        {
            excelFilePath = args[0];
        }
        else
        {
            Console.Write("Enter the path to your Excel file: ");
            excelFilePath = Console.ReadLine()?.Trim() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(excelFilePath))
        {
            Console.WriteLine("❌ No file path provided. Exiting.");
            return;
        }

        try
        {
            var extractor = new ExcelHeaderExtractor();
            
            Console.WriteLine($"\n⏳ Processing: {Path.GetFileName(excelFilePath)}...\n");
            
            // Extract headers from all sheets
            var result = extractor.ExtractHeaders(excelFilePath);
            
            // Display results
            extractor.PrintResults(result);
            
            // Optionally save to file
            Console.Write("\n💾 Save results to file? (y/n): ");
            var saveResponse = Console.ReadLine()?.Trim().ToLower();
            
            if (saveResponse == "y" || saveResponse == "yes")
            {
                string outputPath = Path.Combine(
                    Path.GetDirectoryName(excelFilePath) ?? ".",
                    $"{Path.GetFileNameWithoutExtension(excelFilePath)}_headers.txt"
                );
                
                extractor.SaveToFile(result, outputPath);
            }
            
            Console.WriteLine("\n✅ Done!");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected error: {ex.Message}");
            Console.WriteLine($"   {ex.StackTrace}");
        }
        /*
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  Header Mapper - Fuzzy Column Matching Demo");
        Console.WriteLine("═══════════════════════════════════════════════════════\n");

        try
        {
            // Load schema from JSON files
            var schemaLoader = new SchemaLoader();
            var schema = schemaLoader.LoadAllSchemas();
            
            Console.WriteLine($"✓ Loaded {schema.Count} canonical columns from schema files\n");

            // Configure matching
            var config = new MatchingConfig
            {
                FuzzyMinThreshold = 20,
                RequiredThresholds = new ThresholdConfig
                {
                    AutoMapThreshold = 0.90,
                    ReviewThreshold = 0.75
                },
                OptionalThresholds = new ThresholdConfig
                {
                    AutoMapThreshold = 0.85,
                    ReviewThreshold = 0.70
                }
            };

            var matcher = new HeaderMatcher(schema, config);

            // Demo: Test with various user column headers
            var testHeaders = new List<string>
            {
                // Exact matches
                "date",
                
                // Should match via aliases
                "measurement date",
                "total gas",
                "gas generation",
                
                // Fuzzy matches - typos/variations
                "totl biogas",
                "mixer 1 current",
                "d1 mixer 1 a",
                "biogas yeild",
                
                // Tricky ones
                "Primary Gas Output",
                "Day",
                "chp output",
                
                // Should fail
                "random_column_name"
            };

            Console.WriteLine("Testing Column Mapping:");
            Console.WriteLine("───────────────────────────────────────────────────────\n");

            foreach (var header in testHeaders)
            {
                var result = matcher.MapSingleHeader(header);
                PrintMappingResult(result);
            }

            // Interactive mode
            Console.WriteLine("\n═══════════════════════════════════════════════════════");
            Console.WriteLine("  Interactive Mode");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");
            Console.WriteLine("Enter column headers to map (or 'quit' to exit):\n");

            while (true)
            {
                Console.Write("User Column: ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "quit")
                    break;

                Console.WriteLine("\nTop 3 Matches:");
                Console.WriteLine("───────────────────────────────────────────────────────");
                
                var topMatches = matcher.GetTopMatches(input, 3);
                
                if (topMatches.Count == 0)
                {
                    Console.WriteLine(" No matches found\n");
                }
                else
                {
                    for (int i = 0; i < topMatches.Count; i++)
                    {
                        Console.WriteLine($"\n{i + 1}. {topMatches[i].CanonicalColumn}");
                        Console.WriteLine($"   Confidence: {topMatches[i].Confidence:P0}");
                        Console.WriteLine($"   Action: {GetActionEmoji(topMatches[i].RecommendedAction)} {topMatches[i].RecommendedAction}");
                        Console.WriteLine($"   Details: {topMatches[i].MatchDetails}");
                    }
                    Console.WriteLine();
                }
            }

            Console.WriteLine("\n✓ Done!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static void PrintMappingResult(MappingResult result)
    {
        var actionEmoji = GetActionEmoji(result.RecommendedAction);
        var matchEmoji = result.MatchType switch
        {
            HeaderMatchType.ExactMatch => "✓",
            HeaderMatchType.AliasMatch => "✓",
            HeaderMatchType.FuzzyMatch => "~",
            HeaderMatchType.NoMatch => "✗",
            _ => "?"
        };

        Console.WriteLine($"{matchEmoji} '{result.UserColumn}'");
        
        if (result.MatchType != HeaderMatchType.NoMatch)
        {
            Console.WriteLine($"   → {result.CanonicalColumn}");
            Console.WriteLine($"   Confidence: {result.Confidence:P0} | {result.MatchType}");
            Console.WriteLine($"   Action: {actionEmoji} {result.RecommendedAction}");
        }
        else
        {
            Console.WriteLine($"   → No match found");
        }
        
        Console.WriteLine($"   {result.MatchDetails}");
        Console.WriteLine();
    }

    static string GetActionEmoji(MappingAction action)
    {
        return action switch
        {
            MappingAction.AutoMap => "✓",
            MappingAction.Review => "⚠",
            MappingAction.ManualMap => "⚡",
            _ => "?"
        };
    }
    */
    }
}
