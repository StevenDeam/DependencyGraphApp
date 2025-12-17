using DependencyDashboard.Core.Models;

namespace DependencyDashboard.Core.Graph;

/// <summary>
/// Computes deterministic layout positions for work items in a bracket/tournament style.
/// </summary>
public class GraphLayoutEngine
{
    public const double NodeWidth = 200;
    public const double NodeHeight = 100;
    public const double HorizontalSpacing = 100;
    public const double VerticalSpacing = 40;

    /// <summary>
    /// Computes layout positions for all items in the collection.
    /// </summary>
    public void ComputeLayout(WorkItemCollection collection, IEnumerable<WorkItem>? filteredItems = null)
    {
        var items = (filteredItems ?? collection.Items).ToList();
        if (items.Count == 0) return;

        // First, compute levels for all items
        ComputeLevels(items);

        // Group items by level
        var itemsByLevel = items
            .GroupBy(i => i.ComputedLevel)
            .OrderBy(g => g.Key)
            .ToList();

        // Compute X position based on level
        foreach (var group in itemsByLevel)
        {
            double x = group.Key * (NodeWidth + HorizontalSpacing);
            foreach (var item in group)
            {
                item.X = x;
            }
        }

        // Compute Y position within each level
        foreach (var group in itemsByLevel)
        {
            // Sort by title for deterministic order
            var sortedItems = group.OrderBy(i => i.Title).ThenBy(i => i.Id).ToList();
            for (int i = 0; i < sortedItems.Count; i++)
            {
                sortedItems[i].Y = i * (NodeHeight + VerticalSpacing);
            }
        }

        // Optimize Y positions to minimize edge crossings
        OptimizeYPositions(itemsByLevel);
    }

    /// <summary>
    /// Computes the level for each item.
    /// Level(node) = max(Level(prereq)) + 1
    /// Nodes with no prerequisites start at level 0.
    /// </summary>
    private void ComputeLevels(List<WorkItem> items)
    {
        // Reset computed levels
        foreach (var item in items)
        {
            item.ComputedLevel = -1;
        }

        var itemSet = items.ToHashSet();

        // Use DFS with memoization
        foreach (var item in items)
        {
            ComputeLevelRecursive(item, itemSet, new HashSet<string>());
        }
    }

    private int ComputeLevelRecursive(WorkItem item, HashSet<WorkItem> itemSet, HashSet<string> visiting)
    {
        if (item.ComputedLevel >= 0)
        {
            return item.ComputedLevel;
        }

        // Check for explicit level hint
        if (item.Level.HasValue)
        {
            item.ComputedLevel = item.Level.Value;
            return item.ComputedLevel;
        }

        // Detect cycles (shouldn't happen if validation passed, but be safe)
        if (visiting.Contains(item.Id))
        {
            item.ComputedLevel = 0;
            return 0;
        }

        visiting.Add(item.Id);

        // If no prerequisite or prereq not in filtered set, this is level 0
        if (item.Prerequisite == null || !itemSet.Contains(item.Prerequisite))
        {
            item.ComputedLevel = 0;
        }
        else
        {
            // Level is prereq level + 1
            item.ComputedLevel = ComputeLevelRecursive(item.Prerequisite, itemSet, visiting) + 1;
        }

        visiting.Remove(item.Id);
        return item.ComputedLevel;
    }

    /// <summary>
    /// Optimizes Y positions to reduce edge crossings.
    /// Uses a simple barycenter heuristic.
    /// </summary>
    private void OptimizeYPositions(List<IGrouping<int, WorkItem>> itemsByLevel)
    {
        const int iterations = 3;

        for (int iter = 0; iter < iterations; iter++)
        {
            // Forward pass (from left to right)
            for (int levelIdx = 1; levelIdx < itemsByLevel.Count; levelIdx++)
            {
                var currentLevel = itemsByLevel[levelIdx].ToList();
                OptimizeLevelYPositions(currentLevel, getPrerequisite: true);
            }

            // Backward pass (from right to left)
            for (int levelIdx = itemsByLevel.Count - 2; levelIdx >= 0; levelIdx--)
            {
                var currentLevel = itemsByLevel[levelIdx].ToList();
                OptimizeLevelYPositions(currentLevel, getPrerequisite: false);
            }
        }

        // Final pass: ensure no overlaps and compact
        foreach (var group in itemsByLevel)
        {
            var sortedItems = group.OrderBy(i => i.Y).ToList();
            for (int i = 0; i < sortedItems.Count; i++)
            {
                double minY = i * (NodeHeight + VerticalSpacing);
                if (sortedItems[i].Y < minY)
                {
                    sortedItems[i].Y = minY;
                }
            }
        }
    }

    private void OptimizeLevelYPositions(List<WorkItem> items, bool getPrerequisite)
    {
        foreach (var item in items)
        {
            var connectedItems = getPrerequisite
                ? (item.Prerequisite != null ? new[] { item.Prerequisite } : Array.Empty<WorkItem>())
                : item.Dependents.ToArray();

            if (connectedItems.Length > 0)
            {
                // Set Y to barycenter of connected items
                item.Y = connectedItems.Average(c => c.Y + NodeHeight / 2) - NodeHeight / 2;
            }
        }
    }

    /// <summary>
    /// Gets the total bounds of all positioned items.
    /// </summary>
    public (double Width, double Height) GetBounds(IEnumerable<WorkItem> items)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            return (0, 0);
        }

        double maxX = itemList.Max(i => i.X + NodeWidth);
        double maxY = itemList.Max(i => i.Y + NodeHeight);

        return (maxX, maxY);
    }
}
