using DependencyDashboard.App.ViewModels;
using DependencyDashboard.Core.Graph;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Linq;

namespace DependencyDashboard.App.Controls;

public class GraphCanvas : Canvas
{
    private const double ContentPadding = 20;
    private double _contentWidth;
    private double _contentHeight;

    public GraphCanvas()
    {
        Background = Brushes.Transparent;
        ClipToBounds = false;
    }

    /// <summary>
    /// Gets the calculated content width (for fit-to-screen calculations).
    /// </summary>
    public double ContentWidth => _contentWidth;

    /// <summary>
    /// Gets the calculated content height (for fit-to-screen calculations).
    /// </summary>
    public double ContentHeight => _contentHeight;

    #region Dependency Properties

    public static readonly DependencyProperty WorkItemsProperty =
        DependencyProperty.Register(nameof(WorkItems), typeof(ObservableCollection<WorkItemViewModel>),
            typeof(GraphCanvas), new PropertyMetadata(null, OnWorkItemsChanged));

    public static readonly DependencyProperty PhaseColumnsProperty =
        DependencyProperty.Register(nameof(PhaseColumns), typeof(ObservableCollection<PhaseColumnViewModel>),
            typeof(GraphCanvas), new PropertyMetadata(null, OnPhaseColumnsChanged));

    public static readonly DependencyProperty MilestoneEdgesProperty =
        DependencyProperty.Register(nameof(MilestoneEdges), typeof(ObservableCollection<MilestoneEdgeViewModel>),
            typeof(GraphCanvas), new PropertyMetadata(null, OnMilestoneEdgesChanged));

    public ObservableCollection<WorkItemViewModel> WorkItems
    {
        get => (ObservableCollection<WorkItemViewModel>)GetValue(WorkItemsProperty);
        set => SetValue(WorkItemsProperty, value);
    }

    public ObservableCollection<PhaseColumnViewModel> PhaseColumns
    {
        get => (ObservableCollection<PhaseColumnViewModel>)GetValue(PhaseColumnsProperty);
        set => SetValue(PhaseColumnsProperty, value);
    }

    public ObservableCollection<MilestoneEdgeViewModel> MilestoneEdges
    {
        get => (ObservableCollection<MilestoneEdgeViewModel>)GetValue(MilestoneEdgesProperty);
        set => SetValue(MilestoneEdgesProperty, value);
    }

    #endregion

    #region Property Change Handlers

