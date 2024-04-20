﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Win32.Foundation;
using Snap.Hutao.Win32.System.Com;
using Snap.Hutao.Win32.UI.Shell;
using System.IO;
using static Snap.Hutao.Win32.Macros;
using static Snap.Hutao.Win32.Ole32;
using static Snap.Hutao.Win32.Shell32;

namespace Snap.Hutao.Core.IO;

/// <summary>
/// 文件操作
/// </summary>
[HighQuality]
internal static class FileOperation
{
    /// <summary>
    /// 将指定文件移动到新位置，提供指定新文件名和覆盖目标文件（如果它已存在）的选项。
    /// </summary>
    /// <param name="sourceFileName">要移动的文件的名称。 可以包括相对或绝对路径。</param>
    /// <param name="destFileName">文件的新路径和名称。</param>
    /// <param name="overwrite">如果要覆盖目标文件</param>
    /// <returns>是否发生了移动操作</returns>
    public static bool Move(string sourceFileName, string destFileName, bool overwrite)
    {
        if (!File.Exists(sourceFileName))
        {
            return false;
        }

        if (overwrite)
        {
            File.Move(sourceFileName, destFileName, true);
            return true;
        }

        if (File.Exists(destFileName))
        {
            return false;
        }

        File.Move(sourceFileName, destFileName, false);
        return true;
    }

    public static unsafe bool UnsafeMove(string sourceFileName, string destFileName)
    {
        bool result = false;

        HRESULT hr = CoCreateInstance(in Win32.UI.Shell.FileOperation.CLSID, default, CLSCTX.CLSCTX_INPROC_SERVER, in IFileOperation.IID, out IFileOperation* pFileOperation);
        if (SUCCEEDED(hr))
        {
            hr = SHCreateItemFromParsingName(sourceFileName.AsSpan(), default, in IShellItem.IID, out IShellItem* pSourceShellItem);
            if (SUCCEEDED(hr))
            {
                hr = SHCreateItemFromParsingName(destFileName.AsSpan(), default, in IShellItem.IID, out IShellItem* pDestShellItem);
                if (SUCCEEDED(hr))
                {
                    pFileOperation->MoveItem(pSourceShellItem, pDestShellItem, default, default);

                    if (SUCCEEDED(pFileOperation->PerformOperations()))
                    {
                        result = true;
                    }

                    pDestShellItem->Release();
                }

                pSourceShellItem->Release();
            }

            pFileOperation->Release();
        }

        return result;
    }

    public static unsafe bool UnsafeDelete(string path)
    {
        bool result = false;

        HRESULT hr = CoCreateInstance(in Win32.UI.Shell.FileOperation.CLSID, default, CLSCTX.CLSCTX_INPROC_SERVER, in IFileOperation.IID, out IFileOperation* pFileOperation);
        if (SUCCEEDED(hr))
        {
            hr = SHCreateItemFromParsingName(path.AsSpan(), default, in IShellItem.IID, out IShellItem* pShellItem);
            if (SUCCEEDED(hr))
            {
                pFileOperation->DeleteItem(pShellItem, default);

                if (SUCCEEDED(pFileOperation->PerformOperations()))
                {
                    result = true;
                }

                pShellItem->Release();
            }

            pFileOperation->Release();
        }

        return result;
    }
}