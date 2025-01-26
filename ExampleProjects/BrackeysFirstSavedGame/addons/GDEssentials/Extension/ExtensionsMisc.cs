using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chomp.Essentials;

public static class ExtensionsMisc
{
    // This is to ease in conversion of scripts from Unity to Godot.
    public static CancellationTokenSource StopAllCoroutines(this CancellationTokenSource cts) {
        cts.Cancel();
        cts.Dispose();
        return new CancellationTokenSource();
    }

    public static void CancelAndDispose(this CancellationTokenSource cts) {
        cts.Cancel();
        cts.Dispose();
    }
}
