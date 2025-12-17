using DependencyDashboard.Core.Models;

namespace DependencyDashboard.Core.Validation;

/// <summary>
/// Validates references (ParentId, PrereqId) and builds relationship links.
/// </summary>
public class ReferenceValidator
{
    public void ValidateAndLinkReferences(WorkItemCollection collection)
    {
        var itemsById = collection.ItemsById;

        foreach (var item in collection.Items)
        {
            // Validate and link ParentId
            if (!string.IsNullOrEmpty(item.ParentId))
            {
                if (itemsById.TryGetValue(item.ParentId, out var parent))
                {
                    item.Parent = parent;
                    parent.Children.Add(item);
                }
                else
                {
                    collection.ValidationErrors.Add(new ValidationError
                    {
                        Row = GetRowNumber(collection, item),
                        Column = "ParentId",
                        Message = $"ParentId '{item.ParentId}' references non-existent item"
                    });
                }
            }

            // Validate and link PrereqId
            if (!string.IsNullOrEmpty(item.PrereqId))
            {
                if (itemsById.TryGetValue(item.PrereqId, out var prereq))
                {
                    item.Prerequisite = prereq;
                    prereq.Dependents.Add(item);

                    collection.DependencyEdges.Add(new DependencyEdge
                    {
                        PrereqId = item.PrereqId,
                        DependentId = item.Id,
                        Prerequisite = prereq,
                        Dependent = item
                    });
                }
                else
                {
                    collection.ValidationErrors.Add(new ValidationError
                    {
                        Row = GetRowNumber(collection, item),
                        Column = "PrereqId",
                        Message = $"PrereqId '{item.PrereqId}' references non-existent item"
                    });
                }
            }
        }
    }

    private int GetRowNumber(WorkItemCollection collection, WorkItem item)
    {
        // Row 1 is header, so add 2 to index
        return collection.Items.IndexOf(item) + 2;
    }
}
