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
using AvaloniaColor = Avalonia.Media.Color;
using ScottPlotColor = ScottPlot.Color;

namespace Avalonia_Demo.Controls;

/// <summary>
/// Flame Graph Detail View - Simulates inverted flame graph of execution stack
/// </summary>
public class FlameGraphDetailView
{
    /// <summary>
    /// Execution stack frame data
    /// </summary>
    public class StackFrame
    {
        public string FunctionName { get; set; } = "";
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public int StackLevel { get; set; }
        public ScottPlotColor Color { get; set; } = Colors.Blue;
        public string Module { get; set; } = "";
        public double Duration => EndTime - StartTime;
    }

    /// <summary>
    /// Flame graph configuration
    /// </summary>
    public class FlameGraphConfig
    {
        public string Title { get; set; } = "Flame Graph - Execution Stack";
        public double TimelineStart { get; set; } = 0;
        public double TimelineEnd { get; set; } = 100;
        public int MaxStackDepth { get; set; } = 10;
        public double FrameHeight { get; set; } = 20;
        public List<StackFrame> StackFrames { get; set; } = new();
    }

    private FlameGraphConfig config;
    private List<Rectangle> rectangles = new();
    private List<Text> labels = new();
    private Crosshair crosshair;
    private Tooltip tooltip;
    private AvaPlot plot;

    // Predefined function names and modules
    private static readonly string[] FunctionNames = new[]
    {
        "main()", "processData()", "calculateMetrics()", "renderFrame()", "updateUI()",
        "handleEvent()", "parseInput()", "validateData()", "saveToDatabase()", "loadConfiguration()",
        "initializeSystem()", "cleanupResources()", "allocateMemory()", "optimizePerformance()", "debugTrace()",
        "networkRequest()", "fileIO()", "cryptoOperation()", "imageProcessing()", "audioDecoding()",
        "videoRendering()", "physicsSimulation()", "aiInference()", "dataCompression()", "cacheUpdate()"
    };

    private static readonly string[] ModuleNames = new[]
    {
        "Core", "UI", "Network", "Database", "Graphics", "Audio", "Physics", "AI", "Utils", "Security"
    };

    private static readonly ScottPlotColor[] StackColors = new[]
    {
        ScottPlotColor.FromHex("#FF6B6B"), ScottPlotColor.FromHex("#4ECDC4"), ScottPlotColor.FromHex("#45B7D1"), 
        ScottPlotColor.FromHex("#96CEB4"), ScottPlotColor.FromHex("#FFEAA7"), ScottPlotColor.FromHex("#DDA0DD"),
        ScottPlotColor.FromHex("#98D8C8"), ScottPlotColor.FromHex("#F7DC6F"), ScottPlotColor.FromHex("#BB8FCE"),
        ScottPlotColor.FromHex("#85C1E9"), ScottPlotColor.FromHex("#F8C471"), ScottPlotColor.FromHex("#82E0AA")
    };

    public FlameGraphDetailView(AvaPlot targetPlot)
    {
        plot = targetPlot;
        config = GenerateFlameGraphData();
        SetupPlot();
    }

    /// <summary>
    /// Generate simulated flame graph data
    /// </summary>
    private FlameGraphConfig GenerateFlameGraphData()
    {
        var random = new Random(42);
        var frames = new List<StackFrame>();
        var config = new FlameGraphConfig
        {
            TimelineStart = 0,
            TimelineEnd = 100,
            MaxStackDepth = 8
        };

        // Generate main function call stack
        GenerateStackFrames(frames, random, 0, 100, 0, config.MaxStackDepth);

        config.StackFrames = frames;
        return config;
    }

