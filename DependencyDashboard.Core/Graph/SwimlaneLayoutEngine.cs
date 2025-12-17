using DependencyDashboard.Core.Models;

namespace DependencyDashboard.Core.Graph;

/// <summary>
/// Represents a horizontal swimlane containing a milestone and its children.
/// </summary>
public class SwimLane
{
    public WorkItem Milestone { get; set; } = null!;
    public List<WorkItem> Children { get; set; } = new();
    public double Y { get; set; }
    public double Height { get; set; }
    public double X { get; set; }
    public double Width { get; set; }
    public int HierarchyDepth { get; set; }
}

/// <summary>
/// Computes swimlane layout where items are grouped by parent milestone.
/// </summary>
public class SwimlaneLayoutEngine
{
    public const double LaneHeaderWidth = 180;
    public const double LaneHeaderHeight = 60;
    public const double NodeWidth = 180;
    public const double NodeHeight = 80;
    public const double ColumnSpacing = 40;
    public const double RowSpacing = 20;
    public const double LaneSpacing = 30;
    public const double LanePadding = 15;
    public const double HierarchyIndent = 20;

    private List<SwimLane> _lanes = new();

    public IReadOnlyList<SwimLane> Lanes => _lanes;

    /// <summary>
    /// Computes swimlane layout for all items in the collection.
    /// </summary>
    public void ComputeLayout(WorkItemCollection collection, IEnumerable<WorkItem>? filteredItems = null)
    {
        _lanes.Clear();
        var items = (filteredItems ?? collection.Items).ToList();
        if (items.Count == 0) return;

        // Build lanes from milestones that have children
        var milestonesWithChildren = items
            .Where(i => i.IsMilestone && i.Children.Any(c => items.Contains(c)))
            .ToList();

        // Sort by hierarchy: top-level first, then children
        var sortedMilestones = SortByHierarchy(milestonesWithChildren, items);

        double currentY = LanePadding;

        foreach (var (milestone, depth) in sortedMilestones)
        {
            var childrenInFilter = milestone.Children.Where(c => items.Contains(c)).ToList();
            if (childrenInFilter.Count == 0) continue;

            var lane = new SwimLane
            {
                Milestone = milestone,
                Children = childrenInFilter,
                HierarchyDepth = depth,
                X = depth * HierarchyIndent,
                Y = currentY
            };

            // Compute positions for children within the lane
            ComputeLaneLayout(lane, items);

            _lanes.Add(lane);
            currentY += lane.Height + LaneSpacing;
        }

        // Also handle items without a parent (orphans) - place them in their own area
        var orphanItems = items
            .Where(i => i.Parent == null && !milestonesWithChildren.Contains(i))
            .Where(i => !_lanes.Any(l => l.Children.Contains(i)))
            .ToList();

        if (orphanItems.Count > 0)
        {
            // Create a virtual "root" lane for orphans
            var orphanLane = new SwimLane
            {
                Milestone = null!,
                Children = orphanItems,
                HierarchyDepth = 0,
                X = 0,
                Y = currentY
            };
            ComputeLaneLayout(orphanLane, items);
            _lanes.Add(orphanLane);
        }
    }

    private List<(WorkItem milestone, int depth)> SortByHierarchy(List<WorkItem> milestones, List<WorkItem> allItems)
    {
        var result = new List<(WorkItem, int)>();
        var visited = new HashSet<string>();

        // Find top-level milestones (no parent or parent not in filter)
        var topLevel = milestones
            .Where(m => m.Parent == null || !allItems.Contains(m.Parent))
            .OrderBy(m => m.Title)
            .ToList();

        foreach (var milestone in topLevel)
        {
            AddWithChildren(milestone, 0, milestones, result, visited);
        }

        // Add any remaining milestones that weren't reached
        foreach (var milestone in milestones.Where(m => !visited.Contains(m.Id)))
        {
            result.Add((milestone, 0));
        }

        return result;
    }

