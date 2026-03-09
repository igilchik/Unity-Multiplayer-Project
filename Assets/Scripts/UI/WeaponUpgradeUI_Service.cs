using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUpgradeUI_Service : MonoBehaviour
{
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text costOrMaxText;
    [Range(0f, 1f)][SerializeField] private float inactiveAlpha = 0.35f;

    private PlayerState target;

    public void SetTarget(PlayerState ps)
    {
        if (target != null)
        {
            target.OnCoinsChanged -= OnChanged;
            target.OnWeaponLevelChanged -= OnChanged;
        }

        target = ps;

        upgradeButton.onClick.RemoveListener(OnClickUpgrade);

        if (target != null)
        {
            target.OnCoinsChanged += OnChanged;
            target.OnWeaponLevelChanged += OnChanged;

            upgradeButton.onClick.AddListener(OnClickUpgrade);
            Refresh();
        }
        else
        {
            SetInactiveUnknown();
        }
    }

    private void OnClickUpgrade()
    {
        target?.RequestUpgradeWeapon();
        Refresh();
    }

    private void OnChanged(int _, int __) => Refresh();

    private void Refresh()
    {
        if (target == null) { SetInactiveUnknown(); return; }

        if (target.IsMaxLevel())
        {
            costOrMaxText.text = "MAX";
            upgradeButton.interactable = false;
            SetIconAlpha(inactiveAlpha);
            return;
        }

        int cost = target.GetNextUpgradeCost();
        costOrMaxText.text = cost.ToString();

        bool can = target.Coins.Value >= cost;
        upgradeButton.interactable = can;
        SetIconAlpha(can ? 1f : inactiveAlpha);
    }

    private void SetInactiveUnknown()
    {
        costOrMaxText.text = "-";
        upgradeButton.interactable = false;
        SetIconAlpha(inactiveAlpha);
    }

    private void SetIconAlpha(float a)
    {
        if (icon == null) return;
        var c = icon.color;
        c.a = a;
        icon.color = c;
    }

    private void OnDestroy()
    {
        if (target != null)
        {
            target.OnCoinsChanged -= OnChanged;
            target.OnWeaponLevelChanged -= OnChanged;
        }
    }
}
