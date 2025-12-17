using DependencyDashboard.Core.Models;
using System.Text.RegularExpressions;

namespace DependencyDashboard.Core.Graph;

/// <summary>
/// Represents a phase column in the Phase Matrix layout.
/// </summary>
public class PhaseColumn
{
    public string PhaseName { get; set; } = string.Empty;
    public int PhaseIndex { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public List<AssemblyGroup> Groups { get; set; } = new();
}

/// <summary>
/// Represents an assembly group (milestone with children) within a phase.
/// </summary>
public class AssemblyGroup
{
    public WorkItem Milestone { get; set; } = null!;
    public string PhaseName { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public List<DisciplineRow> DisciplineRows { get; set; } = new();
    public List<WorkItem> AllItems { get; set; } = new();
}

/// <summary>
/// Represents a discipline row within an assembly group.
/// </summary>
public class DisciplineRow
{
    public string Discipline { get; set; } = string.Empty;
    public double Y { get; set; }
    public double Height { get; set; }
    public List<WorkItem> Items { get; set; } = new();
}

/// <summary>
/// Computes Phase Matrix layout where items are organized by Phase columns,
/// Assembly Groups, and Discipline rows.
/// </summary>
public partial class PhaseMatrixLayoutEngine
{
    // Layout constants
    public const double PhaseHeaderHeight = 40;
    public const double PhaseColumnMinWidth = 350;
    public const double PhaseColumnSpacing = 20;
    public const double GroupHeaderHeight = 70;
    public const double GroupPadding = 15;
    public const double GroupSpacing = 20;
    public const double DisciplineLabelWidth = 60;
    public const double DisciplineRowHeight = 90;
    public const double DisciplineRowSpacing = 5;
    public const double NodeWidth = 160;
    public const double NodeHeight = 70;
    public const double NodeSpacing = 20;
    public const double ContentPadding = 20;

    private List<PhaseColumn> _phases = new();

    public IReadOnlyList<PhaseColumn> Phases => _phases;

    /// <summary>
    /// Computes Phase Matrix layout for all items in the collection.
    /// </summary>
    public void ComputeLayout(WorkItemCollection collection, IEnumerable<WorkItem>? filteredItems = null)
    {
        _phases.Clear();
        var items = (filteredItems ?? collection.Items).ToList();
        if (items.Count == 0) return;

        // Step 1: Compute phase inheritance
        ComputePhaseInheritance(items);

        // Step 2: Group items by phase
        var phaseGroups = items
            .GroupBy(i => i.Phase)
            .OrderBy(g => GetPhaseOrder(g.Key))
            .ToList();

        double currentX = ContentPadding;

        foreach (var phaseGroup in phaseGroups)
        {
            var phase = new PhaseColumn
            {
                PhaseName = phaseGroup.Key,
                PhaseIndex = _phases.Count,
                X = currentX,
                Y = ContentPadding
            };

            // Build assembly groups within this phase
            var phaseItems = phaseGroup.ToList();
            BuildAssemblyGroups(phase, phaseItems, items);

            // Calculate phase dimensions
            if (phase.Groups.Count > 0)
            {
                phase.Width = phase.Groups.Max(g => g.Width) + GroupPadding * 2;
                phase.Height = PhaseHeaderHeight + phase.Groups.Sum(g => g.Height) +
                              (phase.Groups.Count - 1) * GroupSpacing + GroupPadding * 2;
            }
            else
            {
                phase.Width = PhaseColumnMinWidth;
                phase.Height = PhaseHeaderHeight + GroupPadding * 2;
            }

            phase.Width = Math.Max(phase.Width, PhaseColumnMinWidth);

            _phases.Add(phase);
            currentX += phase.Width + PhaseColumnSpacing;
        }

        // Normalize phase heights to be consistent
        if (_phases.Count > 0)
        {
            double maxHeight = _phases.Max(p => p.Height);
            foreach (var phase in _phases)
            {
                phase.Height = maxHeight;
            }
        }
    }

    private void ComputePhaseInheritance(List<WorkItem> items)
    {
        // First pass: assign explicit phases
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.PhaseRaw))
            {
                item.Phase = item.PhaseRaw;
            }
        }

        // Second pass: inherit from parent milestones (top-down)
        var processed = new HashSet<string>();
        foreach (var item in items.Where(i => i.Parent == null))
        {
            PropagatePhaseToDescendants(item, item.Phase, processed, items);
        }

