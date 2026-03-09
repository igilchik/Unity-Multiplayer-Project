using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider2D))]
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int amount = 1;

    private Collider2D _col;
    private CoinId _id;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;

        _id = GetComponent<CoinId>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerPickup"))
            return;

        if (_id == null)
            return;

        var ps = other.GetComponentInParent<PlayerState>();
        if (ps == null)
            return;

        if (!ps.IsOwner)
            return;

        if (MatchManagerNGO.Instance != null && MatchManagerNGO.Instance.MatchEnded.Value) return;

        ps.RequestPickCoin(_id.id, amount);

        if (_col != null) _col.enabled = false;
        gameObject.SetActive(false);

        CoinSyncClientQueue.Collect(_id.id);
    }
}
