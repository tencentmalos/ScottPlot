# Timeline Scope Control Demo

这是一个演示 Timeline 和多个 DetailView 同步显示的控件，现在支持每个 DetailView 显示多组数据，每组数据都有自己的标签。

## 功能特性

### ✅ 已实现的核心功能：

1. **Timeline 和多个 DetailView**：
   - Timeline 显示完整的时间序列数据
   - 多个 DetailView 显示不同类型的详细数据
   - 每个 DetailView 都有独特的标题和数据
   - **🆕 每个 DetailView 支持多组数据系列，每组数据显示自己的标签**

2. **Scope 范围选择**：
   - Timeline 中的蓝色半透明区域表示当前选择的范围
   - 通过 HorizontalSpan 实现可视化范围选择
   - 范围选择控制所有 DetailView 的 X 轴显示区间

3. **X 轴同步**：
   - 所有 DetailView 的 X 轴完全同步
   - 当 Timeline 中的 Scope 改变时，所有 DetailView 自动更新显示范围
   - 使用 ScottPlot 的轴链接功能实现同步

4. **Timeline Scope 鼠标交互**：
   - ✅ **拖拽移动**：点击 Scope 区域中央可以拖拽整个范围
   - ✅ **调整大小**：点击 Scope 区域边缘可以调整范围大小
   - ✅ **实时更新**：拖拽过程中 DetailView 实时更新显示

5. **Timeline Y 轴锁定**：
   - Timeline 的 Y 轴被锁定，不能上下拖动
   - 只允许 X 轴方向的平移操作
   - 使用自定义的 MouseDragPan 配置实现

6. **Timeline X 轴最小值限制**：
   - Timeline 的 X 轴最小值被限制为 0
   - 不能拖拽到负数区域
   - 在所有拖拽和缩放操作中都保持此限制

7. **🔒 DetailView X 轴完全锁定**：
   - 使用 ScottPlot AxisRules.LockedHorizontal 锁定 X 轴
   - DetailView 的 X 轴平移和缩放功能完全禁用
   - 仅允许通过 Timeline Scope 和 SharedXAxis 控制 X 轴
   - 保留 Y 轴的平移和缩放功能以便查看数据细节
   - 确保所有 DetailView 的 X 轴完全同步

### 🆕 多数据系列功能：

8. **每个 DetailView 支持多组数据**：
   - ✅ **多条曲线**：每个 DetailView 可以显示多条不同颜色的数据曲线
   - ✅ **独立标签**：每组数据都有自己的标签，显示在图例中
   - ✅ **图例显示**：每个 DetailView 都显示图例，标明各数据系列的名称和颜色
   - ✅ **配置灵活**：每组数据可以有不同的颜色、标签和数据生成器

9. **预定义数据系列配置**：
   - ✅ **DetailView 1 - 多重波函数**：余弦波 + 正弦波
   - ✅ **DetailView 2 - 指数函数**：指数衰减 + 指数增长（受限）
   - ✅ **DetailView 3 - 随机过程**：随机游走 + 布朗运动 + 白噪声

### 🆕 动态功能：

10. **动态增删 DetailView**：
   - ✅ **添加 DetailView**：点击 "Add DetailView" 按钮动态添加新的详细视图
   - ✅ **删除 DetailView**：点击 "Remove Last DetailView" 按钮删除最后一个详细视图
   - ✅ **保持同步**：新增的 DetailView 自动与现有的保持 X 轴同步

10. **智能配置生成**：
    - ✅ **预定义配置**：前几个 DetailView 使用预定义的多数据系列配置
    - ✅ **随机配置**：超出预定义数量时自动生成包含多个数据系列的随机配置
    - ✅ **多样化数据**：包含三角函数、数学函数、信号处理、物理仿真等类别

### 🎮 用户交互：

11. **控制按钮**：
    - ✅ **Reset View**：重置所有视图到默认状态
    - ✅ **Generate New Data**：生成新的随机数据
    - ✅ **Add DetailView**：动态添加新的 DetailView
    - ✅ **Remove Last DetailView**：删除最后一个 DetailView

12. **鼠标交互**：
    - ✅ **Timeline 拖拽**：支持 Scope 的拖拽和调整
    - ✅ **DetailView 滚动**：DetailView 区域支持鼠标滚轮滚动
    - ✅ **智能光标**：根据鼠标位置显示相应的调整光标
    - ✅ **图例交互**：每个 DetailView 显示图例，便于识别不同数据系列

## 技术实现

### 🏗️ 架构设计：

