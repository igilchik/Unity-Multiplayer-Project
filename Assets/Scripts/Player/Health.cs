using System;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    public event EventHandler OnHealthChanged;
    public event EventHandler OnDied;
    public event EventHandler<OnDamageEventArgs> OnDamaged;

    [SerializeField] private int maxHealth = 100;

    public bool IsDead => currentHealth.Value <= 0;

    private NetworkVariable<int> currentHealth = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public class OnDamageEventArgs : EventArgs
    {
        public Vector2 damageSourcePosition;
    }

    private bool diedInvoked;
    private bool deathFxSent;

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthValueChanged;

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        OnHealthChanged?.Invoke(this, EventArgs.Empty);
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthValueChanged;
    }

    public void ApplyDamageServer(int amount, Vector2 sourcePos)
    {
        if (!IsServer) return;
        if (IsDead) return;
        if (amount <= 0) return;

        int old = currentHealth.Value;
        currentHealth.Value = Mathf.Max(0, currentHealth.Value - amount);

        OnDamaged?.Invoke(this, new OnDamageEventArgs { damageSourcePosition = sourcePos });
        DamageFeedbackClientRpc(sourcePos);

        OnHealthChanged?.Invoke(this, System.EventArgs.Empty);

        if (currentHealth.Value <= 0 && !diedInvoked)
        {
            diedInvoked = true;

            if (!deathFxSent)
            {
                deathFxSent = true;
                DieClientRpc();
            }

            OnDied?.Invoke(this, System.EventArgs.Empty);
        }
    }

    private void OnHealthValueChanged(int oldValue, int newValue)
    {
        OnHealthChanged?.Invoke(this, EventArgs.Empty);

        if (!diedInvoked && oldValue > 0 && newValue <= 0)
        {
            diedInvoked = true;

            if (IsServer && !deathFxSent)
            {
                deathFxSent = true;
                DieClientRpc();
            }

            OnDied?.Invoke(this, EventArgs.Empty);
        }
    }

    public int GetHealth() => currentHealth.Value;
    public int GetMaxHealth() => maxHealth;

    public void TakeDamage(int damage, Vector2 damageSourcePosition)
    {
        if (damage <= 0) return;

        if (IsServer) ApplyDamageServer(damage, damageSourcePosition);
        else TakeDamageServerRpc(damage, damageSourcePosition);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TakeDamageServerRpc(int damage, Vector2 damageSourcePosition)
    {
        ApplyDamageServer(damage, damageSourcePosition);
    }

    [Rpc(SendTo.Everyone)]
    private void DieClientRpc()
    {
        var visual = GetComponentInChildren<PlayerVisual>(true);
        if (visual != null)
            visual.PlayDeath();
    }

    [Rpc(SendTo.Everyone)]
    private void DamageFeedbackClientRpc(Vector2 damageSourcePosition)
    {
        OnDamaged?.Invoke(this, new OnDamageEventArgs { damageSourcePosition = damageSourcePosition });
    }
}

