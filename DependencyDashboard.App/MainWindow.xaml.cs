using System.Windows;
using DependencyDashboard.App.ViewModels;

namespace DependencyDashboard.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // Subscribe to size change events
        GraphScrollViewer.SizeChanged += OnScrollViewerSizeChanged;
        GraphCanvas.SizeChanged += OnGraphCanvasSizeChanged;
    }

    private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Update viewport size in ViewModel
        _viewModel.UpdateViewportSize(
            GraphScrollViewer.ViewportWidth,
            GraphScrollViewer.ViewportHeight);
    }

    private void OnGraphCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Update content size in ViewModel
        _viewModel.UpdateContentSize(
            GraphCanvas.ContentWidth,
            GraphCanvas.ContentHeight);
    }
}
