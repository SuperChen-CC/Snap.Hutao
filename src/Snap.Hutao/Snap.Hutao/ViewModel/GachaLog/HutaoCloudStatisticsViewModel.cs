﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Service.GachaLog;

namespace Snap.Hutao.ViewModel.GachaLog;

[Injection(InjectAs.Scoped)]
[ConstructorGenerated(CallBaseConstructor = true)]
internal sealed partial class HutaoCloudStatisticsViewModel : Abstraction.ViewModelSlim
{
    private readonly IGachaLogHutaoCloudService hutaoCloudService;
    private readonly ITaskContext taskContext;

    public HutaoStatistics? Statistics { get; set => SetProperty(ref field, value); }

    protected override async Task LoadAsync()
    {
        await taskContext.SwitchToBackgroundAsync();
        (bool isOk, HutaoStatistics statistics) = await hutaoCloudService.GetCurrentEventStatisticsAsync().ConfigureAwait(false);

        if (isOk)
        {
            await taskContext.SwitchToMainThreadAsync();
            Statistics = statistics;
            IsInitialized = true;
        }
    }
}