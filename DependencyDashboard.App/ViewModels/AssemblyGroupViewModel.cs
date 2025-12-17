using DependencyDashboard.Core.Graph;
using DependencyDashboard.Core.Models;
using System.Windows.Media;

namespace DependencyDashboard.App.ViewModels;

public class AssemblyGroupViewModel : ViewModelBase
{
    private readonly AssemblyGroup _group;

    public AssemblyGroupViewModel(AssemblyGroup group)
    {
        _group = group;
        DisciplineRows = group.DisciplineRows.Select(r => new DisciplineRowViewModel(r)).ToList();
    }

    public AssemblyGroup Group => _group;
    public WorkItem? Milestone => _group.Milestone;
    public bool HasMilestone => _group.Milestone != null;

    public string Title => Milestone?.Title ?? "Unassigned Items";
    public string Id => Milestone?.Id ?? "";
    public string DisplayHeader => HasMilestone ? $"{Title} ({Id})" : Title;

    public double X => _group.X;
    public double Y => _group.Y;
    public double Width => _group.Width;
    public double Height => _group.Height;

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

    public DateTime? TargetDate => Milestone?.TargetDate;
    public string DisplayTargetDate => TargetDate.HasValue ? TargetDate.Value.ToString("yyyy-MM-dd") : "";

    public Brush GroupBackground => new SolidColorBrush(Color.FromArgb(30, 200, 200, 200));
    public Brush GroupBorder => new SolidColorBrush(Color.FromArgb(100, 150, 150, 150));
    public Brush HeaderBackground => new SolidColorBrush(Color.FromArgb(200, 80, 80, 80));

    public List<DisciplineRowViewModel> DisciplineRows { get; }
    public List<WorkItem> AllItems => _group.AllItems;
}

public class DisciplineRowViewModel : ViewModelBase
{
    private readonly DisciplineRow _row;

    public DisciplineRowViewModel(DisciplineRow row)
    {
        _row = row;
    }

    public string Discipline => _row.Discipline;
    public double Y => _row.Y;
    public double Height => _row.Height;
    public List<WorkItem> Items => _row.Items;

    public Brush RowBackground => new SolidColorBrush(Color.FromArgb(15, 100, 100, 100));
    public Brush LabelBackground => new SolidColorBrush(Color.FromArgb(180, 60, 60, 60));
}
