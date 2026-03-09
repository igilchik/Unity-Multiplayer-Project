using System;
using UnityEngine;

public class Sword : MonoBehaviour
{
    [Header("Visual (optional)")]
    [SerializeField] private SwordVisual swordVisual;

    [Header("HitPoint / Overlap")]
    [SerializeField] private Transform hitPoint;
    [SerializeField] private float hitRadius = 0.65f;

    [Header("Timing")]
    [SerializeField] private float attackCooldown = 0.35f;
    [SerializeField] private float attackActiveWindow = 0.15f;

    public event EventHandler OnSwordSwing;

    public bool IsAttacking { get; private set; }

    public Transform HitPoint => hitPoint;
    public float HitRadius => hitRadius;
    public float AttackCooldown => attackCooldown;
    public float AttackActiveWindow => attackActiveWindow;

    public void SetAttacking(bool value) => IsAttacking = value;

    public void PlayAttackFx()
    {
        if (swordVisual != null) swordVisual.PlayAttack();
        OnSwordSwing?.Invoke(this, EventArgs.Empty);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (hitPoint == null) return;
        Gizmos.DrawWireSphere(hitPoint.position, hitRadius);
    }
#endif
}
