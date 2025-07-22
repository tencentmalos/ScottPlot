using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Avalonia_Demo.Controls;

public partial class ucTimelineScope : UserControl
{
    // Data arrays for the plots
    private double[] timelineData;
    private double[] xValues;

    // Plottable objects
    private Signal timelineSignal;
    private HorizontalSpan scopeSpan;

    // UI elements arrays
    private List<AvaPlot> detailPlots = new();
    private List<Border> detailBorders = new();

    // Scope selection variables
    private double scopeStart = 20;
    private double scopeEnd = 80;

    // Detail view instances (using the new separated classes)
    private List<ValueDetailView1D> value1DViews = new();
    private List<ValueDetailView2D> value2DViews = new();
    private List<FlameGraphDetailView> flameGraphViews = new();

    // Track which type each detail view is
    private List<DetailViewType> detailViewTypes = new();

    public enum DetailViewType
    {
        Value1D,
        Value2D,
        FlameGraph
    }

    // DetailView configurations
    public List<DetailViewConfig> detailViewConfigs = new()
    {
        new DetailViewConfig 
        { 
            Title = "DetailView 1 - 2D Array Data (Custom X,Y Coordinates)",
            DataSeries = new List<DataSeriesConfig>
            {
                new DataSeriesConfig 
                { 
                    Label = "Non-uniform X Cosine", 
                    LineColor = Colors.Red,
                    Use2DArray = true,
                    XYDataGenerator = (i) => {
                        // Generate non-uniform X coordinates (logarithmic spacing)
                        double x = Math.Log(i + 1) * 10 + (new Random(42 + i).NextDouble() - 0.5) * 2;
                        // Generate Y coordinates based on cosine function
                        double y = Math.Cos(x * 0.1) * 2 + (new Random(42 + i).NextDouble() - 0.5) * 0.3;
                        return (x, y);
                    }
                },
                new DataSeriesConfig 
                { 
                    Label = "Exponential X Sine", 
                    LineColor = Colors.Blue,
                    Use2DArray = true,
                    XYDataGenerator = (i) => {
                        // Generate exponential X coordinates
                        double x = Math.Pow(1.02, i) - 1 + (new Random(43 + i).NextDouble() - 0.5) * 0.5;
                        // Generate Y coordinates based on sine function
                        double y = Math.Sin(x * 0.05) * 1.5 + (new Random(43 + i).NextDouble() - 0.5) * 0.2;
                        return (x, y);
                    }
                }
            }
        },
        new DetailViewConfig 
        { 
            Title = "DetailView 2 - Exponential Functions",
            DataSeries = new List<DataSeriesConfig>
            {
                new DataSeriesConfig 
                { 
                    Label = "Exponential Decay", 
                    LineColor = Colors.Green,
                    DataGenerator = (i, _) => Math.Exp(-i * 0.005) * Math.Sin(i * 0.1) + (new Random(44).NextDouble() - 0.5) * 0.2
                },
                new DataSeriesConfig 
                { 
                    Label = "Exponential Growth (Limited)", 
                    LineColor = Colors.Orange,
                    DataGenerator = (i, _) => (1 - Math.Exp(-i * 0.01)) * 3 + (new Random(45).NextDouble() - 0.5) * 0.3
                }
            }
        },
        new DetailViewConfig 
        { 
            Title = "DetailView 3 - Random Processes",
            DataSeries = new List<DataSeriesConfig>
            {
                new DataSeriesConfig 
                { 
                    Label = "Random Walk", 
                    LineColor = Colors.Purple,
                    DataGenerator = (i, prevData) => i == 0 ? (new Random(46).NextDouble() - 0.5) * 2 : prevData[i - 1] + (new Random(46).NextDouble() - 0.5) * 0.5
                },
                new DataSeriesConfig 
                { 
                    Label = "Brownian Motion", 
                    LineColor = Colors.Magenta,
                    DataGenerator = (i, prevData) => i == 0 ? 0 : prevData[i - 1] + (new Random(47).NextDouble() - 0.5) * 0.3
                },
                new DataSeriesConfig 
                { 
                    Label = "White Noise", 
                    LineColor = Colors.Gray,
                    DataGenerator = (i, _) => (new Random(48).NextDouble() - 0.5) * 2
                }
            }
        }
    };

