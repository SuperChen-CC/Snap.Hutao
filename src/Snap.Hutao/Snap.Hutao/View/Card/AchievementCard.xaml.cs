﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Control.Extension;

namespace Snap.Hutao.View.Card;

/// <summary>
/// 成就卡片
/// </summary>
internal sealed partial class AchievementCard : Button
{
    /// <summary>
    /// 构造一个新的成就卡片
    /// </summary>
    public AchievementCard()
    {
        this.InitializeDataContext<ViewModel.Achievement.AchievementViewModelSlim>();
        InitializeComponent();
    }
}