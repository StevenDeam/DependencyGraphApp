using DependencyDashboard.App.ViewModels;
using DependencyDashboard.Core.Graph;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DependencyDashboard.App.Controls;

public class GraphCanvas : Canvas
{
    private const double ContentPadding = 20;

    public GraphCanvas()
    {
        Background = Brushes.Transparent;
        ClipToBounds = false;
    }

    #region Dependency Properties

    public static readonly DependencyProperty WorkItemsProperty =
        DependencyProperty.Register(nameof(WorkItems), typeof(ObservableCollection<WorkItemViewModel>),
            typeof(GraphCanvas), new PropertyMetadata(null, OnWorkItemsChanged));

    public static readonly DependencyProperty PhaseColumnsProperty =
        DependencyProperty.Register(nameof(PhaseColumns), typeof(ObservableCollection<PhaseColumnViewModel>),
            typeof(GraphCanvas), new PropertyMetadata(null, OnPhaseColumnsChanged));

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
            Width = 800;
            Height = 600;
            return;
        }

        var (contentWidth, contentHeight) = CalculateBounds();
        contentWidth += ContentPadding;
        contentHeight += ContentPadding;

        Width = contentWidth;
        Height = contentHeight;

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

        // Draw task nodes
        if (WorkItems != null)
        {
            foreach (var item in WorkItems)
            {
                if (item.IsMilestone) continue;
                DrawTaskNode(item);
            }
        }
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

        // Group header background
        var headerBg = new Rectangle
        {
            Width = group.Width - 4,
            Height = PhaseMatrixLayoutEngine.GroupHeaderHeight - 10,
            Fill = group.HeaderBackground,
            RadiusX = 3,
            RadiusY = 3
        };
        SetLeft(headerBg, group.X + 2);
        SetTop(headerBg, group.Y + 2);
        Children.Add(headerBg);

        // Group header content
        var headerPanel = new StackPanel { Width = group.Width - 20 };

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

            headerPanel.Children.Add(infoPanel);
        }

        SetLeft(headerPanel, group.X + 8);
        SetTop(headerPanel, group.Y + 6);
        Children.Add(headerPanel);

        // Draw discipline rows
        foreach (var row in group.DisciplineRows)
        {
            DrawDisciplineRow(group, row);
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
