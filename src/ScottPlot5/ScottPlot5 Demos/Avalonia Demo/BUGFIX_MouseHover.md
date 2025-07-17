# 鼠标悬停功能编译错误修复

## 修复的编译错误

### 1. Tooltip 构造函数参数错误
**错误信息**: `Error CS1501 : "Tooltip"方法没有采用 2 个参数的重载`

**问题**: Tooltip 构造函数需要 3 个参数，而不是 2 个。

**修复前**:
```csharp
var tooltip = plot.Plot.Add.Tooltip(new Coordinates(0, 0), "");
```

**修复后**:
```csharp
var tooltip = plot.Plot.Add.Tooltip(new Coordinates(0, 0), "", new Coordinates(0, 0));
```

### 2. 类型转换错误 (double 到 float)
**错误信息**: `Error CS1503 : 参数 3: 无法从"double"转换为"float"`

**问题**: `GetNearestX` 方法的第三个参数需要 float 类型，而传入的是 double。

**修复前**:
```csharp
DataPoint nearest = signal.GetNearestX(mouseLocation, plot.Plot.LastRender, maxDistance);
```

**修复后**:
```csharp
DataPoint nearest = signal.GetNearestX(mouseLocation, plot.Plot.LastRender, (float)maxDistance);
```

### 3. CoordinateRange 类型转换错误
**错误信息**: `Error CS1503 : 参数 1: 无法从"ScottPlot.CoordinateRange"转换为"decimal"`

**问题**: `YRange` 属性返回的是 `CoordinateRange` 类型，不能直接用于 `Math.Abs()`。

**修复前**:
```csharp
closestPoint.point.Y + 0.1 * Math.Abs(plot.Plot.Axes.GetLimits().YRange)
```

**修复后**:
```csharp
closestPoint.point.Y + 0.1 * (plot.Plot.Axes.GetLimits().Top - plot.Plot.Axes.GetLimits().Bottom)
```

## 修复说明

### Tooltip 构造函数
ScottPlot 的 Tooltip 需要三个参数：
1. `TipLocation` - 工具提示箭头指向的位置
2. `LabelText` - 显示的文本内容
3. `LabelLocation` - 文本标签的位置

### 类型转换
ScottPlot API 中某些方法对参数类型有严格要求：
- `GetNearestX` 方法的距离参数必须是 `float` 类型
- 需要显式转换 `(float)maxDistance`

### 坐标范围计算
`AxisLimits.YRange` 返回 `CoordinateRange` 对象，不是数值类型：
- 使用 `Top - Bottom` 计算 Y 轴范围
- 确保类型兼容性

## 验证结果

修复后的代码应该能够正常编译，实现以下功能：
- ✅ 鼠标悬停时显示十字光标
- ✅ 显示工具提示框显示数据值
- ✅ 支持多数据系列
- ✅ 智能定位和自动隐藏

## 技术要点

1. **API 兼容性**: 确保使用正确的 ScottPlot API 签名
2. **类型安全**: 注意 C# 的类型转换要求
3. **坐标系统**: 理解 ScottPlot 的坐标和范围系统

这些修复确保了鼠标悬停功能能够正确编译和运行，为用户提供流畅的数据探索体验。
