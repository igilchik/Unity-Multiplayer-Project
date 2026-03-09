using UnityEngine;

public class CoinId : MonoBehaviour
{
    public int id;

    private void OnEnable() => CoinRegistry.Register(this);
    private void OnDisable() => CoinRegistry.Unregister(this);
}