    public ucTimelineScope()
    {
        InitializeComponent();
        
        CreateDetailViews();
        GenerateData();
        SetupPlots();
        ChangeXAxisRangeForDetailViews(scopeStart, scopeEnd);
        ChangeSharedAxisRange(scopeStart, scopeEnd);
    }

    public void AddDetailView(DetailViewConfig config)
    {
        detailViewConfigs.Add(config);
        CreateSingleDetailView(detailViewConfigs.Count - 1);
        GenerateDataForNewView(detailViewConfigs.Count - 1);
        ChangeXAxisRangeForDetailViews(scopeStart, scopeEnd);
        ChangeSharedAxisRange(scopeStart, scopeEnd);
        LinkDetailViewAxes();
    }

    public void AddFlameGraphDetailView(string title = "Flame Graph - Execution Stack")
    {
        // Create a special DetailView for flame graph
        var flameGraphConfig = new DetailViewConfig
        {
            Title = title,
            DataSeries = new List<DataSeriesConfig>() // Empty data series for flame graph
        };

        detailViewConfigs.Add(flameGraphConfig);
        CreateSingleDetailView(detailViewConfigs.Count - 1);
        
        // Get the newly created plot
        var newPlot = detailPlots[detailPlots.Count - 1];
        
        // Create flame graph view for this plot
        var flameGraphView = new FlameGraphDetailView(newPlot);
        flameGraphViews.Add(flameGraphView);
        detailViewTypes.Add(DetailViewType.FlameGraph);
        
        // Set fixed layout to match other detail views
        PixelPadding fixedPadding = new(left: 60, right: 10, bottom: 30, top: 10);
        flameGraphView.SetFixedLayout(fixedPadding);
        
        // Update time range for the flame graph
        flameGraphView.UpdateTimeRange(scopeStart, scopeEnd);
        
        // Link axes
        LinkDetailViewAxes();
        ChangeSharedAxisRange(scopeStart, scopeEnd);
        ChangeXAxisRangeForDetailViews(scopeStart, scopeEnd);
    }

    public void RemoveDetailView(int index)
    {
        if (index < 0 || index >= detailViewConfigs.Count) return;

        // Get the plot reference before removing it
        AvaPlot plotToRemove = null;
        if (index < detailPlots.Count)
        {
            plotToRemove = detailPlots[index];
        }

        // Get the view type and config title before removing
        DetailViewType viewType = DetailViewType.Value1D;
        string configTitle = "";
        if (index < detailViewTypes.Count)
        {
            viewType = detailViewTypes[index];
        }
        if (index < detailViewConfigs.Count)
        {
            configTitle = detailViewConfigs[index].Title;
        }

        // Remove from detail view instances based on type
        switch (viewType)
        {
            case DetailViewType.Value1D:
                if (plotToRemove != null)
                {
                    var view1DIndex = value1DViews.FindIndex(v => v.GetPlot() == plotToRemove);
                    if (view1DIndex >= 0) value1DViews.RemoveAt(view1DIndex);
                }
                break;
            case DetailViewType.Value2D:
                if (plotToRemove != null)
                {
                    var view2DIndex = value2DViews.FindIndex(v => v.GetPlot() == plotToRemove);
                    if (view2DIndex >= 0) value2DViews.RemoveAt(view2DIndex);
                }
                break;
            case DetailViewType.FlameGraph:
                var flameIndex = flameGraphViews.FindIndex(v => v.GetConfig().Title == configTitle);
                if (flameIndex >= 0) flameGraphViews.RemoveAt(flameIndex);
                break;
        }

        // Remove from configurations
        detailViewConfigs.RemoveAt(index);

        // Remove UI elements
        if (index < detailBorders.Count)
        {
            DetailViewContainer.Children.Remove(detailBorders[index]);
            detailBorders.RemoveAt(index);
        }

        if (index < detailPlots.Count)
        {
            detailPlots.RemoveAt(index);
        }

        // Remove from view types
        if (index < detailViewTypes.Count)
        {
            detailViewTypes.RemoveAt(index);
        }

        // Re-link axes after removal
        LinkDetailViewAxes();
        ChangeXAxisRangeForDetailViews(scopeStart, scopeEnd);
        ChangeSharedAxisRange(scopeStart, scopeEnd);
    }

