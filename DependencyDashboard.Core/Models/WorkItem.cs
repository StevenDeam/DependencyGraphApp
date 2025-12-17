namespace DependencyDashboard.Core.Models;

/// <summary>
/// Represents a task or milestone work item from the CSV.
/// </summary>
public class WorkItem
{
    // Raw properties from CSV
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public WorkItemType Type { get; set; }
    public string Discipline { get; set; } = string.Empty;
    public int PercentCompleteRaw { get; set; }
    public int Weight { get; set; }
    public string? ParentId { get; set; }
    public string? PrereqId { get; set; }
    public DateTime? TargetDate { get; set; }
    public bool IsNA { get; set; }
    public int? Level { get; set; }
    public string? PhaseRaw { get; set; }

    // Computed phase (after inheritance)
    public string Phase { get; set; } = string.Empty;

    // Computed properties (set by ProgressCalculator)
    public double ComputedPercent { get; set; }
    public WorkItemStatus ComputedStatus { get; set; }
    public HealthStatus Health { get; set; }
    public string? BlockedById { get; set; }

    // Graph layout properties (set by GraphLayoutEngine)
    public double X { get; set; }
    public double Y { get; set; }
    public int ComputedLevel { get; set; }

    // Navigation references (set during graph building)
    public WorkItem? Parent { get; set; }
    public WorkItem? Prerequisite { get; set; }
    public List<WorkItem> Children { get; } = new();
    public List<WorkItem> Dependents { get; } = new();

    public bool IsMilestone => Type == WorkItemType.Milestone;
    public bool IsTask => Type == WorkItemType.Task;
}
