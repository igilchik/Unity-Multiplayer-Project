using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FixedSpawnManagerNGO : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("SpawnPoints")]
    [SerializeField] private Transform[] spawnPoints;

    private readonly Dictionary<ulong, int> assigned = new();
    private bool[] occupied;

    private void Awake()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            enabled = false;
            return;
        }

        occupied = new bool[spawnPoints.Length];

        if (playerPrefab == null)
        {
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            Bind();
        else
            StartCoroutine(BindWhenReady());
    }

    private void OnDisable()
    {
        Unbind();
    }

    private System.Collections.IEnumerator BindWhenReady()
    {
        while (NetworkManager.Singleton == null)
            yield return null;

        Bind();
    }

    private void Bind()
    {
        var nm = NetworkManager.Singleton;
        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;

        if (nm.IsServer)
        {
            foreach (var kv in nm.ConnectedClients)
            {
                var clientId = kv.Key;
                TrySpawnPlayerFor(clientId);
            }
        }
    }

    private void Unbind()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnClientConnectedCallback -= OnClientConnected;
        nm.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        TrySpawnPlayerFor(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        if (assigned.TryGetValue(clientId, out int idx))
        {
            assigned.Remove(clientId);
            if (idx >= 0 && idx < occupied.Length) occupied[idx] = false;
        }
    }

    private void TrySpawnPlayerFor(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        if (nm.ConnectedClients.TryGetValue(clientId, out var cc) && cc.PlayerObject != null)
        {
            PlaceExistingPlayer(cc.PlayerObject, clientId);
            return;
        }

        int spawnIndex = GetOrAssignSpawnIndex(clientId);
        Vector3 pos = spawnPoints[spawnIndex].position;

        var playerObj = Instantiate(playerPrefab, pos, Quaternion.identity);
        playerObj.SpawnAsPlayerObject(clientId, destroyWithScene: true);

        var rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private void PlaceExistingPlayer(NetworkObject playerObj, ulong clientId)
    {
        int spawnIndex = GetOrAssignSpawnIndex(clientId);
        Vector3 pos = spawnPoints[spawnIndex].position;

        playerObj.transform.position = pos;

        var rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private int GetOrAssignSpawnIndex(ulong clientId)
    {
        if (assigned.TryGetValue(clientId, out int existing))
            return existing;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!occupied[i])
            {
                occupied[i] = true;
                assigned[clientId] = i;
                return i;
            }
        }

        int fallback = assigned.Count % spawnPoints.Length;
        assigned[clientId] = fallback;
        return fallback;
    }
}
