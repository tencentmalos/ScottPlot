using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Avalonia_Demo.Controls;

/// <summary>
/// 1D Value Detail View - Handles regular signal plotting with uniform X coordinates
/// </summary>
public class ValueDetailView1D
{
    private AvaPlot plot;
    private DetailViewConfig config;
    private List<double[]> dataArrays = new();
    private List<Signal> signals = new();
    private List<Crosshair> crosshairs = new();
    private Tooltip tooltip;

    public ValueDetailView1D(AvaPlot targetPlot, DetailViewConfig configuration)
    {
        plot = targetPlot;
        config = configuration;
        SetupPlot();
    }

    /// <summary>
    /// Generate data for all series in this detail view
    /// </summary>
    public void GenerateData(int pointCount)
    {
        dataArrays.Clear();

        for (int seriesIndex = 0; seriesIndex < config.DataSeries.Count; seriesIndex++)
        {
            var seriesConfig = config.DataSeries[seriesIndex];
            var data = new double[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                data[i] = seriesConfig.DataGenerator(i, data);
            }

            dataArrays.Add(data);
        }
    }

    /// <summary>
    /// Setup the plot with signals and interactive elements
    /// </summary>
    private void SetupPlot()
    {
        plot.Plot.Clear();
        signals.Clear();
        crosshairs.Clear();

        // Create signals for each data series
        for (int i = 0; i < config.DataSeries.Count; i++)
        {
            var seriesConfig = config.DataSeries[i];
            
            // Create signal (will be populated when GenerateData is called)
            var signal = plot.Plot.Add.Signal(new double[0]);
            signal.Color = seriesConfig.LineColor;
            signal.LineWidth = 2;
            signal.LegendText = seriesConfig.Label;
            signals.Add(signal);

            // Create crosshair for mouse tracking
            var crosshair = plot.Plot.Add.Crosshair(0, 0);
            crosshair.IsVisible = false;
            crosshair.MarkerShape = MarkerShape.OpenCircle;
            crosshair.MarkerSize = 8;
            crosshair.LineColor = seriesConfig.LineColor;
            crosshair.MarkerColor = seriesConfig.LineColor;
            crosshair.LineWidth = 1;
            crosshair.LinePattern = LinePattern.Dashed;
            crosshairs.Add(crosshair);
        }

        // Create tooltip
        tooltip = plot.Plot.Add.Tooltip(new Coordinates(0, 0), "", new Coordinates(0, 0));
        tooltip.IsVisible = false;
        tooltip.FillColor = Color.FromHex("#ffffcc");
        tooltip.LineColor = Colors.Black;
        tooltip.LineWidth = 1;
        tooltip.LabelFontSize = 10;
        tooltip.LabelFontColor = Colors.Black;

        // Setup legend
        plot.Plot.ShowLegend();
        plot.Plot.Legend.Alignment = Alignment.UpperRight;
        plot.Plot.Legend.BackgroundColor = Color.FromHex("#ffffff");
        plot.Plot.Legend.OutlineColor = Color.FromHex("#cccccc");

        // Hide X-axis (controlled by shared axis)
        plot.Plot.Axes.Bottom.IsVisible = false;

        // Setup mouse interaction
        plot.PointerMoved += OnMouseMove;
        plot.PointerExited += OnMouseExit;
    }

    /// <summary>
    /// Update the plot with new data
    /// </summary>
    public void UpdatePlot()
    {
        // Remove old signals and create new ones with updated data
        foreach (var signal in signals)
        {
            plot.Plot.Remove(signal);
        }
        signals.Clear();

        // Create new signals with updated data
        for (int i = 0; i < Math.Min(config.DataSeries.Count, dataArrays.Count); i++)
        {
            var seriesConfig = config.DataSeries[i];
            var signal = plot.Plot.Add.Signal(dataArrays[i]);
            signal.Color = seriesConfig.LineColor;
            signal.LineWidth = 2;
            signal.LegendText = seriesConfig.Label;
            signals.Add(signal);
        }
        
        plot.Refresh();
    }

    /// <summary>
    /// Set fixed layout padding
    /// </summary>
    public void SetFixedLayout(PixelPadding padding)
    {
        plot.Plot.Layout.Fixed(padding);
    }

    /// <summary>
    /// Update X-axis range
    /// </summary>
    public void UpdateXAxisRange(double xMin, double xMax)
    {
        // Use a safer approach that doesn't modify rules during rendering
        plot.Plot.Axes.SetLimitsX(xMin, xMax);
        plot.Plot.Axes.AutoScaleY();
        
        // Find and update existing LockedHorizontal rule
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

    /// <summary>
    /// Handle mouse move event for data point tracking
    /// </summary>
    private void OnMouseMove(object? sender, PointerEventArgs e)
    {
        // Get mouse position and convert to coordinates
        var pos = e.GetPosition(plot);
        Pixel mousePixel = new(pos.X, pos.Y);
        Coordinates mouseLocation = plot.Plot.GetCoordinates(mousePixel);

        // Find the nearest data points for all series
        var nearestPoints = new List<(DataPoint point, string seriesLabel, Color seriesColor, int seriesIndex)>();
        double maxDistance = 15; // Maximum distance to consider a point "near"

        for (int seriesIndex = 0; seriesIndex < signals.Count; seriesIndex++)
        {
            var signal = signals[seriesIndex];
            var seriesConfig = config.DataSeries[seriesIndex];
            
            if (signal != null)
            {
                var nearest = signal.GetNearestX(mouseLocation, plot.Plot.LastRender, (float)maxDistance);
                
                if (nearest.IsReal)
                {
                    nearestPoints.Add((nearest, seriesConfig.Label, seriesConfig.LineColor, seriesIndex));
                }
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
            HideInteractiveElements();
        }
    }

    /// <summary>
    /// Handle mouse exit event
    /// </summary>
    private void OnMouseExit(object? sender, PointerEventArgs e)
    {
        HideInteractiveElements();
    }

    /// <summary>
    /// Hide all interactive elements (crosshairs and tooltip)
    /// </summary>
    private void HideInteractiveElements()
    {
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

    /// <summary>
    /// Get the configuration
    /// </summary>
    public DetailViewConfig GetConfig()
    {
        return config;
    }

    /// <summary>
    /// Get the plot instance
    /// </summary>
    public AvaPlot GetPlot()
    {
        return plot;
    }
}
