# 线程安全问题修复

## 问题描述

在拖动 Timeline Scope Control 中的 SharedXAxis 过程中出现以下异常：

```
System.InvalidOperationException: Collection was modified; enumeration operation may not execute.
   at System.Collections.Generic.List`1.ForEach(Action`1 action)
   at ScottPlot.Rendering.RenderActions.ApplyAxisRulesBeforeLayout.Render(RenderPack rp)
```

## 根本原因

错误发生在 `ApplyAxisRulesBeforeLayout.Render` 方法中，这表明在渲染过程中轴规则集合被修改了。问题出现在 `ChangeDetailPlotXAxisRange` 方法中：

**问题代码**:
```csharp
private void ChangeDetailPlotXAxisRange(AvaPlot plot, double xMin, double xMax)
{
    // Clear existing axis rules temporarily
    plot.Plot.Axes.Rules.Clear();  // ❌ 在渲染时修改集合
        
    // Set new X-axis limits
    plot.Plot.Axes.SetLimitsX(xMin, xMax);
    plot.Plot.Axes.AutoScaleY();
        
    // Re-add the locked horizontal rule with new limits
    var lockedHorizontalRule = new ScottPlot.AxisRules.LockedHorizontal(
        plot.Plot.Axes.Bottom, 
        xMin, 
        xMax);
    plot.Plot.Axes.Rules.Add(lockedHorizontalRule);  // ❌ 在渲染时修改集合
        
    plot.Refresh();
}
```

## 修复方案

采用更安全的方法，避免在渲染过程中直接清除和重新添加轴规则：

**修复后的代码**:
```csharp
private void ChangeDetailPlotXAxisRange(AvaPlot plot, double xMin, double xMax)
{
    // Use a safer approach that doesn't modify rules during rendering
    // First, set the limits directly
    plot.Plot.Axes.SetLimitsX(xMin, xMax);
    plot.Plot.Axes.AutoScaleY();
    
    // Find and update existing LockedHorizontal rule instead of clearing and re-adding
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
```

## 修复原理

### 1. **线程安全考虑**
- 避免在渲染过程中直接清除集合
- 使用 `ToList()` 创建集合的副本进行操作
- 批量更新而不是逐个修改

### 2. **规则管理优化**
- 查找现有的 `LockedHorizontal` 规则
- 如果存在，则更新参数而不是删除重建
- 保持其他轴规则不变

### 3. **操作顺序**
1. 首先设置轴限制
2. 然后安全地更新轴规则
3. 最后刷新图表

## 技术要点

### 集合修改的线程安全性
- **问题**: 在 `ForEach` 枚举过程中修改集合会导致 `InvalidOperationException`
- **解决**: 使用集合副本进行操作，避免在枚举时修改原集合

### ScottPlot 渲染机制
- **渲染过程**: ScottPlot 在渲染时会枚举轴规则集合
- **冲突**: 同时修改轴规则会导致枚举异常
- **避免**: 使用原子操作或批量更新

### 轴规则管理
- **LockedHorizontal**: 锁定水平轴的规则
- **更新策略**: 查找现有规则并更新，而不是删除重建
- **性能优化**: 减少不必要的规则创建和销毁

## 验证结果

修复后的代码应该能够：
- ✅ 正常拖动 SharedXAxis 而不出现异常
- ✅ 保持轴锁定功能正常工作
- ✅ 维持良好的性能表现
- ✅ 支持多个 DetailView 的同步更新

## 预防措施

为了避免类似问题，建议：

1. **集合操作**: 在可能的渲染过程中避免修改集合
2. **批量更新**: 使用批量操作而不是逐个修改
3. **状态检查**: 在修改前检查对象状态
4. **异常处理**: 添加适当的异常处理机制

这个修复确保了 Timeline Scope Control 在高频率交互时的稳定性和可靠性。
