namespace DependencyDashboard.Core.Models;

/// <summary>
/// Represents a dependency edge from prerequisite to dependent item.
/// </summary>
public class DependencyEdge
{
    public string PrereqId { get; set; } = string.Empty;
    public string DependentId { get; set; } = string.Empty;

    public WorkItem? Prerequisite { get; set; }
    public WorkItem? Dependent { get; set; }
}
