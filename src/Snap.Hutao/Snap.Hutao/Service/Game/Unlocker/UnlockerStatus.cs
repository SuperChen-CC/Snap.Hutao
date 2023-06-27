﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Abstraction;

namespace Snap.Hutao.Service.Game.Unlocker;

/// <summary>
/// 解锁状态
/// </summary>
internal sealed class UnlockerStatus : ICloneable<UnlockerStatus>
{
    /// <summary>
    /// 状态描述
    /// </summary>
    public string Description { get; set; } = default!;

    /// <summary>
    /// 查找模块状态
    /// </summary>
    public FindModuleResult FindModuleState { get; set; }

    /// <summary>
    /// 当前解锁器是否有效
    /// </summary>
    public bool IsUnlockerValid { get; set; } = true;

    /// <summary>
    /// FPS 字节地址
    /// </summary>
    public nuint FpsAddress { get; set; }

    public UnlockerStatus Clone()
    {
        throw new NotImplementedException();
    }
}