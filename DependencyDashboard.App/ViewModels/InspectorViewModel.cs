using DependencyDashboard.Core.Models;
using System.Collections.ObjectModel;

namespace DependencyDashboard.App.ViewModels;

public class InspectorViewModel : ViewModelBase
{
    private WorkItemViewModel? _selectedItem;
    private readonly MainViewModel _mainViewModel;

    public InspectorViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        Children = new ObservableCollection<WorkItemSummary>();
        Dependents = new ObservableCollection<WorkItemSummary>();
        DisciplineBreakdown = new ObservableCollection<DisciplineProgress>();
    }

    public WorkItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                UpdateDetails();
            }
        }
    }

    public bool HasSelection => SelectedItem != null;
    public string Id => SelectedItem?.Id ?? "";
    public string Title => SelectedItem?.Title ?? "";
    public string Type => SelectedItem?.Type.ToString() ?? "";
    public string Discipline => SelectedItem?.Discipline ?? "";
    public string Status => SelectedItem?.StatusText ?? "";
    public string Percent => SelectedItem?.DisplayPercent ?? "";
    public string TargetDate => SelectedItem?.DisplayTargetDate ?? "";
    public string Health => SelectedItem?.HealthText ?? "";
    public bool ShowHealth => SelectedItem?.IsMilestone ?? false;
    public bool ShowTargetDate => SelectedItem?.IsMilestone ?? false;

    // Prerequisite
    public bool HasPrerequisite => SelectedItem?.Model.Prerequisite != null;
    public string PrereqId => SelectedItem?.Model.Prerequisite?.Id ?? "";
    public string PrereqTitle => SelectedItem?.Model.Prerequisite?.Title ?? "";
    public string PrereqStatus => SelectedItem?.Model.Prerequisite?.ComputedStatus.ToString() ?? "";
    public string PrereqPercent => SelectedItem?.Model.Prerequisite != null
        ? $"{SelectedItem.Model.Prerequisite.ComputedPercent:F0}%"
        : "";

    public ObservableCollection<WorkItemSummary> Children { get; }
    public ObservableCollection<WorkItemSummary> Dependents { get; }
    public ObservableCollection<DisciplineProgress> DisciplineBreakdown { get; }

    public bool HasChildren => Children.Count > 0;
    public bool HasDependents => Dependents.Count > 0;
    public bool HasDisciplineBreakdown => DisciplineBreakdown.Count > 0;

    private void UpdateDetails()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Type));
        OnPropertyChanged(nameof(Discipline));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Percent));
        OnPropertyChanged(nameof(TargetDate));
        OnPropertyChanged(nameof(Health));
        OnPropertyChanged(nameof(ShowHealth));
        OnPropertyChanged(nameof(ShowTargetDate));
        OnPropertyChanged(nameof(HasPrerequisite));
        OnPropertyChanged(nameof(PrereqId));
        OnPropertyChanged(nameof(PrereqTitle));
        OnPropertyChanged(nameof(PrereqStatus));
        OnPropertyChanged(nameof(PrereqPercent));

        Children.Clear();
        Dependents.Clear();
        DisciplineBreakdown.Clear();

        if (SelectedItem == null) return;

        // Populate children (sorted by: Blocked first, then lowest %)
        var sortedChildren = SelectedItem.Model.Children
            .OrderByDescending(c => c.ComputedStatus == WorkItemStatus.Blocked)
            .ThenBy(c => c.ComputedPercent)
            .ToList();

        foreach (var child in sortedChildren)
        {
            Children.Add(new WorkItemSummary(child, NavigateToItem));
        }

        // Populate dependents
        foreach (var dep in SelectedItem.Model.Dependents)
        {
            Dependents.Add(new WorkItemSummary(dep, NavigateToItem));
        }

        // Discipline breakdown for milestones
        if (SelectedItem.IsMilestone)
        {
            var allDescendantTasks = GetAllDescendantTasks(SelectedItem.Model)
                .Where(t => !t.IsNA)
                .ToList();

            var byDiscipline = allDescendantTasks
                .GroupBy(t => t.Discipline)
                .OrderBy(g => g.Key);

            foreach (var group in byDiscipline)
            {
                var tasks = group.ToList();
                var totalWeight = tasks.Sum(t => t.Weight);
                double avgPercent;
                if (totalWeight == 0)
                {
                    avgPercent = tasks.Average(t => t.PercentCompleteRaw);
                }
                else
                {
                    avgPercent = tasks.Sum(t => t.PercentCompleteRaw * t.Weight) / (double)totalWeight;
                }

                DisciplineBreakdown.Add(new DisciplineProgress
                {
                    Discipline = group.Key,
                    AveragePercent = avgPercent,
                    TaskCount = tasks.Count
                });
            }
        }

        OnPropertyChanged(nameof(HasChildren));
        OnPropertyChanged(nameof(HasDependents));
        OnPropertyChanged(nameof(HasDisciplineBreakdown));
    }

    private IEnumerable<WorkItem> GetAllDescendantTasks(WorkItem item)
    {
        foreach (var child in item.Children)
        {
            if (child.IsTask)
            {
                yield return child;
            }
            else
            {
                foreach (var descendant in GetAllDescendantTasks(child))
                {
                    yield return descendant;
                }
            }
        }
    }

    private void NavigateToItem(string id)
    {
        _mainViewModel.SelectItemById(id);
    }

    public void NavigateToPrerequisite()
    {
        if (HasPrerequisite && SelectedItem?.Model.Prerequisite != null)
        {
            _mainViewModel.SelectItemById(SelectedItem.Model.Prerequisite.Id);
        }
    }
}

public class WorkItemSummary : ViewModelBase
{
    private readonly Action<string> _navigateAction;

    public WorkItemSummary(WorkItem item, Action<string> navigateAction)
    {
        _navigateAction = navigateAction;
        Id = item.Id;
        Title = item.Title;
        Percent = item.IsTask ? item.PercentCompleteRaw : (int)item.ComputedPercent;
        Status = item.ComputedStatus;
        IsMilestone = item.IsMilestone;

        NavigateCommand = new RelayCommand(() => _navigateAction(Id));
    }

    public string Id { get; }
    public string Title { get; }
    public int Percent { get; }
    public WorkItemStatus Status { get; }
    public bool IsMilestone { get; }

    public string DisplayText => $"{Title} ({Id}) - {Percent}%";
    public string StatusText => Status switch
    {
        WorkItemStatus.Blocked => "BLOCKED",
        WorkItemStatus.Done => "DONE",
        WorkItemStatus.InProgress => "IN PROGRESS",
        WorkItemStatus.NotStarted => "NOT STARTED",
        WorkItemStatus.NotApplicable => "N/A",
        _ => Status.ToString()
    };

    public System.Windows.Media.Brush StatusBrush => Status switch
    {
        WorkItemStatus.NotStarted => System.Windows.Media.Brushes.Gray,
        WorkItemStatus.InProgress => System.Windows.Media.Brushes.DodgerBlue,
        WorkItemStatus.Blocked => System.Windows.Media.Brushes.OrangeRed,
        WorkItemStatus.Done => System.Windows.Media.Brushes.Green,
        WorkItemStatus.NotApplicable => System.Windows.Media.Brushes.LightGray,
        _ => System.Windows.Media.Brushes.Gray
    };

    public RelayCommand NavigateCommand { get; }
}

public class DisciplineProgress
{
    public string Discipline { get; set; } = "";
    public double AveragePercent { get; set; }
    public int TaskCount { get; set; }

    public string DisplayText => $"{Discipline}: {AveragePercent:F0}% ({TaskCount} tasks)";
}