        // Third pass: default any remaining items to "Phase 1"
        bool anyPhaseExists = items.Any(i => !string.IsNullOrEmpty(i.Phase));
        string defaultPhase = anyPhaseExists ? "Unassigned" : "Phase 1";

        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.Phase))
            {
                item.Phase = defaultPhase;
            }
        }
    }

    private void PropagatePhaseToDescendants(WorkItem item, string inheritedPhase, HashSet<string> processed, List<WorkItem> allItems)
    {
        if (processed.Contains(item.Id)) return;
        processed.Add(item.Id);

        // Use own phase if defined, otherwise inherit
        string currentPhase = !string.IsNullOrEmpty(item.PhaseRaw) ? item.PhaseRaw : inheritedPhase;
        item.Phase = currentPhase;

        // Propagate to children
        foreach (var child in item.Children.Where(c => allItems.Contains(c)))
        {
            PropagatePhaseToDescendants(child, currentPhase, processed, allItems);
        }
    }

    private static int GetPhaseOrder(string phaseName)
    {
        // Try to extract leading number from phase name (e.g., "Phase 2 ..." -> 2)
        var match = PhaseNumberRegex().Match(phaseName);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int num))
        {
            return num;
        }
        // Fall back to lexicographic ordering (use a high base number)
        return 1000 + phaseName.GetHashCode() % 1000;
    }

    [GeneratedRegex(@"^(?:Phase\s*)?(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex PhaseNumberRegex();

    private void BuildAssemblyGroups(PhaseColumn phase, List<WorkItem> phaseItems, List<WorkItem> allItems)
    {
        // Find milestones that have children as assembly groups
        var groupMilestones = phaseItems
            .Where(i => i.IsMilestone && i.Children.Any(c => phaseItems.Contains(c)))
            .OrderBy(m => m.Title)
            .ToList();

        // Also find orphan items (no parent milestone in this phase)
        var assignedItems = new HashSet<string>();
        foreach (var milestone in groupMilestones)
        {
            CollectDescendantIds(milestone, assignedItems, phaseItems);
            assignedItems.Add(milestone.Id);
        }

        var orphanItems = phaseItems
            .Where(i => !assignedItems.Contains(i.Id) && !i.IsMilestone)
            .ToList();

        double currentY = phase.Y + PhaseHeaderHeight + GroupPadding;

        // Build each assembly group
        foreach (var milestone in groupMilestones)
        {
            var group = BuildAssemblyGroup(milestone, phase.X + GroupPadding, currentY, phaseItems, allItems);
            group.PhaseName = phase.PhaseName;
            phase.Groups.Add(group);
            currentY += group.Height + GroupSpacing;
        }

        // Handle orphan items as a pseudo-group
        if (orphanItems.Count > 0)
        {
            var orphanGroup = BuildOrphanGroup(orphanItems, phase.X + GroupPadding, currentY, allItems);
            orphanGroup.PhaseName = phase.PhaseName;
            phase.Groups.Add(orphanGroup);
        }
    }

    private void CollectDescendantIds(WorkItem item, HashSet<string> ids, List<WorkItem> phaseItems)
    {
        foreach (var child in item.Children.Where(c => phaseItems.Contains(c)))
        {
            ids.Add(child.Id);
            CollectDescendantIds(child, ids, phaseItems);
        }
    }

    private AssemblyGroup BuildAssemblyGroup(WorkItem milestone, double x, double y,
        List<WorkItem> phaseItems, List<WorkItem> allItems)
    {
        var group = new AssemblyGroup
        {
            Milestone = milestone,
            X = x,
            Y = y
        };

        // Get all descendant tasks in this phase
        var descendantTasks = GetDescendantTasks(milestone, phaseItems);
        group.AllItems = descendantTasks;

        // Group by discipline
        var byDiscipline = descendantTasks
            .GroupBy(t => t.Discipline)
            .OrderBy(g => GetDisciplineOrder(g.Key))
            .ToList();

        double rowY = y + GroupHeaderHeight;
        double maxRowWidth = 0;

        foreach (var disciplineGroup in byDiscipline)
        {
            var row = new DisciplineRow
            {
                Discipline = disciplineGroup.Key,
                Y = rowY,
                Height = DisciplineRowHeight,
                Items = disciplineGroup.OrderBy(t => ComputeLocalDepth(t, descendantTasks)).ThenBy(t => t.Title).ToList()
            };

            // Position items within the row
            double itemX = x + DisciplineLabelWidth + NodeSpacing;
            foreach (var item in row.Items)
            {
                item.X = itemX;
                item.Y = rowY + (DisciplineRowHeight - NodeHeight) / 2;
                itemX += NodeWidth + NodeSpacing;
            }

            double rowWidth = DisciplineLabelWidth + row.Items.Count * (NodeWidth + NodeSpacing) + NodeSpacing;
            maxRowWidth = Math.Max(maxRowWidth, rowWidth);

            group.DisciplineRows.Add(row);
            rowY += DisciplineRowHeight + DisciplineRowSpacing;
        }

        group.Width = Math.Max(maxRowWidth + GroupPadding * 2, PhaseColumnMinWidth - GroupPadding * 2);
        group.Height = GroupHeaderHeight +
                      byDiscipline.Count * (DisciplineRowHeight + DisciplineRowSpacing) +
                      GroupPadding;

        return group;
    }

    private AssemblyGroup BuildOrphanGroup(List<WorkItem> orphanItems, double x, double y, List<WorkItem> allItems)
    {
        var group = new AssemblyGroup
        {
            Milestone = null!,
            X = x,
            Y = y,
            AllItems = orphanItems
        };

        // Group by discipline
        var byDiscipline = orphanItems
            .GroupBy(t => t.Discipline)
            .OrderBy(g => GetDisciplineOrder(g.Key))
            .ToList();

        double rowY = y + GroupHeaderHeight;
        double maxRowWidth = 0;

        foreach (var disciplineGroup in byDiscipline)
        {
            var row = new DisciplineRow
            {
                Discipline = disciplineGroup.Key,
                Y = rowY,
                Height = DisciplineRowHeight,
                Items = disciplineGroup.OrderBy(t => t.Title).ToList()
            };

            double itemX = x + DisciplineLabelWidth + NodeSpacing;
            foreach (var item in row.Items)
            {
                item.X = itemX;
                item.Y = rowY + (DisciplineRowHeight - NodeHeight) / 2;
                itemX += NodeWidth + NodeSpacing;
            }

            double rowWidth = DisciplineLabelWidth + row.Items.Count * (NodeWidth + NodeSpacing) + NodeSpacing;
            maxRowWidth = Math.Max(maxRowWidth, rowWidth);

            group.DisciplineRows.Add(row);
            rowY += DisciplineRowHeight + DisciplineRowSpacing;
        }

        group.Width = Math.Max(maxRowWidth + GroupPadding * 2, PhaseColumnMinWidth - GroupPadding * 2);
        group.Height = GroupHeaderHeight +
                      byDiscipline.Count * (DisciplineRowHeight + DisciplineRowSpacing) +
                      GroupPadding;

        return group;
    }

    private List<WorkItem> GetDescendantTasks(WorkItem milestone, List<WorkItem> phaseItems)
    {
        var result = new List<WorkItem>();
        CollectDescendantTasks(milestone, result, phaseItems);
        return result;
    }

    private void CollectDescendantTasks(WorkItem item, List<WorkItem> result, List<WorkItem> phaseItems)
    {
        foreach (var child in item.Children.Where(c => phaseItems.Contains(c)))
        {
            if (child.IsTask)
            {
                result.Add(child);
            }
            CollectDescendantTasks(child, result, phaseItems);
        }
    }

    private static int ComputeLocalDepth(WorkItem item, List<WorkItem> groupItems)
    {
        // Compute depth based on prerequisite chain within the group
        int depth = 0;
        var current = item.Prerequisite;
        while (current != null && groupItems.Contains(current))
        {
            depth++;
            current = current.Prerequisite;
        }
        return depth;
    }

    private static int GetDisciplineOrder(string discipline)
    {
        return discipline.ToUpperInvariant() switch
        {
            "HW" => 0,
            "FW" => 1,
            "SYSTEM" => 2,
            "TEST" => 3,
            _ => 100
        };
    }

    /// <summary>
    /// Gets the total bounds of all positioned phases and groups.
    /// </summary>
    public (double Width, double Height) GetBounds()
    {
        if (_phases.Count == 0)
        {
            return (800, 600);
        }

        double maxX = _phases.Max(p => p.X + p.Width);
        double maxY = _phases.Max(p => p.Y + p.Height);

        return (maxX + ContentPadding, maxY + ContentPadding);
    }

    /// <summary>
    /// Finds the phase column containing a work item.
    /// </summary>
    public PhaseColumn? FindPhaseForItem(WorkItem item)
    {
        return _phases.FirstOrDefault(p =>
            p.Groups.Any(g => g.AllItems.Contains(item) || g.Milestone == item));
    }

    /// <summary>
    /// Finds the assembly group containing a work item.
    /// </summary>
    public AssemblyGroup? FindGroupForItem(WorkItem item)
    {
        foreach (var phase in _phases)
        {
            var group = phase.Groups.FirstOrDefault(g => g.AllItems.Contains(item) || g.Milestone == item);
            if (group != null) return group;
        }
        return null;
    }

    /// <summary>
    /// Checks if an edge crosses phase boundaries.
    /// </summary>
    public bool IsCrossPhaseEdge(WorkItem prereq, WorkItem dependent)
    {
        var prereqPhase = FindPhaseForItem(prereq);
        var dependentPhase = FindPhaseForItem(dependent);
        return prereqPhase != dependentPhase;
    }

    /// <summary>
    /// Checks if an edge crosses assembly group boundaries.
    /// </summary>
    public bool IsCrossGroupEdge(WorkItem prereq, WorkItem dependent)
    {
        var prereqGroup = FindGroupForItem(prereq);
        var dependentGroup = FindGroupForItem(dependent);
        return prereqGroup != dependentGroup;
    }
}
