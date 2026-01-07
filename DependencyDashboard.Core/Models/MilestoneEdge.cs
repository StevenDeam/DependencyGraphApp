namespace DependencyDashboard.Core.Models;

/// <summary>
/// Represents a visual edge between milestone assembly groups for bracket rendering.
/// </summary>
public class MilestoneEdge
{
    public WorkItem FromMilestone { get; set; } = null!;
    public WorkItem ToMilestone { get; set; } = null!;

    // Computed layout coordinates for rendering
    public double FromX { get; set; }
    public double FromY { get; set; }
    public double ToX { get; set; }
    public double ToY { get; set; }
}
