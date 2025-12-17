using DependencyDashboard.Core;
using DependencyDashboard.Core.Graph;
using DependencyDashboard.Core.Models;
using System.Windows.Media;

namespace DependencyDashboard.App.ViewModels;

public class SwimLaneViewModel : ViewModelBase
{
    private readonly SwimLane _lane;
    private readonly WorkItemService _service;

    public SwimLaneViewModel(SwimLane lane, WorkItemService service)
    {
        _lane = lane;
        _service = service;
    }

    public SwimLane Lane => _lane;
    public WorkItem? Milestone => _lane.Milestone;
    public bool HasMilestone => _lane.Milestone != null;

    public string Title => Milestone?.Title ?? "Unassigned Items";
    public string Id => Milestone?.Id ?? "";
    public string DisplayHeader => HasMilestone ? $"{Title} ({Id})" : Title;

    public double X => _lane.X;
    public double Y => _lane.Y;
    public double Width => _lane.Width;
    public double Height => _lane.Height;
    public int HierarchyDepth => _lane.HierarchyDepth;

    public double ComputedPercent => Milestone?.ComputedPercent ?? 0;
    public string DisplayPercent => $"{ComputedPercent:F0}%";

    public WorkItemStatus ComputedStatus => Milestone?.ComputedStatus ?? WorkItemStatus.NotStarted;
    public string StatusText => ComputedStatus switch
    {
        WorkItemStatus.NotStarted => "Not Started",
        WorkItemStatus.InProgress => "In Progress",
        WorkItemStatus.Blocked => "Blocked",
        WorkItemStatus.Done => "Done",
        WorkItemStatus.NotApplicable => "N/A",
        _ => ComputedStatus.ToString()
    };

    public Brush StatusBrush => ComputedStatus switch
    {
        WorkItemStatus.NotStarted => Brushes.Gray,
        WorkItemStatus.InProgress => Brushes.DodgerBlue,
        WorkItemStatus.Blocked => Brushes.OrangeRed,
        WorkItemStatus.Done => Brushes.Green,
        WorkItemStatus.NotApplicable => Brushes.LightGray,
        _ => Brushes.Gray
    };

    public HealthStatus Health => Milestone?.Health ?? HealthStatus.NoDate;
    public Brush HealthBrush => Health switch
    {
        HealthStatus.Green => Brushes.Green,
        HealthStatus.Yellow => Brushes.Orange,
        HealthStatus.Red => Brushes.Red,
        HealthStatus.NoDate => Brushes.Gray,
        _ => Brushes.Gray
    };

    public Brush LaneBackground
    {
        get
        {
            // Alternating lane colors based on hierarchy depth
            byte alpha = (byte)(HierarchyDepth % 2 == 0 ? 15 : 25);
            return new SolidColorBrush(Color.FromArgb(alpha, 100, 149, 237));
        }
    }

    public Brush LaneBorder => new SolidColorBrush(Color.FromArgb(60, 100, 149, 237));

    public double HeaderX => X;
    public double HeaderY => Y;
    public double HeaderWidth => SwimlaneLayoutEngine.LaneHeaderWidth;
    public double HeaderHeight => Height;

    public List<WorkItem> Children => _lane.Children;

    public bool IsCrossLaneEdge(WorkItem prereq, WorkItem dependent)
    {
        return _service.IsCrossLaneEdge(prereq, dependent);
    }
}
