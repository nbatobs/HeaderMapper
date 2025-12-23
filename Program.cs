using HeaderMapper.Models;
using HeaderMapper.Services;

namespace HeaderMapper;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  Header Mapper - Excel to Schema Matching");
        Console.WriteLine("═══════════════════════════════════════════════════════\n");

        try
        {
            // 1. Load canonical schema
            var schemaLoader = new SchemaLoader();
            var schema = schemaLoader.LoadAllSchemas();
            Console.WriteLine($"✓ Loaded {schema.Count} canonical columns from schema files\n");

            // 2. Configure matching
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

            // 3. Get Excel file path
            string? excelFilePath = null;
            
            if (args.Length > 0)
            {
                excelFilePath = args[0];
            }
            else
            {
                Console.WriteLine("Enter path to Excel file (or press Enter to skip):");
                Console.Write("Excel File: ");
                excelFilePath = Console.ReadLine()?.Trim();
            }

            // 4. Process Excel file if provided
            if (!string.IsNullOrWhiteSpace(excelFilePath) && File.Exists(excelFilePath))
            {
                ProcessExcelFile(excelFilePath, matcher);
            }
            else if (!string.IsNullOrWhiteSpace(excelFilePath))
            {
                Console.WriteLine($"❌ File not found: {excelFilePath}");
                Console.WriteLine("\nFalling back to interactive mode...\n");
                RunInteractiveMode(matcher);
            }
            else
            {
                // No file provided - run interactive mode
                Console.WriteLine("\n═══════════════════════════════════════════════════════");
                Console.WriteLine("  Interactive Header Testing Mode");
                Console.WriteLine("═══════════════════════════════════════════════════════\n");
                RunInteractiveMode(matcher);
            }

            Console.WriteLine("\n✅ Done!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"   {ex.StackTrace}");
        }
    }

    static void ProcessExcelFile(string filePath, HeaderMatcher matcher)
    {
        Console.WriteLine($"📊 Processing Excel file: {Path.GetFileName(filePath)}\n");

        // Extract headers from Excel
        var extractor = new ExcelHeaderExtractor();
        var excelResult = extractor.ExtractHeaders(filePath);

        Console.WriteLine($"✓ Extracted headers from {excelResult.Sheets.Count} sheet(s)\n");

        // Process each sheet
        foreach (var sheet in excelResult.Sheets)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine($"  Sheet: {sheet.SheetName}");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine($"Header Rows Detected: {sheet.HeaderRowCount}");
            Console.WriteLine($"Total Columns: {sheet.Headers.Count}");
            Console.WriteLine();

            var autoMapped = 0;
            var needsReview = 0;
            var needsManual = 0;

            // Match each header
            foreach (var header in sheet.Headers)
            {
                var match = matcher.MapSingleHeader(header);
                PrintMatchResult(match);

                switch (match.RecommendedAction)
                {
                    case MappingAction.AutoMap:
                        autoMapped++;
                        break;
                    case MappingAction.Review:
                        needsReview++;
                        break;
                    case MappingAction.ManualMap:
                        needsManual++;
                        break;
                }
            }

            // Sheet summary
            Console.WriteLine("\n───────────────────────────────────────────────────────");
            Console.WriteLine($"Sheet Summary:");
            Console.WriteLine($"  ✓ Auto-mapped:     {autoMapped,3} ({(sheet.Headers.Count > 0 ? (double)autoMapped / sheet.Headers.Count : 0):P0})");
            Console.WriteLine($"  ⚠ Needs review:    {needsReview,3} ({(sheet.Headers.Count > 0 ? (double)needsReview / sheet.Headers.Count : 0):P0})");
            Console.WriteLine($"  ⚡ Manual mapping:  {needsManual,3} ({(sheet.Headers.Count > 0 ? (double)needsManual / sheet.Headers.Count : 0):P0})");
            Console.WriteLine("───────────────────────────────────────────────────────\n");
        }

        // Overall summary
        var totalHeaders = excelResult.Sheets.Sum(s => s.Headers.Count);
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine($"  Overall Summary");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine($"Total Headers Processed: {totalHeaders}");
        Console.WriteLine($"Sheets: {excelResult.Sheets.Count}");
    }

    static void RunInteractiveMode(HeaderMatcher matcher)
    {
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
                Console.WriteLine("❌ No matches found\n");
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
    }

    static void PrintMatchResult(MappingResult result)
    {
        var actionEmoji = GetActionEmoji(result.RecommendedAction);

        Console.Write($"{actionEmoji} ");
        Console.Write($"{result.UserColumn,-35}");
        
        if (result.MatchType != HeaderMatchType.NoMatch)
        {
            Console.Write($" → {result.CanonicalColumn,-25}");
            Console.Write($" {result.Confidence:P0}");
        }
        else
        {
            Console.Write($" → {"No match",-25}");
        }
        
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
}

