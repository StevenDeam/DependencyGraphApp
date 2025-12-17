namespace DependencyDashboard.App.ViewModels;

public class FilterItemViewModel : ViewModelBase
{
    private bool _isChecked = true;
    private string _name = string.Empty;
    private int _count;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    public string DisplayText => $"{Name} ({Count})";
}
