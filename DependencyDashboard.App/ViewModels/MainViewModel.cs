using DependencyDashboard.Core;
using DependencyDashboard.Core.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Printing;
using System.Windows.Documents;
using ValidationError = DependencyDashboard.Core.Validation.ValidationError;

namespace DependencyDashboard.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly WorkItemService _service;
    private WorkItemCollection? _collection;
    private string _searchText = string.Empty;
    private bool _showMilestonesOnly;
    private bool _showBlockedPathsOnly;
    private WorkItemViewModel? _selectedItem;
    private string _statusMessage = "Ready. Open a CSV file to begin.";
    private bool _hasErrors;
    private bool _isLoaded;
    private int _selectedTabIndex;
    private double _zoomLevel = 1.0;
    private double _panX;
    private double _panY;
    private string? _loadedFilePath;

    public MainViewModel()
    {
        _service = new WorkItemService();

        WorkItems = new ObservableCollection<WorkItemViewModel>();
        Edges = new ObservableCollection<DependencyEdgeViewModel>();
        FilteredWorkItems = new ObservableCollection<WorkItemViewModel>();
        FilteredEdges = new ObservableCollection<DependencyEdgeViewModel>();
        ValidationErrors = new ObservableCollection<ValidationError>();
        DisciplineFilters = new ObservableCollection<FilterItemViewModel>();
        StatusFilters = new ObservableCollection<FilterItemViewModel>();
        HealthFilters = new ObservableCollection<FilterItemViewModel>();
        MilestoneItems = new ObservableCollection<MilestoneRowViewModel>();

        OpenCommand = new RelayCommand(OpenFile);
        ReloadCommand = new RelayCommand(ReloadFile, () => _loadedFilePath != null);
        ExportPngCommand = new RelayCommand(ExportPng, () => IsLoaded);
        ExportPdfCommand = new RelayCommand(ExportPdf, () => IsLoaded);
        ClearFiltersCommand = new RelayCommand(ClearFilters);
        ZoomInCommand = new RelayCommand(() => ZoomLevel = Math.Min(ZoomLevel * 1.2, 5.0));
        ZoomOutCommand = new RelayCommand(() => ZoomLevel = Math.Max(ZoomLevel / 1.2, 0.2));
        ResetViewCommand = new RelayCommand(ResetView);

        InitializeFilters();
    }

    public ObservableCollection<WorkItemViewModel> WorkItems { get; }
    public ObservableCollection<DependencyEdgeViewModel> Edges { get; }
    public ObservableCollection<WorkItemViewModel> FilteredWorkItems { get; }
    public ObservableCollection<DependencyEdgeViewModel> FilteredEdges { get; }
    public ObservableCollection<ValidationError> ValidationErrors { get; }
    public ObservableCollection<FilterItemViewModel> DisciplineFilters { get; }
    public ObservableCollection<FilterItemViewModel> StatusFilters { get; }
    public ObservableCollection<FilterItemViewModel> HealthFilters { get; }
    public ObservableCollection<MilestoneRowViewModel> MilestoneItems { get; }

    public ICommand OpenCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand ExportPngCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand ResetViewCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool ShowMilestonesOnly
    {
        get => _showMilestonesOnly;
        set
        {
            if (SetProperty(ref _showMilestonesOnly, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool ShowBlockedPathsOnly
    {
        get => _showBlockedPathsOnly;
        set
        {
            if (SetProperty(ref _showBlockedPathsOnly, value))
            {
                ApplyFilters();
            }
        }
    }

    public WorkItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != null)
            {
                _selectedItem.IsSelected = false;
            }
            if (SetProperty(ref _selectedItem, value))
            {
                if (_selectedItem != null)
                {
                    _selectedItem.IsSelected = true;
                }
                OnPropertyChanged(nameof(HasSelection));
                UpdateHighlighting();
            }
        }
    }

    public bool HasSelection => SelectedItem != null;

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
        set => SetProperty(ref _isLoaded, value);
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public double ZoomLevel
    {
        get => _zoomLevel;
        set => SetProperty(ref _zoomLevel, value);
    }

    public double PanX
    {
        get => _panX;
        set => SetProperty(ref _panX, value);
    }

    public double PanY
    {
        get => _panY;
        set => SetProperty(ref _panY, value);
    }

    public string WindowTitle => _loadedFilePath != null
        ? $"Dependency Dashboard - {Path.GetFileName(_loadedFilePath)}"
        : "Dependency Dashboard";

    private void InitializeFilters()
    {
        // Initialize status filters
        StatusFilters.Add(new FilterItemViewModel { Name = "Not Started" });
        StatusFilters.Add(new FilterItemViewModel { Name = "In Progress" });
        StatusFilters.Add(new FilterItemViewModel { Name = "Blocked" });
        StatusFilters.Add(new FilterItemViewModel { Name = "Done" });
        StatusFilters.Add(new FilterItemViewModel { Name = "N/A" });

        foreach (var filter in StatusFilters)
        {
            filter.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FilterItemViewModel.IsChecked))
                {
                    ApplyFilters();
                }
            };
        }

        // Initialize health filters
        HealthFilters.Add(new FilterItemViewModel { Name = "On Track" });
        HealthFilters.Add(new FilterItemViewModel { Name = "At Risk" });
        HealthFilters.Add(new FilterItemViewModel { Name = "Critical" });
        HealthFilters.Add(new FilterItemViewModel { Name = "No Date" });

        foreach (var filter in HealthFilters)
        {
            filter.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FilterItemViewModel.IsChecked))
                {
                    ApplyFilters();
                }
            };
        }
    }

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
                Edges.Clear();
                FilteredWorkItems.Clear();
                FilteredEdges.Clear();
                MilestoneItems.Clear();
                return;
            }

            // Build view models
            WorkItems.Clear();
            var vmMap = new Dictionary<string, WorkItemViewModel>();
            foreach (var item in _collection.Items)
            {
                var vm = new WorkItemViewModel(item);
                WorkItems.Add(vm);
                vmMap[item.Id] = vm;
            }

            Edges.Clear();
            foreach (var edge in _collection.DependencyEdges)
            {
                var edgeVm = new DependencyEdgeViewModel(edge);
                Edges.Add(edgeVm);
            }

            // Update discipline filters
            DisciplineFilters.Clear();
            foreach (var discipline in _collection.Disciplines)
            {
                var count = _collection.Items.Count(i => i.Discipline == discipline);
                var filter = new FilterItemViewModel { Name = discipline, Count = count };
                filter.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(FilterItemViewModel.IsChecked))
                    {
                        ApplyFilters();
                    }
                };
                DisciplineFilters.Add(filter);
            }

            // Update status filter counts
            foreach (var filter in StatusFilters)
            {
                filter.Count = filter.Name switch
                {
                    "Not Started" => _collection.Items.Count(i => i.ComputedStatus == WorkItemStatus.NotStarted),
                    "In Progress" => _collection.Items.Count(i => i.ComputedStatus == WorkItemStatus.InProgress),
                    "Blocked" => _collection.Items.Count(i => i.ComputedStatus == WorkItemStatus.Blocked),
                    "Done" => _collection.Items.Count(i => i.ComputedStatus == WorkItemStatus.Done),
                    "N/A" => _collection.Items.Count(i => i.ComputedStatus == WorkItemStatus.NotApplicable),
                    _ => 0
                };
            }

            // Update health filter counts
            foreach (var filter in HealthFilters)
            {
                filter.Count = filter.Name switch
                {
                    "On Track" => _collection.Milestones.Count(i => i.Health == HealthStatus.Green),
                    "At Risk" => _collection.Milestones.Count(i => i.Health == HealthStatus.Yellow),
                    "Critical" => _collection.Milestones.Count(i => i.Health == HealthStatus.Red),
                    "No Date" => _collection.Milestones.Count(i => i.Health == HealthStatus.NoDate),
                    _ => 0
                };
            }

            // Build milestone dashboard rows
            MilestoneItems.Clear();
            foreach (var milestone in _collection.Milestones)
            {
                var blockedCount = _service.GetBlockedDescendants(milestone).Count();
                var topBlocker = _service.FindTopBlocker(milestone, _collection);
                MilestoneItems.Add(new MilestoneRowViewModel(milestone, blockedCount, topBlocker));
            }

            ApplyFilters();
            ResetView();

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

    private void ApplyFilters()
    {
        if (_collection == null) return;

        var selectedDisciplines = DisciplineFilters
            .Where(f => f.IsChecked)
            .Select(f => f.Name)
            .ToHashSet();

        var selectedStatuses = new HashSet<WorkItemStatus>();
        foreach (var filter in StatusFilters.Where(f => f.IsChecked))
        {
            var status = filter.Name switch
            {
                "Not Started" => WorkItemStatus.NotStarted,
                "In Progress" => WorkItemStatus.InProgress,
                "Blocked" => WorkItemStatus.Blocked,
                "Done" => WorkItemStatus.Done,
                "N/A" => WorkItemStatus.NotApplicable,
                _ => WorkItemStatus.NotStarted
            };
            selectedStatuses.Add(status);
        }

        var selectedHealths = new HashSet<HealthStatus>();
        foreach (var filter in HealthFilters.Where(f => f.IsChecked))
        {
            var health = filter.Name switch
            {
                "On Track" => HealthStatus.Green,
                "At Risk" => HealthStatus.Yellow,
                "Critical" => HealthStatus.Red,
                "No Date" => HealthStatus.NoDate,
                _ => HealthStatus.NoDate
            };
            selectedHealths.Add(health);
        }

        var searchLower = SearchText?.ToLowerInvariant() ?? "";

        FilteredWorkItems.Clear();
        FilteredEdges.Clear();

        var filteredIds = new HashSet<string>();

        foreach (var vm in WorkItems)
        {
            bool passesFilter = true;

            // Search filter
            if (!string.IsNullOrEmpty(searchLower))
            {
                passesFilter = vm.Id.ToLowerInvariant().Contains(searchLower) ||
                              vm.Title.ToLowerInvariant().Contains(searchLower);
            }

            // Discipline filter
            if (passesFilter && selectedDisciplines.Count > 0)
            {
                passesFilter = selectedDisciplines.Contains(vm.Discipline);
            }

            // Status filter
            if (passesFilter)
            {
                passesFilter = selectedStatuses.Contains(vm.ComputedStatus);
            }

            // Health filter (milestones only)
            if (passesFilter && vm.IsMilestone)
            {
                passesFilter = selectedHealths.Contains(vm.Health);
            }

            // Milestones only toggle
            if (passesFilter && ShowMilestonesOnly)
            {
                passesFilter = vm.IsMilestone;
            }

            // Blocked paths only toggle
            if (passesFilter && ShowBlockedPathsOnly)
            {
                passesFilter = vm.ComputedStatus == WorkItemStatus.Blocked ||
                              !string.IsNullOrEmpty(vm.BlockedById);
            }

            vm.IsFiltered = passesFilter;
            if (passesFilter)
            {
                FilteredWorkItems.Add(vm);
                filteredIds.Add(vm.Id);
            }
        }

        // Filter edges
        foreach (var edge in Edges)
        {
            bool passesFilter = edge.Prerequisite != null &&
                               edge.Dependent != null &&
                               filteredIds.Contains(edge.Prerequisite.Id) &&
                               filteredIds.Contains(edge.Dependent.Id);

            edge.IsFiltered = passesFilter;
            if (passesFilter)
            {
                FilteredEdges.Add(edge);
            }
        }

        // Recompute layout for filtered items
        if (FilteredWorkItems.Count > 0)
        {
            var filteredModels = FilteredWorkItems.Select(vm => vm.Model);
            _service.RecomputeLayout(_collection, filteredModels);

            foreach (var vm in FilteredWorkItems)
            {
                vm.Refresh();
            }
            foreach (var edge in FilteredEdges)
            {
                edge.Refresh();
            }
        }
    }

    private void ClearFilters()
    {
        SearchText = "";
        ShowMilestonesOnly = false;
        ShowBlockedPathsOnly = false;

        foreach (var filter in DisciplineFilters)
        {
            filter.IsChecked = true;
        }
        foreach (var filter in StatusFilters)
        {
            filter.IsChecked = true;
        }
        foreach (var filter in HealthFilters)
        {
            filter.IsChecked = true;
        }
    }

    private void ResetView()
    {
        ZoomLevel = 1.0;
        PanX = 0;
        PanY = 0;
    }

    private void UpdateHighlighting()
    {
        // Clear all highlighting
        foreach (var vm in WorkItems)
        {
            vm.IsHighlighted = false;
        }
        foreach (var edge in Edges)
        {
            edge.IsHighlighted = false;
        }

        if (SelectedItem == null) return;

        // Highlight prerequisite chain
        var current = SelectedItem.Model.Prerequisite;
        while (current != null)
        {
            var vm = WorkItems.FirstOrDefault(w => w.Id == current.Id);
            if (vm != null) vm.IsHighlighted = true;
            current = current.Prerequisite;
        }

        // Highlight dependent chain
        HighlightDependents(SelectedItem.Model);

        // Highlight related edges
        foreach (var edge in Edges)
        {
            if (edge.Prerequisite?.Id == SelectedItem.Id ||
                edge.Dependent?.Id == SelectedItem.Id)
            {
                edge.IsHighlighted = true;
            }
        }
    }

    private void HighlightDependents(WorkItem item)
    {
        foreach (var dependent in item.Dependents)
        {
            var vm = WorkItems.FirstOrDefault(w => w.Id == dependent.Id);
            if (vm != null)
            {
                vm.IsHighlighted = true;
            }
            HighlightDependents(dependent);
        }
    }

    public void SelectItemById(string id)
    {
        var vm = WorkItems.FirstOrDefault(w => w.Id == id);
        if (vm != null)
        {
            SelectedItem = vm;
        }
    }

    public void ExportGraphVisual(FrameworkElement graphCanvas, string format)
    {
        if (format == "png")
        {
            ExportToPng(graphCanvas);
        }
        else if (format == "pdf")
        {
            ExportToPdf(graphCanvas);
        }
    }

    private void ExportPng()
    {
        // This will be called from the view with the canvas
        StatusMessage = "Use File > Export PNG from the menu to export";
    }

    private void ExportPdf()
    {
        // This will be called from the view with the canvas
        StatusMessage = "Use File > Export PDF from the menu to export";
    }

    public void ExportToPng(FrameworkElement canvas)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG Image (*.png)|*.png",
            DefaultExt = ".png",
            FileName = $"DependencyGraph_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var bounds = VisualTreeHelper.GetDescendantBounds(canvas);
                if (bounds.IsEmpty)
                {
                    bounds = new Rect(0, 0, canvas.ActualWidth, canvas.ActualHeight);
                }

                var dpi = 96d;
                var renderTarget = new RenderTargetBitmap(
                    (int)(bounds.Width * dpi / 96),
                    (int)(bounds.Height * dpi / 96),
                    dpi, dpi, PixelFormats.Pbgra32);

                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    dc.DrawRectangle(new VisualBrush(canvas), null, new Rect(new Point(), bounds.Size));
                }
                renderTarget.Render(visual);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using var stream = File.Create(dialog.FileName);
                encoder.Save(stream);

                StatusMessage = $"Exported to {dialog.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }
    }

    public void ExportToPdf(FrameworkElement canvas)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF Document (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            FileName = $"DependencyGraph_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                // Use XPS printing to PDF
                var printDialog = new PrintDialog();
                printDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;

                var bounds = VisualTreeHelper.GetDescendantBounds(canvas);
                if (bounds.IsEmpty)
                {
                    bounds = new Rect(0, 0, canvas.ActualWidth, canvas.ActualHeight);
                }

                // Create a document for printing
                var doc = new FixedDocument();
                var page = new FixedPage
                {
                    Width = printDialog.PrintableAreaWidth,
                    Height = printDialog.PrintableAreaHeight
                };

                // Scale to fit
                double scaleX = printDialog.PrintableAreaWidth / bounds.Width;
                double scaleY = printDialog.PrintableAreaHeight / bounds.Height;
                double scale = Math.Min(scaleX, scaleY) * 0.9; // 90% to leave margin

                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    dc.PushTransform(new ScaleTransform(scale, scale));
                    dc.DrawRectangle(new VisualBrush(canvas), null, new Rect(new Point(), bounds.Size));
                    dc.Pop();
                }

                var image = new System.Windows.Controls.Image();
                var renderTarget = new RenderTargetBitmap(
                    (int)(bounds.Width * scale),
                    (int)(bounds.Height * scale),
                    96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(visual);
                image.Source = renderTarget;
                image.Width = bounds.Width * scale;
                image.Height = bounds.Height * scale;

                page.Children.Add(image);
                var pageContent = new PageContent();
                ((System.Windows.Markup.IAddChild)pageContent).AddChild(page);
                doc.Pages.Add(pageContent);

                // For actual PDF, we'd need a PDF library. For now, use XPS
                var xpsPath = Path.ChangeExtension(dialog.FileName, ".xps");
                using (var xpsDoc = new System.Windows.Xps.Packaging.XpsDocument(xpsPath, FileAccess.ReadWrite))
                {
                    var writer = System.Windows.Xps.Packaging.XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                    writer.Write(doc);
                }

                StatusMessage = $"Exported to {xpsPath} (XPS format - use virtual PDF printer for PDF)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }
    }
}

