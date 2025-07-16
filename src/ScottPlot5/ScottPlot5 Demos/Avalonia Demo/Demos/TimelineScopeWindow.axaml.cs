using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia_Demo.ViewModels.Demos;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.ComponentModel;
using System.Linq;

namespace Avalonia_Demo.Demos;

public class TimelineScopeDemo : IDemo
{
    public string Title => "Timeline Scope Control";
    public string Description => "Demonstrates a timeline with draggable scope selection that controls multiple synchronized detail views";

    public Window GetWindow()
    {
        return new TimelineScopeWindow();
    }
}

public partial class TimelineScopeWindow : Window
{
    private TimelineScopeViewModel TypedDataContext => (DataContext as TimelineScopeViewModel) ?? throw new ArgumentException(nameof(DataContext));

    // Data arrays for the plots
    private double[] timelineData;
    private double[] detailData1;
    private double[] detailData2;
    private double[] detailData3;
    private double[] detailData4;
    private double[] detailData5;
    private double[] xValues;

    // Plottable objects
    private Signal timelineSignal;
    private Signal detailSignal1;
    private Signal detailSignal2;
    private Signal detailSignal3;
    private Signal detailSignal4;
    private Signal detailSignal5;
    private HorizontalSpan scopeSpan;

    // Scope selection variables
    private double scopeStart = 20;
    private double scopeEnd = 80;

    public TimelineScopeWindow()
    {
        InitializeComponent();
        DataContext = new TimelineScopeViewModel();
        
        GenerateData();
        SetupPlots();
        UpdateDetailViews();
    }

    private void GenerateData()
    {
        // Generate sample data with 1000 points
        int pointCount = 1000;
        xValues = new double[pointCount];
        timelineData = new double[pointCount];
        detailData1 = new double[pointCount];
        detailData2 = new double[pointCount];
        detailData3 = new double[pointCount];
        detailData4 = new double[pointCount];
        detailData5 = new double[pointCount];

        Random rand = new Random(42);
        
        for (int i = 0; i < pointCount; i++)
        {
            xValues[i] = i;
            
            // Timeline data: combination of sine wave and noise
            timelineData[i] = Math.Sin(i * 0.02) + (rand.NextDouble() - 0.5) * 0.3;
            
            // Detail data 1: cosine wave with different frequency
            detailData1[i] = Math.Cos(i * 0.05) * 2 + (rand.NextDouble() - 0.5) * 0.5;
            
            // Detail data 2: exponential decay with noise
            detailData2[i] = Math.Exp(-i * 0.005) * Math.Sin(i * 0.1) + (rand.NextDouble() - 0.5) * 0.2;
            
            // Detail data 3: random walk
            if (i == 0)
                detailData3[i] = (rand.NextDouble() - 0.5) * 2;
            else
                detailData3[i] = detailData3[i - 1] + (rand.NextDouble() - 0.5) * 0.5;
            
            // Detail data 4: sine + cosine combination
            detailData4[i] = Math.Sin(i * 0.03) + Math.Cos(i * 0.07) * 0.5 + (rand.NextDouble() - 0.5) * 0.3;
            
            // Detail data 5: polynomial function with noise
            double x = i * 0.01;
            detailData5[i] = x * x * x - 3 * x * x + 2 * x + (rand.NextDouble() - 0.5) * 0.4;
        }
    }

    private void SetupPlots()
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
        
        // Setup Detail Plot 1
        detailSignal1 = DetailPlot1.Plot.Add.Signal(detailData1);
        detailSignal1.Color = Colors.Red;
        detailSignal1.LineWidth = 2;
        
        // Setup Detail Plot 2
        detailSignal2 = DetailPlot2.Plot.Add.Signal(detailData2);
        detailSignal2.Color = Colors.Green;
        detailSignal2.LineWidth = 2;
        
        // Setup Detail Plot 3
        detailSignal3 = DetailPlot3.Plot.Add.Signal(detailData3);
        detailSignal3.Color = Colors.Purple;
        detailSignal3.LineWidth = 2;
        
        // Setup Detail Plot 4
        detailSignal4 = DetailPlot4.Plot.Add.Signal(detailData4);
        detailSignal4.Color = Colors.Orange;
        detailSignal4.LineWidth = 2;
        
        // Setup Detail Plot 5
        detailSignal5 = DetailPlot5.Plot.Add.Signal(detailData5);
        detailSignal5.Color = Colors.Brown;
        detailSignal5.LineWidth = 2;
        
        // Link the X-axes of all detail plots
        DetailPlot1.Plot.Axes.Link(DetailPlot2, x: true, y: false);
        DetailPlot1.Plot.Axes.Link(DetailPlot3, x: true, y: false);
        DetailPlot1.Plot.Axes.Link(DetailPlot4, x: true, y: false);
        DetailPlot1.Plot.Axes.Link(DetailPlot5, x: true, y: false);
        
        // Add mouse interaction for scope selection using built-in functionality
        TimelinePlot.PointerPressed += OnTimelineMouseDown;
        TimelinePlot.PointerReleased += OnTimelineMouseUp;
        TimelinePlot.PointerMoved += OnTimelineMouseMove;
        
        // Configure DetailView plots to use mouse wheel for ScrollViewer instead of zooming
        DetailPlot1.PointerWheelChanged += HandleDetailViewMouseWheel;
        DetailPlot2.PointerWheelChanged += HandleDetailViewMouseWheel;
        DetailPlot3.PointerWheelChanged += HandleDetailViewMouseWheel;
        DetailPlot4.PointerWheelChanged += HandleDetailViewMouseWheel;
        DetailPlot5.PointerWheelChanged += HandleDetailViewMouseWheel;
        
