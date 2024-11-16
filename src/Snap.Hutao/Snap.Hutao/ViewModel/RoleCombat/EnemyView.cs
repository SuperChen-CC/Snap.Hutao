﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model;
using Snap.Hutao.Web.Hoyolab.Takumi.GameRecord.RoleCombat;

namespace Snap.Hutao.ViewModel.RoleCombat;

internal sealed class EnemyView : INameIcon
{
    private EnemyView(RoleCombatEnemy roleCombatEnemy)
    {
        Name = roleCombatEnemy.Name;
        Icon = roleCombatEnemy.Icon.ToUri();
        Level = $"Lv. {roleCombatEnemy.Level}";
    }

    public string Name { get; set; }

    public Uri Icon { get; set; }

    public string Level { get; set; }

    public static EnemyView From(RoleCombatEnemy roleCombatEnemy)
    {
        return new(roleCombatEnemy);
    }
}