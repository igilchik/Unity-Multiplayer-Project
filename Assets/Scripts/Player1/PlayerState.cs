using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [Header("Coins")]
    [SerializeField] private int startCoins = 0;

    [Header("Weapon / Upgrade")]
    [SerializeField] private int[] damageByLevel = { 10, 20, 30 };
    [SerializeField] private int[] upgradeCosts = { 10, 15 }; 
    [SerializeField] private Health health;

    [Header("Nickname")]
    [SerializeField] private string nicknameDebug;

    public NetworkVariable<int> Coins = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> WeaponLevel = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Damage = new(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsAlive = new(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<Vector2> Facing = new(
        Vector2.down, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString32Bytes> PlayerName = new(
        new FixedString32Bytes("Player"),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public event Action<int, int> OnCoinsChanged;
    public event Action<int, int> OnWeaponLevelChanged;
    public event Action<int, int> OnDamageChanged;
    public event Action<string> OnNameChanged;

    public event Action<PlayerState, bool, bool> IsAliveChanged;

    private readonly HashSet<int> pickedCoinIdsServer = new();

    public override void OnNetworkSpawn()
    {
        Coins.OnValueChanged += HandleCoinsChanged;
        WeaponLevel.OnValueChanged += HandleWeaponLevelChanged;
        Damage.OnValueChanged += HandleDamageChanged;
        PlayerName.OnValueChanged += HandleNameChanged;
        IsAlive.OnValueChanged += HandleAliveChanged;

        if (health == null) health = GetComponent<Health>();
        if (health != null)
        {
            health.OnDied -= HandleHealthDied; 
            health.OnDied += HandleHealthDied;
        }

        if (IsServer)
        {
            Coins.Value = startCoins;
            WeaponLevel.Value = 0;
            Damage.Value = (damageByLevel != null && damageByLevel.Length > 0) ? damageByLevel[0] : 10;

            IsAlive.Value = true;
            Facing.Value = Vector2.down;

            pickedCoinIdsServer.Clear();

            if (LobbyManagerNGO.Instance != null)
            {
                string nick = LobbyManagerNGO.Instance.GetNicknameFor(OwnerClientId);
                PlayerName.Value = new FixedString32Bytes(SanitizeNick(nick));
            }
            else
            {
                PlayerName.Value = new FixedString32Bytes($"Player_{OwnerClientId}");
            }

            if (MatchManagerNGO.Instance != null)
                MatchManagerNGO.Instance.RegisterPlayer(this);
        }

        nicknameDebug = PlayerName.Value.ToString();
    }

    public override void OnNetworkDespawn()
    {
        Coins.OnValueChanged -= HandleCoinsChanged;
        WeaponLevel.OnValueChanged -= HandleWeaponLevelChanged;
        Damage.OnValueChanged -= HandleDamageChanged;
        PlayerName.OnValueChanged -= HandleNameChanged;
        IsAlive.OnValueChanged -= HandleAliveChanged;


        if (health != null)
        health.OnDied -= HandleHealthDied;
        
        if (IsServer && MatchManagerNGO.Instance != null)
            MatchManagerNGO.Instance.UnregisterPlayer(this);
    }

    private void HandleCoinsChanged(int oldV, int newV) => OnCoinsChanged?.Invoke(oldV, newV);
    private void HandleWeaponLevelChanged(int oldV, int newV) => OnWeaponLevelChanged?.Invoke(oldV, newV);
    private void HandleDamageChanged(int oldV, int newV) => OnDamageChanged?.Invoke(oldV, newV);

    private void HandleNameChanged(FixedString32Bytes oldV, FixedString32Bytes newV)
    {
        nicknameDebug = newV.ToString();
        OnNameChanged?.Invoke(nicknameDebug);
    }

    private void HandleAliveChanged(bool oldV, bool newV)
    {
        IsAliveChanged?.Invoke(this, oldV, newV);
        
        if (IsServer && oldV == true && newV == false)
        {
            if (MatchManagerNGO.Instance != null)
                MatchManagerNGO.Instance.NotifyPlayerDiedServer(this);
        }
    }


    private void HandleHealthDied(object sender, System.EventArgs e)
    {
        if (!IsServer) return;

        if (!IsAlive.Value) return;

        IsAlive.Value = false;
    }



    public int MaxWeaponLevel()
    {
        int len = (damageByLevel == null) ? 1 : damageByLevel.Length;
        return Mathf.Max(0, len - 1);
    }

    public bool IsMaxLevel()
    {
        return WeaponLevel.Value >= MaxWeaponLevel();
    }

    public int GetNextUpgradeCost()
    {
        if (IsMaxLevel()) return int.MaxValue;

        int lvl = WeaponLevel.Value;
        if (upgradeCosts == null || upgradeCosts.Length == 0) return int.MaxValue;
        if (lvl < 0 || lvl >= upgradeCosts.Length) return int.MaxValue;
        return upgradeCosts[lvl];
    }

    public void RequestUpgradeWeapon()
    {
        if (MatchManagerNGO.Instance != null && MatchManagerNGO.Instance.MatchEnded.Value) return;
        if (IsServer) ServerTryUpgrade(OwnerClientId);
        else RequestUpgradeWeaponServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestUpgradeWeaponServerRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        ServerTryUpgrade(sender);
    }

    private void ServerTryUpgrade(ulong targetClientId)
    {
        if (!IsServer) return;

        var ps = FindPlayerStateByClientId(targetClientId);
        if (ps == null) return;

        if (ps.IsMaxLevel()) return;

        int cost = ps.GetNextUpgradeCost();
        if (cost == int.MaxValue) return;
        if (ps.Coins.Value < cost) return;

        ps.Coins.Value -= cost;
        ps.WeaponLevel.Value++;

        int lvl = Mathf.Clamp(ps.WeaponLevel.Value, 0, ps.MaxWeaponLevel());
        if (damageByLevel != null && lvl < damageByLevel.Length)
            ps.Damage.Value = damageByLevel[lvl];
    }


    public void RequestPickCoin(int coinId, int amount)
    {
        if (amount <= 0) return;
        if (MatchManagerNGO.Instance != null && MatchManagerNGO.Instance.MatchEnded.Value) return;

        if (IsServer)
        {
            ServerTryPickCoin(OwnerClientId, coinId, amount);
            return;
        }

        RequestPickCoinServerRpc(coinId, amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPickCoinServerRpc(int coinId, int amount, RpcParams rpcParams = default)
    {
        if (amount <= 0) return;

        ulong sender = rpcParams.Receive.SenderClientId;
        ServerTryPickCoin(sender, coinId, amount);
    }

    private void ServerTryPickCoin(ulong targetClientId, int coinId, int amount)
    {
        if (!IsServer) return;

        if (pickedCoinIdsServer.Contains(coinId))
            return;

        pickedCoinIdsServer.Add(coinId);

        var ps = FindPlayerStateByClientId(targetClientId);
        if (ps == null) return;

        ps.Coins.Value += amount;

        HideCoinClientRpc(coinId);
    }

    [Rpc(SendTo.Everyone)]
    private void HideCoinClientRpc(int coinId, RpcParams rpcParams = default)
    {
        var all = UnityEngine.Object.FindObjectsByType<CoinId>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].id == coinId)
            {
                all[i].gameObject.SetActive(false);
                return;
            }
        }
    }

    public int GetSpentCoinsForUpgrades()
    {
        int lvl = WeaponLevel.Value;

        if (upgradeCosts == null || upgradeCosts.Length == 0) return 0;
        if (lvl <= 0) return 0;

        int spent = 0;
        int steps = Mathf.Min(lvl, upgradeCosts.Length);
        for (int i = 0; i < steps; i++)
            spent += upgradeCosts[i];

        return spent;
    }

    public int GetTotalCoinsCollected()
    {
        return Coins.Value + GetSpentCoinsForUpgrades();
    }



    [ServerRpc]
    public void SubmitFacingServerRpc(Vector2 dir)
    {
        if (!IsServer) return;
        if (dir.sqrMagnitude < 0.0001f) return;
        Facing.Value = dir.normalized;
    }


    public string GetName() => PlayerName.Value.ToString();

    private string SanitizeNick(string nick)
    {
        if (string.IsNullOrWhiteSpace(nick)) return "Player";
        nick = nick.Trim();
        if (nick.Length > 16) nick = nick.Substring(0, 16);
        return nick;
    }

    

    public bool IsAliveServer()
    {
        return IsAlive.Value;
    }


    public void ServerResetForNewRound()
    {
        if (!IsServer) return;

        Coins.Value = startCoins;
        WeaponLevel.Value = 0;
        Damage.Value = (damageByLevel != null && damageByLevel.Length > 0) ? damageByLevel[0] : 10;

        IsAlive.Value = true;
        Facing.Value = Vector2.down;

        pickedCoinIdsServer.Clear();
    }


    private PlayerState FindPlayerStateByClientId(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return null;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var cc)) return null;
        if (cc.PlayerObject == null) return null;
        return cc.PlayerObject.GetComponent<PlayerState>();
    }


    public void ServerMarkDead()
    {
        if (!IsServer) return;
        if (!IsAlive.Value) return;

        IsAlive.Value = false; 
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestMarkDeadServerRpc(RpcParams rpcParams = default)
    {
        ServerMarkDead();
    }
}
