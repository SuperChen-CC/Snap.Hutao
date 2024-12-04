// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Core.Shell;

internal interface IShellLinkInterop
{
    ValueTask<bool> TryCreateDesktopShoutcutForElevatedLaunchAsync();
}