    /// <summary>
    /// Recursively generate execution stack frames
    /// </summary>
    private void GenerateStackFrames(List<StackFrame> frames, Random random, double startTime, double endTime, int currentLevel, int maxLevel)
    {
        if (currentLevel >= maxLevel || endTime - startTime < 1) return;

        double currentTime = startTime;
        int functionIndex = 0;

        while (currentTime < endTime)
        {
            // Randomly select function name and duration
            var functionName = FunctionNames[random.Next(FunctionNames.Length)];
            var module = ModuleNames[random.Next(ModuleNames.Length)];
            var duration = Math.Min(random.NextDouble() * 15 + 2, endTime - currentTime);
            var frameEndTime = currentTime + duration;

            // Use different colors for different levels
            var color = StackColors[currentLevel % StackColors.Length];
            
            // Add transparency variation
            var alpha = (byte)(180 + (currentLevel * 10) % 75);
            color = new ScottPlotColor(color.R, color.G, color.B, alpha);

            var frame = new StackFrame
            {
                FunctionName = $"{module}.{functionName}",
                StartTime = currentTime,
                EndTime = frameEndTime,
                StackLevel = currentLevel,
                Color = color,
                Module = module
            };

            frames.Add(frame);

            // Recursively generate sub-calls (with probability)
            if (random.NextDouble() > 0.3 && currentLevel < maxLevel - 1)
            {
                // Generate sub-calls during current function execution
                var subCallStart = currentTime + duration * 0.1;
                var subCallEnd = frameEndTime - duration * 0.1;
                
                if (subCallEnd > subCallStart)
                {
                    GenerateStackFrames(frames, random, subCallStart, subCallEnd, currentLevel + 1, maxLevel);
                }
            }

            currentTime = frameEndTime + random.NextDouble() * 2; // Add small gap
            functionIndex++;
        }
    }

    /// <summary>
    /// Setup flame graph plotting
    /// </summary>
    private void SetupPlot()
    {
        plot.Plot.Clear();
        rectangles.Clear();
        labels.Clear();

        // Draw each stack frame as rectangle
        foreach (var frame in config.StackFrames)
        {
            // Calculate rectangle position (inverted flame graph: stack bottom at top)
            var x = frame.StartTime;
            var width = frame.Duration;
            var y = (config.MaxStackDepth - frame.StackLevel - 1) * config.FrameHeight;
            var height = config.FrameHeight * 0.9; // Leave some gap

            // Create rectangle
            var rect = plot.Plot.Add.Rectangle(x, x + width, y, y + height);
            rect.FillColor = frame.Color;
            rect.LineColor = ScottPlotColor.FromHex("#333333");
            rect.LineWidth = 0.5f;
            rectangles.Add(rect);

            // Add text label (only for rectangles wide enough to display text)
            if (width > 5)
            {
                var text = plot.Plot.Add.Text(frame.FunctionName, x + width / 2, y + height / 2);
                text.LabelFontSize = (float)Math.Max(8, Math.Min(12, width / frame.FunctionName.Length * 2));
                text.LabelFontColor = Colors.Black;
                text.LabelAlignment = Alignment.MiddleCenter;
                labels.Add(text);
            }
        }

        // Setup axes
        plot.Plot.Axes.SetLimitsX(config.TimelineStart, config.TimelineEnd);
        plot.Plot.Axes.SetLimitsY(-config.FrameHeight, config.MaxStackDepth * config.FrameHeight);
        
        // Hide Y-axis (flame graphs typically don't show Y-axis ticks)
        plot.Plot.Axes.Left.IsVisible = false;
        plot.Plot.Axes.Right.IsVisible = false;
        
        // Set X-axis label
        plot.Plot.Axes.Bottom.Label.Text = "Time (ms)";
        
        // Hide grid
        plot.Plot.Grid.IsVisible = false;

        // Add mouse hover functionality
        SetupMouseInteraction();
    }