public class MilestoneRowViewModel : ViewModelBase
{
    private readonly WorkItem _milestone;

    public MilestoneRowViewModel(WorkItem milestone, int blockedCount, WorkItem? topBlocker)
    {
        _milestone = milestone;
        BlockedCount = blockedCount;
        TopBlocker = topBlocker;
    }

    public string Id => _milestone.Id;
    public string Title => _milestone.Title;
    public string DisplayName => $"{Title} ({Id})";
    public DateTime? TargetDate => _milestone.TargetDate;
    public string DisplayTargetDate => TargetDate.HasValue ? TargetDate.Value.ToString("yyyy-MM-dd") : "";
    public double ComputedPercent => _milestone.ComputedPercent;
    public string DisplayPercent => $"{ComputedPercent:F0}%";
    public WorkItemStatus ComputedStatus => _milestone.ComputedStatus;
    public HealthStatus Health => _milestone.Health;
    public int BlockedCount { get; }
    public WorkItem? TopBlocker { get; }
    public string TopBlockerDisplay => TopBlocker != null ? $"{TopBlocker.Title} ({TopBlocker.Id})" : "";

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

    public System.Windows.Media.Brush StatusBrush => ComputedStatus switch
    {
        WorkItemStatus.NotStarted => System.Windows.Media.Brushes.Gray,
        WorkItemStatus.InProgress => System.Windows.Media.Brushes.DodgerBlue,
        WorkItemStatus.Blocked => System.Windows.Media.Brushes.OrangeRed,
        WorkItemStatus.Done => System.Windows.Media.Brushes.Green,
        WorkItemStatus.NotApplicable => System.Windows.Media.Brushes.LightGray,
        _ => System.Windows.Media.Brushes.Gray
    };

    public System.Windows.Media.Brush HealthBrush => Health switch
    {
        HealthStatus.Green => System.Windows.Media.Brushes.Green,
        HealthStatus.Yellow => System.Windows.Media.Brushes.Orange,
        HealthStatus.Red => System.Windows.Media.Brushes.Red,
        HealthStatus.NoDate => System.Windows.Media.Brushes.Gray,
        _ => System.Windows.Media.Brushes.Gray
    };
}
