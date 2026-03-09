using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSpawnManagerNGO : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private readonly HashSet<ulong> spawned = new HashSet<ulong>();

    private bool hooked;
    private Coroutine initRoutine;

    private void OnEnable()
    {
        if (initRoutine != null) StopCoroutine(initRoutine);
        initRoutine = StartCoroutine(ServerInitRoutine());
    }

    private void OnDisable()
    {
        if (initRoutine != null)
        {
            StopCoroutine(initRoutine);
            initRoutine = null;
        }

        Unhook();
    }

    private IEnumerator ServerInitRoutine()
    {
        while (NetworkManager.Singleton == null)
            yield return null;

        var nm = NetworkManager.Singleton;

        float t = 0f;
        while (!nm.IsListening && t < 10f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!nm.IsListening)
        {
            Debug.LogWarning("NetworkManager is not listening.");
            yield break;
        }

        Hook();

        if (!nm.IsServer)
            yield break;

        yield return null;

        SpawnForAllConnected();
    }

    private void Hook()
    {
        if (hooked) return;

        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnClientDisconnectCallback += OnClientDisconnected;

        if (nm.SceneManager != null)
            nm.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;

        hooked = true;
    }

    private void Unhook()
    {
        if (!hooked) return;

        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnClientDisconnectCallback -= OnClientDisconnected;

        if (nm.SceneManager != null)
            nm.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;

        hooked = false;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        spawned.Remove(clientId);
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        foreach (var clientId in clientsCompleted)
            SpawnForClientIfNeeded(clientId);
    }

    private void SpawnForAllConnected()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        foreach (var clientId in nm.ConnectedClientsIds)
            SpawnForClientIfNeeded(clientId);
    }

    private void SpawnForClientIfNeeded(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer || !nm.IsListening) return;

        if (spawned.Contains(clientId))
            return;

        if (playerPrefab == null)
        {
            Debug.LogError("missing playerPrefab");
            return;
        }

        Vector3 pos = Vector3.zero;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int idx = (int)(clientId % (ulong)spawnPoints.Length);
            pos = spawnPoints[idx].position;
        }

        var obj = Instantiate(playerPrefab, pos, Quaternion.identity);

        obj.SpawnAsPlayerObject(clientId, true);

        spawned.Add(clientId);
    }
}