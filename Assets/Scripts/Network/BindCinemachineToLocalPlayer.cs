using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BindCinemachineToLocalPlayer : MonoBehaviour
{
    [SerializeField] private MonoBehaviour virtualCameraBehaviour; 

    private void OnEnable()
    {
        StartCoroutine(BindRoutine());
    }

    private IEnumerator BindRoutine()
    {
        while (NetworkManager.Singleton == null) yield return null;
        while (!NetworkManager.Singleton.IsListening) yield return null;

        var sm = NetworkManager.Singleton.SpawnManager;
        while (sm.GetLocalPlayerObject() == null) yield return null;

        var localPlayer = sm.GetLocalPlayerObject();

        if (virtualCameraBehaviour == null)
        {
            virtualCameraBehaviour = GetComponentInChildren<MonoBehaviour>(true);
        }

        if (virtualCameraBehaviour == null)
        {
            yield break;
        }

        var type = virtualCameraBehaviour.GetType();
        var followProp = type.GetProperty("Follow");
        if (followProp == null || !followProp.CanWrite)
        {
            yield break;
        }

        followProp.SetValue(virtualCameraBehaviour, localPlayer.transform);

        enabled = false;
    }
}
