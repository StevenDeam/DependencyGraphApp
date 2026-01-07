using DependencyDashboard.Core;
using DependencyDashboard.Core.Graph;
using DependencyDashboard.Core.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using ValidationError = DependencyDashboard.Core.Validation.ValidationError;

namespace DependencyDashboard.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly WorkItemService _service;
    private WorkItemCollection? _collection;
    private string _statusMessage = "Ready. Open a CSV file to begin.";
    private bool _hasErrors;
    private bool _isLoaded;
    private string? _loadedFilePath;
    private ObservableCollection<PhaseColumnViewModel> _phaseColumns = new();
    private Dictionary<string, bool> _collapseStates = new();

    public MainViewModel()
    {
        _service = new WorkItemService();

        WorkItems = new ObservableCollection<WorkItemViewModel>();
        ValidationErrors = new ObservableCollection<ValidationError>();
        MilestoneEdges = new ObservableCollection<MilestoneEdgeViewModel>();

        OpenCommand = new RelayCommand(OpenFile);
        ReloadCommand = new RelayCommand(ReloadFile, () => _loadedFilePath != null);
    }

    public ObservableCollection<WorkItemViewModel> WorkItems { get; }
    public ObservableCollection<ValidationError> ValidationErrors { get; }
    public ObservableCollection<MilestoneEdgeViewModel> MilestoneEdges { get; }
    public ObservableCollection<PhaseColumnViewModel> PhaseColumns => _phaseColumns;

    public ICommand OpenCommand { get; }
    public ICommand ReloadCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool HasErrors
    {
        get => _hasErrors;
        set => SetProperty(ref _hasErrors, value);
    }

    public bool IsLoaded
    {
        get => _isLoaded;
        set
        {
            if (SetProperty(ref _isLoaded, value))
            {
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    public bool ShowEmptyState => !IsLoaded && !HasErrors;

    public string WindowTitle => _loadedFilePath != null
        ? $"Dependency Dashboard - {Path.GetFileName(_loadedFilePath)}"
        : "Dependency Dashboard";

    private void OpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Title = "Open Work Items CSV"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadFile(dialog.FileName);
        }
    }

    private void ReloadFile()
    {
        if (_loadedFilePath != null)
        {
            LoadFile(_loadedFilePath);
        }
    }

    private void LoadFile(string filePath)
    {
        try
        {
            _loadedFilePath = filePath;
            OnPropertyChanged(nameof(WindowTitle));

            StatusMessage = $"Loading {Path.GetFileName(filePath)}...";

            _collection = _service.LoadAndProcess(filePath);

            ValidationErrors.Clear();
            foreach (var error in _collection.ValidationErrors)
            {
                ValidationErrors.Add(error);
            }

            HasErrors = _collection.HasErrors;

            if (HasErrors)
            {
                StatusMessage = $"Loaded with {ValidationErrors.Count} error(s). Fix the CSV and reload.";
                IsLoaded = false;
                WorkItems.Clear();
                _phaseColumns.Clear();
                return;
            }

            // Build view models
            WorkItems.Clear();
            foreach (var item in _collection.Items)
            {
                var vm = new WorkItemViewModel(item);
                WorkItems.Add(vm);
            }

            // Compute Phase Matrix layout with current collapse states
            _service.RecomputeLayout(_collection, _collection.Items, GraphLayoutMode.PhaseMatrix, _collapseStates);

            // Refresh positions after layout
            foreach (var vm in WorkItems)
            {
                vm.Refresh();
            }

            // Build phase column view models with collapse callback
            _phaseColumns.Clear();
            foreach (var phase in _service.GetPhaseColumns())
            {
                _phaseColumns.Add(new PhaseColumnViewModel(phase, OnGroupCollapseToggled));
            }

            // Build milestone edge view models for bracket rendering
            MilestoneEdges.Clear();
            foreach (var edge in _service.GetMilestoneEdges())
            {
                MilestoneEdges.Add(new MilestoneEdgeViewModel(edge));
            }

            IsLoaded = true;
            StatusMessage = $"Loaded {_collection.Items.Count} items ({_collection.Milestones.Count()} milestones, {_collection.Tasks.Count()} tasks)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
            HasErrors = true;
            IsLoaded = false;
        }
    }

    private void OnGroupCollapseToggled(AssemblyGroupViewModel group)
    {
        if (_collection == null) return;

        // Update collapse states dictionary
        _collapseStates[group.Id] = group.IsCollapsed;

        // Recompute layout with updated collapse states
        _service.RecomputeLayout(_collection, _collection.Items, GraphLayoutMode.PhaseMatrix, _collapseStates);

        // Refresh work item positions
        foreach (var vm in WorkItems)
        {
            vm.Refresh();
        }

        // Rebuild phase column view models (positions have changed)
        _phaseColumns.Clear();
        foreach (var phase in _service.GetPhaseColumns())
        {
            _phaseColumns.Add(new PhaseColumnViewModel(phase, OnGroupCollapseToggled));
        }

        // Rebuild milestone edge view models (positions have changed)
        MilestoneEdges.Clear();
        foreach (var edge in _service.GetMilestoneEdges())
        {
            MilestoneEdges.Add(new MilestoneEdgeViewModel(edge));
        }
    }
}
