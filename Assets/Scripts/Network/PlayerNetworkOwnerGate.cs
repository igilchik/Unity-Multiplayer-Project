using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkOwnerGate : NetworkBehaviour
{
    [Header("Disable behaviours if not owner")]
    [SerializeField] private MonoBehaviour[] disableIfNotOwner;

    [Header("Disable  GameObjects if not owner CLIENTS")]
    [SerializeField] private GameObject[] disableObjectsIfNotOwnerClient;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer) return;

        if (!IsOwner)
        {
            foreach (var mb in disableIfNotOwner)
            {
                if (mb != null) mb.enabled = false;
            }
        }

        if (!IsOwner && !IsServer)
        {
            foreach (var go in disableObjectsIfNotOwnerClient)
            {
                if (go != null) go.SetActive(false);
            }
        }
    }
}