﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Core.Setting;
using Snap.Hutao.Core.Windowing;
using Snap.Hutao.Core.Windowing.Abstraction;
using Snap.Hutao.Win32.UI.WindowsAndMessaging;
using Windows.Graphics;

namespace Snap.Hutao;

/// <summary>
/// 指引窗口
/// </summary>
[Injection(InjectAs.Singleton)]
internal sealed partial class GuideWindow : Window,
    IXamlWindowExtendContentIntoTitleBar,
    IXamlWindowRectPersisted,
    IXamlWindowSubclassMinMaxInfoHandler
{
    private const int MinWidth = 1000;
    private const int MinHeight = 650;

    private const int MaxWidth = 1200;
    private const int MaxHeight = 800;

    public GuideWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        this.InitializeController(serviceProvider);
    }

    public FrameworkElement TitleBarAccess { get => DragableGrid; }

    public string PersistRectKey { get => SettingKeys.GuideWindowRect; }

    public SizeInt32 InitSize { get; } = new(MinWidth, MinHeight);

    public unsafe void HandleMinMaxInfo(ref MINMAXINFO info, double scalingFactor)
    {
        info.ptMinTrackSize.x = (int)Math.Max(MinWidth * scalingFactor, info.ptMinTrackSize.x);
        info.ptMinTrackSize.y = (int)Math.Max(MinHeight * scalingFactor, info.ptMinTrackSize.y);
        info.ptMaxTrackSize.x = (int)Math.Min(MaxWidth * scalingFactor, info.ptMaxTrackSize.x);
        info.ptMaxTrackSize.y = (int)Math.Min(MaxHeight * scalingFactor, info.ptMaxTrackSize.y);
    }
}