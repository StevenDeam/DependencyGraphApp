using DependencyDashboard.Core.Graph;
using DependencyDashboard.Core.Models;
using System.Windows;
using System.Windows.Media;

namespace DependencyDashboard.App.ViewModels;

public class DependencyEdgeViewModel : ViewModelBase
{
    private readonly DependencyEdge _model;
    private bool _isHighlighted;
    private bool _isFiltered = true;

    public DependencyEdgeViewModel(DependencyEdge model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public DependencyEdge Model => _model;
    public WorkItem? Prerequisite => _model.Prerequisite;
    public WorkItem? Dependent => _model.Dependent;

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            if (SetProperty(ref _isHighlighted, value))
            {
                OnPropertyChanged(nameof(StrokeBrush));
                OnPropertyChanged(nameof(StrokeThickness));
            }
        }
    }

    public bool IsFiltered
    {
        get => _isFiltered;
        set => SetProperty(ref _isFiltered, value);
    }

    public Brush StrokeBrush => IsHighlighted ? Brushes.Orange : Brushes.DarkGray;
    public double StrokeThickness => IsHighlighted ? 2 : 1;

    // Edge geometry for drawing
    public Point StartPoint
    {
        get
        {
            if (Prerequisite == null) return new Point(0, 0);
            return new Point(
                Prerequisite.X + GraphLayoutEngine.NodeWidth,
                Prerequisite.Y + GraphLayoutEngine.NodeHeight / 2);
        }
    }

    public Point EndPoint
    {
        get
        {
            if (Dependent == null) return new Point(0, 0);
            return new Point(
                Dependent.X,
                Dependent.Y + GraphLayoutEngine.NodeHeight / 2);
        }
    }

    // Control points for Bezier curve
    public Point ControlPoint1
    {
        get
        {
            var start = StartPoint;
            var end = EndPoint;
            double offset = (end.X - start.X) / 3;
            return new Point(start.X + offset, start.Y);
        }
    }

    public Point ControlPoint2
    {
        get
        {
            var start = StartPoint;
            var end = EndPoint;
            double offset = (end.X - start.X) / 3;
            return new Point(end.X - offset, end.Y);
        }
    }

    // Arrow head points
    public PointCollection ArrowHead
    {
        get
        {
            var end = EndPoint;
            const double arrowSize = 8;

            // Calculate direction from control point to end
            var cp2 = ControlPoint2;
            var dx = end.X - cp2.X;
            var dy = end.Y - cp2.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.001) length = 1;
            dx /= length;
            dy /= length;

            // Perpendicular direction
            var px = -dy;
            var py = dx;

            var p1 = new Point(end.X - arrowSize * dx + arrowSize / 2 * px, end.Y - arrowSize * dy + arrowSize / 2 * py);
            var p2 = new Point(end.X - arrowSize * dx - arrowSize / 2 * px, end.Y - arrowSize * dy - arrowSize / 2 * py);

            return new PointCollection { p1, end, p2 };
        }
    }

    public PathGeometry EdgeGeometry
    {
        get
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = StartPoint };

            var bezier = new BezierSegment(ControlPoint1, ControlPoint2, EndPoint, true);
            figure.Segments.Add(bezier);

            geometry.Figures.Add(figure);
            return geometry;
        }
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(StartPoint));
        OnPropertyChanged(nameof(EndPoint));
        OnPropertyChanged(nameof(ControlPoint1));
        OnPropertyChanged(nameof(ControlPoint2));
        OnPropertyChanged(nameof(ArrowHead));
        OnPropertyChanged(nameof(EdgeGeometry));
    }
}
