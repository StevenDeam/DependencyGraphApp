using DependencyDashboard.Core.Models;
using System.Windows.Media;

namespace DependencyDashboard.App.ViewModels;

public class WorkItemViewModel : ViewModelBase
{
    private readonly WorkItem _model;
    private bool _isSelected;
    private bool _isHighlighted;
    private bool _isFiltered = true;

    public WorkItemViewModel(WorkItem model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public WorkItem Model => _model;

    public string Id => _model.Id;
    public string Title => _model.Title;
    public WorkItemType Type => _model.Type;
    public string Discipline => _model.Discipline;
    public int PercentCompleteRaw => _model.PercentCompleteRaw;
    public int Weight => _model.Weight;
    public string? ParentId => _model.ParentId;
    public string? PrereqId => _model.PrereqId;
    public DateTime? TargetDate => _model.TargetDate;
    public bool IsNA => _model.IsNA;

    public double ComputedPercent => _model.ComputedPercent;
    public WorkItemStatus ComputedStatus => _model.ComputedStatus;
    public HealthStatus Health => _model.Health;
    public string? BlockedById => _model.BlockedById;

    public double X => _model.X;
    public double Y => _model.Y;
    public int ComputedLevel => _model.ComputedLevel;

    public bool IsMilestone => _model.IsMilestone;
    public bool IsTask => _model.IsTask;

    public string DisplayPercent => IsMilestone
        ? $"{ComputedPercent:F0}%"
        : $"{PercentCompleteRaw}%";

    public string DisplayTargetDate => TargetDate.HasValue
        ? TargetDate.Value.ToString("yyyy-MM-dd")
        : "";

    public string StatusText => ComputedStatus switch
    {
        WorkItemStatus.NotStarted => "Not Started",
        WorkItemStatus.InProgress => "In Progress",
        WorkItemStatus.Blocked => "Blocked",
        WorkItemStatus.Done => "Done",
        WorkItemStatus.NotApplicable => "N/A",
        _ => ComputedStatus.ToString()
    };

    public string HealthText => Health switch
    {
        HealthStatus.Green => "On Track",
        HealthStatus.Yellow => "At Risk",
        HealthStatus.Red => "Critical",
        HealthStatus.NoDate => "No Date",
        _ => Health.ToString()
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

    public Brush HealthBrush => Health switch
    {
        HealthStatus.Green => Brushes.Green,
        HealthStatus.Yellow => Brushes.Orange,
        HealthStatus.Red => Brushes.Red,
        HealthStatus.NoDate => Brushes.Gray,
        _ => Brushes.Gray
    };

    public Brush NodeBackground => IsMilestone
        ? new SolidColorBrush(Color.FromRgb(240, 248, 255)) // AliceBlue
        : new SolidColorBrush(Color.FromRgb(255, 255, 240)); // Ivory

    public Brush NodeBorder => IsSelected
        ? Brushes.DodgerBlue
        : (IsHighlighted ? Brushes.Orange : Brushes.DarkGray);

    public double NodeBorderThickness => IsSelected ? 3 : (IsHighlighted ? 2 : 1);

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(NodeBorder));
                OnPropertyChanged(nameof(NodeBorderThickness));
            }
        }
    }

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            if (SetProperty(ref _isHighlighted, value))
            {
                OnPropertyChanged(nameof(NodeBorder));
                OnPropertyChanged(nameof(NodeBorderThickness));
            }
        }
    }

    public bool IsFiltered
    {
        get => _isFiltered;
        set => SetProperty(ref _isFiltered, value);
    }

    public string TooltipText =>
        $"Id: {Id}\n" +
        $"Title: {Title}\n" +
        $"Type: {Type}\n" +
        $"Discipline: {Discipline}\n" +
        $"Status: {StatusText}\n" +
        $"Progress: {DisplayPercent}" +
        (IsMilestone && TargetDate.HasValue ? $"\nTarget: {DisplayTargetDate}\nHealth: {HealthText}" : "") +
        (!string.IsNullOrEmpty(BlockedById) ? $"\nBlocked by: {BlockedById}" : "");

    public void Refresh()
    {
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(ComputedPercent));
        OnPropertyChanged(nameof(ComputedStatus));
        OnPropertyChanged(nameof(Health));
        OnPropertyChanged(nameof(DisplayPercent));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(HealthText));
        OnPropertyChanged(nameof(StatusBrush));
        OnPropertyChanged(nameof(HealthBrush));
        OnPropertyChanged(nameof(BlockedById));
        OnPropertyChanged(nameof(TooltipText));
    }
}
