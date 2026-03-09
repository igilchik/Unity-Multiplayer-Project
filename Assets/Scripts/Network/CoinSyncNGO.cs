using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class CoinSyncNGO : NetworkBehaviour
{
    public static CoinSyncNGO Instance { get; private set; }

    private NetworkList<int> collectedIds = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        collectedIds.OnListChanged += OnCollectedChanged;

        ApplyAllCollected();

        CoinSyncClientQueue.Flush();

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (collectedIds != null)
            collectedIds.OnListChanged -= OnCollectedChanged;

        if (Instance == this) Instance = null;

        

        base.OnNetworkDespawn();
    }

    private void OnCollectedChanged(NetworkListEvent<int> _)
    {
        StartCoroutine(ApplyNextFrame());
    }

    private IEnumerator ApplyNextFrame()
    {
        yield return null; 
        ApplyAllCollected();
    }

    private void ApplyAllCollected()
    {
        for (int i = 0; i < collectedIds.Count; i++)
            CoinRegistry.Hide(collectedIds[i]);
    }

    public bool IsCollected(int coinId)
    {
        for (int i = 0; i < collectedIds.Count; i++)
            if (collectedIds[i] == coinId) return true;
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestCollectServerRpc(int coinId, ServerRpcParams rpcParams = default)
    {
        for (int i = 0; i < collectedIds.Count; i++)
            if (collectedIds[i] == coinId)
                return;

        collectedIds.Add(coinId);
    }
}