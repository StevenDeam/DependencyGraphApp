using DependencyDashboard.Core.Models;

namespace DependencyDashboard.Core.Validation;

/// <summary>
/// Detects cycles in the dependency graph.
/// </summary>
public class CycleDetector
{
    public void DetectCycles(WorkItemCollection collection)
    {
        var cycles = FindCycles(collection.Items);
        foreach (var cycle in cycles)
        {
            collection.ValidationErrors.Add(new ValidationError
            {
                Row = 0,
                Message = $"Cyclic dependency detected: {string.Join(" -> ", cycle)} -> {cycle[0]}"
            });
        }
    }

    private List<List<string>> FindCycles(List<WorkItem> items)
    {
        var cycles = new List<List<string>>();
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var path = new List<string>();

        foreach (var item in items)
        {
            if (!visited.Contains(item.Id))
            {
                DetectCycleDFS(item, visited, recursionStack, path, cycles);
            }
        }

        return cycles;
    }

    private void DetectCycleDFS(
        WorkItem item,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> path,
        List<List<string>> cycles)
    {
        visited.Add(item.Id);
        recursionStack.Add(item.Id);
        path.Add(item.Id);

        // Follow all prerequisite chains (supports multiple prerequisites)
        foreach (var prereq in item.Prerequisites)
        {
            if (!visited.Contains(prereq.Id))
            {
                DetectCycleDFS(prereq, visited, recursionStack, path, cycles);
            }
            else if (recursionStack.Contains(prereq.Id))
            {
                // Found a cycle
                var cycleStart = path.IndexOf(prereq.Id);
                if (cycleStart >= 0)
                {
                    var cycle = path.Skip(cycleStart).ToList();
                    cycles.Add(cycle);
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        recursionStack.Remove(item.Id);
    }

    /// <summary>
    /// Checks if there are any cycles in the dependency graph (returns true if cycles exist).
    /// </summary>
    public bool HasCycles(WorkItemCollection collection)
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var item in collection.Items)
        {
            if (HasCycleDFS(item, visited, recursionStack))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasCycleDFS(WorkItem item, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(item.Id))
        {
            return true;
        }

        if (visited.Contains(item.Id))
        {
            return false;
        }

        visited.Add(item.Id);
        recursionStack.Add(item.Id);

        // Check all prerequisites (supports multiple prerequisites)
        foreach (var prereq in item.Prerequisites)
        {
            if (HasCycleDFS(prereq, visited, recursionStack))
            {
                return true;
            }
        }

        recursionStack.Remove(item.Id);
        return false;
    }
}
