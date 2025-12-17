using DependencyDashboard.App.ViewModels;
using DependencyDashboard.Core.Graph;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DependencyDashboard.App.Controls;

public class GraphCanvas : Canvas
{
    private Point _lastMousePosition;
    private bool _isPanning;
    private readonly ScaleTransform _scaleTransform;
    private readonly TranslateTransform _translateTransform;
    private readonly TransformGroup _transformGroup;

    // Padding around content for scrollable area
    private const double ContentPadding = 50;
    // Header height for level labels (bracket mode)
    private const double LaneHeaderHeight = 30;
    // Merge spine offset from node
    private const double MergeSpineOffset = 15;

    public GraphCanvas()
    {
        Background = Brushes.White;
        ClipToBounds = true;

        _scaleTransform = new ScaleTransform(1, 1);
        _translateTransform = new TranslateTransform(0, 0);
        _transformGroup = new TransformGroup();
        _transformGroup.Children.Add(_scaleTransform);
        _transformGroup.Children.Add(_translateTransform);

        MouseWheel += OnMouseWheel;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseMove += OnMouseMove;
    }

    #region Dependency Properties

    public static readonly DependencyProperty WorkItemsProperty =
        DependencyProperty.Register(nameof(WorkItems), typeof(ObservableCollection<WorkItemViewModel>),
            typeof(GraphCanvas), new PropertyMetadata(null, OnWorkItemsChanged));

    public static readonly DependencyProperty EdgesProperty =
        DependencyProperty.Register(nameof(Edges), typeof(ObservableCollection<DependencyEdgeViewModel>),
            typeof(GraphCanvas), new PropertyMetadata(null, OnEdgesChanged));

    public static readonly DependencyProperty SwimLanesProperty =
        DependencyProperty.Register(nameof(SwimLanes), typeof(ObservableCollection<SwimLaneViewModel>),
            typeof(GraphCanvas), new PropertyMetadata(null, OnSwimLanesChanged));

