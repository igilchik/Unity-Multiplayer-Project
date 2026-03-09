using TMPro;
using UnityEngine;

public class CoinsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text coinsText;

    private PlayerState target;

    public void SetTarget(PlayerState ps)
    {
        // отписка от старого
        if (target != null)
            target.OnCoinsChanged -= HandleCoinsChanged;

        target = ps;

        if (target != null)
        {
            target.OnCoinsChanged += HandleCoinsChanged;
            coinsText.text = target.Coins.Value.ToString();
        }
        else
        {
            coinsText.text = "-";
        }
    }

    private void HandleCoinsChanged(int oldValue, int newValue)
    {
        coinsText.text = newValue.ToString();
    }

    private void OnDestroy()
    {
        if (target != null)
            target.OnCoinsChanged -= HandleCoinsChanged;
    }
}
