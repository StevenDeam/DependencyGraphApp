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
    private bool _isFitToScreen;
    private double _zoomScale = 1.0;
    private double _viewportWidth;
    private double _viewportHeight;
    private double _contentWidth;
    private double _contentHeight;

    public MainViewModel()
    {
        _service = new WorkItemService();

        WorkItems = new ObservableCollection<WorkItemViewModel>();
        ValidationErrors = new ObservableCollection<ValidationError>();
        MilestoneEdges = new ObservableCollection<MilestoneEdgeViewModel>();

        OpenCommand = new RelayCommand(OpenFile);
        ReloadCommand = new RelayCommand(ReloadFile, () => _loadedFilePath != null);
        ToggleFitToScreenCommand = new RelayCommand(ToggleFitToScreen);
    }

    public ObservableCollection<WorkItemViewModel> WorkItems { get; }
    public ObservableCollection<ValidationError> ValidationErrors { get; }
    public ObservableCollection<MilestoneEdgeViewModel> MilestoneEdges { get; }
    public ObservableCollection<PhaseColumnViewModel> PhaseColumns => _phaseColumns;

    public ICommand OpenCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand ToggleFitToScreenCommand { get; }

    public bool IsFitToScreen
    {
        get => _isFitToScreen;
        set
        {
            if (SetProperty(ref _isFitToScreen, value))
            {
                RecalculateZoomScale();
                OnPropertyChanged(nameof(FitToScreenButtonText));
            }
        }
    }

    public double ZoomScale
    {
        get => _zoomScale;
        private set => SetProperty(ref _zoomScale, value);
    }

    public string FitToScreenButtonText => IsFitToScreen ? "100%" : "Fit";

    public string ZoomPercentText => $"{ZoomScale * 100:F0}%";

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

        // Recalculate zoom if fit-to-screen is active
        if (IsFitToScreen)
        {
            RecalculateZoomScale();
        }
    }

    private void ToggleFitToScreen()
    {
        IsFitToScreen = !IsFitToScreen;
    }

    /// <summary>
    /// Called by the view when viewport size changes or content dimensions change.
    /// </summary>
    public void UpdateViewportSize(double viewportWidth, double viewportHeight)
    {
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;

        if (IsFitToScreen)
        {
            RecalculateZoomScale();
        }
    }

    /// <summary>
    /// Called by the view when content dimensions change.
    /// </summary>
    public void UpdateContentSize(double contentWidth, double contentHeight)
    {
        _contentWidth = contentWidth;
        _contentHeight = contentHeight;

        if (IsFitToScreen)
        {
            RecalculateZoomScale();
        }
    }

    private void RecalculateZoomScale()
    {
        if (!IsFitToScreen)
        {
            ZoomScale = 1.0;
            OnPropertyChanged(nameof(ZoomPercentText));
            return;
        }

        if (_contentWidth <= 0 || _contentHeight <= 0 ||
            _viewportWidth <= 0 || _viewportHeight <= 0)
        {
            ZoomScale = 1.0;
            OnPropertyChanged(nameof(ZoomPercentText));
            return;
        }

        double scaleX = _viewportWidth / _contentWidth;
        double scaleY = _viewportHeight / _contentHeight;

        // Use the smaller scale to fit entire content, with 5% margin
        double scale = Math.Min(scaleX, scaleY) * 0.95;

        // Don't zoom in beyond 100%
        ZoomScale = Math.Min(scale, 1.0);
        OnPropertyChanged(nameof(ZoomPercentText));
    }
}
