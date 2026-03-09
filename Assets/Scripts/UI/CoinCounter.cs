using System;
using UnityEngine;

public class CoinCounter : MonoBehaviour
{
    public int Coins { get; private set; }

    public event Action<int> OnCoinsChanged;

    public bool CanSpend(int amount) => Coins >= amount;

    public void AddCoin(int amount = 1)
    {
        Coins += amount;
        OnCoinsChanged?.Invoke(Coins);
    }

    public bool TrySpend(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }
}