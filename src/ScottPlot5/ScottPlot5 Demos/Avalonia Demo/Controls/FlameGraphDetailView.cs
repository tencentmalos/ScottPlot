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
/// 火焰图详细视图 - 模拟执行栈的倒置火焰图
/// </summary>
public class FlameGraphDetailView
{
    /// <summary>
    /// 执行栈帧数据
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
    /// 火焰图配置
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

    // 预定义的函数名和模块
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
    /// 生成模拟的火焰图数据
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

        // 生成主函数调用栈
        GenerateStackFrames(frames, random, 0, 100, 0, config.MaxStackDepth);

        config.StackFrames = frames;
        return config;
    }

    /// <summary>
    /// 递归生成执行栈帧
    /// </summary>
    private void GenerateStackFrames(List<StackFrame> frames, Random random, double startTime, double endTime, int currentLevel, int maxLevel)
    {
        if (currentLevel >= maxLevel || endTime - startTime < 1) return;

        double currentTime = startTime;
        int functionIndex = 0;

        while (currentTime < endTime)
        {
            // 随机选择函数名和持续时间
            var functionName = FunctionNames[random.Next(FunctionNames.Length)];
            var module = ModuleNames[random.Next(ModuleNames.Length)];
            var duration = Math.Min(random.NextDouble() * 15 + 2, endTime - currentTime);
            var frameEndTime = currentTime + duration;

            // 为不同层级使用不同颜色
            var color = StackColors[currentLevel % StackColors.Length];
            
            // 添加透明度变化
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

            // 递归生成子调用（有概率）
            if (random.NextDouble() > 0.3 && currentLevel < maxLevel - 1)
            {
                // 在当前函数执行期间生成子调用
                var subCallStart = currentTime + duration * 0.1;
                var subCallEnd = frameEndTime - duration * 0.1;
                
                if (subCallEnd > subCallStart)
                {
                    GenerateStackFrames(frames, random, subCallStart, subCallEnd, currentLevel + 1, maxLevel);
                }
            }

            currentTime = frameEndTime + random.NextDouble() * 2; // 添加小间隔
            functionIndex++;
        }
    }

    /// <summary>
    /// 设置火焰图绘图
    /// </summary>
    private void SetupPlot()
    {
        plot.Plot.Clear();
        rectangles.Clear();
        labels.Clear();

        // 绘制每个栈帧为矩形
        foreach (var frame in config.StackFrames)
        {
            // 计算矩形位置（倒置火焰图：栈底在上方）
            var x = frame.StartTime;
            var width = frame.Duration;
            var y = (config.MaxStackDepth - frame.StackLevel - 1) * config.FrameHeight;
            var height = config.FrameHeight * 0.9; // 留一点间隙

            // 创建矩形
            var rect = plot.Plot.Add.Rectangle(x, x + width, y, y + height);
            rect.FillColor = frame.Color;
            rect.LineColor = ScottPlotColor.FromHex("#333333");
            rect.LineWidth = 0.5f;
            rectangles.Add(rect);

            // 添加文本标签（只有足够宽的矩形才显示文本）
            if (width > 5)
            {
                var text = plot.Plot.Add.Text(frame.FunctionName, x + width / 2, y + height / 2);
                text.LabelFontSize = (float)Math.Max(8, Math.Min(12, width / frame.FunctionName.Length * 2));
                text.LabelFontColor = Colors.Black;
                text.LabelAlignment = Alignment.MiddleCenter;
                labels.Add(text);
            }
        }

        // 设置坐标轴
        plot.Plot.Axes.SetLimitsX(config.TimelineStart, config.TimelineEnd);
        plot.Plot.Axes.SetLimitsY(-config.FrameHeight, config.MaxStackDepth * config.FrameHeight);
        
        // 隐藏Y轴（火焰图通常不显示Y轴刻度）
        plot.Plot.Axes.Left.IsVisible = false;
        plot.Plot.Axes.Right.IsVisible = false;
        
        // 设置X轴标签
        plot.Plot.Axes.Bottom.Label.Text = "Time (ms)";
        
        // 隐藏网格
        plot.Plot.Grid.IsVisible = false;

        // 添加鼠标悬停功能
        SetupMouseInteraction();
    }

    /// <summary>
    /// 设置鼠标交互
    /// </summary>
    private void SetupMouseInteraction()
    {
        // 添加十字光标
        crosshair = plot.Plot.Add.Crosshair(0, 0);
        crosshair.IsVisible = false;
        crosshair.LineColor = Colors.Red;
        crosshair.LineWidth = 1;
        crosshair.LinePattern = LinePattern.Dashed;

        // 添加工具提示
        tooltip = plot.Plot.Add.Tooltip(new Coordinates(0, 0), "", new Coordinates(0, 0));
        tooltip.IsVisible = false;
        tooltip.FillColor = Color.FromHex("#ffffcc");
        tooltip.LineColor = Colors.Black;
        tooltip.LineWidth = 1;
        tooltip.LabelFontSize = 10;
        tooltip.LabelFontColor = Colors.Black;

        // 绑定鼠标事件
        plot.PointerMoved += OnMouseMove;
        plot.PointerExited += OnMouseExit;
    }

    /// <summary>
    /// 鼠标移动事件处理
    /// </summary>
    private void OnMouseMove(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(plot);
        Pixel mousePixel = new(pos.X, pos.Y);
        Coordinates mouseLocation = plot.Plot.GetCoordinates(mousePixel);

        // 查找鼠标位置下的栈帧
        var hoveredFrame = FindFrameAtPosition(mouseLocation.X, mouseLocation.Y);

        if (hoveredFrame != null)
        {
            // 显示十字光标
            crosshair.IsVisible = true;
            crosshair.Position = mouseLocation;

            // 构建工具提示文本
            var tooltipText = new StringBuilder();
            tooltipText.AppendLine($"Function: {hoveredFrame.FunctionName}");
            tooltipText.AppendLine($"Module: {hoveredFrame.Module}");
            tooltipText.AppendLine($"Start: {hoveredFrame.StartTime:F2} ms");
            tooltipText.AppendLine($"End: {hoveredFrame.EndTime:F2} ms");
            tooltipText.AppendLine($"Duration: {hoveredFrame.Duration:F2} ms");
            tooltipText.AppendLine($"Stack Level: {hoveredFrame.StackLevel}");

            // 显示调用栈路径
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

            // 设置工具提示位置
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
            // 隐藏十字光标和工具提示
            if (crosshair.IsVisible || tooltip.IsVisible)
            {
                crosshair.IsVisible = false;
                tooltip.IsVisible = false;
                plot.Refresh();
            }
        }
    }

    /// <summary>
    /// 鼠标离开事件处理
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
    /// 查找指定位置的栈帧
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
    /// 获取调用栈路径
    /// </summary>
    private List<StackFrame> GetStackPath(StackFrame targetFrame)
    {
        var path = new List<StackFrame>();
        
        // 找到同一时间点的所有栈帧，按层级排序
        var timePoint = (targetFrame.StartTime + targetFrame.EndTime) / 2;
        var framesAtTime = config.StackFrames
            .Where(f => f.StartTime <= timePoint && f.EndTime >= timePoint)
            .OrderBy(f => f.StackLevel)
            .ToList();

        // 构建从根到目标帧的路径
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
    /// 更新时间范围
    /// </summary>
    public void UpdateTimeRange(double startTime, double endTime)
    {
        plot.Plot.Axes.SetLimitsX(startTime, endTime);
        plot.Refresh();
    }

    /// <summary>
    /// 重新生成数据
    /// </summary>
    public void RegenerateData()
    {
        config = GenerateFlameGraphData();
        SetupPlot();
        plot.Refresh();
    }

    /// <summary>
    /// 获取配置信息
    /// </summary>
    public FlameGraphConfig GetConfig()
    {
        return config;
    }

    /// <summary>
    /// 设置固定布局
    /// </summary>
    public void SetFixedLayout(PixelPadding padding)
    {
        plot.Plot.Layout.Fixed(padding);
    }
}
