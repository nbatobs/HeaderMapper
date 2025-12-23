using System.Text;
using OfficeOpenXml;

namespace HeaderMapper.Services;

/// <summary>
/// Extracts headers from Excel files, intelligently detecting and merging multiple header rows
/// </summary>
public class ExcelHeaderExtractor
{
    public class SheetHeaders
    {
        public string SheetName { get; set; } = string.Empty;
        public List<string> Headers { get; set; } = new();
        public int HeaderRowCount { get; set; }
    }

    public class ExcelHeaderResult
    {
        public string FilePath { get; set; } = string.Empty;
        public List<SheetHeaders> Sheets { get; set; } = new();
    }

    /// <summary>
    /// Extracts all headers from all sheets in an Excel file
    /// </summary>
    public ExcelHeaderResult ExtractHeaders(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Excel file not found: {filePath}");
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var result = new ExcelHeaderResult
        {
            FilePath = filePath
        };

        using var package = new ExcelPackage(new FileInfo(filePath));
        
        foreach (var worksheet in package.Workbook.Worksheets)
        {
            var sheetHeaders = ExtractSheetHeaders(worksheet);
            result.Sheets.Add(sheetHeaders);
        }

        return result;
    }

    /// <summary>
    /// Extracts headers from a single worksheet, detecting multiple header rows
    /// </summary>
    private SheetHeaders ExtractSheetHeaders(ExcelWorksheet worksheet)
    {
        var sheetHeaders = new SheetHeaders
        {
            SheetName = worksheet.Name
        };

        if (worksheet.Dimension == null)
        {
            return sheetHeaders; // Empty sheet
        }

        int headerRowCount = DetectHeaderRowCount(worksheet);
        sheetHeaders.HeaderRowCount = headerRowCount;

        int columnCount = worksheet.Dimension.End.Column;

        for (int col = 1; col <= columnCount; col++)
        {
            string mergedHeader = BuildMergedHeader(worksheet, headerRowCount, col);
            
            if (!string.IsNullOrWhiteSpace(mergedHeader))
            {
                sheetHeaders.Headers.Add(mergedHeader);
            }
        }

        return sheetHeaders;
    }

    /// <summary>
    /// Detects how many rows contain header information
    /// Uses heuristics: looks for rows with text values followed by numeric data
    /// </summary>
    private int DetectHeaderRowCount(ExcelWorksheet worksheet)
    {
        int maxHeaderRows = Math.Min(5, worksheet.Dimension.End.Row); // Check up to 5 rows
        int dataStartRow = 1;

        for (int row = 1; row <= maxHeaderRows; row++)
        {
            bool hasNumericData = false;
            bool hasTextData = false;
            int nonEmptyCells = 0;

            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var cell = worksheet.Cells[row, col];
                var value = cell.Value;

                if (value != null)
                {
                    nonEmptyCells++;

                    if (IsNumericValue(value))
                    {
                        hasNumericData = true;
                    }
                    else if (value is string strValue && !string.IsNullOrWhiteSpace(strValue))
                    {
                        hasTextData = true;
                    }
                }
            }

            // If we find a row with mostly numeric data, headers end before this row
            if (hasNumericData && nonEmptyCells > worksheet.Dimension.End.Column * 0.3)
            {
                dataStartRow = row;
                break;
            }

            // If row is mostly text, it's likely a header row
            if (hasTextData && nonEmptyCells > 0)
            {
                dataStartRow = row + 1;
            }
        }

        return Math.Max(1, dataStartRow - 1);
    }

    /// <summary>
    /// Builds a merged header string from multiple header rows
    /// Example: Row1: "%DM", Row2: "Maize DM" -> "%DM Maize DM"
    /// Handles merged cells: if "%DM" is merged across columns, applies it to all spanned columns
    /// </summary>
    private string BuildMergedHeader(ExcelWorksheet worksheet, int headerRowCount, int column)
    {
        var headerParts = new List<string>();

        for (int row = 1; row <= headerRowCount; row++)
        {
            var cell = worksheet.Cells[row, column];
            string text = string.Empty;
            
            // Check if this cell is part of a merged range
            if (cell.Merge)
            {
                // Find the merged range that contains this cell
                var mergedRange = GetMergedCellValue(worksheet, row, column);
                text = mergedRange?.Trim() ?? string.Empty;
            }
            else
            {
                // Regular cell
                var cellValue = cell.Value;
                if (cellValue != null)
                {
                    text = cellValue.ToString()?.Trim() ?? string.Empty;
                }
            }
            
            if (!string.IsNullOrWhiteSpace(text))
            {
                headerParts.Add(text);
            }
        }

        return string.Join(" ", headerParts);
    }

    /// <summary>
    /// Gets the value from a merged cell by finding the top-left cell of the merged range
    /// </summary>
    private string? GetMergedCellValue(ExcelWorksheet worksheet, int row, int column)
    {
        // Iterate through all merged cells in the worksheet
        foreach (var mergedAddress in worksheet.MergedCells)
        {
            var range = worksheet.Cells[mergedAddress];
            
            // Check if our cell is within this merged range
            if (row >= range.Start.Row && row <= range.End.Row &&
                column >= range.Start.Column && column <= range.End.Column)
            {
                // Get the value from the top-left cell of the merged range
                var topLeftCell = worksheet.Cells[range.Start.Row, range.Start.Column];
                return topLeftCell.Value?.ToString();
            }
        }
        
        return null;
    }

    /// <summary>
    /// Checks if a value is numeric (int, double, decimal, DateTime treated as numeric data)
    /// </summary>
    private bool IsNumericValue(object value)
    {
        return value is int || value is long || value is double || 
               value is decimal || value is float || value is DateTime;
    }

    /// <summary>
    /// Pretty prints the extraction results to console
    /// </summary>
    public void PrintResults(ExcelHeaderResult result)
    {
        Console.WriteLine($"\n📄 File: {Path.GetFileName(result.FilePath)}");
        Console.WriteLine(new string('═', 70));

        foreach (var sheet in result.Sheets)
        {
            Console.WriteLine($"\n📊 Sheet: {sheet.SheetName}");
            Console.WriteLine($"   Header Rows Detected: {sheet.HeaderRowCount}");
            Console.WriteLine($"   Total Columns: {sheet.Headers.Count}");
            Console.WriteLine(new string('─', 70));

            for (int i = 0; i < sheet.Headers.Count; i++)
            {
                Console.WriteLine($"   [{i + 1,3}] {sheet.Headers[i]}");
            }
        }

        Console.WriteLine(new string('═', 70));
    }

    /// <summary>
    /// Saves the extracted headers to a text file
    /// </summary>
    public void SaveToFile(ExcelHeaderResult result, string outputPath)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"Excel File: {result.FilePath}");
        sb.AppendLine($"Extracted on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(new string('=', 70));

        foreach (var sheet in result.Sheets)
        {
            sb.AppendLine();
            sb.AppendLine($"Sheet: {sheet.SheetName}");
            sb.AppendLine($"Header Rows: {sheet.HeaderRowCount}");
            sb.AppendLine($"Columns: {sheet.Headers.Count}");
            sb.AppendLine(new string('-', 70));

            foreach (var header in sheet.Headers)
            {
                sb.AppendLine(header);
            }
        }

        File.WriteAllText(outputPath, sb.ToString());
        Console.WriteLine($"\n✓ Headers saved to: {outputPath}");
    }
}