    private static void OnWorkItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GraphCanvas canvas)
        {
            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= canvas.OnCollectionChanged;
            }
            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += canvas.OnCollectionChanged;
            }
            canvas.RebuildVisuals();
        }
    }

    private static void OnPhaseColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GraphCanvas canvas)
        {
            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= canvas.OnCollectionChanged;
            }
            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += canvas.OnCollectionChanged;
            }
            canvas.RebuildVisuals();
        }
    }

    private static void OnMilestoneEdgesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GraphCanvas canvas)
        {
            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= canvas.OnCollectionChanged;
            }
            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += canvas.OnCollectionChanged;
            }
            canvas.RebuildVisuals();
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildVisuals();
    }

    #endregion

    #region Rendering

    private void RebuildVisuals()
    {
        Children.Clear();

        if (PhaseColumns == null || PhaseColumns.Count == 0)
        {
            _contentWidth = 800;
            _contentHeight = 600;
            Width = _contentWidth;
            Height = _contentHeight;
            return;
        }

        var (calcWidth, calcHeight) = CalculateBounds();
        _contentWidth = calcWidth + ContentPadding;
        _contentHeight = calcHeight + ContentPadding;

        Width = _contentWidth;
        Height = _contentHeight;

        // Draw milestone edges FIRST (behind everything else)
        DrawMilestoneEdges();

        // Draw phase column backgrounds
        foreach (var phase in PhaseColumns)
        {
            DrawPhaseColumn(phase);
        }

        // Draw assembly groups
        foreach (var phase in PhaseColumns)
        {
            foreach (var group in phase.Groups)
            {
                DrawAssemblyGroup(group);
            }
        }

        // Build set of item IDs in collapsed groups
        var collapsedItemIds = new HashSet<string>();
        foreach (var phase in PhaseColumns)
        {
            foreach (var group in phase.Groups.Where(g => g.IsCollapsed))
            {
                foreach (var item in group.AllItems)
                {
                    collapsedItemIds.Add(item.Id);
                }
            }
        }

        // Draw task nodes (skip items in collapsed groups)
        if (WorkItems != null)
        {
            foreach (var item in WorkItems.Where(i => !i.IsMilestone && !collapsedItemIds.Contains(i.Id)))
            {
                DrawTaskNode(item);
            }
        }
    }

    private void DrawMilestoneEdges()
    {
        if (MilestoneEdges == null || MilestoneEdges.Count == 0) return;

        foreach (var edge in MilestoneEdges)
        {
            DrawMilestoneEdge(edge);
        }
    }

    private void DrawMilestoneEdge(MilestoneEdgeViewModel edge)
    {
        // Manhattan routing: horizontal from source, then vertical, then horizontal to target
        double fromX = edge.FromX;
        double fromY = edge.FromY;
        double toX = edge.ToX;
        double toY = edge.ToY;

        // Midpoint X for the vertical segment
        double midX = (fromX + toX) / 2;

        var path = new Path
        {
            Stroke = edge.LineBrush,
            StrokeThickness = edge.LineThickness,
            StrokeLineJoin = PenLineJoin.Round
        };

        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = new Point(fromX, fromY) };

        // Horizontal to midpoint
        figure.Segments.Add(new LineSegment(new Point(midX, fromY), true));
        // Vertical to target Y
        figure.Segments.Add(new LineSegment(new Point(midX, toY), true));
        // Horizontal to target
        figure.Segments.Add(new LineSegment(new Point(toX, toY), true));

        geometry.Figures.Add(figure);
        path.Data = geometry;

        Children.Add(path);

        // Draw arrowhead at the end
        DrawArrowhead(toX, toY, edge.LineBrush);
    }

    private void DrawArrowhead(double x, double y, Brush brush)
    {
        double arrowSize = 8;
        var arrow = new Polygon
        {
            Fill = brush,
            Points = new PointCollection
            {
                new Point(x, y),
                new Point(x - arrowSize, y - arrowSize / 2),
                new Point(x - arrowSize, y + arrowSize / 2)
            }
        };
        Children.Add(arrow);
    }

    private (double Width, double Height) CalculateBounds()
    {
        if (PhaseColumns == null || PhaseColumns.Count == 0)
        {
            return (800, 600);
        }

        double maxX = PhaseColumns.Max(p => p.X + p.Width);
        double maxY = PhaseColumns.Max(p => p.Y + p.Height);

        return (maxX + ContentPadding, maxY + ContentPadding);
    }

    private void DrawPhaseColumn(PhaseColumnViewModel phase)
    {
        // Column background
        var columnBg = new Rectangle
        {
            Width = phase.Width,
            Height = phase.Height,
            Fill = phase.ColumnBackground,
            Stroke = phase.ColumnBorder,
            StrokeThickness = 1,
            RadiusX = 6,
            RadiusY = 6
        };
        SetLeft(columnBg, phase.X);
        SetTop(columnBg, phase.Y);
        Children.Add(columnBg);

        // Phase header
        var headerBg = new Rectangle
        {
            Width = phase.Width - 4,
            Height = PhaseMatrixLayoutEngine.PhaseHeaderHeight - 4,
            Fill = phase.HeaderBackground,
            RadiusX = 4,
            RadiusY = 4
        };
        SetLeft(headerBg, phase.X + 2);
        SetTop(headerBg, phase.Y + 2);
        Children.Add(headerBg);

        // Phase name
        var phaseName = new TextBlock
        {
            Text = phase.PhaseName,
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = phase.Width - 20
        };
        SetLeft(phaseName, phase.X + 10);
        SetTop(phaseName, phase.Y + 10);
        Children.Add(phaseName);
    }

    private void DrawAssemblyGroup(AssemblyGroupViewModel group)
    {
        // Group background
        var groupBg = new Rectangle
        {
            Width = group.Width,
            Height = group.Height,
            Fill = group.GroupBackground,
            Stroke = group.GroupBorder,
            StrokeThickness = 1,
            RadiusX = 4,
            RadiusY = 4
        };
        SetLeft(groupBg, group.X);
        SetTop(groupBg, group.Y);
        Children.Add(groupBg);

        // Group header background (smaller when collapsed)
        double headerHeight = group.IsCollapsed
            ? PhaseMatrixLayoutEngine.GroupCollapsedHeight - 6
            : PhaseMatrixLayoutEngine.GroupHeaderHeight - 10;

        var headerBg = new Rectangle
        {
            Width = group.Width - 4,
            Height = headerHeight,
            Fill = group.HeaderBackground,
            RadiusX = 3,
            RadiusY = 3
        };
        SetLeft(headerBg, group.X + 2);
        SetTop(headerBg, group.Y + 2);
        Children.Add(headerBg);

        // Collapse/expand button
        var collapseButton = new Button
        {
            Content = group.CollapseButtonText,
            Width = 20,
            Height = 20,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(0),
            Background = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
            BorderBrush = Brushes.Transparent,
            Foreground = Brushes.Black,
            Cursor = System.Windows.Input.Cursors.Hand,
            Command = group.ToggleCollapseCommand
        };
        SetLeft(collapseButton, group.X + group.Width - 26);
        SetTop(collapseButton, group.Y + 6);
        Children.Add(collapseButton);

        // Group header content (adjust width for collapse button)
        var headerPanel = new StackPanel { Width = group.Width - 50 };

        // Title
        var title = new TextBlock
        {
            Text = group.DisplayHeader,
            FontWeight = FontWeights.Bold,
            FontSize = 11,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        headerPanel.Children.Add(title);

        if (group.HasMilestone)
        {
            // Progress and status row
            var infoPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };

            var percentText = new TextBlock
            {
                Text = group.DisplayPercent,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 8, 0)
            };
            infoPanel.Children.Add(percentText);

            // Status pill
            var statusPill = new Border
            {
                Background = group.StatusBrush,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1)
            };
            statusPill.Child = new TextBlock
            {
                Text = group.StatusText,
                FontSize = 9,
                Foreground = Brushes.White
            };
            infoPanel.Children.Add(statusPill);

            // Health indicator
            var healthDot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = group.HealthBrush,
                Margin = new Thickness(8, 2, 0, 0)
            };
            infoPanel.Children.Add(healthDot);

            // Target date
            if (!string.IsNullOrEmpty(group.DisplayTargetDate))
            {
                var dateText = new TextBlock
                {
                    Text = group.DisplayTargetDate,
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    Margin = new Thickness(8, 2, 0, 0)
                };
                infoPanel.Children.Add(dateText);
            }

            // Show task count when collapsed
            if (group.IsCollapsed)
            {
                var countText = new TextBlock
                {
                    Text = $"({group.AllItems.Count} tasks)",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    Margin = new Thickness(8, 2, 0, 0)
                };
                infoPanel.Children.Add(countText);
            }

            headerPanel.Children.Add(infoPanel);
        }

        SetLeft(headerPanel, group.X + 8);
        SetTop(headerPanel, group.Y + 6);
        Children.Add(headerPanel);

        // Only draw discipline rows if not collapsed
        if (!group.IsCollapsed)
        {
            foreach (var row in group.DisciplineRows)
            {
                DrawDisciplineRow(group, row);
            }
        }
    }

    private void DrawDisciplineRow(AssemblyGroupViewModel group, DisciplineRowViewModel row)
    {
        // Row background
        double rowBgLeft = group.X + PhaseMatrixLayoutEngine.DisciplineLabelWidth + PhaseMatrixLayoutEngine.GroupHorizontalPadding;
        double rowBgWidth = group.Width - PhaseMatrixLayoutEngine.DisciplineLabelWidth - PhaseMatrixLayoutEngine.GroupHorizontalPadding * 2;

        var rowBg = new Rectangle
        {
            Width = Math.Max(rowBgWidth, 50),
            Height = row.Height - 4,
            Fill = row.RowBackground,
            RadiusX = 2,
            RadiusY = 2
        };
        SetLeft(rowBg, rowBgLeft);
        SetTop(rowBg, row.Y + 2);
        Children.Add(rowBg);

        // Discipline label
        var labelBg = new Border
        {
            Width = PhaseMatrixLayoutEngine.DisciplineLabelWidth - 6,
            Height = row.Height - 8,
            Background = row.LabelBackground,
            CornerRadius = new CornerRadius(3)
        };
        var label = new TextBlock
        {
            Text = row.Discipline,
            FontWeight = FontWeights.SemiBold,
            FontSize = 10,
            Foreground = Brushes.White,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        labelBg.Child = label;

        SetLeft(labelBg, group.X + PhaseMatrixLayoutEngine.GroupHorizontalPadding);
        SetTop(labelBg, row.Y + 4);
        Children.Add(labelBg);
    }

    private void DrawTaskNode(WorkItemViewModel item)
    {
        double nodeWidth = PhaseMatrixLayoutEngine.NodeWidth;
        double nodeHeight = PhaseMatrixLayoutEngine.NodeHeight;

        var border = new Border
        {
            Width = nodeWidth,
            Height = nodeHeight,
            Background = item.NodeBackground,
            BorderBrush = item.NodeBorder,
            BorderThickness = new Thickness(item.NodeBorderThickness),
            CornerRadius = new CornerRadius(4),
            ToolTip = item.TooltipText
        };

        border.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            BlurRadius = 3,
            ShadowDepth = 1,
            Opacity = 0.15,
            Color = Colors.Black
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Title
        var title = new TextBlock
        {
            Text = item.Title,
            FontWeight = FontWeights.SemiBold,
            FontSize = 9,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(4, 3, 4, 0)
        };
        Grid.SetRow(title, 0);
        grid.Children.Add(title);

        // ID
        var id = new TextBlock
        {
            Text = item.Id,
            FontSize = 7,
            Foreground = Brushes.Gray,
            Margin = new Thickness(4, 0, 4, 0)
        };
        Grid.SetRow(id, 1);
        grid.Children.Add(id);

        // Bottom row: percent and status
        var bottomPanel = new DockPanel { Margin = new Thickness(4, 2, 4, 3) };

        var percent = new TextBlock
        {
            Text = item.DisplayPercent,
            FontSize = 12,
            FontWeight = FontWeights.Bold
        };
        DockPanel.SetDock(percent, Dock.Left);
        bottomPanel.Children.Add(percent);

        var statusPill = new Border
        {
            Background = item.StatusBrush,
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(3, 0, 3, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        statusPill.Child = new TextBlock
        {
            Text = item.StatusText,
            FontSize = 7,
            Foreground = Brushes.White
        };
        DockPanel.SetDock(statusPill, Dock.Right);
        bottomPanel.Children.Add(statusPill);

        Grid.SetRow(bottomPanel, 2);
        grid.Children.Add(bottomPanel);

        border.Child = grid;

        SetLeft(border, item.X);
        SetTop(border, item.Y);
        Children.Add(border);
    }

    #endregion
}
