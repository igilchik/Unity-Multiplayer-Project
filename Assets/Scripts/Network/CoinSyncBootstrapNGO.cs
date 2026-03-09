using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class CoinSyncBootstrapNGO : MonoBehaviour
{
    [SerializeField] private NetworkObject coinSyncPrefab;

    private Coroutine bootRoutine;
    private Coroutine spawnRoutine;

    private void OnEnable()
    {
        if (bootRoutine != null) StopCoroutine(bootRoutine);
        bootRoutine = StartCoroutine(BootRoutine());
    }

    private void OnDisable()
    {
        if (bootRoutine != null) StopCoroutine(bootRoutine);
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);

        var nm = NetworkManager.Singleton;
        if (nm != null)
            nm.OnServerStarted -= OnServerStarted;
    }

    private IEnumerator BootRoutine()
    {
        while (NetworkManager.Singleton == null)
            yield return null;

        var nm = NetworkManager.Singleton;


        nm.OnServerStarted -= OnServerStarted;
        nm.OnServerStarted += OnServerStarted;

        TrySpawnNow("BootRoutine()");
    }

    private void OnServerStarted()
    {
        TrySpawnNow("OnServerStarted()");
    }

    public void TrySpawnNow(string from)
    {
        var nm = NetworkManager.Singleton;


        if (nm == null || !nm.IsServer)
            return;

        if (CoinSyncNGO.Instance != null && CoinSyncNGO.Instance.IsSpawned)
        {
            return;
        }

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        yield return null;

        if (coinSyncPrefab == null)
        {
            yield break;
        }

        if (CoinSyncNGO.Instance != null && CoinSyncNGO.Instance.IsSpawned)
        {
            yield break;
        }

        var obj = Instantiate(coinSyncPrefab);
        DontDestroyOnLoad(obj.gameObject);

        obj.Spawn(true);
    }
}