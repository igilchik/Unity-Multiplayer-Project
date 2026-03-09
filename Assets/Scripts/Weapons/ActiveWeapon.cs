using Unity.Netcode;
using UnityEngine;

public class ActiveWeapon : MonoBehaviour
{
    [SerializeField] private Sword sword;

    private Player player;
    private NetworkObject netObj;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        netObj = GetComponentInParent<NetworkObject>();

        if (sword == null)
            sword = GetComponentInChildren<Sword>(true);
    }

    private void Update()
    {
        if (player == null || netObj == null) return;
        if (!netObj.IsOwner) return;
        if (player.IsDead()) return;

        FollowMousePositionLocal();
    }

    public Sword GetActiveWeapon() => sword;

    private void FollowMousePositionLocal()
    {
        if (GameInput.Instance == null) return;

        Vector3 mousePos = GameInput.Instance.GetMousePosition();
        Vector3 playerPos = player.transform.position;

        transform.rotation = (mousePos.x < playerPos.x)
            ? Quaternion.Euler(0, 180, 0)
            : Quaternion.Euler(0, 0, 0);
    }
}
