# HeaderMapper

A fuzzy column header matching system for mapping customer data files to a canonical database schema.

## Features

- **Layer 1: Exact & Alias Matching** - Fast dictionary lookup for known variations
- **Layer 2: Fuzzy String Matching** - Handles typos, abbreviations, and minor variations using Levenshtein distance
- **Layer 3: Confidence-Based Actions** - Smart thresholds for auto-mapping vs manual review
- **Multi-Schema Support** - Loads multiple domain schemas (feeding, production, stirrer, tank data)

## Quick Start

```bash
# Restore packages
dotnet restore

# Run the app
dotnet run
```

## Usage

The app runs in two modes:

### 1. Demo Mode
Automatically tests a variety of column headers to demonstrate matching capabilities.

### 2. Interactive Mode
Enter your own column headers to see how they map to the canonical schema.

```
User Column: total gas produced
```

Output:
```
Top 3 Matches:
───────────────────────────────────────────────────────

1. total_biogas_m3
   Confidence: 85%
   Action: ✓ AutoMap
   Details: Matched against 'total gas'
```

## Configuration

Adjust matching thresholds in `Program.cs`:

```csharp
var config = new MatchingConfig
{
    FuzzyMinThreshold = 60,  // Minimum fuzzy score (0-100)
    RequiredThresholds = new ThresholdConfig
    {
        AutoMapThreshold = 0.90,   // Required fields: 90%+ confidence to auto-map
        ReviewThreshold = 0.75      // 75%+ needs review
    },
    OptionalThresholds = new ThresholdConfig
    {
        AutoMapThreshold = 0.85,   // Optional fields: 85%+ to auto-map
        ReviewThreshold = 0.70      // 70%+ needs review
    }
};
```

## Architecture

```
User Column Header
   ↓
Normalize (lowercase, remove punctuation)
   ↓
Layer 1: Exact Match → CanonicalName
   ↓
Layer 2: Alias Match → Known Aliases
   ↓
Layer 3: Fuzzy Match → Levenshtein Distance
   ↓
Confidence Scoring
   ↓
Action: AutoMap | Review | ManualMap
```

## Schema Format

Column schemas are defined in JSON:

```json
{
  "total_biogas_m3": {
    "canonicalName": "total_biogas_m3",
    "description": "Total volume of biogas produced during the day.",
    "dataType": "number",
    "required": true,
    "exampleValues": ["12500", "18300"],
    "aliases": [
      "total biogas",
      "gas generation",
      "total biogas (m3)",
      "biogas produced",
      "total gas"
    ]
  }
}
```

## Dependencies

- **FuzzySharp** (2.0.2) - Fuzzy string matching
- **System.Text.Json** (8.0.5) - JSON schema parsing

## Next Steps

To extend this into a production system:

1. Add data-type validation using example values
2. Implement customer-specific mapping persistence
3. Add semantic matching for description-based similarity
4. Build a web API or UI for human-in-the-loop review
5. Add unit detection and conversion
6. Implement batch file processing