    /// <summary>
    /// Setup mouse interaction
    /// </summary>
    private void SetupMouseInteraction()
    {
        // Add crosshair
        crosshair = plot.Plot.Add.Crosshair(0, 0);
        crosshair.IsVisible = false;
        crosshair.LineColor = Colors.Red;
        crosshair.LineWidth = 1;
        crosshair.LinePattern = LinePattern.Dashed;

        // Add tooltip
        tooltip = plot.Plot.Add.Tooltip(new Coordinates(0, 0), "", new Coordinates(0, 0));
        tooltip.IsVisible = false;
        tooltip.FillColor = Color.FromHex("#ffffcc");
        tooltip.LineColor = Colors.Black;
        tooltip.LineWidth = 1;
        tooltip.LabelFontSize = 10;
        tooltip.LabelFontColor = Colors.Black;

        // Bind mouse events
        plot.PointerMoved += OnMouseMove;
        plot.PointerExited += OnMouseExit;
    }

    /// <summary>
    /// Handle mouse move event
    /// </summary>
    private void OnMouseMove(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(plot);
        Pixel mousePixel = new(pos.X, pos.Y);
        Coordinates mouseLocation = plot.Plot.GetCoordinates(mousePixel);

        // Find stack frame under mouse position
        var hoveredFrame = FindFrameAtPosition(mouseLocation.X, mouseLocation.Y);

        if (hoveredFrame != null)
        {
            // Show crosshair
            crosshair.IsVisible = true;
            crosshair.Position = mouseLocation;

            // Build tooltip text
            var tooltipText = new StringBuilder();
            tooltipText.AppendLine($"Function: {hoveredFrame.FunctionName}");
            tooltipText.AppendLine($"Module: {hoveredFrame.Module}");
            tooltipText.AppendLine($"Start: {hoveredFrame.StartTime:F2} ms");
            tooltipText.AppendLine($"End: {hoveredFrame.EndTime:F2} ms");
            tooltipText.AppendLine($"Duration: {hoveredFrame.Duration:F2} ms");
            tooltipText.AppendLine($"Stack Level: {hoveredFrame.StackLevel}");

            // Show call stack path
            var stackPath = GetStackPath(hoveredFrame);
            if (stackPath.Count > 1)
            {
                tooltipText.AppendLine();
                tooltipText.AppendLine("Call Stack:");
                for (int i = 0; i < stackPath.Count; i++)
                {
                    var indent = new string(' ', i * 2);
                    tooltipText.AppendLine($"{indent}→ {stackPath[i].FunctionName}");
                }
            }

            // Set tooltip position
            var tooltipPosition = new Coordinates(
                mouseLocation.X + 5,
                mouseLocation.Y + 10
            );

            tooltip.LabelText = tooltipText.ToString().Trim();
            tooltip.TipLocation = mouseLocation;
            tooltip.LabelLocation = tooltipPosition;
            tooltip.IsVisible = true;

            plot.Refresh();
        }
        else
        {
            // Hide crosshair and tooltip
            if (crosshair.IsVisible || tooltip.IsVisible)
            {
                crosshair.IsVisible = false;
                tooltip.IsVisible = false;
                plot.Refresh();
            }
        }
    }

    /// <summary>
    /// Handle mouse exit event
    /// </summary>
    private void OnMouseExit(object? sender, PointerEventArgs e)
    {
        if (crosshair.IsVisible || tooltip.IsVisible)
        {
            crosshair.IsVisible = false;
            tooltip.IsVisible = false;
            plot.Refresh();
        }
    }

    /// <summary>
    /// Find stack frame at specified position
    /// </summary>
    private StackFrame? FindFrameAtPosition(double x, double y)
    {
        foreach (var frame in config.StackFrames)
        {
            var frameY = (config.MaxStackDepth - frame.StackLevel - 1) * config.FrameHeight;
            var frameHeight = config.FrameHeight * 0.9;

            if (x >= frame.StartTime && x <= frame.EndTime &&
                y >= frameY && y <= frameY + frameHeight)
            {
                return frame;
            }
        }
        return null;
    }