    public static readonly DependencyProperty LayoutModeProperty =
        DependencyProperty.Register(nameof(LayoutMode), typeof(GraphLayoutMode), typeof(GraphCanvas),
            new PropertyMetadata(GraphLayoutMode.Bracket, OnLayoutModeChanged));

    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(nameof(ZoomLevel), typeof(double), typeof(GraphCanvas),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnZoomLevelChanged));

    public static readonly DependencyProperty PanXProperty =
        DependencyProperty.Register(nameof(PanX), typeof(double), typeof(GraphCanvas),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPanChanged));

    public static readonly DependencyProperty PanYProperty =
        DependencyProperty.Register(nameof(PanY), typeof(double), typeof(GraphCanvas),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPanChanged));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(WorkItemViewModel), typeof(GraphCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ObservableCollection<WorkItemViewModel> WorkItems
    {
        get => (ObservableCollection<WorkItemViewModel>)GetValue(WorkItemsProperty);
        set => SetValue(WorkItemsProperty, value);
    }

    public ObservableCollection<DependencyEdgeViewModel> Edges
    {
        get => (ObservableCollection<DependencyEdgeViewModel>)GetValue(EdgesProperty);
        set => SetValue(EdgesProperty, value);
    }

    public ObservableCollection<SwimLaneViewModel> SwimLanes
    {
        get => (ObservableCollection<SwimLaneViewModel>)GetValue(SwimLanesProperty);
        set => SetValue(SwimLanesProperty, value);
    }

    public GraphLayoutMode LayoutMode
    {
        get => (GraphLayoutMode)GetValue(LayoutModeProperty);
        set => SetValue(LayoutModeProperty, value);
    }

    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public double PanX
    {
        get => (double)GetValue(PanXProperty);
        set => SetValue(PanXProperty, value);
    }

    public double PanY
    {
        get => (double)GetValue(PanYProperty);
        set => SetValue(PanYProperty, value);
    }

    public WorkItemViewModel? SelectedItem
    {
        get => (WorkItemViewModel?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
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

    private static void OnEdgesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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

    private static void OnSwimLanesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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

    private static void OnLayoutModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GraphCanvas canvas)
        {
            canvas.RebuildVisuals();
        }
    }

    private static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GraphCanvas canvas && e.NewValue is double zoom)
        {
            canvas._scaleTransform.ScaleX = zoom;
            canvas._scaleTransform.ScaleY = zoom;
            canvas.UpdateCanvasSize();
        }
    }

    private static void OnPanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GraphCanvas canvas)
        {
            canvas._translateTransform.X = canvas.PanX;
            canvas._translateTransform.Y = canvas.PanY;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildVisuals();
    }

    #endregion

    #region Layout Calculation

    private (double minX, double minY, double maxX, double maxY) CalculateBounds()
    {
        if (WorkItems == null || WorkItems.Count == 0)
        {
            return (0, 0, 800, 600);
        }

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var item in WorkItems)
        {
            minX = Math.Min(minX, item.X);
            minY = Math.Min(minY, item.Y);
            maxX = Math.Max(maxX, item.X + GraphLayoutEngine.NodeWidth);
            maxY = Math.Max(maxY, item.Y + GraphLayoutEngine.NodeHeight);
        }

        return (minX, minY, maxX, maxY);
    }

    private (double Width, double Height) CalculateSwimlaneBounds()
    {
        if (SwimLanes == null || SwimLanes.Count == 0)
        {
            return (800, 600);
        }

        double maxX = SwimLanes.Max(l => l.X + l.Width);
        double maxY = SwimLanes.Max(l => l.Y + l.Height);

        return (maxX + ContentPadding, maxY + ContentPadding);
    }

    private void UpdateCanvasSize()
    {
        if (LayoutMode == GraphLayoutMode.Swimlane)
        {
            var (w, h) = CalculateSwimlaneBounds();
            Width = (w + ContentPadding) * ZoomLevel;
            Height = (h + ContentPadding) * ZoomLevel;
        }
        else
        {
            var (minX, minY, maxX, maxY) = CalculateBounds();
            double contentWidth = (maxX - minX) + ContentPadding * 2;
            double contentHeight = (maxY - minY) + ContentPadding * 2 + LaneHeaderHeight;
            Width = contentWidth * ZoomLevel;
            Height = contentHeight * ZoomLevel;
        }
    }

    #endregion

    #region Visual Rendering

    private void RebuildVisuals()
    {
        Children.Clear();

        if (LayoutMode == GraphLayoutMode.Swimlane)
        {
            RebuildSwimlaneVisuals();
        }
        else
        {
            RebuildBracketVisuals();
        }
    }

    private void RebuildBracketVisuals()
    {
        if (WorkItems == null || WorkItems.Count == 0)
        {
            Width = 800;
            Height = 600;
            return;
        }

        var (minX, minY, maxX, maxY) = CalculateBounds();

        double offsetX = -minX + ContentPadding;
        double offsetY = -minY + ContentPadding + LaneHeaderHeight;

        double contentWidth = (maxX - minX) + ContentPadding * 2;
        double contentHeight = (maxY - minY) + ContentPadding * 2 + LaneHeaderHeight;

        Width = contentWidth * ZoomLevel;
        Height = contentHeight * ZoomLevel;

        var container = new Canvas
        {
            RenderTransform = _transformGroup,
            Width = contentWidth,
            Height = contentHeight
        };

        // 1. Draw swimlane backgrounds (level columns)
        DrawLevelSwimlanes(container, offsetX, offsetY, maxY - minY + ContentPadding);

        // 2. Draw merge spines
        DrawMergeSpines(container, offsetX, offsetY);

        // 3. Draw edges
        if (Edges != null)
        {
            foreach (var edge in Edges)
            {
                DrawBracketEdge(container, edge, offsetX, offsetY);
            }
        }

        // 4. Draw nodes
        foreach (var item in WorkItems)
        {
            DrawNode(container, item, item.X + offsetX, item.Y + offsetY);
        }

        Children.Add(container);
    }

    private void RebuildSwimlaneVisuals()
    {
        if (SwimLanes == null || SwimLanes.Count == 0)
        {
            // Fall back to bracket if no swimlanes
            if (WorkItems != null && WorkItems.Count > 0)
            {
                RebuildBracketVisuals();
            }
            else
            {
                Width = 800;
                Height = 600;
            }
            return;
        }

        var (contentWidth, contentHeight) = CalculateSwimlaneBounds();
        contentWidth += ContentPadding;
        contentHeight += ContentPadding;

        Width = contentWidth * ZoomLevel;
        Height = contentHeight * ZoomLevel;

        var container = new Canvas
        {
            RenderTransform = _transformGroup,
            Width = contentWidth,
            Height = contentHeight
        };

        double offsetX = ContentPadding;
        double offsetY = ContentPadding;

        // 1. Draw lane backgrounds
        foreach (var lane in SwimLanes)
        {
            DrawSwimLaneBackground(container, lane, offsetX, offsetY);
        }

        // 2. Draw edges (cross-lane edges thicker)
        if (Edges != null)
        {
            foreach (var edge in Edges)
            {
                DrawSwimlaneEdge(container, edge, offsetX, offsetY);
            }
        }

        // 3. Draw lane headers
        foreach (var lane in SwimLanes)
        {
            DrawSwimLaneHeader(container, lane, offsetX, offsetY);
        }

        // 4. Draw nodes
        if (WorkItems != null)
        {
            foreach (var item in WorkItems)
            {
                DrawNode(container, item, item.X + offsetX, item.Y + offsetY);
            }
        }

        Children.Add(container);
    }

    private void DrawSwimLaneBackground(Canvas container, SwimLaneViewModel lane, double offsetX, double offsetY)
    {
        var rect = new Rectangle
        {
            Width = lane.Width,
            Height = lane.Height,
            Fill = lane.LaneBackground,
            Stroke = lane.LaneBorder,
            StrokeThickness = 1,
            RadiusX = 4,
            RadiusY = 4
        };

        Canvas.SetLeft(rect, lane.X + offsetX);
        Canvas.SetTop(rect, lane.Y + offsetY);
        container.Children.Add(rect);
    }

    private void DrawSwimLaneHeader(Canvas container, SwimLaneViewModel lane, double offsetX, double offsetY)
    {
        // Lane header background
        var headerBg = new Rectangle
        {
            Width = lane.HeaderWidth - 4,
            Height = lane.Height - 4,
            Fill = new SolidColorBrush(Color.FromArgb(200, 70, 130, 180)),
            RadiusX = 4,
            RadiusY = 4
        };

        Canvas.SetLeft(headerBg, lane.X + offsetX + 2);
        Canvas.SetTop(headerBg, lane.Y + offsetY + 2);
        container.Children.Add(headerBg);

        // Header content panel
        var headerPanel = new StackPanel
        {
            Width = lane.HeaderWidth - 16,
            Margin = new Thickness(8)
        };

        // Title
        var title = new TextBlock
        {
            Text = lane.Title,
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 36
        };
        headerPanel.Children.Add(title);

        if (lane.HasMilestone)
        {
            // ID
            var id = new TextBlock
            {
                Text = lane.Id,
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 220, 240)),
                Margin = new Thickness(0, 2, 0, 4)
            };
            headerPanel.Children.Add(id);

            // Progress
            var progressPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var percentText = new TextBlock
            {
                Text = lane.DisplayPercent,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            progressPanel.Children.Add(percentText);

            // Health indicator
            var healthDot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = lane.HealthBrush,
                Margin = new Thickness(8, 4, 0, 0)
            };
            progressPanel.Children.Add(healthDot);
            headerPanel.Children.Add(progressPanel);

            // Status pill
            var statusPill = new Border
            {
                Background = lane.StatusBrush,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 4, 0, 0)
            };
            var statusText = new TextBlock
            {
                Text = lane.StatusText,
                FontSize = 9,
                Foreground = Brushes.White
            };
            statusPill.Child = statusText;
            headerPanel.Children.Add(statusPill);
        }

        Canvas.SetLeft(headerPanel, lane.X + offsetX + 4);
        Canvas.SetTop(headerPanel, lane.Y + offsetY + 4);
        container.Children.Add(headerPanel);
    }

    private void DrawSwimlaneEdge(Canvas container, DependencyEdgeViewModel edge, double offsetX, double offsetY)
    {
        if (edge.Prerequisite == null || edge.Dependent == null) return;

        var startX = edge.Prerequisite.X + SwimlaneLayoutEngine.NodeWidth + offsetX;
        var startY = edge.Prerequisite.Y + SwimlaneLayoutEngine.NodeHeight / 2 + offsetY;
        var endX = edge.Dependent.X + offsetX;
        var endY = edge.Dependent.Y + SwimlaneLayoutEngine.NodeHeight / 2 + offsetY;

        // Check if cross-lane edge
        bool isCrossLane = false;
        if (SwimLanes != null)
        {
            var prereqLane = SwimLanes.FirstOrDefault(l => l.Children.Contains(edge.Prerequisite));
            var depLane = SwimLanes.FirstOrDefault(l => l.Children.Contains(edge.Dependent));
            isCrossLane = prereqLane != depLane;
        }

        // Create path
        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = new Point(startX, startY) };

        double controlOffset = Math.Abs(endX - startX) / 3;
        var bezier = new BezierSegment(
            new Point(startX + controlOffset, startY),
            new Point(endX - controlOffset, endY),
            new Point(endX, endY),
            true);
        figure.Segments.Add(bezier);
        geometry.Figures.Add(figure);

        var strokeBrush = isCrossLane
            ? new SolidColorBrush(Color.FromRgb(80, 80, 80))
            : edge.StrokeBrush;

        var path = new Path
        {
            Stroke = strokeBrush,
            StrokeThickness = isCrossLane ? 2.5 : edge.StrokeThickness,
            Data = geometry,
            StrokeDashArray = isCrossLane ? null : null
        };
        container.Children.Add(path);

        // Arrow head
        const double arrowSize = 8;
        var dx = 1.0;
        var dy = 0.0;
        var px = 0.0;
        var py = 1.0;

        var p1 = new Point(endX - arrowSize * dx + arrowSize / 2 * px, endY - arrowSize * dy + arrowSize / 2 * py);
        var p2 = new Point(endX - arrowSize * dx - arrowSize / 2 * px, endY - arrowSize * dy - arrowSize / 2 * py);

        var arrow = new Polygon
        {
            Points = new PointCollection { p1, new Point(endX, endY), p2 },
            Fill = strokeBrush
        };
        container.Children.Add(arrow);

        // Cross-lane gate indicator
        if (isCrossLane)
        {
            var gateIndicator = new Rectangle
            {
                Width = 4,
                Height = 12,
                Fill = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                RadiusX = 2,
                RadiusY = 2
            };
            Canvas.SetLeft(gateIndicator, endX - 6);
            Canvas.SetTop(gateIndicator, endY - 6);
            container.Children.Add(gateIndicator);
        }
    }

    #region Bracket Mode Drawing (existing)

    private void DrawLevelSwimlanes(Canvas container, double offsetX, double offsetY, double laneHeight)
    {
        if (WorkItems == null || WorkItems.Count == 0) return;

        var levels = WorkItems
            .GroupBy(i => i.ComputedLevel)
            .OrderBy(g => g.Key)
            .ToList();

        if (levels.Count == 0) return;

        var lightColor = new SolidColorBrush(Color.FromArgb(20, 100, 149, 237));
        var darkColor = new SolidColorBrush(Color.FromArgb(35, 100, 149, 237));

        foreach (var levelGroup in levels)
        {
            var level = levelGroup.Key;
            var items = levelGroup.ToList();

            double laneX = items.Min(i => i.X) + offsetX - 20;
            double laneWidth = GraphLayoutEngine.NodeWidth + 40;

            var laneRect = new Rectangle
            {
                Width = laneWidth,
                Height = laneHeight + LaneHeaderHeight,
                Fill = level % 2 == 0 ? lightColor : darkColor,
                Stroke = new SolidColorBrush(Color.FromArgb(50, 100, 149, 237)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(laneRect, laneX);
            Canvas.SetTop(laneRect, 0);
            container.Children.Add(laneRect);

            var levelLabel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 70, 130, 180)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 2, 8, 2),
                Child = new TextBlock
                {
                    Text = $"Level {level}",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White
                }
            };
            Canvas.SetLeft(levelLabel, laneX + (laneWidth - 60) / 2);
            Canvas.SetTop(levelLabel, 5);
            container.Children.Add(levelLabel);
        }
    }

    private void DrawMergeSpines(Canvas container, double offsetX, double offsetY)
    {
        if (WorkItems == null || Edges == null) return;

        var incomingEdgeCounts = new Dictionary<string, int>();
        foreach (var edge in Edges)
        {
            if (edge.Dependent != null)
            {
                var id = edge.Dependent.Id;
                if (!incomingEdgeCounts.ContainsKey(id))
                    incomingEdgeCounts[id] = 0;
                incomingEdgeCounts[id]++;
            }
        }

        var mergeNodes = WorkItems
            .Where(w => incomingEdgeCounts.TryGetValue(w.Id, out var count) && count >= 2)
            .ToList();

        foreach (var mergeNode in mergeNodes)
        {
            var incomingEdges = Edges
                .Where(e => e.Dependent?.Id == mergeNode.Id)
                .ToList();

            if (incomingEdges.Count < 2) continue;

            var edgeYPositions = incomingEdges
                .Where(e => e.Prerequisite != null)
                .Select(e => e.Prerequisite!.Y + GraphLayoutEngine.NodeHeight / 2 + offsetY)
                .OrderBy(y => y)
                .ToList();

            if (edgeYPositions.Count < 2) continue;

            double spineX = mergeNode.X + offsetX - MergeSpineOffset;
            double spineTop = edgeYPositions.First() - 5;
            double spineBottom = edgeYPositions.Last() + 5;

            var spineLine = new Line
            {
                X1 = spineX,
                Y1 = spineTop,
                X2 = spineX,
                Y2 = spineBottom,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                StrokeThickness = 3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            container.Children.Add(spineLine);

            double nodeEntryY = mergeNode.Y + GraphLayoutEngine.NodeHeight / 2 + offsetY;
            var connectorLine = new Line
            {
                X1 = spineX,
                Y1 = nodeEntryY,
                X2 = mergeNode.X + offsetX,
                Y2 = nodeEntryY,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                StrokeThickness = 2
            };
            container.Children.Add(connectorLine);

            foreach (var edgeY in edgeYPositions)
            {
                var tickLine = new Line
                {
                    X1 = spineX - 5,
                    Y1 = edgeY,
                    X2 = spineX,
                    Y2 = edgeY,
                    Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    StrokeThickness = 2,
                    StrokeStartLineCap = PenLineCap.Round
                };
                container.Children.Add(tickLine);
            }

            var mergeIndicator = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Color.FromRgb(70, 130, 180)),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(mergeIndicator, spineX - 5);
            Canvas.SetTop(mergeIndicator, nodeEntryY - 5);
            container.Children.Add(mergeIndicator);
        }
    }

    private void DrawBracketEdge(Canvas container, DependencyEdgeViewModel edge, double offsetX, double offsetY)
    {
        if (edge.Prerequisite == null || edge.Dependent == null) return;

        var startX = edge.Prerequisite.X + GraphLayoutEngine.NodeWidth + offsetX;
        var startY = edge.Prerequisite.Y + GraphLayoutEngine.NodeHeight / 2 + offsetY;
        var endX = edge.Dependent.X + offsetX;
        var endY = edge.Dependent.Y + GraphLayoutEngine.NodeHeight / 2 + offsetY;

        var incomingCount = Edges?.Count(e => e.Dependent?.Id == edge.Dependent.Id) ?? 0;
        if (incomingCount >= 2)
        {
            endX = edge.Dependent.X + offsetX - MergeSpineOffset - 5;
        }

        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = new Point(startX, startY) };

        double controlOffset = (endX - startX) / 3;
        var bezier = new BezierSegment(
            new Point(startX + controlOffset, startY),
            new Point(endX - controlOffset, endY),
            new Point(endX, endY),
            true);
        figure.Segments.Add(bezier);
        geometry.Figures.Add(figure);

        var path = new Path
        {
            Stroke = edge.StrokeBrush,
            StrokeThickness = edge.StrokeThickness,
            Data = geometry
        };
        container.Children.Add(path);

        if (incomingCount < 2)
        {
            const double arrowSize = 8;
            var dx = endX - (endX - controlOffset);
            var dy = endY - endY;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.001) { dx = 1; length = 1; }
            dx /= length;
            dy /= length;
            var px = -dy;
            var py = dx;

            var p1 = new Point(endX - arrowSize * dx + arrowSize / 2 * px, endY - arrowSize * dy + arrowSize / 2 * py);
            var p2 = new Point(endX - arrowSize * dx - arrowSize / 2 * px, endY - arrowSize * dy - arrowSize / 2 * py);

            var arrow = new Polygon
            {
                Points = new PointCollection { p1, new Point(endX, endY), p2 },
                Fill = edge.StrokeBrush
            };
            container.Children.Add(arrow);
        }
    }

    #endregion

    private void DrawNode(Canvas container, WorkItemViewModel item, double x, double y)
    {
        var node = CreateNodeVisual(item);
        Canvas.SetLeft(node, x);
        Canvas.SetTop(node, y);
        container.Children.Add(node);
    }

    private FrameworkElement CreateNodeVisual(WorkItemViewModel item)
    {
        double nodeWidth = LayoutMode == GraphLayoutMode.Swimlane
            ? SwimlaneLayoutEngine.NodeWidth
            : GraphLayoutEngine.NodeWidth;
        double nodeHeight = LayoutMode == GraphLayoutMode.Swimlane
            ? SwimlaneLayoutEngine.NodeHeight
            : GraphLayoutEngine.NodeHeight;

        var border = new Border
        {
            Width = nodeWidth,
            Height = nodeHeight,
            Background = item.NodeBackground,
            BorderBrush = item.NodeBorder,
            BorderThickness = new Thickness(item.NodeBorderThickness),
            CornerRadius = new CornerRadius(6),
            Cursor = Cursors.Hand,
            Tag = item,
            ToolTip = item.TooltipText
        };

        border.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            BlurRadius = 5,
            ShadowDepth = 2,
            Opacity = 0.2,
            Color = Colors.Black
        };

        border.MouseLeftButtonDown += OnNodeClick;

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock
        {
            Text = item.Title,
            FontWeight = FontWeights.Bold,
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(6, 4, 6, 0)
        };
        Grid.SetRow(title, 0);
        grid.Children.Add(title);

        var id = new TextBlock
        {
            Text = item.Id,
            FontSize = 9,
            Foreground = Brushes.Gray,
            Margin = new Thickness(6, 1, 6, 0)
        };
        Grid.SetRow(id, 1);
        grid.Children.Add(id);

        var centerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 2, 6, 2)
        };

        var percent = new TextBlock
        {
            Text = item.DisplayPercent,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        centerPanel.Children.Add(percent);

        Grid.SetRow(centerPanel, 2);
        grid.Children.Add(centerPanel);

        var bottomPanel = new DockPanel
        {
            Margin = new Thickness(6, 0, 6, 4)
        };

        var statusPill = new Border
        {
            Background = item.StatusBrush,
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(4, 1, 4, 1),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var statusText = new TextBlock
        {
            Text = item.StatusText,
            FontSize = 8,
            Foreground = Brushes.White
        };
        statusPill.Child = statusText;
        DockPanel.SetDock(statusPill, Dock.Left);
        bottomPanel.Children.Add(statusPill);

        if (item.IsMilestone)
        {
            var milestoneInfo = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            if (item.TargetDate.HasValue)
            {
                var dateText = new TextBlock
                {
                    Text = item.DisplayTargetDate,
                    FontSize = 8,
                    Foreground = Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 3, 0)
                };
                milestoneInfo.Children.Add(dateText);
            }

            var healthIndicator = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = item.HealthBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            milestoneInfo.Children.Add(healthIndicator);

            DockPanel.SetDock(milestoneInfo, Dock.Right);
            bottomPanel.Children.Add(milestoneInfo);
        }

        if (!string.IsNullOrEmpty(item.BlockedById))
        {
            var blockedText = new TextBlock
            {
                Text = $"Blocked: {item.BlockedById}",
                FontSize = 7,
                Foreground = Brushes.OrangeRed,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0)
            };
            bottomPanel.Children.Add(blockedText);
        }

        Grid.SetRow(bottomPanel, 3);
        grid.Children.Add(bottomPanel);

        border.Child = grid;
        return border;
    }

    #endregion

    #region Mouse Interaction

    private void OnNodeClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is WorkItemViewModel item)
        {
            SelectedItem = item;
            e.Handled = true;
        }
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var pos = e.GetPosition(this);
        var delta = e.Delta > 0 ? 1.2 : 1 / 1.2;
        var newZoom = Math.Max(0.2, Math.Min(5.0, ZoomLevel * delta));

        var oldZoom = ZoomLevel;
        ZoomLevel = newZoom;

        var zoomRatio = newZoom / oldZoom;
        PanX = pos.X - (pos.X - PanX) * zoomRatio;
        PanY = pos.Y - (pos.Y - PanY) * zoomRatio;

        e.Handled = true;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == this || (e.OriginalSource is Canvas) || (e.OriginalSource is Rectangle))
        {
            _isPanning = true;
            _lastMousePosition = e.GetPosition(this);
            CaptureMouse();
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            ReleaseMouseCapture();
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_isPanning)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMousePosition;
            _lastMousePosition = currentPosition;

            PanX += delta.X;
            PanY += delta.Y;
        }
    }

    #endregion
}
