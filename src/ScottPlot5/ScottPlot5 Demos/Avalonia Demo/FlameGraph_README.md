# 火焰图 DetailView 功能说明

## 🔥 概述

火焰图 DetailView 是 Timeline Scope Control 的一个特殊视图，用于模拟和可视化执行栈的调用关系。它采用倒置火焰图的形式，栈底在上方，栈顶在下方，能够直观地展示函数调用的层次结构和时间分布。

## 🎯 功能特性

### 1. **倒置火焰图显示**
- **栈底在上方**: 主函数和系统调用位于图表顶部
- **栈顶在下方**: 深层嵌套的函数调用位于图表底部
- **时间轴**: 横轴表示时间进度，纵轴表示调用栈深度

### 2. **智能数据生成**
- **模拟执行栈**: 自动生成真实的函数调用序列
- **递归调用**: 支持嵌套函数调用的模拟
- **随机化**: 每次生成不同的调用模式和持续时间
- **多模块**: 包含 Core、UI、Network、Database 等不同模块

### 3. **丰富的交互功能**
- **鼠标悬停**: 显示详细的函数信息和调用栈路径
- **十字光标**: 精确定位到鼠标位置的栈帧
- **工具提示**: 显示函数名、模块、执行时间、调用层级等信息
- **调用栈追踪**: 显示完整的调用路径

### 4. **视觉设计**
- **颜色编码**: 不同层级使用不同颜色，便于区分
- **透明度变化**: 深层调用使用更高的透明度
- **文本标签**: 足够宽的栈帧显示函数名
- **动态字体**: 根据栈帧宽度调整字体大小

## 🔧 技术实现

### 核心类结构

#### `FlameGraphDetailView`
主要的火焰图实现类，包含：
- **StackFrame**: 执行栈帧数据结构
- **FlameGraphConfig**: 火焰图配置信息
- **数据生成**: 模拟执行栈的算法
- **交互处理**: 鼠标事件和工具提示

#### `StackFrame` 数据结构
```csharp
public class StackFrame
{
    public string FunctionName { get; set; }    // 函数名
    public double StartTime { get; set; }       // 开始时间
    public double EndTime { get; set; }         // 结束时间
    public int StackLevel { get; set; }         // 栈层级
    public Color Color { get; set; }            // 显示颜色
    public string Module { get; set; }          // 所属模块
    public double Duration => EndTime - StartTime; // 持续时间
}
```

### 数据生成算法

#### 递归栈帧生成
```csharp
private void GenerateStackFrames(List<StackFrame> frames, Random random, 
    double startTime, double endTime, int currentLevel, int maxLevel)
{
    // 1. 在当前层级生成函数调用
    // 2. 随机选择函数名和持续时间
    // 3. 递归生成子调用（有概率）
    // 4. 为不同层级分配不同颜色
}
```

#### 预定义函数库
- **25个函数名**: 涵盖系统各个方面的典型函数
- **10个模块**: Core、UI、Network、Database、Graphics 等
- **12种颜色**: 为不同层级提供视觉区分

## 🎨 使用方法

### 1. **添加火焰图视图**
```csharp
// 在 Timeline Scope Control 中添加火焰图
TimelineScopeControl.AddFlameGraphDetailView("My Flame Graph");
```

### 2. **交互操作**
- **鼠标悬停**: 移动鼠标到任意栈帧上查看详细信息
- **时间范围控制**: 通过 Timeline 的 scope 选择控制显示范围
- **数据重新生成**: 点击 "Generate New Data" 按钮生成新的调用栈

### 3. **工具提示信息**
悬停时显示的信息包括：
```
Function: Core.processData()
Module: Core
Start: 25.34 ms
End: 42.67 ms
Duration: 17.33 ms
Stack Level: 2

Call Stack:
→ Core.main()
  → UI.updateUI()
    → Core.processData()
```

## 🔍 技术细节

### 1. **坐标系统**
- **X轴**: 时间轴，单位为毫秒
- **Y轴**: 栈深度，倒置显示（栈底在上）
- **矩形绘制**: 每个栈帧表示为一个彩色矩形

### 2. **颜色管理**
```csharp
// 12种预定义颜色，循环使用
private static readonly Color[] StackColors = new[]
{
    Color.FromHex("#FF6B6B"), Color.FromHex("#4ECDC4"), 
    Color.FromHex("#45B7D1"), Color.FromHex("#96CEB4"),
    // ... 更多颜色
};

// 添加透明度变化
var alpha = (byte)(180 + (currentLevel * 10) % 75);
color = Color.FromArgb(alpha, color.R, color.G, color.B);
```

### 3. **性能优化**
- **高效查找**: 使用空间索引快速定位鼠标下的栈帧
- **按需渲染**: 只有足够宽的栈帧才显示文本标签
- **内存管理**: 合理管理矩形和文本对象的生命周期

### 4. **时间同步**
- **与 Timeline 同步**: 自动响应 scope 选择的变化
- **实时更新**: 拖拽 scope 时实时更新显示范围
- **轴对齐**: 与其他 DetailView 保持 X 轴对齐

## 📊 应用场景

### 1. **性能分析**
- 可视化函数调用的时间分布
- 识别性能瓶颈和热点函数
- 分析调用栈的深度和复杂性

### 2. **调试辅助**
- 理解程序的执行流程
- 追踪函数调用关系
- 定位异常和错误的源头

### 3. **系统监控**
- 实时监控系统的执行状态
- 分析不同模块的活跃程度
- 检测异常的调用模式

### 4. **教学演示**
- 展示程序执行的可视化过程
- 帮助理解调用栈的概念
- 演示不同算法的执行特征

## 🚀 扩展可能

### 1. **数据源集成**
- 连接真实的性能分析工具
- 导入实际的调用栈数据
- 支持多种数据格式

### 2. **高级功能**
- 添加搜索和过滤功能
- 支持调用栈的折叠和展开
- 提供统计信息和汇总视图

### 3. **自定义选项**
- 可配置的颜色方案
- 自定义函数名和模块
- 调整显示参数和布局

这个火焰图 DetailView 为 Timeline Scope Control 增加了强大的执行栈可视化能力，提供了直观、交互式的性能分析和调试工具。