    public void GenerateNewData()
    {
        GenerateData();
        
        // Clear and recreate timeline plot
        TimelinePlot.Plot.Clear();
        
        // Recreate timeline signal
        timelineSignal = TimelinePlot.Plot.Add.Signal(timelineData);
        timelineSignal.Color = Colors.Blue;
        timelineSignal.LineWidth = 2;
        
        // Recreate scope span
        scopeSpan = TimelinePlot.Plot.Add.HorizontalSpan(scopeStart, scopeEnd);
        scopeSpan.FillColor = Color.FromHex("#3388ff44");
        scopeSpan.LineColor = Color.FromHex("#3388ff");
        scopeSpan.LineWidth = 2;
        scopeSpan.IsDraggable = true;
        scopeSpan.IsResizable = true;
        
        // Re-setup SharedXAxisPlot
        SetupSharedXAxisPlot();
        
        // Regenerate data for all detail views
        for (int i = 0; i < detailViewTypes.Count; i++)
        {
            GenerateDataForView(i);
        }
        
        // Regenerate flame graph data
        foreach (var flameGraphView in flameGraphViews)
        {
            flameGraphView.RegenerateData();
        }
        
        // Reset view
        ResetView();
    }

    public void ResetView()
    {
        // Reset scope to default
        scopeStart = 20;
        scopeEnd = 80;
        
        // Update scope span by recreating it
        TimelinePlot.Plot.Remove(scopeSpan);
        scopeSpan = TimelinePlot.Plot.Add.HorizontalSpan(scopeStart, scopeEnd);
        scopeSpan.FillColor = Color.FromHex("#3388ff44");
        scopeSpan.LineColor = Color.FromHex("#3388ff");
        scopeSpan.LineWidth = 2;
        scopeSpan.IsDraggable = true;
        scopeSpan.IsResizable = true;
        
        // Reset all plot views
        TimelinePlot.Plot.Axes.AutoScale();
        TimelinePlot.Plot.Axes.Bottom.Min = 0;
        
        ChangeXAxisRangeForDetailViews(scopeStart, scopeEnd);
        TimelinePlot.Refresh();
    }

    private void CreateDetailViews()
    {
        DetailViewContainer.Children.Clear();
        detailPlots.Clear();
        detailBorders.Clear();
        value1DViews.Clear();
        value2DViews.Clear();
        flameGraphViews.Clear();
        detailViewTypes.Clear();

        for (int i = 0; i < detailViewConfigs.Count; i++)
        {
            CreateSingleDetailView(i);
        }
    }

    private void CreateSingleDetailView(int index)
    {
        var config = detailViewConfigs[index];
        
        // Create border
        var border = new Border
        {
            BorderBrush = Avalonia.Media.Brushes.Gray,
            BorderThickness = new Thickness(1, 0, 1, 1),
            Margin = new Thickness(0, 0, 0, index == detailViewConfigs.Count - 1 ? 8 : 0),
            Height = 250 // Increased height to accommodate legend
        };

        // Create grid
        var grid = new Grid();

        // Create text block
        var textBlock = new TextBlock
        {
            Text = config.Title,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(5),
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Background = Avalonia.Media.Brushes.White,
            Padding = new Thickness(2)
        };

        // Create plot
        var plot = new AvaPlot();

        // Add to grid
        grid.Children.Add(plot);
        grid.Children.Add(textBlock);

        // Add to border
        border.Child = grid;

        // Add to container
        DetailViewContainer.Children.Add(border);

        // Store references
        detailPlots.Add(plot);
        detailBorders.Add(border);

        // Determine view type and create appropriate detail view instance
        bool has2DData = config.DataSeries.Any(s => s.Use2DArray);
        
        if (has2DData)
        {
            var view2D = new ValueDetailView2D(plot, config);
            value2DViews.Add(view2D);
            detailViewTypes.Add(DetailViewType.Value2D);
        }
        else
        {
            var view1D = new ValueDetailView1D(plot, config);
            value1DViews.Add(view1D);
            detailViewTypes.Add(DetailViewType.Value1D);
        }

        // Set fixed layout
        PixelPadding fixedPadding = new(left: 60, right: 10, bottom: 30, top: 10);
        plot.Plot.Layout.Fixed(fixedPadding);
        
        // Configure user input
        ConfigureDetailViewUserInput(plot);
    }

