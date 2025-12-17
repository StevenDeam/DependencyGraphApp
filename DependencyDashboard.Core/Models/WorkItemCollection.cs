using DependencyDashboard.Core.Validation;

namespace DependencyDashboard.Core.Models;

/// <summary>
/// Container for all work items and their relationships.
/// </summary>
public class WorkItemCollection
{
    public List<WorkItem> Items { get; } = new();
    public List<DependencyEdge> DependencyEdges { get; } = new();
    public List<ValidationError> ValidationErrors { get; } = new();
    public string? SourceFilePath { get; set; }
    public DateTime LoadedAt { get; set; }

    private Dictionary<string, WorkItem>? _itemsById;

    public Dictionary<string, WorkItem> ItemsById
    {
        get
        {
            _itemsById ??= Items.ToDictionary(i => i.Id);
            return _itemsById;
        }
    }

    public void InvalidateCache()
    {
        _itemsById = null;
    }

    public IEnumerable<WorkItem> Milestones => Items.Where(i => i.IsMilestone);
    public IEnumerable<WorkItem> Tasks => Items.Where(i => i.IsTask);
    public IEnumerable<string> Disciplines => Items.Select(i => i.Discipline).Distinct().OrderBy(d => d);

    public bool HasErrors => ValidationErrors.Any(e => e.Severity == ValidationErrorSeverity.Error);
    public bool IsValid => !HasErrors;
}
