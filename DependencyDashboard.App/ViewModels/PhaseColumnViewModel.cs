using DependencyDashboard.Core.Graph;
using System.Windows.Media;

namespace DependencyDashboard.App.ViewModels;

public class PhaseColumnViewModel : ViewModelBase
{
    private readonly PhaseColumn _phase;

    public PhaseColumnViewModel(PhaseColumn phase)
    {
        _phase = phase;
        Groups = phase.Groups.Select(g => new AssemblyGroupViewModel(g)).ToList();
    }

    public PhaseColumn Phase => _phase;
    public string PhaseName => _phase.PhaseName;
    public int PhaseIndex => _phase.PhaseIndex;

    public double X => _phase.X;
    public double Y => _phase.Y;
    public double Width => _phase.Width;
    public double Height => _phase.Height;

    public List<AssemblyGroupViewModel> Groups { get; }

    public Brush HeaderBackground => new SolidColorBrush(Color.FromArgb(220, 70, 130, 180));
    public Brush ColumnBackground => new SolidColorBrush(Color.FromArgb(15, 70, 130, 180));
    public Brush ColumnBorder => new SolidColorBrush(Color.FromArgb(60, 70, 130, 180));
}
