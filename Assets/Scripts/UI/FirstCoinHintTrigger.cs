using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class FirstCoinHintTrigger : MonoBehaviour
{
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private int needCoins = 10;

    private PlayerState target;

    private void OnEnable()
    {
        StartCoroutine(BindLocalPlayerThenListen());
    }

    private IEnumerator BindLocalPlayerThenListen()
    {
        while (NetworkManager.Singleton == null) yield return null;
        while (!NetworkManager.Singleton.IsListening) yield return null;
        while (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() == null) yield return null;

        var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        target = localPlayer.GetComponent<PlayerState>();

        if (target != null)
        {
            target.OnCoinsChanged += OnCoinsChanged;
            Refresh(target.Coins.Value);
        }
    }

    private void OnCoinsChanged(int oldValue, int newValue) => Refresh(newValue);

    private void Refresh(int coins)
    {
        if (hintPanel == null) return;
        hintPanel.SetActive(coins < needCoins);
    }

    private void OnDestroy()
    {
        if (target != null)
            target.OnCoinsChanged -= OnCoinsChanged;
    }
}
