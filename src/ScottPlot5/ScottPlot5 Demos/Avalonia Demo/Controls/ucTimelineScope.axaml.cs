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
    // Data series configuration for a single DetailView
    public class DataSeriesConfig
    {
        public string Label { get; set; } = "";
        public Color LineColor { get; set; } = Colors.Blue;
        public Func<int, double[], double> DataGenerator { get; set; } = (i, _) => 0;
        public Func<int, (double x, double y)>? XYDataGenerator { get; set; } = null; // For 2D array data with custom X,Y coordinates
        public bool Use2DArray { get; set; } = false; // Flag to indicate if this series uses 2D array data
    }

    // DetailView configuration that can contain multiple data series
    public class DetailViewConfig
    {
        public string Title { get; set; } = "";
        public List<DataSeriesConfig> DataSeries { get; set; } = new();
    }

    // Data arrays for the plots
    private double[] timelineData;
    private double[] xValues;
    private List<List<double[]>> detailDataArrays = new(); // [DetailView][DataSeries][DataPoints]
    private List<List<(double[] x, double[] y)>> detail2DDataArrays = new(); // [DetailView][DataSeries][(X,Y)]

    // Plottable objects
    private Signal timelineSignal;
    private List<List<Signal>> detailSignals = new(); // [DetailView][DataSeries]
    private List<List<Scatter>> detailScatters = new(); // [DetailView][DataSeries] for 2D array data
    private HorizontalSpan scopeSpan;

    // UI elements arrays
    private List<AvaPlot> detailPlots = new();
    private List<Border> detailBorders = new();

    // Mouse hover functionality
    private List<List<Crosshair>> detailCrosshairs = new(); // [DetailView][DataSeries]
    private List<Tooltip> detailTooltips = new();

    // Scope selection variables
    private double scopeStart = 20;
    private double scopeEnd = 80;

    // Flame graph detail views
    private List<FlameGraphDetailView> flameGraphViews = new();

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
        RegenerateDataForNewView();
        SetupNewDetailView(detailPlots.Count - 1);
        ChangeXAxisRangeForDetailViews(scopeStart, scopeEnd);
        ChangeSharedAxisRange(scopeStart, scopeEnd);
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
        
        // Set fixed layout to match other detail views
        PixelPadding fixedPadding = new(left: 60, right: 10, bottom: 30, top: 10);
        flameGraphView.SetFixedLayout(fixedPadding);
        
        // Update time range for the flame graph
        flameGraphView.UpdateTimeRange(scopeStart, scopeEnd);
        
        // Link axes
        LinkDetailViewAxes();
        ChangeSharedAxisRange(scopeStart, scopeEnd);
    }

    public void RemoveDetailView(int index)
    {
        if (index < 0 || index >= detailViewConfigs.Count) return;

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

        if (index < detailSignals.Count)
        {
            detailSignals.RemoveAt(index);
        }

        if (index < detailDataArrays.Count)
        {
            detailDataArrays.RemoveAt(index);
        }

        // Re-link axes after removal
        LinkDetailViewAxes();
        ChangeXAxisRangeForDetailViews(scopeStart, scopeEnd);
        ChangeSharedAxisRange(scopeStart, scopeEnd);
    }

    public void GenerateNewData()
    {
        GenerateData();
        
        // Clear and recreate plots with new data
        TimelinePlot.Plot.Clear();
        foreach (var plot in detailPlots)
        {
            plot.Plot.Clear();
        }
        
        // Clear crosshairs and tooltips lists
        detailCrosshairs.Clear();
        detailTooltips.Clear();
        
        // Recreate timeline signal
        timelineSignal = TimelinePlot.Plot.Add.Signal(timelineData);
        timelineSignal.Color = Colors.Blue;
        timelineSignal.LineWidth = 2;
        
        // Recreate detail signals
        detailSignals.Clear();
        for (int i = 0; i < detailPlots.Count; i++)
        {
            var seriesSignals = new List<Signal>();
            var config = detailViewConfigs[i];
            
            for (int j = 0; j < config.DataSeries.Count; j++)
            {
                var signal = detailPlots[i].Plot.Add.Signal(detailDataArrays[i][j]);
                signal.Color = config.DataSeries[j].LineColor;
                signal.LineWidth = 2;
                signal.LegendText = config.DataSeries[j].Label;
                seriesSignals.Add(signal);
            }
            
            detailSignals.Add(seriesSignals);
            
            // Show legend for this DetailView
            detailPlots[i].Plot.ShowLegend();
        }
        
        // Recreate scope span
        scopeSpan = TimelinePlot.Plot.Add.HorizontalSpan(scopeStart, scopeEnd);
        scopeSpan.FillColor = Color.FromHex("#3388ff44");
        scopeSpan.LineColor = Color.FromHex("#3388ff");
        scopeSpan.LineWidth = 2;
        scopeSpan.IsDraggable = true;
        scopeSpan.IsResizable = true;
        
        // Re-setup SharedXAxisPlot
        SetupSharedXAxisPlot();
        
        // Re-setup all detail plots
        SetupAllDetailPlots();
        
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
    }

    private void GenerateData()
    {
        // Generate sample data with 1000 points
        int pointCount = 1000;
        xValues = new double[pointCount];
        timelineData = new double[pointCount];
        detailDataArrays.Clear();
        detail2DDataArrays.Clear();

        // Initialize data arrays for each detail view and its data series
        for (int i = 0; i < detailViewConfigs.Count; i++)
        {
            var detailViewData = new List<double[]>();
            var detail2DViewData = new List<(double[] x, double[] y)>();
            var config = detailViewConfigs[i];
            
            for (int j = 0; j < config.DataSeries.Count; j++)
            {
                var seriesConfig = config.DataSeries[j];
                if (seriesConfig.Use2DArray && seriesConfig.XYDataGenerator != null)
                {
                    // For 2D array data, create separate X and Y arrays
                    detail2DViewData.Add((new double[pointCount], new double[pointCount]));
                    detailViewData.Add(new double[pointCount]); // Still need this for compatibility
                }
                else
                {
                    detailViewData.Add(new double[pointCount]);
                    detail2DViewData.Add((new double[0], new double[0])); // Empty for non-2D data
                }
            }
            
            detailDataArrays.Add(detailViewData);
            detail2DDataArrays.Add(detail2DViewData);
        }

        Random rand = new Random(42);
        
        for (int i = 0; i < pointCount; i++)
        {
            xValues[i] = i;
            
            // Timeline data: combination of sine wave and noise
            timelineData[i] = Math.Sin(i * 0.02) + (rand.NextDouble() - 0.5) * 0.3;
            
            // Generate data for each detail view and its data series
            for (int detailIndex = 0; detailIndex < detailViewConfigs.Count; detailIndex++)
            {
                var config = detailViewConfigs[detailIndex];
                for (int seriesIndex = 0; seriesIndex < config.DataSeries.Count; seriesIndex++)
                {
                    var seriesConfig = config.DataSeries[seriesIndex];
                    
                    if (seriesConfig.Use2DArray && seriesConfig.XYDataGenerator != null)
                    {
                        // Generate 2D array data with custom X,Y coordinates
                        var (x, y) = seriesConfig.XYDataGenerator(i);
                        detail2DDataArrays[detailIndex][seriesIndex].x[i] = x;
                        detail2DDataArrays[detailIndex][seriesIndex].y[i] = y;
                        detailDataArrays[detailIndex][seriesIndex][i] = y; // Store Y for compatibility
                    }
                    else
                    {
                        // Generate regular 1D data
                        detailDataArrays[detailIndex][seriesIndex][i] = seriesConfig.DataGenerator(i, detailDataArrays[detailIndex][seriesIndex]);
                    }
                }
            }
        }
    }

    private void RegenerateDataForNewView()
    {
        if (detailDataArrays.Count < detailViewConfigs.Count)
        {
            int pointCount = timelineData?.Length ?? 1000;
            var config = detailViewConfigs[detailViewConfigs.Count - 1];
            var detailViewData = new List<double[]>();
            
            for (int j = 0; j < config.DataSeries.Count; j++)
            {
                var newData = new double[pointCount];
                var seriesConfig = config.DataSeries[j];
                
                for (int i = 0; i < pointCount; i++)
                {
                    newData[i] = seriesConfig.DataGenerator(i, newData);
                }
                
                detailViewData.Add(newData);
            }
            
            detailDataArrays.Add(detailViewData);
        }
    }

    private void SetupPlots()
    {
        SetupTimelinePlot();
        SetupSharedXAxisPlot();
        SetupAllDetailPlots();
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
        // throw new NotImplementedException();
        ChangeXAxisBySharedAxisDraged();
    }

    private void ChangeXAxisBySharedAxisDraged()
    {
        scopeStart = SharedXAxisPlot.Plot.Axes.Bottom.Min;
        scopeEnd = SharedXAxisPlot.Plot.Axes.Bottom.Max;
        
        ChangeTimelineSpanRange(scopeStart, scopeEnd);
        ChangeXAxisRangeForDetailViews(scopeStart, scopeEnd);
    }

    private void SetupAllDetailPlots()
    {
        detailSignals.Clear();
        PixelPadding fixedPadding = new(left: 60, right: 10, bottom: 30, top: 10);

        for (int i = 0; i < detailPlots.Count; i++)
        {
            SetupSingleDetailPlot(i, fixedPadding);
        }
    }

    private void SetupSingleDetailPlot(int index, PixelPadding fixedPadding)
    {
        var plot = detailPlots[index];
        var config = detailViewConfigs[index];
        var detailViewData = detailDataArrays[index];

        // Setup detail plot signals/scatters for each data series
        var seriesSignals = new List<Signal>();
        var seriesScatters = new List<Scatter>();
        
        for (int j = 0; j < config.DataSeries.Count; j++)
        {
            var seriesConfig = config.DataSeries[j];
            
            if (seriesConfig.Use2DArray && detail2DDataArrays.Count > index && detail2DDataArrays[index].Count > j)
            {
                // Use Scatter plot for 2D array data
                var xData = detail2DDataArrays[index][j].x;
                var yData = detail2DDataArrays[index][j].y;
                
                var scatter = plot.Plot.Add.Scatter(xData, yData);
                scatter.Color = seriesConfig.LineColor;
                scatter.LineWidth = 2;
                scatter.MarkerSize = 0; // No markers, just lines
                scatter.LegendText = seriesConfig.Label;
                seriesScatters.Add(scatter);
                
                // Add empty signal for compatibility
                seriesSignals.Add(null!);
            }
            else
            {
                // Use Signal plot for regular 1D data
                var data = detailViewData[j];
                
                var signal = plot.Plot.Add.Signal(data);
                signal.Color = seriesConfig.LineColor;
                signal.LineWidth = 2;
                signal.LegendText = seriesConfig.Label;
                seriesSignals.Add(signal);
                
                // Add empty scatter for compatibility
                seriesScatters.Add(null!);
            }
        }
        
        detailSignals.Add(seriesSignals);
        
        // Ensure we have enough scatter lists
        if (detailScatters.Count <= index)
        {
            while (detailScatters.Count <= index)
            {
                detailScatters.Add(new List<Scatter>());
            }
        }
        detailScatters[index] = seriesScatters;

        // Add crosshairs for mouse tracking - one for each data series
        var seriesCrosshairs = new List<Crosshair>();
        for (int j = 0; j < config.DataSeries.Count; j++)
        {
            var crosshair = plot.Plot.Add.Crosshair(0, 0);
            crosshair.IsVisible = false;
            crosshair.MarkerShape = MarkerShape.OpenCircle;
            crosshair.MarkerSize = 8;
            crosshair.LineColor = config.DataSeries[j].LineColor;
            crosshair.MarkerColor = config.DataSeries[j].LineColor;
            crosshair.LineWidth = 1;
            crosshair.LinePattern = LinePattern.Dashed;
            seriesCrosshairs.Add(crosshair);
        }
        
        // Ensure we have enough crosshairs for all detail views
        if (detailCrosshairs.Count <= index)
        {
            while (detailCrosshairs.Count <= index)
            {
                detailCrosshairs.Add(new List<Crosshair>());
            }
        }
        detailCrosshairs[index] = seriesCrosshairs;

        // Add tooltip for displaying data values
        var tooltip = plot.Plot.Add.Tooltip(new Coordinates(0, 0), "", new Coordinates(0, 0));
        tooltip.IsVisible = false;
        tooltip.FillColor = Color.FromHex("#ffffcc");
        tooltip.LineColor = Colors.Black;
        tooltip.LineWidth = 1;
        tooltip.LabelFontSize = 10;
        tooltip.LabelFontColor = Colors.Black;
        
        // Ensure we have enough tooltips for all detail views
        if (detailTooltips.Count <= index)
        {
            while (detailTooltips.Count <= index)
            {
                detailTooltips.Add(null!);
            }
        }
        detailTooltips[index] = tooltip;

        // Show legend for this DetailView
        plot.Plot.ShowLegend();
        
        // Position legend in the upper right corner
        plot.Plot.Legend.Alignment = Alignment.UpperRight;
        plot.Plot.Legend.BackgroundColor = Color.FromHex("#ffffff");
        plot.Plot.Legend.OutlineColor = Color.FromHex("#cccccc");

        // Hide X-axis for detail plot
        plot.Plot.Axes.Bottom.IsVisible = false;
        
        // Set the same fixed padding for alignment
        plot.Plot.Layout.Fixed(fixedPadding);
        
        // Add mouse move event handler for this detail plot
        plot.PointerMoved += (sender, e) => OnDetailPlotMouseMove(sender, e, index);
        plot.PointerExited += (sender, e) => OnDetailPlotMouseExit(sender, e, index);
        
        // Lock DetailView X-axis interactions - only allow control through SharedXAxis
        ConfigureDetailViewUserInput(plot);
    }

    private void SetupNewDetailView(int index)
    {
        PixelPadding fixedPadding = new(left: 60, right: 10, bottom: 30, top: 10);
        SetupSingleDetailPlot(index, fixedPadding);
        LinkDetailViewAxes();
    }

    private void LinkDetailViewAxes()
    {
        if (detailPlots.Count == 0) return;

        // Link all detail plots to the first one
        for (int i = 1; i < detailPlots.Count; i++)
        {
            SharedXAxisPlot.Plot.Axes.Link(detailPlots[0], x: true, y: false);
            //detailPlots[0].Plot.Axes.Link(detailPlots[i], x: true, y: false);
        }
        
        // Link SharedXAxisPlot to the first detail plot
        SharedXAxisPlot.Plot.Axes.Link(detailPlots[0], x: true, y: false);
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

    private void ChangeDetailPlotXAxisRange(AvaPlot plot, double xMin, double xMax)
    {
        // Use a safer approach that doesn't modify rules during rendering
        // First, set the limits directly
        plot.Plot.Axes.SetLimitsX(xMin, xMax);
        plot.Plot.Axes.AutoScaleY();
        
        // Find and update existing LockedHorizontal rule instead of clearing and re-adding
        var existingRule = plot.Plot.Axes.Rules.OfType<ScottPlot.AxisRules.LockedHorizontal>().FirstOrDefault();
        if (existingRule != null)
        {
            // Remove the old rule safely
            var rulesList = plot.Plot.Axes.Rules.ToList();
            rulesList.Remove(existingRule);
            plot.Plot.Axes.Rules.Clear();
            
            // Add updated rule
            var newRule = new ScottPlot.AxisRules.LockedHorizontal(
                plot.Plot.Axes.Bottom, 
                xMin, 
                xMax);
            
            // Add all rules back
            foreach (var rule in rulesList)
            {
                plot.Plot.Axes.Rules.Add(rule);
            }
            plot.Plot.Axes.Rules.Add(newRule);
        }
        else
        {
            // Add new rule if none exists
            var lockedHorizontalRule = new ScottPlot.AxisRules.LockedHorizontal(
                plot.Plot.Axes.Bottom, 
                xMin, 
                xMax);
            plot.Plot.Axes.Rules.Add(lockedHorizontalRule);
        }
        
        plot.Refresh();
    }
    
    private void ChangeXAxisRangeForDetailViews(double xMin, double xMax)
    {
        // Update X-axis limits for all detail views and shared X-axis based on scope selection
        foreach (var plot in detailPlots)
        {
            ChangeDetailPlotXAxisRange(plot, xMin, xMax);
        }

        // Update flame graph views time range
        foreach (var flameGraphView in flameGraphViews)
        {
            flameGraphView.UpdateTimeRange(xMin, xMax);
        }

        // ChangeSharedAxisRange(xMin, xMax);
    }

    private void ChangeTimelineSpanRange(double xMin, double xMax)
    {
        scopeSpan.X1 = xMin;
        scopeSpan.X2 = xMax;
        TimelinePlot.Refresh();
    }

    private void OnDetailPlotMouseMove(object? sender, PointerEventArgs e, int plotIndex)
    {
        if (plotIndex >= detailPlots.Count || plotIndex >= detailCrosshairs.Count || plotIndex >= detailTooltips.Count)
            return;

        var plot = detailPlots[plotIndex];
        var crosshairs = detailCrosshairs[plotIndex];
        var tooltip = detailTooltips[plotIndex];
        var config = detailViewConfigs[plotIndex];

        // Get mouse position and convert to coordinates
        var pos = e.GetPosition(plot);
        Pixel mousePixel = new(pos.X, pos.Y);
        Coordinates mouseLocation = plot.Plot.GetCoordinates(mousePixel);

        // Find the nearest data points for all series in this DetailView
        var nearestPoints = new List<(DataPoint point, string seriesLabel, Color seriesColor, int seriesIndex)>();
        double maxDistance = 15; // Maximum distance to consider a point "near"

        for (int seriesIndex = 0; seriesIndex < config.DataSeries.Count; seriesIndex++)
        {
            var seriesConfig = config.DataSeries[seriesIndex];
            DataPoint nearest = new();
            
            if (seriesConfig.Use2DArray && plotIndex < detailScatters.Count && seriesIndex < detailScatters[plotIndex].Count)
            {
                // Handle 2D array data (Scatter plot)
                var scatter = detailScatters[plotIndex][seriesIndex];
                if (scatter != null)
                {
                    nearest = scatter.GetNearestX(mouseLocation, plot.Plot.LastRender, (float)maxDistance);
                }
            }
            else if (plotIndex < detailSignals.Count && seriesIndex < detailSignals[plotIndex].Count)
            {
                // Handle regular 1D data (Signal plot)
                var signal = detailSignals[plotIndex][seriesIndex];
                if (signal != null)
                {
                    nearest = signal.GetNearestX(mouseLocation, plot.Plot.LastRender, (float)maxDistance);
                }
            }
            
            if (nearest.IsReal)
            {
                nearestPoints.Add((nearest, seriesConfig.Label, seriesConfig.LineColor, seriesIndex));
            }
        }

        if (nearestPoints.Count > 0)
        {
            // Show crosshairs for all nearby points
            for (int i = 0; i < crosshairs.Count; i++)
            {
                var crosshair = crosshairs[i];
                var nearestPoint = nearestPoints.FirstOrDefault(p => p.seriesIndex == i);
                
                if (nearestPoint.point.IsReal)
                {
                    crosshair.IsVisible = true;
                    crosshair.Position = nearestPoint.point.Coordinates;
                    crosshair.MarkerColor = nearestPoint.seriesColor;
                }
                else
                {
                    crosshair.IsVisible = false;
                }
            }

            // Find the closest point among all series for tooltip positioning
            var closestPoint = nearestPoints.OrderBy(p => Math.Abs(p.point.X - mouseLocation.X)).First();

            // Build tooltip text with all nearby points
            var tooltipText = new StringBuilder();
            tooltipText.AppendLine($"X: {closestPoint.point.X:F2}");
            tooltipText.AppendLine();

            foreach (var (point, label, color, _) in nearestPoints.OrderBy(p => p.seriesLabel))
            {
                tooltipText.AppendLine($"{label}: {point.Y:F3}");
            }

            // Position tooltip near the mouse but offset to avoid covering data
            var tooltipPosition = new Coordinates(
                closestPoint.point.X + (mouseLocation.X > closestPoint.point.X ? 5 : -5),
                closestPoint.point.Y + 0.1 * (plot.Plot.Axes.GetLimits().Top - plot.Plot.Axes.GetLimits().Bottom)
            );

            tooltip.LabelText = tooltipText.ToString().Trim();
            tooltip.TipLocation = closestPoint.point.Coordinates;
            tooltip.LabelLocation = tooltipPosition;
            tooltip.IsVisible = true;

            plot.Refresh();
        }
        else
        {
            // Hide all crosshairs and tooltip when no point is near
            bool needsRefresh = false;
            
            foreach (var crosshair in crosshairs)
            {
                if (crosshair.IsVisible)
                {
                    crosshair.IsVisible = false;
                    needsRefresh = true;
                }
            }
            
            if (tooltip.IsVisible)
            {
                tooltip.IsVisible = false;
                needsRefresh = true;
            }
            
            if (needsRefresh)
            {
                plot.Refresh();
            }
        }
    }

    private void OnDetailPlotMouseExit(object? sender, PointerEventArgs e, int plotIndex)
    {
        if (plotIndex >= detailCrosshairs.Count || plotIndex >= detailTooltips.Count)
            return;

        var crosshairs = detailCrosshairs[plotIndex];
        var tooltip = detailTooltips[plotIndex];
        var plot = detailPlots[plotIndex];

        // Hide all crosshairs and tooltip when mouse exits the plot
        bool needsRefresh = false;
        
        foreach (var crosshair in crosshairs)
        {
            if (crosshair.IsVisible)
            {
                crosshair.IsVisible = false;
                needsRefresh = true;
            }
        }
        
        if (tooltip.IsVisible)
        {
            tooltip.IsVisible = false;
            needsRefresh = true;
        }
        
        if (needsRefresh)
        {
            plot.Refresh();
        }
    }
}
