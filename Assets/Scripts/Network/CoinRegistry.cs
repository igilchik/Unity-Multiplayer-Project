using System.Collections.Generic;
using UnityEngine;

public static class CoinRegistry
{
    private static readonly Dictionary<int, CoinId> coins = new();

    public static void Register(CoinId c)
    {
        if (c == null) return;
        coins[c.id] = c;

        if (CoinSyncNGO.Instance != null && CoinSyncNGO.Instance.IsSpawned)
        {
            
        }
    }

    public static void Unregister(CoinId c)
    {
        if (c == null) return;
        if (coins.TryGetValue(c.id, out var cur) && cur == c)
            coins.Remove(c.id);
    }

    public static void Hide(int coinId)
    {
        if (coins.TryGetValue(coinId, out var c) && c != null)
            c.gameObject.SetActive(false);
    }

    public static bool Exists(int coinId)
    {
        return coins.TryGetValue(coinId, out var c) && c != null && c.gameObject.activeInHierarchy;
    }
}