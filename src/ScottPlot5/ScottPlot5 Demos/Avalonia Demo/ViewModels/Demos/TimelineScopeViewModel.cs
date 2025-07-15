using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia_Demo.ViewModels.Demos;

public partial class TimelineScopeViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _scopeStart = 20;

    [ObservableProperty]
    private double _scopeEnd = 80;

    [ObservableProperty]
    private bool _isTimelineLocked = true;

    [ObservableProperty]
    private bool _detailViewsSynchronized = true;
}