- **UserControl 封装**：TimelineScopeControl 作为可重用的用户控件
- **多层配置驱动**：使用 DetailViewConfig 和 DataSeriesConfig 类定义多层配置
- **数组化管理**：使用嵌套 List 管理多个 DetailView 和每个 DetailView 的多个数据系列
- **轴同步机制**：使用 ScottPlot 的 Axes.Link 功能实现 X 轴同步

### 📊 数据结构：

```csharp
// 单个数据系列的配置
public class DataSeriesConfig
{
    public string Label { get; set; } = "";
    public Color LineColor { get; set; } = Colors.Blue;
    public Func<int, double[], double> DataGenerator { get; set; } = (i, _) => 0;
}

// DetailView 配置，包含多个数据系列
public class DetailViewConfig
{
    public string Title { get; set; } = "";
    public List<DataSeriesConfig> DataSeries { get; set; } = new();
}
```

### 🔗 关键技术点：

1. **多数据系列管理**：
   - 使用三维数据结构：`List<List<double[]>> detailDataArrays` [DetailView][DataSeries][DataPoints]
   - 使用二维信号数组：`List<List<Signal>> detailSignals` [DetailView][DataSeries]
   - 每个 DetailView 循环处理其包含的所有数据系列

2. **图例显示**：
   - 每个 DetailView 自动显示图例
   - 图例位置设置为右上角
   - 每个数据系列的标签和颜色在图例中显示

3. **配置驱动的数据生成**：
   - 每个数据系列有独立的 DataGenerator 函数
   - 支持依赖历史数据的算法（如随机游走）
   - 灵活的颜色和标签配置

4. **动态扩展**：
   - 支持运行时添加包含多个数据系列的 DetailView
   - 自动处理轴同步和图例更新
   - 智能的随机配置生成

## 预定义配置示例

### DetailView 1 - 多重波函数：
- **余弦波**（红色）：`Math.Cos(i * 0.05) * 2`
- **正弦波**（蓝色）：`Math.Sin(i * 0.04) * 1.5`

### DetailView 2 - 指数函数：
- **指数衰减**（绿色）：`Math.Exp(-i * 0.005) * Math.Sin(i * 0.1)`
- **指数增长（受限）**（橙色）：`(1 - Math.Exp(-i * 0.01)) * 3`

### DetailView 3 - 随机过程：
- **随机游走**（紫色）：依赖前一个值的累积随机过程
- **布朗运动**（洋红色）：另一种随机游走变体
- **白噪声**（灰色）：纯随机噪声

## 使用方法

1. **基本操作**：
   - 在 Timeline 中拖拽蓝色区域来选择范围
   - 观察所有 DetailView 如何同步更新显示范围
   - 查看每个 DetailView 的图例，了解不同数据系列的含义

2. **多数据系列观察**：
   - 每个 DetailView 显示多条不同颜色的曲线
   - 图例显示在每个 DetailView 的右上角
   - 不同数据系列有不同的数学特性和视觉表现

3. **动态管理**：
   - 点击 "Add DetailView" 添加新的详细视图（包含多个数据系列）
   - 点击 "Remove Last DetailView" 删除视图
   - 观察新增的视图如何自动同步并显示图例

4. **数据更新**：
   - 点击 "Generate New Data" 生成新数据（所有数据系列都会更新）
   - 点击 "Reset View" 重置到默认状态

## 扩展性

这个控件设计为高度可扩展：

- **自定义数据系列**：通过修改 DataSeriesConfig 可以添加任意类型的数据系列
- **灵活的数据源**：DataGenerator 函数可以连接到任何数据源或算法
- **样式定制**：每个数据系列可以有独立的颜色、标签和线型配置
- **图例定制**：可以调整图例的位置、样式和显示内容
- **交互增强**：可以添加数据系列的显示/隐藏切换功能

## 新功能亮点

### 🌟 多数据系列支持：
- **一个 DetailView，多条曲线**：每个 DetailView 不再局限于单一数据，可以同时显示多个相关的数据系列
- **清晰的标识**：每个数据系列都有独特的颜色和标签，通过图例清晰标识
- **逻辑分组**：相关的数据系列被组织在同一个 DetailView 中，便于对比分析

### 🌟 智能配置系统：
- **分类配置**：预定义配置按功能分类（波函数、指数函数、随机过程等）
- **自动扩展**：超出预定义配置时自动生成包含多个数据系列的随机配置
- **一致性保证**：所有 DetailView 都遵循相同的多数据系列模式

这个实现展示了如何使用 ScottPlot 和 Avalonia 创建支持多数据系列的复杂数据可视化控件，具有良好的用户体验、清晰的数据标识和强大的扩展性。
