using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class HUDBinder : MonoBehaviour
{
    [SerializeField] private CoinsUI coinsUI;
    [SerializeField] private WeaponUpgradeUI_Service upgradeUI;
    [SerializeField] private WeaponUpgradeService upgradeService;
    [SerializeField] private HealthBarSpriteUI healthUI;

    private bool bound;

    private void OnEnable()
    {
        bound = false;
        StartCoroutine(BindRoutine());
    }

    private IEnumerator BindRoutine()
    {
        while (NetworkManager.Singleton == null) yield return null;
        while (!NetworkManager.Singleton.IsListening) yield return null;

        var sm = NetworkManager.Singleton.SpawnManager;
        while (sm.GetLocalPlayerObject() == null) yield return null;

        if (bound) yield break;
        bound = true;

        var localPlayer = sm.GetLocalPlayerObject();

        var ps = localPlayer.GetComponent<PlayerState>();
        var h = localPlayer.GetComponent<Health>();


        if (healthUI != null) healthUI.SetTarget(h);
        if (coinsUI != null) coinsUI.SetTarget(ps);
        if (upgradeUI != null) upgradeUI.SetTarget(ps);
        if (upgradeService != null) upgradeService.Bind(ps);
    }
}
