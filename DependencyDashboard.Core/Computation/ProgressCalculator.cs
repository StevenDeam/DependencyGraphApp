using DependencyDashboard.Core.Models;

namespace DependencyDashboard.Core.Computation;

/// <summary>
/// Calculates progress, status, and health for work items per the spec.
/// </summary>
public class ProgressCalculator
{
    /// <summary>
    /// Computes all calculated fields for the work item collection.
    /// Must be called after references are linked.
    /// </summary>
    public void ComputeAll(WorkItemCollection collection)
    {
        // First pass: compute progress for all items
        foreach (var item in collection.Items)
        {
            ComputeProgress(item);
        }

        // Second pass: compute status (depends on progress being computed)
        foreach (var item in collection.Items)
        {
            ComputeStatus(item);
        }

        // Third pass: compute health for milestones
        foreach (var item in collection.Milestones)
        {
            ComputeHealth(item);
        }
    }

    /// <summary>
    /// Computes the percent complete for an item.
    /// For tasks: uses raw percent from CSV.
    /// For milestones: weighted average of all descendant tasks (excluding NA).
    /// </summary>
    private void ComputeProgress(WorkItem item)
    {
        if (item.IsNA)
        {
            item.ComputedPercent = 0;
            return;
        }

        if (item.IsTask)
        {
            item.ComputedPercent = item.PercentCompleteRaw;
            return;
        }

        // Milestone: compute weighted average of all descendant tasks
        var descendantTasks = GetAllDescendantTasks(item).ToList();

        if (descendantTasks.Count == 0)
        {
            item.ComputedPercent = 0;
            return;
        }

        double totalWeight = descendantTasks.Sum(t => t.Weight);

        if (totalWeight == 0)
        {
            // Fall back to equal weights (weight = 1)
            totalWeight = descendantTasks.Count;
            item.ComputedPercent = descendantTasks.Sum(t => t.PercentCompleteRaw) / totalWeight;
        }
        else
        {
            double weightedSum = descendantTasks.Sum(t => t.PercentCompleteRaw * t.Weight);
            item.ComputedPercent = weightedSum / totalWeight;
        }
    }

    /// <summary>
    /// Gets all descendant tasks (not milestones) in the hierarchy, excluding NA items.
    /// </summary>
    private IEnumerable<WorkItem> GetAllDescendantTasks(WorkItem item)
    {
        foreach (var child in item.Children)
        {
            if (child.IsNA) continue;

            if (child.IsTask)
            {
                yield return child;
            }
            else
            {
                // Recursively get tasks from child milestone
                foreach (var descendant in GetAllDescendantTasks(child))
                {
                    yield return descendant;
                }
            }
        }
    }

    /// <summary>
    /// Computes the status for an item per the spec:
    /// - Done if PercentComplete >= 100
    /// - NotStarted if PercentComplete == 0 and not blocked
    /// - InProgress if 0 < PercentComplete < 100 and not blocked
    /// - Blocked if PrereqId exists and prerequisite item is not Done
    /// - NA if explicitly marked NA
    /// </summary>
    private void ComputeStatus(WorkItem item)
    {
        if (item.IsNA)
        {
            item.ComputedStatus = WorkItemStatus.NotApplicable;
            return;
        }

        var percent = item.IsTask ? item.PercentCompleteRaw : item.ComputedPercent;

        // Check if done first
        if (percent >= 100)
        {
            item.ComputedStatus = WorkItemStatus.Done;
            item.BlockedById = null;
            return;
        }

        // Check if blocked (any prerequisite not Done causes blocked status)
        var blockingPrereq = item.Prerequisites.FirstOrDefault(p => p.ComputedStatus != WorkItemStatus.Done);
        if (blockingPrereq != null)
        {
            item.ComputedStatus = WorkItemStatus.Blocked;
            item.BlockedById = blockingPrereq.Id;
            return;
        }

        // Not blocked
        item.BlockedById = null;

        if (percent == 0)
        {
            item.ComputedStatus = WorkItemStatus.NotStarted;
        }
        else
        {
            item.ComputedStatus = WorkItemStatus.InProgress;
        }
    }

    /// <summary>
    /// Computes health for milestones per the spec:
    /// - Green if Done OR (not blocked AND DaysToTarget > 14)
    /// - Yellow if not Done AND (blocked OR DaysToTarget between 7 and 14 inclusive)
    /// - Red if not Done AND (DaysToTarget <= 6)
    /// - NoDate if no target date
    /// </summary>
    private void ComputeHealth(WorkItem milestone)
    {
        if (!milestone.TargetDate.HasValue)
        {
            milestone.Health = HealthStatus.NoDate;
            return;
        }

        if (milestone.ComputedStatus == WorkItemStatus.Done)
        {
            milestone.Health = HealthStatus.Green;
            return;
        }

        var daysToTarget = (milestone.TargetDate.Value.Date - DateTime.Today).Days;

        if (daysToTarget <= 6)
        {
            milestone.Health = HealthStatus.Red;
        }
        else if (milestone.ComputedStatus == WorkItemStatus.Blocked || (daysToTarget >= 7 && daysToTarget <= 14))
        {
            milestone.Health = HealthStatus.Yellow;
        }
        else // daysToTarget > 14 and not blocked
        {
            milestone.Health = HealthStatus.Green;
        }
    }

    /// <summary>
    /// Gets all blocked descendant tasks for a milestone.
    /// </summary>
    public IEnumerable<WorkItem> GetBlockedDescendants(WorkItem milestone)
    {
        return GetAllDescendantTasks(milestone)
            .Where(t => t.ComputedStatus == WorkItemStatus.Blocked);
    }

    /// <summary>
    /// Finds the top blocker for a milestone.
    /// Per spec: the prerequisite item that appears most frequently in blocked descendant chains.
    /// </summary>
    public WorkItem? FindTopBlocker(WorkItem milestone, WorkItemCollection collection)
    {
        var blockedDescendants = GetBlockedDescendants(milestone).ToList();
        if (blockedDescendants.Count == 0)
        {
            return null;
        }

        // Count how many times each prerequisite appears in blocker chains
        var blockerCounts = new Dictionary<string, int>();

        foreach (var blocked in blockedDescendants)
        {
            var current = blocked.Prerequisite;
            while (current != null && current.ComputedStatus != WorkItemStatus.Done)
            {
                if (!blockerCounts.ContainsKey(current.Id))
                {
                    blockerCounts[current.Id] = 0;
                }
                blockerCounts[current.Id]++;
                current = current.Prerequisite;
            }
        }

        if (blockerCounts.Count == 0)
        {
            return null;
        }

        // Find the item(s) with the highest count
        var maxCount = blockerCounts.Values.Max();
        var topBlockers = blockerCounts
            .Where(kv => kv.Value == maxCount)
            .Select(kv => collection.ItemsById[kv.Key])
            .ToList();

        if (topBlockers.Count == 1)
        {
            return topBlockers[0];
        }

        // Tie-breaker: choose the one with earliest target date (milestone) else lexical Id
        return topBlockers
            .OrderBy(b => b.TargetDate ?? DateTime.MaxValue)
            .ThenBy(b => b.Id)
            .First();
    }

    /// <summary>
    /// Follows the prerequisite chain from an item until null or Done.
    /// </summary>
    public List<WorkItem> FindBlockerChain(WorkItem item)
    {
        var chain = new List<WorkItem>();
        var current = item.Prerequisite;

        while (current != null && current.ComputedStatus != WorkItemStatus.Done)
        {
            chain.Add(current);
            current = current.Prerequisite;
        }

        return chain;
    }
}