    /// <summary>
    /// Get call stack path
    /// </summary>
    private List<StackFrame> GetStackPath(StackFrame targetFrame)
    {
        var path = new List<StackFrame>();
        
        // Find all stack frames at the same time point, sorted by level
        var timePoint = (targetFrame.StartTime + targetFrame.EndTime) / 2;
        var framesAtTime = config.StackFrames
            .Where(f => f.StartTime <= timePoint && f.EndTime >= timePoint)
            .OrderBy(f => f.StackLevel)
            .ToList();

        // Build path from root to target frame
        for (int level = 0; level <= targetFrame.StackLevel; level++)
        {
            var frameAtLevel = framesAtTime.FirstOrDefault(f => f.StackLevel == level);
            if (frameAtLevel != null)
            {
                path.Add(frameAtLevel);
            }
        }

        return path;
    }

    /// <summary>
    /// Update time range
    /// </summary>
    public void UpdateTimeRange(double startTime, double endTime)
    {
        plot.Plot.Axes.SetLimitsX(startTime, endTime);
        
        // Recalculate text label display and font size
        UpdateTextLabels(startTime, endTime);
        
        plot.Refresh();
    }

    /// <summary>
    /// Update text label display and font size
    /// </summary>
    private void UpdateTextLabels(double visibleStartTime, double visibleEndTime)
    {
        var visibleTimeRange = visibleEndTime - visibleStartTime;
        
        // Clear existing text labels
        foreach (var label in labels)
        {
            plot.Plot.Remove(label);
        }
        labels.Clear();

        // Re-add text labels, adjusting display conditions and font size based on current zoom level
        foreach (var frame in config.StackFrames)
        {
            // Check if stack frame is within visible range
            if (frame.EndTime < visibleStartTime || frame.StartTime > visibleEndTime)
                continue;

            var x = frame.StartTime;
            var width = frame.Duration;
            var y = (config.MaxStackDepth - frame.StackLevel - 1) * config.FrameHeight;
            var height = config.FrameHeight * 0.9;

            // Calculate pixel width at current zoom level (approximate)
            var pixelWidth = (width / visibleTimeRange) * 800; // Assume chart width is about 800 pixels
            
            // Adjust text display conditions based on zoom level
            var minWidthForText = Math.Max(2, visibleTimeRange / 50); // Dynamically adjust minimum width
            
            if (width > minWidthForText && pixelWidth > 30) // Ensure enough pixel space to display text
            {
                var text = plot.Plot.Add.Text(frame.FunctionName, x + width / 2, y + height / 2);
                
                // Dynamically adjust font size based on available space
                var baseFontSize = Math.Max(8, Math.Min(14, pixelWidth / frame.FunctionName.Length * 1.5));
                
                // Further adjust font size based on zoom level
                var zoomFactor = Math.Min(2.0, Math.Max(0.5, 100.0 / visibleTimeRange));
                var adjustedFontSize = baseFontSize * Math.Sqrt(zoomFactor);
                
                text.LabelFontSize = (float)Math.Max(6, Math.Min(16, adjustedFontSize));
                text.LabelFontColor = Colors.Black;
                text.LabelAlignment = Alignment.MiddleCenter;
                
                // Truncate text if too long
                if (pixelWidth < frame.FunctionName.Length * text.LabelFontSize * 0.6)
                {
                    var maxChars = Math.Max(3, (int)(pixelWidth / (text.LabelFontSize * 0.6)));
                    if (frame.FunctionName.Length > maxChars)
                    {
                        text.LabelText = frame.FunctionName.Substring(0, maxChars - 2) + "..";
                    }
                }
                
                labels.Add(text);
            }
        }
    }

    /// <summary>
    /// Regenerate data
    /// </summary>
    public void RegenerateData()
    {
        config = GenerateFlameGraphData();
        SetupPlot();
        plot.Refresh();
    }

    /// <summary>
    /// Get configuration information
    /// </summary>
    public FlameGraphConfig GetConfig()
    {
        return config;
    }

    /// <summary>
    /// Set fixed layout
    /// </summary>
    public void SetFixedLayout(PixelPadding padding)
    {
        plot.Plot.Layout.Fixed(padding);
    }
}
