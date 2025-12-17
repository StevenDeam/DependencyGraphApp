using System.Windows;
using System.Windows.Controls;
using DependencyDashboard.App.ViewModels;
using DependencyDashboard.Core.Models;

namespace DependencyDashboard.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void ExportPng_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsLoaded)
        {
            _viewModel.ExportToPng(GraphCanvas);
        }
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsLoaded)
        {
            _viewModel.ExportToPdf(GraphCanvas);
        }
    }

    private void JumpToPrereq_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedItem?.Model.Prerequisite != null)
        {
            _viewModel.SelectItemById(_viewModel.SelectedItem.Model.Prerequisite.Id);
        }
    }

    private void JumpToDependent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string id)
        {
            _viewModel.SelectItemById(id);
        }
    }
}