        // Remove mouse wheel zoom from DetailView plots
        DetailPlot1.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        DetailPlot2.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        DetailPlot3.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        DetailPlot4.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        DetailPlot5.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        
        // Configure user input for timeline (disable Y-axis panning)
        // Start with default responses and then modify
        TimelinePlot.UserInputProcessor.Reset();
        
        // Remove the default pan response and add our custom one with Y-lock
        TimelinePlot.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseDragPan>();
        
        // Add left click drag pan with vertical lock
        var panButton = ScottPlot.Interactivity.StandardMouseButtons.Left;
        var panResponse = new ScottPlot.Interactivity.UserActionResponses.MouseDragPan(panButton);
        panResponse.LockY = true;
        TimelinePlot.UserInputProcessor.UserActionResponses.Add(panResponse);
        
        // Refresh all plots
        TimelinePlot.Refresh();
        DetailPlot1.Refresh();
        DetailPlot2.Refresh();
        DetailPlot3.Refresh();
        DetailPlot4.Refresh();
        DetailPlot5.Refresh();
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
            
            UpdateDetailViews();
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
            
            UpdateDetailViews();
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

    private void HandleDetailViewMouseWheel(object? sender, PointerWheelEventArgs e)
    {
        // Don't let the DetailView plots handle mouse wheel events for zooming.
        // Instead, let the ScrollViewer handle them for scrolling.
        // Set e.Handled = false to allow the event to bubble up to ScrollViewer
        e.Handled = false;
    }

    private void UpdateDetailViews()
    {
        // Set the X-axis limits for all detail views based on scope selection
        DetailPlot1.Plot.Axes.SetLimitsX(scopeStart, scopeEnd);
        DetailPlot2.Plot.Axes.SetLimitsX(scopeStart, scopeEnd);
        DetailPlot3.Plot.Axes.SetLimitsX(scopeStart, scopeEnd);
        DetailPlot4.Plot.Axes.SetLimitsX(scopeStart, scopeEnd);
        DetailPlot5.Plot.Axes.SetLimitsX(scopeStart, scopeEnd);
        
        // Auto-scale Y-axis for the visible range
        DetailPlot1.Plot.Axes.AutoScaleY();
        DetailPlot2.Plot.Axes.AutoScaleY();
        DetailPlot3.Plot.Axes.AutoScaleY();
        DetailPlot4.Plot.Axes.AutoScaleY();
        DetailPlot5.Plot.Axes.AutoScaleY();
        
        DetailPlot1.Refresh();
        DetailPlot2.Refresh();
        DetailPlot3.Refresh();
        DetailPlot4.Refresh();
        DetailPlot5.Refresh();
    }

    private void ResetView_Click(object? sender, RoutedEventArgs e)
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
        
        UpdateDetailViews();
        TimelinePlot.Refresh();
    }

    private void GenerateNewData_Click(object? sender, RoutedEventArgs e)
    {
        GenerateData();
        
        // Clear and recreate plots with new data
        TimelinePlot.Plot.Clear();
        DetailPlot1.Plot.Clear();
        DetailPlot2.Plot.Clear();
        DetailPlot3.Plot.Clear();
        DetailPlot4.Plot.Clear();
        DetailPlot5.Plot.Clear();
        
        // Recreate signals with new data
        timelineSignal = TimelinePlot.Plot.Add.Signal(timelineData);
        timelineSignal.Color = Colors.Blue;
        timelineSignal.LineWidth = 2;
        
        detailSignal1 = DetailPlot1.Plot.Add.Signal(detailData1);
        detailSignal1.Color = Colors.Red;
        detailSignal1.LineWidth = 2;
        
        detailSignal2 = DetailPlot2.Plot.Add.Signal(detailData2);
        detailSignal2.Color = Colors.Green;
        detailSignal2.LineWidth = 2;
        
        detailSignal3 = DetailPlot3.Plot.Add.Signal(detailData3);
        detailSignal3.Color = Colors.Purple;
        detailSignal3.LineWidth = 2;
        
        detailSignal4 = DetailPlot4.Plot.Add.Signal(detailData4);
        detailSignal4.Color = Colors.Orange;
        detailSignal4.LineWidth = 2;
        
        detailSignal5 = DetailPlot5.Plot.Add.Signal(detailData5);
        detailSignal5.Color = Colors.Brown;
        detailSignal5.LineWidth = 2;
        
        // Recreate scope span
        scopeSpan = TimelinePlot.Plot.Add.HorizontalSpan(scopeStart, scopeEnd);
        scopeSpan.FillColor = Color.FromHex("#3388ff44");
        scopeSpan.LineColor = Color.FromHex("#3388ff");
        scopeSpan.LineWidth = 2;
        scopeSpan.IsDraggable = true;
        scopeSpan.IsResizable = true;
        
        // Re-link the X-axes of all detail plots
        DetailPlot1.Plot.Axes.Link(DetailPlot2, x: true, y: false);
        DetailPlot1.Plot.Axes.Link(DetailPlot3, x: true, y: false);
        DetailPlot1.Plot.Axes.Link(DetailPlot4, x: true, y: false);
        DetailPlot1.Plot.Axes.Link(DetailPlot5, x: true, y: false);
        
        // Re-configure DetailView plots to use mouse wheel for ScrollViewer instead of zooming
        DetailPlot1.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        DetailPlot2.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        DetailPlot3.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        DetailPlot4.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        DetailPlot5.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        
        // Reset view
        ResetView_Click(sender, e);
    }
}