    private void AddWithChildren(WorkItem milestone, int depth, List<WorkItem> allMilestones,
        List<(WorkItem, int)> result, HashSet<string> visited)
    {
        if (visited.Contains(milestone.Id)) return;
        visited.Add(milestone.Id);

        result.Add((milestone, depth));

        // Find child milestones
        var childMilestones = milestone.Children
            .Where(c => c.IsMilestone && allMilestones.Contains(c))
            .OrderBy(c => c.Title)
            .ToList();

        foreach (var child in childMilestones)
        {
            AddWithChildren(child, depth + 1, allMilestones, result, visited);
        }
    }

    private void ComputeLaneLayout(SwimLane lane, List<WorkItem> allItems)
    {
        var children = lane.Children;
        if (children.Count == 0)
        {
            lane.Width = LaneHeaderWidth + LanePadding * 2;
            lane.Height = LaneHeaderHeight + LanePadding * 2;
            return;
        }

        // Compute step for each child based on prerequisites within the lane
        var stepMap = new Dictionary<string, int>();
        foreach (var child in children)
        {
            stepMap[child.Id] = ComputeStep(child, children, stepMap);
        }

        // Group children by step
        var byStep = children.GroupBy(c => stepMap[c.Id]).OrderBy(g => g.Key).ToList();

        // Compute X positions based on step
        double contentStartX = lane.X + LaneHeaderWidth + LanePadding;
        int maxStep = byStep.Count > 0 ? byStep.Max(g => g.Key) : 0;

        // For each step column, stack items vertically
        var columnHeights = new Dictionary<int, double>();
        double maxColumnHeight = 0;

        foreach (var stepGroup in byStep)
        {
            int step = stepGroup.Key;
            double columnX = contentStartX + step * (NodeWidth + ColumnSpacing);
            double columnY = 0;

            var itemsInStep = stepGroup.OrderBy(i => i.Title).ToList();
            foreach (var item in itemsInStep)
            {
                item.X = columnX;
                item.Y = lane.Y + LanePadding + columnY;
                item.ComputedLevel = step;
                columnY += NodeHeight + RowSpacing;
            }

            columnHeights[step] = columnY - RowSpacing;
            maxColumnHeight = Math.Max(maxColumnHeight, columnHeights[step]);
        }

        // Set lane dimensions
        lane.Width = LaneHeaderWidth + LanePadding * 2 + (maxStep + 1) * (NodeWidth + ColumnSpacing);
        lane.Height = Math.Max(LaneHeaderHeight, maxColumnHeight) + LanePadding * 2;

        // Adjust Y positions to be relative to lane
        foreach (var child in children)
        {
            child.Y = lane.Y + LanePadding + (child.Y - lane.Y - LanePadding);
        }
    }

    private int ComputeStep(WorkItem item, List<WorkItem> laneChildren, Dictionary<string, int> stepMap)
    {
        if (stepMap.TryGetValue(item.Id, out var cached))
        {
            return cached;
        }

        // If no prerequisite or prerequisite is outside the lane, step is 0
        if (item.Prerequisite == null || !laneChildren.Contains(item.Prerequisite))
        {
            return 0;
        }

        // Step is prereq step + 1
        return ComputeStep(item.Prerequisite, laneChildren, stepMap) + 1;
    }

    /// <summary>
    /// Gets the total bounds of all positioned items and lanes.
    /// </summary>
    public (double Width, double Height) GetBounds()
    {
        if (_lanes.Count == 0)
        {
            return (800, 600);
        }

        double maxX = _lanes.Max(l => l.X + l.Width);
        double maxY = _lanes.Max(l => l.Y + l.Height);

        return (maxX + LanePadding, maxY + LanePadding);
    }

    /// <summary>
    /// Checks if an edge crosses lanes (prerequisite and dependent are in different lanes).
    /// </summary>
    public bool IsCrossLaneEdge(WorkItem prereq, WorkItem dependent)
    {
        var prereqLane = _lanes.FirstOrDefault(l => l.Children.Contains(prereq));
        var dependentLane = _lanes.FirstOrDefault(l => l.Children.Contains(dependent));

        return prereqLane != dependentLane;
    }
}
