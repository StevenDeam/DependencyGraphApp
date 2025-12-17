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
    private readonly PhaseMatrixLayoutEngine _phaseMatrixLayoutEngine;

    public WorkItemService()
    {
        _csvLoader = new CsvLoader();
        _referenceValidator = new ReferenceValidator();
        _cycleDetector = new CycleDetector();
        _progressCalculator = new ProgressCalculator();
        _phaseMatrixLayoutEngine = new PhaseMatrixLayoutEngine();
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

        // Step 5: Compute Phase Matrix layout
        _phaseMatrixLayoutEngine.ComputeLayout(collection, collection.Items);

        return collection;
    }

    /// <summary>
    /// Recomputes layout for a filtered subset of items.
    /// </summary>
    public void RecomputeLayout(WorkItemCollection collection, IEnumerable<WorkItem> filteredItems, GraphLayoutMode layoutMode = GraphLayoutMode.PhaseMatrix)
    {
        _phaseMatrixLayoutEngine.ComputeLayout(collection, filteredItems);
    }

    /// <summary>
    /// Gets phase matrix data for rendering.
    /// </summary>
    public IReadOnlyList<PhaseColumn> GetPhaseColumns()
    {
        return _phaseMatrixLayoutEngine.Phases;
    }

    /// <summary>
    /// Gets phase matrix layout bounds.
    /// </summary>
    public (double Width, double Height) GetPhaseMatrixBounds()
    {
        return _phaseMatrixLayoutEngine.GetBounds();
    }
}
