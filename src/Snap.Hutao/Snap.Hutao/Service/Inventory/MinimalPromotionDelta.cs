﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Google.OrTools.LinearSolver;
using Microsoft.Extensions.Caching.Memory;
using Snap.Hutao.Core.Diagnostics;
using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Model.Metadata.Abstraction;
using Snap.Hutao.Model.Primitive;
using Snap.Hutao.Service.Metadata;
using Snap.Hutao.ViewModel.User;
using Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using CalculableAvatar = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.Avatar;
using CalculableWeapon = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.Weapon;
using MetadataAvatar = Snap.Hutao.Model.Metadata.Avatar.Avatar;
using MetadataWeapon = Snap.Hutao.Model.Metadata.Weapon.Weapon;

namespace Snap.Hutao.Service.Inventory;

[ConstructorGenerated]
[Injection(InjectAs.Singleton)]
internal sealed partial class MinimalPromotionDelta
{
    private readonly ILogger<MinimalPromotionDelta> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly IMetadataService metadataService;
    private readonly IMemoryCache memoryCache;

    public async ValueTask<List<AvatarPromotionDelta>> GetAsync(UserAndUid userAndUid)
    {
        List<AvatarPromotionDelta>? result = await memoryCache.GetOrCreateAsync($"{nameof(MinimalPromotionDelta)}.Cache", async entry =>
        {
            List<CalculableAvatar> calculableAvatars;
            List<CalculableWeapon> calculableWeapons;
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                CalculateClient calculateClient = scope.ServiceProvider.GetRequiredService<CalculateClient>();
                calculableAvatars = await calculateClient.GetAllAvatarsAsync(userAndUid).ConfigureAwait(false);
                calculableWeapons = await calculateClient.GetAllWeaponsAsync(userAndUid).ConfigureAwait(false);
            }

            ImmutableDictionary<AvatarId, MetadataAvatar> idToAvatarMap = await metadataService.GetIdToAvatarMapAsync().ConfigureAwait(false);
            ImmutableDictionary<WeaponId, MetadataWeapon> idToWeaponMap = await metadataService.GetIdToWeaponMapAsync().ConfigureAwait(false);

            List<ICultivationItemsAccess> cultivationItemsEntryList = Create([.. calculableAvatars, .. calculableWeapons], idToAvatarMap, idToWeaponMap);

            using (ValueStopwatch.MeasureExecution(logger))
            {
                List<ICultivationItemsAccess> minimal = Minimize(cultivationItemsEntryList);
                minimal.Sort(CultivationItemsAccessComparer.Shared);
                return ToPromotionDeltaList(minimal);
            }
        }).ConfigureAwait(false);

        ArgumentNullException.ThrowIfNull(result);
        return result;
    }

    private static List<ICultivationItemsAccess> Create(List<PromotionDelta> items, ImmutableDictionary<AvatarId, MetadataAvatar> idToAvatarMap, ImmutableDictionary<WeaponId, MetadataWeapon> idToWeaponMap)
    {
        List<ICultivationItemsAccess> cultivationItems = [];
        foreach (ref readonly PromotionDelta item in CollectionsMarshal.AsSpan(items))
        {
            if (idToAvatarMap.TryGetValue(item.Id, out MetadataAvatar? avatar))
            {
                cultivationItems.Add(avatar);
                continue;
            }

            if (idToWeaponMap.TryGetValue(item.Id, out MetadataWeapon? weapon))
            {
                cultivationItems.Add(weapon);
                continue;
            }
        }

        return cultivationItems;
    }

    private static List<ICultivationItemsAccess> Minimize(List<ICultivationItemsAccess> cultivationItems)
    {
        using (Solver? solver = Solver.CreateSolver("SCIP"))
        {
            ArgumentNullException.ThrowIfNull(solver);

            Objective objective = solver.Objective();
            objective.SetMinimization();

            Dictionary<ICultivationItemsAccess, Variable> itemVariableMap = [];
            foreach (ref readonly ICultivationItemsAccess item in CollectionsMarshal.AsSpan(cultivationItems))
            {
                Variable variable = solver.MakeBoolVar(item.Name);
                itemVariableMap[item] = variable;
                objective.SetCoefficient(variable, 1);
            }

            Dictionary<MaterialId, Constraint> materialConstraintMap = [];
            foreach (ref readonly ICultivationItemsAccess item in CollectionsMarshal.AsSpan(cultivationItems))
            {
                foreach (ref readonly MaterialId materialId in item.CultivationItems.AsSpan())
                {
                    ref Constraint? constraint = ref CollectionsMarshal.GetValueRefOrAddDefault(materialConstraintMap, materialId, out _);
                    if (constraint is null)
                    {
                        constraint = solver.MakeConstraint(double.NegativeInfinity, double.PositiveInfinity, $"{materialId}");

                        Variable penalty = solver.MakeNumVar(0, double.PositiveInfinity, $"penalty_{materialId}");
                        objective.SetCoefficient(penalty, 1000);
                        constraint.SetCoefficient(penalty, 1);
                    }

                    constraint.SetCoefficient(itemVariableMap[item], 1);
                    constraint.SetBounds(3, double.PositiveInfinity);
                }
            }

            Solver.ResultStatus status = solver.Solve();
            HutaoException.ThrowIf(status != Solver.ResultStatus.OPTIMAL, "Unable to solve minimal item set");

            List<ICultivationItemsAccess> results = [];
            foreach ((ICultivationItemsAccess item, Variable variable) in itemVariableMap)
            {
                if (variable.SolutionValue() > 0.5)
                {
                    results.Add(item);
                }
            }

            return results;
        }
    }

    private static List<AvatarPromotionDelta> ToPromotionDeltaList(List<ICultivationItemsAccess> cultivationItems)
    {
        List<AvatarPromotionDelta> deltas = [];
        int currentWeaponEmptyAvatarIndex = 0;

        foreach (ref readonly ICultivationItemsAccess item in CollectionsMarshal.AsSpan(cultivationItems))
        {
            switch (item)
            {
                case MetadataAvatar avatar:
                    deltas.Add(new()
                    {
                        AvatarId = avatar.Id,
                        AvatarLevelCurrent = 1,
                        AvatarLevelTarget = 90,
                        SkillList = avatar.SkillDepot.CompositeSkillsNoInherents.SelectAsArray(skill => new PromotionDelta
                        {
                            Id = skill.GroupId,
                            LevelCurrent = 1,
                            LevelTarget = 10,
                        }),
                    });

                    break;

                case MetadataWeapon weapon:
                    AvatarPromotionDelta delta;
                    if (currentWeaponEmptyAvatarIndex < deltas.Count)
                    {
                        delta = deltas[currentWeaponEmptyAvatarIndex++];
                    }
                    else
                    {
                        delta = new();
                        deltas.Add(delta);
                    }

                    delta.Weapon = new()
                    {
                        Id = weapon.Id,
                        LevelCurrent = 1,
                        LevelTarget = 90,
                    };

                    break;
            }
        }

        return deltas;
    }

    private sealed class CultivationItemsAccessComparer : IComparer<ICultivationItemsAccess>
    {
        private static readonly LazySlim<CultivationItemsAccessComparer> LazyShared = new(() => new());

        public static CultivationItemsAccessComparer Shared { get => LazyShared.Value; }

        public int Compare(ICultivationItemsAccess? x, ICultivationItemsAccess? y)
        {
            return (x, y) switch
            {
                (MetadataAvatar, MetadataWeapon) => -1,
                (MetadataWeapon, MetadataAvatar) => 1,
                _ => 0,
            };
        }
    }
}