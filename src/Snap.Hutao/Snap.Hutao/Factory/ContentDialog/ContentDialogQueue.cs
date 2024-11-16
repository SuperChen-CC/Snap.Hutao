﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Core.LifeCycle;
using System.Collections.Concurrent;

namespace Snap.Hutao.Factory.ContentDialog;

[Injection(InjectAs.Singleton, typeof(IContentDialogQueue))]
[SuppressMessage("", "SH003")]
[SuppressMessage("", "SH100")]
[SuppressMessage("", "RS0030")]
[ConstructorGenerated]
internal sealed partial class ContentDialogQueue : IContentDialogQueue
{
    private static readonly Action<Task<ContentDialogResult>, object?> Continuation = RunContinuation;

    private readonly ConcurrentQueue<Func<Task>> dialogQueue = [];

    private readonly ICurrentXamlWindowReference currentWindowReference;
    private readonly ILogger<ContentDialogQueue> logger;
    private readonly ITaskContext taskContext;

    public bool IsDialogShowing { get => currentWindowReference.Window is not null && field; private set; }

    public ValueContentDialogTask EnqueueAndShowAsync(Microsoft.UI.Xaml.Controls.ContentDialog contentDialog)
    {
        TaskCompletionSource queueSource = new();
        TaskCompletionSource<ContentDialogResult> resultSource = new();

        dialogQueue.Enqueue(async () =>
        {
            try
            {
                await taskContext.SwitchToMainThreadAsync();
                queueSource.TrySetResult();
                contentDialog.ShowAsync().AsTask()
                    .ContinueWith(Continuation, resultSource, default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                    .SafeForget(logger);
                contentDialog.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            }
            catch (Exception ex)
            {
                resultSource.SetException(ex);
            }
            finally
            {
                ShowNextDialog().SafeForget(logger);
            }
        });

        if (!IsDialogShowing)
        {
            ShowNextDialog();
        }

        return new(queueSource.Task, resultSource.Task);
    }

    private static void RunContinuation(Task<ContentDialogResult> task, object? state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ((TaskCompletionSource<ContentDialogResult>)state).SetResult(task.Result);
    }

    private Task ShowNextDialog()
    {
        if (dialogQueue.TryDequeue(out Func<Task>? showNextDialogAsync))
        {
            IsDialogShowing = true;
            return showNextDialogAsync();
        }

        IsDialogShowing = false;
        return Task.CompletedTask;
    }
}