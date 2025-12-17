namespace DependencyDashboard.Core.Validation;

/// <summary>
/// Represents a CSV validation error with row and column information.
/// </summary>
public class ValidationError
{
    public int Row { get; set; }
    public string? Column { get; set; }
    public string Message { get; set; } = string.Empty;
    public ValidationErrorSeverity Severity { get; set; } = ValidationErrorSeverity.Error;

    public override string ToString()
    {
        var location = Column != null ? $"Row {Row}, Column '{Column}'" : $"Row {Row}";
        return $"[{Severity}] {location}: {Message}";
    }
}

public enum ValidationErrorSeverity
{
    Warning,
    Error
}
