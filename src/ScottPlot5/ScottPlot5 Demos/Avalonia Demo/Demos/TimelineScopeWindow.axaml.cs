using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia_Demo.ViewModels.Demos;
using Avalonia_Demo.Controls;
using ScottPlot;
using System;
using System.Collections.Generic;

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

    // Sample configurations for new DetailViews
    private readonly DetailViewConfig[] sampleConfigs = new[]
    {
        new DetailViewConfig 
        { 
            Title = "DetailView - Trigonometric Functions",
            DataSeries = new List<DataSeriesConfig>
            {
                new DataSeriesConfig 
                { 
                    Label = "Sine Wave", 
                    LineColor = Colors.Cyan,
                    DataGenerator = (i, _) => Math.Sin(i * 0.08) * 1.5 + (new Random(123).NextDouble() - 0.5) * 0.3
                },
                new DataSeriesConfig 
                { 
                    Label = "Tangent Wave", 
                    LineColor = Colors.Navy,
                    DataGenerator = (i, _) => Math.Tanh(Math.Sin(i * 0.06)) * 2 + (new Random(124).NextDouble() - 0.5) * 0.2
                }
            }
        },
        new DetailViewConfig 
        { 
            Title = "DetailView - Mathematical Functions",
            DataSeries = new List<DataSeriesConfig>
            {
                new DataSeriesConfig 
                { 
                    Label = "Logarithmic Function", 
                    LineColor = Colors.Magenta,
                    DataGenerator = (i, _) => Math.Log(i + 1) * 0.5 + (new Random(456).NextDouble() - 0.5) * 0.2
                },
                new DataSeriesConfig 
                { 
                    Label = "Square Root Function", 
                    LineColor = Colors.Teal,
                    DataGenerator = (i, _) => Math.Sqrt(i) * 0.1 + (new Random(457).NextDouble() - 0.5) * 0.3
                }
            }
        },
        new DetailViewConfig 
        { 
            Title = "DetailView - Signal Processing",
            DataSeries = new List<DataSeriesConfig>
            {
                new DataSeriesConfig 
                { 
                    Label = "Square Wave", 
                    LineColor = Colors.Yellow,
                    DataGenerator = (i, _) => Math.Sign(Math.Sin(i * 0.1)) * 2 + (new Random(789).NextDouble() - 0.5) * 0.4
                },
                new DataSeriesConfig 
                { 
                    Label = "Sawtooth Wave", 
                    LineColor = Colors.LightBlue,
                    DataGenerator = (i, _) => (i % 100) / 50.0 - 1.0 + (new Random(790).NextDouble() - 0.5) * 0.3
                },
                new DataSeriesConfig 
                { 
                    Label = "Triangle Wave", 
                    LineColor = Colors.Lime,
                    DataGenerator = (i, _) => 2 * Math.Abs((i % 100) / 50.0 - 1.0) - 1.0 + (new Random(791).NextDouble() - 0.5) * 0.2
                }
            }
        },
        new DetailViewConfig 
        { 
            Title = "DetailView - Physics Simulation",
            DataSeries = new List<DataSeriesConfig>
            {
                new DataSeriesConfig 
                { 
                    Label = "Damped Oscillation", 
                    LineColor = Colors.Pink,
                    DataGenerator = (i, _) => Math.Exp(-i * 0.01) * Math.Cos(i * 0.2) + (new Random(131415).NextDouble() - 0.5) * 0.2
                },
                new DataSeriesConfig 
                { 
                    Label = "Forced Oscillation", 
                    LineColor = Colors.Coral,
                    DataGenerator = (i, _) => Math.Sin(i * 0.15) * Math.Exp(-i * 0.005) + (new Random(131416).NextDouble() - 0.5) * 0.15
                }
            }
        }
    };

    private int nextConfigIndex = 0;

    public TimelineScopeWindow()
    {
        InitializeComponent();
        DataContext = new TimelineScopeViewModel();
    }

    private void ResetView_Click(object? sender, RoutedEventArgs e)
    {
        TimelineScopeControl.ResetView();
    }

    private void GenerateNewData_Click(object? sender, RoutedEventArgs e)
    {
        TimelineScopeControl.GenerateNewData();
    }

    private void AddDetailView_Click(object? sender, RoutedEventArgs e)
    {
        if (nextConfigIndex < sampleConfigs.Length)
        {
            TimelineScopeControl.AddDetailView(sampleConfigs[nextConfigIndex]);
            nextConfigIndex++;
        }
        else
        {
            // Create a random configuration when we run out of predefined ones
            var random = new Random();
            var colors = new[] { Colors.Red, Colors.Green, Colors.Blue, Colors.Orange, Colors.Purple, Colors.Brown, Colors.Cyan, Colors.Magenta, Colors.Yellow, Colors.Pink };
            
            var config = new DetailViewConfig
            {
                Title = $"DetailView {nextConfigIndex + 1} - Random Functions",
                DataSeries = new List<DataSeriesConfig>
                {
                    new DataSeriesConfig
                    {
                        Label = "Random Function 1",
                        LineColor = colors[random.Next(colors.Length)],
                        DataGenerator = (i, _) => Math.Sin(i * random.NextDouble() * 0.1) * random.NextDouble() * 3 + (random.NextDouble() - 0.5) * 0.5
                    },
                    new DataSeriesConfig
                    {
                        Label = "Random Function 2",
                        LineColor = colors[random.Next(colors.Length)],
                        DataGenerator = (i, _) => Math.Cos(i * random.NextDouble() * 0.08) * random.NextDouble() * 2 + (random.NextDouble() - 0.5) * 0.3
                    }
                }
            };
            
            TimelineScopeControl.AddDetailView(config);
            nextConfigIndex++;
        }
    }

    private void RemoveDetailView_Click(object? sender, RoutedEventArgs e)
    {
        // Remove the last DetailView (index -1 means remove last)
        var currentCount = TimelineScopeControl.detailViewConfigs?.Count ?? 0;
        if (currentCount > 1) // Keep at least one DetailView
        {
            TimelineScopeControl.RemoveDetailView(currentCount - 1);
            nextConfigIndex = Math.Max(0, nextConfigIndex - 1);
        }
    }

    private void AddFlameGraphDetailView_Click(object? sender, RoutedEventArgs e)
    {
        TimelineScopeControl.AddFlameGraphDetailView($"Flame Graph {nextConfigIndex + 1} - Execution Stack");
        nextConfigIndex++;
    }
}
