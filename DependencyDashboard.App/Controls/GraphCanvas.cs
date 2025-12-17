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

    public static readonly DependencyProperty WorkItemsProperty =
        DependencyProperty.Register(nameof(WorkItems), typeof(ObservableCollection<WorkItemViewModel>),
            typeof(GraphCanvas), new PropertyMetadata(null, OnWorkItemsChanged));

    public static readonly DependencyProperty EdgesProperty =
        DependencyProperty.Register(nameof(Edges), typeof(ObservableCollection<DependencyEdgeViewModel>),
            typeof(GraphCanvas), new PropertyMetadata(null, OnEdgesChanged));

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

    private static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GraphCanvas canvas && e.NewValue is double zoom)
        {
            canvas._scaleTransform.ScaleX = zoom;
            canvas._scaleTransform.ScaleY = zoom;
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

    private void RebuildVisuals()
    {
        Children.Clear();

        // Create a container that will be transformed
        var container = new Canvas
        {
            RenderTransform = _transformGroup
        };

        // Draw edges first (below nodes)
        if (Edges != null)
        {
            foreach (var edge in Edges)
            {
                DrawEdge(container, edge);
            }
        }

        // Draw nodes
        if (WorkItems != null)
        {
            foreach (var item in WorkItems)
            {
                DrawNode(container, item);
            }
        }

        Children.Add(container);
    }

    private void DrawEdge(Canvas container, DependencyEdgeViewModel edge)
    {
        if (edge.Prerequisite == null || edge.Dependent == null) return;

        var path = new Path
        {
            Stroke = edge.StrokeBrush,
            StrokeThickness = edge.StrokeThickness,
            Data = edge.EdgeGeometry
        };

        container.Children.Add(path);

        // Draw arrow head
        var arrow = new Polygon
        {
            Points = edge.ArrowHead,
            Fill = edge.StrokeBrush
        };
        container.Children.Add(arrow);
    }

    private void DrawNode(Canvas container, WorkItemViewModel item)
    {
        var node = CreateNodeVisual(item);
        Canvas.SetLeft(node, item.X);
        Canvas.SetTop(node, item.Y);
        container.Children.Add(node);
    }

    private FrameworkElement CreateNodeVisual(WorkItemViewModel item)
    {
        var border = new Border
        {
            Width = GraphLayoutEngine.NodeWidth,
            Height = GraphLayoutEngine.NodeHeight,
            Background = item.NodeBackground,
            BorderBrush = item.NodeBorder,
            BorderThickness = new Thickness(item.NodeBorderThickness),
            CornerRadius = new CornerRadius(6),
            Cursor = Cursors.Hand,
            Tag = item,
            ToolTip = item.TooltipText
        };

        border.MouseLeftButtonDown += OnNodeClick;

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Title
        var title = new TextBlock
        {
            Text = item.Title,
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(8, 6, 8, 0)
        };
        Grid.SetRow(title, 0);
        grid.Children.Add(title);

        // Id
        var id = new TextBlock
        {
            Text = item.Id,
            FontSize = 10,
            Foreground = Brushes.Gray,
            Margin = new Thickness(8, 2, 8, 0)
        };
        Grid.SetRow(id, 1);
        grid.Children.Add(id);

        // Center area with percent
        var centerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 4, 8, 4)
        };

        var percent = new TextBlock
        {
            Text = item.DisplayPercent,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        centerPanel.Children.Add(percent);

        Grid.SetRow(centerPanel, 2);
        grid.Children.Add(centerPanel);

        // Bottom row: status pill and (for milestones) health + date
        var bottomPanel = new DockPanel
        {
            Margin = new Thickness(8, 0, 8, 6)
        };

        // Status pill
        var statusPill = new Border
        {
            Background = item.StatusBrush,
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(6, 2, 6, 2),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var statusText = new TextBlock
        {
            Text = item.StatusText,
            FontSize = 9,
            Foreground = Brushes.White
        };
        statusPill.Child = statusText;
        DockPanel.SetDock(statusPill, Dock.Left);
        bottomPanel.Children.Add(statusPill);

        // For milestones: target date and health indicator
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
                    FontSize = 9,
                    Foreground = Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0)
                };
                milestoneInfo.Children.Add(dateText);
            }

            var healthIndicator = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = item.HealthBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            milestoneInfo.Children.Add(healthIndicator);

            DockPanel.SetDock(milestoneInfo, Dock.Right);
            bottomPanel.Children.Add(milestoneInfo);
        }

        // Blocked indicator
        if (!string.IsNullOrEmpty(item.BlockedById))
        {
            var blockedText = new TextBlock
            {
                Text = $"Blocked: {item.BlockedById}",
                FontSize = 8,
                Foreground = Brushes.OrangeRed,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };
            bottomPanel.Children.Add(blockedText);
        }

        Grid.SetRow(bottomPanel, 3);
        grid.Children.Add(bottomPanel);

        border.Child = grid;
        return border;
    }

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

        // Zoom towards mouse position
        var oldZoom = ZoomLevel;
        ZoomLevel = newZoom;

        // Adjust pan to keep the point under cursor
        var zoomRatio = newZoom / oldZoom;
        PanX = pos.X - (pos.X - PanX) * zoomRatio;
        PanY = pos.Y - (pos.Y - PanY) * zoomRatio;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == this || (e.OriginalSource is Canvas))
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
}
