using System.Windows;
using DependencyDashboard.App.ViewModels;

namespace DependencyDashboard.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
