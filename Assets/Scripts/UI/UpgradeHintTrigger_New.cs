using UnityEngine;

public class UpgradeHintTrigger_New : MonoBehaviour
{
    [SerializeField] private WeaponUpgradeService upgrade;
    [SerializeField] private BottomHintPulse hint;

    private void Awake()
    {
        if (upgrade == null) upgrade = FindFirstObjectByType<WeaponUpgradeService>();
        if (hint == null) hint = FindFirstObjectByType<BottomHintPulse>();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (upgrade == null) upgrade = FindFirstObjectByType<WeaponUpgradeService>();
        if (hint == null) hint = FindFirstObjectByType<BottomHintPulse>();

        if (upgrade != null)
            upgrade.OnUpgraded += HandleUpgraded;
    }

    private void Unsubscribe()
    {
        if (upgrade != null)
            upgrade.OnUpgraded -= HandleUpgraded;
    }

    private void HandleUpgraded()
    {
        if (hint == null)
        {
            hint = FindFirstObjectByType<BottomHintPulse>();
            if (hint == null) return;
        }

        if (upgrade == null)
        {
            upgrade = FindFirstObjectByType<WeaponUpgradeService>();
            if (upgrade == null) return;
        }

        if (upgrade.IsMax)
        {
            hint.ShowForSeconds("Weapon fully upgraded!", 5f);
            return;
        }

        int nextCost = upgrade.NextCost;
        if (nextCost > 0)
            hint.ShowForSeconds($"Earn {nextCost} coins to upgrade", 5f);
    }
}
