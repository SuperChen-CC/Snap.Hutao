﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Service.Game;
using Snap.Hutao.Service.Game.Package.Advanced;
using Snap.Hutao.Service.Game.Scheme;
using Snap.Hutao.Web.Hoyolab.HoyoPlay.Connect;
using Snap.Hutao.Web.Hoyolab.HoyoPlay.Connect.Branch;
using Snap.Hutao.Web.Hoyolab.HoyoPlay.Connect.ChannelSDK;
using Snap.Hutao.Web.Response;
using System.IO;

namespace Snap.Hutao.ViewModel.Game;

[ConstructorGenerated]
[Injection(InjectAs.Singleton)]
internal sealed partial class GamePackageViewModel : Abstraction.ViewModel
{
    private readonly IGamePackageService gamePackageService;
    private readonly LaunchGameShared launchGameShared;
    private readonly IServiceProvider serviceProvider;
    private readonly LaunchOptions launchOptions;
    private readonly ITaskContext taskContext;

    public GameBranch? GameBranch { get; set => SetProperty(ref field, value); }

    public LaunchScheme? LaunchScheme { get; set => SetProperty(ref field, value); }

    public Version? LocalVersion
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(LocalVersionText));
                OnPropertyChanged(nameof(IsUpdateAvailable));
            }
        }
    }

    public Version? RemoteVersion
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(RemoteVersionText));
                OnPropertyChanged(nameof(IsUpdateAvailable));
            }
        }
    }

    public Version? PreVersion
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(PreDownloadTitle));
                OnPropertyChanged(nameof(IsPredownloadButtonEnabled));
            }
        }
    }

    public string LocalVersionText { get => LocalVersion is null ? "Unknown" : SH.FormatViewModelGamePackageLocalVersion(LocalVersion); }

    public string RemoteVersionText { get => SH.FormatViewModelGamePackageRemoteVersion(RemoteVersion); }

    public string PreDownloadTitle { get => SH.FormatViewModelGamePackagePreVersion(PreVersion); }

    public bool IsUpdateAvailable { get => LocalVersion < RemoteVersion; }

    public bool IsPredownloadButtonEnabled
    {
        get
        {
            if (PreVersion is null)
            {
                return false;
            }

            if (LocalVersion >= PreVersion)
            {
                return false;
            }

            if (!launchOptions.TryGetGameFileSystem(out _))
            {
                return false;
            }

            return !IsPredownloadFinished;
        }
    }

    public bool IsPredownloadFinished
    {
        get
        {
            if (!launchOptions.TryGetGameFileSystem(out GameFileSystem? gameFileSystem))
            {
                return false;
            }

            if (!File.Exists(gameFileSystem.PredownloadStatusPath))
            {
                return false;
            }

            if (JsonSerializer.Deserialize<PredownloadStatus>(File.ReadAllText(gameFileSystem.PredownloadStatusPath)) is { } predownloadStatus)
            {
                int fileCount = Directory.GetFiles(gameFileSystem.ChunksDirectory).Length - 1;
                return predownloadStatus.Finished && fileCount == predownloadStatus.TotalBlocks;
            }

            return false;
        }
    }

    public async ValueTask ForceLoadAsync()
    {
        await LoadOverrideAsync().ConfigureAwait(false);
        await taskContext.SwitchToMainThreadAsync();
        IsInitialized = true;
    }

    protected override async ValueTask<bool> LoadOverrideAsync()
    {
        if (launchGameShared.GetCurrentLaunchSchemeFromConfigFile() is not { } launchScheme)
        {
            return false;
        }

        GameBranchesWrapper? branchesWrapper;
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            HoyoPlayClient hoyoPlayClient = scope.ServiceProvider.GetRequiredService<HoyoPlayClient>();
            Response<GameBranchesWrapper> branchResp = await hoyoPlayClient.GetBranchesAsync(launchScheme).ConfigureAwait(false);

            if (!ResponseValidator.TryValidate(branchResp, serviceProvider, out branchesWrapper))
            {
                return false;
            }
        }

        if (branchesWrapper.GameBranches.FirstOrDefault(b => b.Game.Id == launchScheme.GameId) is { } branch)
        {
            await taskContext.SwitchToMainThreadAsync();
            GameBranch = branch;
            LaunchScheme = launchScheme;

            RemoteVersion = new(branch.Main.Tag);
            PreVersion = branch.PreDownload is { Tag: { } tag } ? new(tag) : default;

            if (!launchOptions.TryGetGameFileSystem(out GameFileSystem? gameFileSystem))
            {
                return true;
            }

            if (gameFileSystem.TryGetGameVersion(out string? localVersion))
            {
                LocalVersion = new(localVersion);
            }

            if (!IsUpdateAvailable && PreVersion is null && File.Exists(gameFileSystem.PredownloadStatusPath))
            {
                File.Delete(gameFileSystem.PredownloadStatusPath);
            }

            return true;
        }

        return false;
    }

    [Command("StartCommand")]
    private async Task StartAsync(string state)
    {
        if (!IsInitialized)
        {
            return;
        }

        GamePackageOperationKind operationKind = Enum.Parse<GamePackageOperationKind>(state);

        if (!launchOptions.TryGetGameFileSystem(out GameFileSystem? gameFileSystem))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(GameBranch);
        ArgumentNullException.ThrowIfNull(LaunchScheme);
        ArgumentNullException.ThrowIfNull(LocalVersion);

        LaunchScheme targetLaunchScheme = LaunchScheme;

        GameChannelSDKsWrapper? channelSDKsWrapper;
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            HoyoPlayClient hoyoPlayClient = scope.ServiceProvider.GetRequiredService<HoyoPlayClient>();
            Response<GameChannelSDKsWrapper> sdkResp = await hoyoPlayClient.GetChannelSDKAsync(targetLaunchScheme).ConfigureAwait(false);

            if (!ResponseValidator.TryValidate(sdkResp, serviceProvider, out channelSDKsWrapper))
            {
                return;
            }
        }

        GameChannelSDK? gameChannelSDK = channelSDKsWrapper.GameChannelSDKs.FirstOrDefault(sdk => sdk.Game.Id == targetLaunchScheme.GameId);

        GamePackageOperationContext context = new(
            serviceProvider,
            operationKind,
            gameFileSystem,
            GameBranch.Main.GetTaggedCopy(LocalVersion.ToString()),
            operationKind is GamePackageOperationKind.Predownload ? GameBranch.PreDownload : GameBranch.Main,
            gameChannelSDK,
            default);

        if (!await gamePackageService.StartOperationAsync(context).ConfigureAwait(false))
        {
            // Operation canceled
            return;
        }

        await taskContext.SwitchToMainThreadAsync();

        switch (operationKind)
        {
            case GamePackageOperationKind.Verify:
                break;
            case GamePackageOperationKind.Update:
                LocalVersion = RemoteVersion;
                OnPropertyChanged(nameof(IsUpdateAvailable));
                break;
            case GamePackageOperationKind.Predownload:
                OnPropertyChanged(nameof(IsPredownloadButtonEnabled));
                OnPropertyChanged(nameof(IsPredownloadFinished));
                break;
        }
    }
}