    private void GenerateData()
    {
        // Generate sample data with 1000 points
        int pointCount = 1000;
        xValues = new double[pointCount];
        timelineData = new double[pointCount];

        Random rand = new Random(42);
        
        for (int i = 0; i < pointCount; i++)
        {
            xValues[i] = i;
            
            // Timeline data: combination of sine wave and noise
            timelineData[i] = Math.Sin(i * 0.02) + (rand.NextDouble() - 0.5) * 0.3;
        }

        // Generate data for all detail views
        for (int i = 0; i < detailViewTypes.Count; i++)
        {
            GenerateDataForView(i);
        }
    }

    private void GenerateDataForView(int index)
    {
        if (index >= detailViewTypes.Count) return;

        int pointCount = timelineData?.Length ?? 1000;
        var viewType = detailViewTypes[index];

        switch (viewType)
        {
            case DetailViewType.Value1D:
                var view1DIndex = GetView1DIndex(index);
                if (view1DIndex >= 0 && view1DIndex < value1DViews.Count)
                {
                    value1DViews[view1DIndex].GenerateData(pointCount);
                    value1DViews[view1DIndex].UpdatePlot();
                }
                break;
            case DetailViewType.Value2D:
                var view2DIndex = GetView2DIndex(index);
                if (view2DIndex >= 0 && view2DIndex < value2DViews.Count)
                {
                    value2DViews[view2DIndex].GenerateData(pointCount);
                    value2DViews[view2DIndex].UpdatePlot();
                }
                break;
            case DetailViewType.FlameGraph:
                // Flame graphs generate their own data
                break;
        }
    }

    private void GenerateDataForNewView(int index)
    {
        GenerateDataForView(index);
    }

    private int GetView1DIndex(int detailViewIndex)
    {
        int count = 0;
        for (int i = 0; i < detailViewIndex && i < detailViewTypes.Count; i++)
        {
            if (detailViewTypes[i] == DetailViewType.Value1D)
                count++;
        }
        return count;
    }

    private int GetView2DIndex(int detailViewIndex)
    {
        int count = 0;
        for (int i = 0; i < detailViewIndex && i < detailViewTypes.Count; i++)
        {
            if (detailViewTypes[i] == DetailViewType.Value2D)
                count++;
        }
        return count;
    }

    private int GetFlameGraphIndex(int detailViewIndex)
    {
        int count = 0;
        for (int i = 0; i < detailViewIndex && i < detailViewTypes.Count; i++)
        {
            if (detailViewTypes[i] == DetailViewType.FlameGraph)
                count++;
        }
        return count;
    }

    private void SetupPlots()
    {
        SetupTimelinePlot();
        SetupSharedXAxisPlot();
        LinkDetailViewAxes();
        ConfigureUserInput();
    }

    private void SetupTimelinePlot()
    {
        // Setup Timeline Plot
        timelineSignal = TimelinePlot.Plot.Add.Signal(timelineData);
        timelineSignal.Color = Colors.Blue;
        timelineSignal.LineWidth = 2;
        
        // Add scope selection span with draggable functionality
        scopeSpan = TimelinePlot.Plot.Add.HorizontalSpan(scopeStart, scopeEnd);
        scopeSpan.FillColor = Color.FromHex("#3388ff44");
        scopeSpan.LineColor = Color.FromHex("#3388ff");
        scopeSpan.LineWidth = 2;
        scopeSpan.IsDraggable = true;
        scopeSpan.IsResizable = true;
        
        // Configure Timeline plot axes
        TimelinePlot.Plot.Axes.SetLimitsX(0, timelineData.Length - 1);
        TimelinePlot.Plot.Axes.AutoScale();
        
        // Lock Y-axis for timeline and set X minimum to 0
        TimelinePlot.Plot.Axes.Bottom.Min = 0;
    }

