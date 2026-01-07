using DependencyDashboard.Core.Models;
using System.Windows.Media;

namespace DependencyDashboard.App.ViewModels;

/// <summary>
/// ViewModel for milestone-to-milestone edges in the bracket visualization.
/// </summary>
public class MilestoneEdgeViewModel : ViewModelBase
{
    private readonly MilestoneEdge _edge;

    public MilestoneEdgeViewModel(MilestoneEdge edge)
    {
        _edge = edge;
    }

    public MilestoneEdge Edge => _edge;

    public double FromX => _edge.FromX;
    public double FromY => _edge.FromY;
    public double ToX => _edge.ToX;
    public double ToY => _edge.ToY;

    public string FromId => _edge.FromMilestone.Id;
    public string ToId => _edge.ToMilestone.Id;

    public Brush LineBrush => new SolidColorBrush(Color.FromRgb(70, 130, 180)); // SteelBlue
    public double LineThickness => 2;
}
