using ScottPlot;
using System;
using System.Collections.Generic;

namespace Avalonia_Demo.Controls;

/// <summary>
/// Data series configuration for a single DetailView
/// </summary>
public class DataSeriesConfig
{
    public string Label { get; set; } = "";
    public Color LineColor { get; set; } = Colors.Blue;
    public Func<int, double[], double> DataGenerator { get; set; } = (i, _) => 0;
    public Func<int, (double x, double y)>? XYDataGenerator { get; set; } = null; // For 2D array data with custom X,Y coordinates
    public bool Use2DArray { get; set; } = false; // Flag to indicate if this series uses 2D array data
}

/// <summary>
/// DetailView configuration that can contain multiple data series
/// </summary>
public class DetailViewConfig
{
    public string Title { get; set; } = "";
    public List<DataSeriesConfig> DataSeries { get; set; } = new();
}
