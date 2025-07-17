# 编译错误修复：Color 类型冲突

## 🐛 问题描述

在 FlameGraphDetailView.cs 中出现编译错误：
```
Error CS0104 : "Color"是"Avalonia.Media.Color"和"ScottPlot.Color"之间的不明确的引用
```

## 🔍 问题原因

由于同时引用了两个包含 Color 类型的命名空间：
- `Avalonia.Media.Color` - Avalonia UI 框架的颜色类型
- `ScottPlot.Color` - ScottPlot 绘图库的颜色类型

编译器无法确定代码中的 `Color` 引用指向哪个类型。

## ✅ 解决方案

### 1. 添加类型别名
在文件顶部添加类型别名来区分两种 Color 类型：

```csharp
using AvaloniaColor = Avalonia.Media.Color;
using ScottPlotColor = ScottPlot.Color;
```

### 2. 移除冲突的 using 语句
移除了 `using Avalonia.Media;` 语句，避免直接引入 Color 类型。

### 3. 明确指定 Color 类型
将所有 ScottPlot 相关的 Color 使用改为明确的 `ScottPlotColor`：

### 4. 修复 ScottPlot.Color API 调用
ScottPlot.Color 使用 `FromARGB` 而不是 `FromArgb`：

### 5. 修复类型转换问题
将 double 类型显式转换为 float 类型：

#### 修改前：
```csharp
public Color Color { get; set; } = Colors.Blue;
private static readonly Color[] StackColors = new[] { ... };
color = Color.FromArgb(alpha, color.R, color.G, color.B);
rect.LineColor = Color.FromHex("#333333");
text.LabelFontSize = Math.Max(8, Math.Min(12, width / frame.FunctionName.Length * 2));
```

#### 修改后：
```csharp
public ScottPlotColor Color { get; set; } = Colors.Blue;
private static readonly ScottPlotColor[] StackColors = new[] { ... };
color = ScottPlotColor.FromARGB(alpha, color.R, color.G, color.B); // 注意：FromARGB 不是 FromArgb
rect.LineColor = ScottPlotColor.FromHex("#333333");
text.LabelFontSize = (float)Math.Max(8, Math.Min(12, width / frame.FunctionName.Length * 2)); // 显式转换为 float
```

## 📝 修改的文件

- **FlameGraphDetailView.cs** - 主要的火焰图实现文件

## 🔧 修改详情

### 1. 命名空间和别名
```csharp
// 添加类型别名
using AvaloniaColor = Avalonia.Media.Color;
using ScottPlotColor = ScottPlot.Color;

// 移除冲突的 using
// using Avalonia.Media; // 已移除
```

### 2. StackFrame 类
```csharp
public class StackFrame
{
    // 明确使用 ScottPlot.Color
    public ScottPlotColor Color { get; set; } = Colors.Blue;
    // ... 其他属性
}
```

### 3. 静态颜色数组
```csharp
private static readonly ScottPlotColor[] StackColors = new[]
{
    ScottPlotColor.FromHex("#FF6B6B"), 
    ScottPlotColor.FromHex("#4ECDC4"), 
    // ... 其他颜色
};
```

### 4. 颜色操作方法
```csharp
// 透明度处理
color = ScottPlotColor.FromArgb(alpha, color.R, color.G, color.B);

// 矩形边框颜色
rect.LineColor = ScottPlotColor.FromHex("#333333");
```

## ✨ 验证结果

修复后的代码应该能够正常编译，不再出现 Color 类型冲突的错误。

## 📚 最佳实践

### 1. 类型别名使用
当项目中存在同名类型冲突时，使用类型别名是最佳解决方案：
```csharp
using TypeAlias = Namespace.Type;
```

### 2. 明确类型引用
在可能产生歧义的地方，明确指定完整的类型名称：
```csharp
// 好的做法
ScottPlot.Color plotColor = ScottPlot.Color.Red;

// 避免的做法（可能产生歧义）
Color color = Color.Red;
```

### 3. 命名空间管理
合理管理 using 语句，避免引入不必要的命名空间：
```csharp
// 只引入需要的命名空间
using ScottPlot;
using ScottPlot.Plottables;

// 使用别名处理冲突
using ScottPlotColor = ScottPlot.Color;
```

这个修复确保了火焰图功能能够正常编译和运行，同时保持了代码的清晰性和可维护性。
