using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public static class CoinSyncClientQueue
{
    private static readonly HashSet<int> pending = new HashSet<int>();

    public static void Collect(int coinId)
    {
        
        if (TrySendNow(coinId))
            return;

        pending.Add(coinId);
    }

    public static void Flush()
    {
        if (pending.Count == 0)
            return;

        var copy = new List<int>(pending);
        for (int i = 0; i < copy.Count; i++)
        {
            if (TrySendNow(copy[i]))
                pending.Remove(copy[i]);
        }
    }

    private static bool TrySendNow(int coinId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
            return false;

        var sync = CoinSyncNGO.Instance != null ? CoinSyncNGO.Instance : Object.FindFirstObjectByType<CoinSyncNGO>();
        if (sync == null || !sync.IsSpawned)
            return false;

        sync.RequestCollectServerRpc(coinId);
        return true;
    }
}