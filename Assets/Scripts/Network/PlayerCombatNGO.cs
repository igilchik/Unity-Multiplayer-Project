using Unity.Netcode;
using UnityEngine;

public class PlayerCombatNGO : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Sword sword;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private Health health;
    [SerializeField] private Transform hitPointOverride;

    [Header("Hit settings")]
    [SerializeField] private LayerMask hittableMask = 0;
    [SerializeField] private float hitRadius = 0.65f;

    [Header("Optional (plants)")]
    [SerializeField] private LayerMask plantMask = 0;

    private float _nextAttackTime;

    private Transform HitPoint => hitPointOverride != null ? hitPointOverride : (sword != null ? sword.HitPoint : null);
    private float Radius => (sword != null ? sword.HitRadius : hitRadius);

    private void Awake()
    {
        if (playerState == null) playerState = GetComponent<PlayerState>();
        if (health == null) health = GetComponent<Health>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (health != null)
            health.OnDied += Health_OnDied;
    }

    public override void OnNetworkDespawn()
    {
        if (health != null)
            health.OnDied -= Health_OnDied;

        base.OnNetworkDespawn();
    }

    private void Health_OnDied(object sender, System.EventArgs e)
    {
        DisableWeaponLocal();
    }

    private void DisableWeaponLocal()
    {
        if (sword != null)
            sword.gameObject.SetActive(false); 

        enabled = false;
    }

    public void RequestAttack()
    {
        if (!IsOwner || !IsSpawned) return;
        if (sword == null) return;
        if (health != null && health.IsDead) return; 

        float cd = (sword != null) ? sword.AttackCooldown : 0.35f;
        if (Time.time < _nextAttackTime) return;
        _nextAttackTime = Time.time + cd;

        sword.SetAttacking(true);
        sword.PlayAttackFx();
        CancelInvoke(nameof(EndAttackWindow));
        Invoke(nameof(EndAttackWindow), sword.AttackActiveWindow);

        Vector2 dir = GetLocalAttackDir();
        AttackServerRpc(dir);
    }

    private Vector2 GetLocalAttackDir()
    {
        Transform hp = HitPoint;
        if (hp == null) return Vector2.right;

        Vector2 dir = (Vector2)(hp.position - transform.position);
        if (dir.sqrMagnitude < 0.0001f) return Vector2.right;
        return dir.normalized;
    }

    private void EndAttackWindow()
    {
        if (sword != null) sword.SetAttacking(false);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void AttackServerRpc(Vector2 attackDir)
    {
        if (!IsServer) return;
        if (sword == null) return;
        if (health != null && health.IsDead) return;

        Vector2 dir = attackDir;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir = dir.normalized;

        float reach = 0.8f;
        Transform hpT = HitPoint;
        if (hpT != null)
        {
            reach = hpT.localPosition.magnitude;
            if (reach < 0.01f) reach = 0.8f;
        }

        Vector2 hitPos = (Vector2)transform.position + dir * reach;

        AttackClientRpc();

        int damage = 10;
        if (playerState != null)
            damage = playerState.Damage.Value;

        var hits = Physics2D.OverlapCircleAll(hitPos, Radius, hittableMask);

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];

            var targetHealth = col.GetComponentInParent<Health>();
            if (targetHealth == null) continue;

            var targetNO = targetHealth.GetComponent<NetworkObject>();
            if (targetNO == null) continue;

            if (targetNO.OwnerClientId == OwnerClientId) continue;

            if (targetHealth.IsDead) continue;

            targetHealth.ApplyDamageServer(damage, transform.position);
        }


        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            var h = col.GetComponentInParent<Health>();
            var no = (h != null) ? h.GetComponent<NetworkObject>() : null;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void AttackClientRpc()
    {
        if (IsOwner) return;
        if (sword == null) return;
        if (health != null && health.IsDead) return;

        sword.PlayAttackFx();
    }
}
