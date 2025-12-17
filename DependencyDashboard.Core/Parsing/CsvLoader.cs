using DependencyDashboard.Core.Models;
using DependencyDashboard.Core.Validation;
using System.Globalization;

namespace DependencyDashboard.Core.Parsing;

/// <summary>
/// Parses and validates CSV files containing work items.
/// </summary>
public class CsvLoader
{
    private static readonly string[] RequiredColumns =
    {
        "Id", "Title", "Type", "Discipline", "Weight"
    };

    public WorkItemCollection Load(string filePath)
    {
        var collection = new WorkItemCollection
        {
            SourceFilePath = filePath,
            LoadedAt = DateTime.Now
        };

        if (!File.Exists(filePath))
        {
            collection.ValidationErrors.Add(new ValidationError
            {
                Row = 0,
                Message = $"File not found: {filePath}"
            });
            return collection;
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch (Exception ex)
        {
            collection.ValidationErrors.Add(new ValidationError
            {
                Row = 0,
                Message = $"Failed to read file: {ex.Message}"
            });
            return collection;
        }

        if (lines.Length == 0)
        {
            collection.ValidationErrors.Add(new ValidationError
            {
                Row = 0,
                Message = "CSV file is empty"
            });
            return collection;
        }

        // Parse header
        var headerLine = lines[0];
        var headers = ParseCsvLine(headerLine);
        var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            columnIndex[headers[i].Trim()] = i;
        }

        // Validate required columns
        foreach (var required in RequiredColumns)
        {
            if (!columnIndex.ContainsKey(required))
            {
                collection.ValidationErrors.Add(new ValidationError
                {
                    Row = 1,
                    Column = required,
                    Message = $"Missing required column: {required}"
                });
            }
        }

        if (collection.HasErrors)
        {
            return collection;
        }

        // Parse data rows
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int rowNum = 2; rowNum <= lines.Length; rowNum++)
        {
            var line = lines[rowNum - 1];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = ParseCsvLine(line);
            var item = ParseWorkItem(values, columnIndex, rowNum, collection.ValidationErrors, seenIds);
            if (item != null)
            {
                collection.Items.Add(item);
            }
        }

        collection.InvalidateCache();
        return collection;
    }

    private WorkItem? ParseWorkItem(
        string[] values,
        Dictionary<string, int> columnIndex,
        int rowNum,
        List<ValidationError> errors,
        HashSet<string> seenIds)
    {
        var item = new WorkItem();

        // Id (required)
        var id = GetValue(values, columnIndex, "Id")?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(id))
        {
            errors.Add(new ValidationError
            {
                Row = rowNum,
                Column = "Id",
                Message = "Id is required and cannot be empty"
            });
            return null;
        }
        if (!seenIds.Add(id))
        {
            errors.Add(new ValidationError
            {
                Row = rowNum,
                Column = "Id",
                Message = $"Duplicate Id: {id}"
            });
            return null;
        }
        item.Id = id;

        // Title (required)
        var title = GetValue(values, columnIndex, "Title")?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(new ValidationError
            {
                Row = rowNum,
                Column = "Title",
                Message = "Title is required and cannot be empty"
            });
        }
        item.Title = title;

        // Type (required)
        var typeStr = GetValue(values, columnIndex, "Type")?.Trim() ?? "";
        if (!Enum.TryParse<WorkItemType>(typeStr, ignoreCase: true, out var type))
        {
            errors.Add(new ValidationError
            {
                Row = rowNum,
                Column = "Type",
                Message = $"Invalid Type: '{typeStr}'. Must be 'Task' or 'Milestone'"
            });
            return null;
        }
        item.Type = type;

        // Discipline (required)
        var discipline = GetValue(values, columnIndex, "Discipline")?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(discipline))
        {
            errors.Add(new ValidationError
            {
                Row = rowNum,
                Column = "Discipline",
                Message = "Discipline is required and cannot be empty"
            });
        }
        item.Discipline = discipline;

        // PercentComplete (required for tasks)
        var percentStr = GetValue(values, columnIndex, "PercentComplete")?.Trim() ?? "";
        if (item.IsTask)
        {
            if (string.IsNullOrWhiteSpace(percentStr))
            {
                errors.Add(new ValidationError
                {
                    Row = rowNum,
                    Column = "PercentComplete",
                    Message = "PercentComplete is required for tasks"
                });
            }
            else if (!int.TryParse(percentStr, out var percent) || percent < 0 || percent > 100)
            {
                errors.Add(new ValidationError
                {
                    Row = rowNum,
                    Column = "PercentComplete",
                    Message = $"PercentComplete must be an integer between 0 and 100, got: '{percentStr}'"
                });
            }
            else
            {
                item.PercentCompleteRaw = percent;
            }
        }
        else if (!string.IsNullOrWhiteSpace(percentStr) && int.TryParse(percentStr, out var milestonePercent))
        {
            item.PercentCompleteRaw = milestonePercent;
        }

        // Weight (required, >= 0)
        var weightStr = GetValue(values, columnIndex, "Weight")?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(weightStr))
        {
            errors.Add(new ValidationError
            {
                Row = rowNum,
                Column = "Weight",
                Message = "Weight is required"
            });
        }
        else if (!int.TryParse(weightStr, out var weight) || weight < 0)
        {
            errors.Add(new ValidationError
            {
                Row = rowNum,
                Column = "Weight",
                Message = $"Weight must be a non-negative integer, got: '{weightStr}'"
            });
        }
        else
        {
            item.Weight = weight;
        }

        // ParentId (optional)
        var parentId = GetValue(values, columnIndex, "ParentId")?.Trim();
        item.ParentId = string.IsNullOrWhiteSpace(parentId) ? null : parentId;

        // PrereqId (optional)
        var prereqId = GetValue(values, columnIndex, "PrereqId")?.Trim();
        item.PrereqId = string.IsNullOrWhiteSpace(prereqId) ? null : prereqId;

        // TargetDate (required for milestones)
        var targetDateStr = GetValue(values, columnIndex, "TargetDate")?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(targetDateStr))
        {
            if (DateTime.TryParse(targetDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var targetDate))
            {
                item.TargetDate = targetDate;
            }
            else
            {
                errors.Add(new ValidationError
                {
                    Row = rowNum,
                    Column = "TargetDate",
                    Message = $"Invalid TargetDate format: '{targetDateStr}'. Use YYYY-MM-DD"
                });
            }
        }

        // IsNA (optional, default false)
        var isNaStr = GetValue(values, columnIndex, "IsNA")?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(isNaStr))
        {
            if (bool.TryParse(isNaStr, out var isNa))
            {
                item.IsNA = isNa;
            }
            else if (isNaStr.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                     isNaStr.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                item.IsNA = true;
            }
        }

        // Level (optional)
        var levelStr = GetValue(values, columnIndex, "Level")?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(levelStr))
        {
            if (int.TryParse(levelStr, out var level) && level >= 0)
            {
                item.Level = level;
            }
        }

        return item;
    }

    private static string? GetValue(string[] values, Dictionary<string, int> columnIndex, string columnName)
    {
        if (!columnIndex.TryGetValue(columnName, out var index) || index >= values.Length)
        {
            return null;
        }
        return values[index];
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }
        values.Add(current.ToString());

        return values.ToArray();
    }
}
