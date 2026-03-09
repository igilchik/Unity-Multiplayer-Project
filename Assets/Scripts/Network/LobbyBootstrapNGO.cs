using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class LobbyBootstrapNGO : MonoBehaviour
{
    [SerializeField] private NetworkObject lobbyManagerPrefab;

    private Coroutine _initRoutine;

    private void Awake()
    {
        if (lobbyManagerPrefab == null)
            Debug.LogError("[LobbyBootstrapNGO] LobbyManager Prefab NOT assigned!");
    }

    private void OnEnable()
    {
        if (_initRoutine != null) StopCoroutine(_initRoutine);
        _initRoutine = StartCoroutine(InitWhenNetworkManagerReady());
    }

    private void OnDisable()
    {
        if (_initRoutine != null)
        {
            StopCoroutine(_initRoutine);
            _initRoutine = null;
        }

        var nm = NetworkManager.Singleton;
        if (nm != null)
            nm.OnServerStarted -= HandleServerStarted;
    }

    private IEnumerator InitWhenNetworkManagerReady()
    {
        float t = 0f;
        const float timeout = 5f;

        while (NetworkManager.Singleton == null && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[LobbyBootstrapNGO] NetworkManager.Singleton still null after waiting.");
            yield break;
        }

        nm.OnServerStarted -= HandleServerStarted;
        nm.OnServerStarted += HandleServerStarted;

        if (nm.IsServer && nm.IsListening)
            TrySpawn();
    }

    private void HandleServerStarted()
    {
        TrySpawn();
    }

    private void TrySpawn()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (!nm.IsServer || !nm.IsListening)
            return;

        if (LobbyManagerNGO.Instance != null && LobbyManagerNGO.Instance.IsSpawned)
            return;

        if (lobbyManagerPrefab == null)
        {
            Debug.LogError("[LobbyBootstrapNGO] lobbyManagerPrefab == null");
            return;
        }

        var obj = Instantiate(lobbyManagerPrefab);
        obj.Spawn(destroyWithScene: false);
    }
}