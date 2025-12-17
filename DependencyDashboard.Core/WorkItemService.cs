using DependencyDashboard.Core.Computation;
using DependencyDashboard.Core.Graph;
using DependencyDashboard.Core.Models;
using DependencyDashboard.Core.Parsing;
using DependencyDashboard.Core.Validation;

namespace DependencyDashboard.Core;

/// <summary>
/// Main service that orchestrates loading, validation, and computation.
/// </summary>
public class WorkItemService
{
    private readonly CsvLoader _csvLoader;
    private readonly ReferenceValidator _referenceValidator;
    private readonly CycleDetector _cycleDetector;
    private readonly ProgressCalculator _progressCalculator;
    private readonly GraphLayoutEngine _layoutEngine;

    public WorkItemService()
    {
        _csvLoader = new CsvLoader();
        _referenceValidator = new ReferenceValidator();
        _cycleDetector = new CycleDetector();
        _progressCalculator = new ProgressCalculator();
        _layoutEngine = new GraphLayoutEngine();
    }

    /// <summary>
    /// Loads a CSV file, validates it, computes all derived values, and layouts the graph.
    /// </summary>
    public WorkItemCollection LoadAndProcess(string filePath)
    {
        // Step 1: Parse CSV
        var collection = _csvLoader.Load(filePath);
        if (collection.HasErrors)
        {
            return collection;
        }

        // Step 2: Validate references and build links
        _referenceValidator.ValidateAndLinkReferences(collection);
        if (collection.HasErrors)
        {
            return collection;
        }

        // Step 3: Detect cycles
        _cycleDetector.DetectCycles(collection);
        if (collection.HasErrors)
        {
            return collection;
        }

        // Step 4: Compute progress, status, health
        _progressCalculator.ComputeAll(collection);

        // Step 5: Compute graph layout
        _layoutEngine.ComputeLayout(collection);

        return collection;
    }

    /// <summary>
    /// Recomputes layout for a filtered subset of items.
    /// </summary>
    public void RecomputeLayout(WorkItemCollection collection, IEnumerable<WorkItem> filteredItems)
    {
        _layoutEngine.ComputeLayout(collection, filteredItems);
    }

    /// <summary>
    /// Gets all blocked descendants for a milestone.
    /// </summary>
    public IEnumerable<WorkItem> GetBlockedDescendants(WorkItem milestone)
    {
        return _progressCalculator.GetBlockedDescendants(milestone);
    }

    /// <summary>
    /// Finds the top blocker for a milestone.
    /// </summary>
    public WorkItem? FindTopBlocker(WorkItem milestone, WorkItemCollection collection)
    {
        return _progressCalculator.FindTopBlocker(milestone, collection);
    }

    /// <summary>
    /// Finds the blocker chain for an item.
    /// </summary>
    public List<WorkItem> FindBlockerChain(WorkItem item)
    {
        return _progressCalculator.FindBlockerChain(item);
    }

    /// <summary>
    /// Gets graph layout bounds.
    /// </summary>
    public (double Width, double Height) GetGraphBounds(IEnumerable<WorkItem> items)
    {
        return _layoutEngine.GetBounds(items);
    }
}
