using System;
using UnityEngine;

public class WeaponUpgradeService : MonoBehaviour
{
    [SerializeField] private PlayerState state;

    public event Action OnUpgraded;

    private int lastKnownLevel = -1;

    private static readonly int[] DamageByLevel = { 10, 20, 30 };
    private static readonly int[] UpgradeCostByLevel = { 10, 15 };

    public void Bind(PlayerState ps)
    {
        if (state != null)
            state.OnWeaponLevelChanged -= HandleWeaponLevelChanged;

        state = ps;
        lastKnownLevel = state ? state.WeaponLevel.Value : -1;

        if (state != null)
            state.OnWeaponLevelChanged += HandleWeaponLevelChanged;
    }

    private void HandleWeaponLevelChanged(int oldLevel, int newLevel)
    {
        if (newLevel > oldLevel)
            OnUpgraded?.Invoke();

        lastKnownLevel = newLevel;
    }

    public int Coins => state ? state.Coins.Value : 0;
    public int WeaponLevel => state ? state.WeaponLevel.Value : 0;
    public int Damage => state ? state.Damage.Value : DamageByLevel[0];

    public bool IsMax => state && state.WeaponLevel.Value >= (DamageByLevel.Length - 1);

    public int NextCost
    {
        get
        {
            if (!state) return -1;
            int lvl = state.WeaponLevel.Value;
            if (lvl >= DamageByLevel.Length - 1) return -1;
            return UpgradeCostByLevel[lvl];
        }
    }

    public bool CanUpgrade => state && !IsMax && Coins >= NextCost;

    public void RequestUpgrade()
    {
        if (!state) return;
        state.RequestUpgradeWeapon();
    }

    private void OnDestroy()
    {
        if (state != null)
            state.OnWeaponLevelChanged -= HandleWeaponLevelChanged;
    }
}