    private void SetupSharedXAxisPlot()
    {
        SharedXAxisPlot.Plot.Clear();
        SharedXAxisPlot.Plot.Axes.SetLimitsX(scopeStart, scopeEnd);
        
        // Hide everything except the bottom X-axis for SharedXAxisPlot
        SharedXAxisPlot.Plot.Axes.Left.IsVisible = false;
        SharedXAxisPlot.Plot.Axes.Right.IsVisible = false;
        SharedXAxisPlot.Plot.Axes.Top.IsVisible = false;
        SharedXAxisPlot.Plot.Grid.IsVisible = false;
        
        // Set fixed padding for proper alignment
        PixelPadding fixedPadding = new(left: 60, right: 10, bottom: 30, top: 10);
        SharedXAxisPlot.Plot.Layout.Fixed(fixedPadding);
        
        SharedXAxisPlot.PointerWheelChanged += SharedXAxisPlotOnPointerWheelChanged;
        SharedXAxisPlot.PointerMoved += SharedXAxisPlotOnPointerMoved;
    }

    private void SharedXAxisPlotOnPointerMoved(object? sender, PointerEventArgs e)
    {
        var properties = e.GetCurrentPoint(null).Properties;
        if (properties.IsLeftButtonPressed)
        {
            ChangeXAxisBySharedAxisDraged();
        }
    }

