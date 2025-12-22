using System.Text.Json;
using HeaderMapper.Models;

namespace HeaderMapper.Services;

public class SchemaLoader
{
    public Dictionary<string, ColumnSchema> LoadSchema(string jsonFilePath)
    {
        var jsonContent = File.ReadAllText(jsonFilePath);
        var schema = JsonSerializer.Deserialize<Dictionary<string, ColumnSchema>>(jsonContent);
        
        if (schema == null)
        {
            throw new InvalidOperationException($"Failed to load schema from {jsonFilePath}");
        }

        return schema;
    }

    public Dictionary<string, ColumnSchema> LoadAllSchemas(string directory = "aliases")
    {
        var allSchemas = new Dictionary<string, ColumnSchema>();
        var jsonFiles = new[]
        {
            "feeding-data-alias.json",
            "production-data-alias.json",
            "stirrer-data-alias.json",
            "tank-data-alias.json"
        };

        foreach (var file in jsonFiles)
        {
            var filePath = Path.Combine(directory, file);
            if (File.Exists(filePath))
            {
                var schema = LoadSchema(filePath);
                foreach (var kvp in schema)
                {
                    if (!allSchemas.ContainsKey(kvp.Key))
                    {
                        allSchemas[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        return allSchemas;
    }
}