    private void SharedXAxisPlotOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        ChangeXAxisBySharedAxisDraged();
    }

    private void ChangeXAxisBySharedAxisDraged()
    {
        scopeStart = SharedXAxisPlot.Plot.Axes.Bottom.Min;
        scopeEnd = SharedXAxisPlot.Plot.Axes.Bottom.Max;
        
        ChangeTimelineSpanRange(scopeStart, scopeEnd);
        ChangeXAxisRangeForDetailViews(scopeStart, scopeEnd);
    }

    private void LinkDetailViewAxes()
    {
        if (detailPlots.Count == 0) return;

        // Link all detail plots to the SharedXAxisPlot
        for (int i = 0; i < detailPlots.Count; i++)
        {
            detailPlots[i].Plot.Axes.Link(SharedXAxisPlot, x: true, y: false);
        }
    }

    private void ConfigureUserInput()
    {
        // Add mouse interaction for scope selection
        TimelinePlot.PointerPressed += OnTimelineMouseDown;
        TimelinePlot.PointerReleased += OnTimelineMouseUp;
        TimelinePlot.PointerMoved += OnTimelineMouseMove;
        
        // Configure user input for timeline (disable Y-axis panning)
        TimelinePlot.UserInputProcessor.Reset();
        TimelinePlot.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseDragPan>();
        
        // Add left click drag pan with vertical lock
        var panButton = ScottPlot.Interactivity.StandardMouseButtons.Left;
        var panResponse = new ScottPlot.Interactivity.UserActionResponses.MouseDragPan(panButton);
        panResponse.LockY = true;
        TimelinePlot.UserInputProcessor.UserActionResponses.Add(panResponse);
        
        // Refresh all plots
        TimelinePlot.Refresh();
        foreach (var plot in detailPlots)
        {
            plot.Refresh();
        }
    }

    private AxisSpanUnderMouse? SpanBeingDragged = null;

    private void OnTimelineMouseDown(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(TimelinePlot);
        var spanUnderMouse = GetSpanUnderMouse((float)pos.X, (float)pos.Y);
        if (spanUnderMouse is not null)
        {
            SpanBeingDragged = spanUnderMouse;
            TimelinePlot.UserInputProcessor.Disable(); // disable panning while dragging
        }
    }

    private void OnTimelineMouseUp(object? sender, PointerEventArgs e)
    {
        if (SpanBeingDragged is not null)
        {
            // Update scope values from the span
            scopeStart = Math.Max(0, scopeSpan.X1); // Ensure X >= 0
            scopeEnd = scopeSpan.X2;
            
            // Ensure we don't go beyond data bounds
            if (scopeEnd > timelineData.Length - 1)
            {
                scopeEnd = timelineData.Length - 1;
            }
            if (scopeStart < 0)
            {
                scopeStart = 0;
            }
            
            ChangeXAxisRangeForDetailViews(scopeStart, scopeEnd);
            ChangeSharedAxisRange(scopeStart, scopeEnd);
        }
        
        SpanBeingDragged = null;
        TimelinePlot.UserInputProcessor.Enable(); // enable panning
        TimelinePlot.Refresh();
    }

    private void OnTimelineMouseMove(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(TimelinePlot);
        if (SpanBeingDragged is not null)
        {
            // currently dragging something so update it
            Coordinates mouseNow = TimelinePlot.Plot.GetCoordinates(new Pixel(pos.X, pos.Y));
            SpanBeingDragged.DragTo(mouseNow);
            
            // Update scope values and detail views in real-time
            scopeStart = Math.Max(0, scopeSpan.X1); // Ensure X >= 0
            scopeEnd = scopeSpan.X2;
            
            // Ensure we don't go beyond data bounds
            if (scopeEnd > timelineData.Length - 1)
            {
                scopeEnd = timelineData.Length - 1;
            }
            if (scopeStart < 0)
            {
                scopeStart = 0;
            }
            
            ChangeXAxisRangeForDetailViews(scopeStart, scopeEnd);
            ChangeSharedAxisRange(scopeStart, scopeEnd);
            TimelinePlot.Refresh();
        }
        else
        {
            // not dragging anything so just set the cursor based on what's under the mouse
            var spanUnderMouse = GetSpanUnderMouse((float)pos.X, (float)pos.Y);
            if (spanUnderMouse is null) Cursor = new(StandardCursorType.Arrow);
            else if (spanUnderMouse.IsResizingHorizontally) Cursor = new(StandardCursorType.SizeWestEast);
            else if (spanUnderMouse.IsResizingVertically) Cursor = new(StandardCursorType.SizeNorthSouth);
            else if (spanUnderMouse.IsMoving) Cursor = new(StandardCursorType.SizeAll);
        }
    }

    private AxisSpanUnderMouse? GetSpanUnderMouse(float x, float y)
    {
        CoordinateRect rect = TimelinePlot.Plot.GetCoordinateRect(x, y, radius: 10);

        foreach (AxisSpan span in TimelinePlot.Plot.GetPlottables<AxisSpan>().Reverse())
        {
            AxisSpanUnderMouse? spanUnderMouse = span.UnderMouse(rect);
            if (spanUnderMouse is not null)
                return spanUnderMouse;
        }

        return null;
    }

    private void ConfigureDetailViewUserInput(AvaPlot plot)
    {
        // Lock DetailView X-axis using AxisRules - only allow control through SharedXAxis
        // Get current X-axis limits to lock them
        AxisLimits currentLimits = plot.Plot.Axes.GetLimits();
        
        // Add a LockedHorizontal rule to prevent X-axis changes
        var lockedHorizontalRule = new ScottPlot.AxisRules.LockedHorizontal(
            plot.Plot.Axes.Bottom, 
            currentLimits.Left, 
            currentLimits.Right);
        
        plot.Plot.Axes.Rules.Add(lockedHorizontalRule);
    }

    private void ChangeSharedAxisRange(double xMin, double xMax)
    {
        SharedXAxisPlot.Plot.Axes.SetLimitsX(xMin, xMax);
        SharedXAxisPlot.Refresh();
    }
    
    private void ChangeXAxisRangeForDetailViews(double xMin, double xMax)
    {
        // Update X-axis limits for all detail views based on scope selection
        for (int i = 0; i < detailViewTypes.Count; i++)
        {
            var viewType = detailViewTypes[i];
            
            switch (viewType)
            {
                case DetailViewType.Value1D:
                    var view1DIndex = GetView1DIndex(i);
                    if (view1DIndex >= 0 && view1DIndex < value1DViews.Count)
                    {
                        value1DViews[view1DIndex].UpdateXAxisRange(xMin, xMax);
                    }
                    break;
                case DetailViewType.Value2D:
                    var view2DIndex = GetView2DIndex(i);
                    if (view2DIndex >= 0 && view2DIndex < value2DViews.Count)
                    {
                        value2DViews[view2DIndex].UpdateXAxisRange(xMin, xMax);
                    }
                    break;
                case DetailViewType.FlameGraph:
                    var flameIndex = GetFlameGraphIndex(i);
                    if (flameIndex >= 0 && flameIndex < flameGraphViews.Count)
                    {
                        flameGraphViews[flameIndex].UpdateTimeRange(xMin, xMax);
                    }
                    break;
            }
        }
    }

    private void ChangeTimelineSpanRange(double xMin, double xMax)
    {
        scopeSpan.X1 = xMin;
        scopeSpan.X2 = xMax;
        TimelinePlot.Refresh();
    }
}
