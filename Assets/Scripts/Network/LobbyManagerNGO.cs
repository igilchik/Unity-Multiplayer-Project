using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManagerNGO : NetworkBehaviour
{
    public static LobbyManagerNGO Instance { get; private set; }
    public static string LocalDesiredNickname = "";
    public event Action OnPlayersChanged;

    [Serializable]
    public struct LobbyPlayerInfo : INetworkSerializable, IEquatable<LobbyPlayerInfo>
    {
        public ulong ClientId;
        public FixedString32Bytes Nickname;

        public LobbyPlayerInfo(ulong clientId, string nickname)
        {
            ClientId = clientId;
            Nickname = new FixedString32Bytes(string.IsNullOrWhiteSpace(nickname) ? $"player_{clientId}" : nickname.Trim());
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Nickname);
        }

        public bool Equals(LobbyPlayerInfo other) => ClientId == other.ClientId;
    }

    private NetworkList<LobbyPlayerInfo> players;

    private static readonly Dictionary<ulong, string> _pendingNickByClientId = new Dictionary<ulong, string>();
    private static bool _approvalConfigured;

    public static void EnsureConnectionApproval(NetworkManager nm)
    {
        if (nm == null) return;

        nm.NetworkConfig.ConnectionApproval = true;

        nm.ConnectionApprovalCallback = ApprovalCheck;
        _approvalConfigured = true;
    }

    private static void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string nick = "";
        try
        {
            if (request.Payload != null && request.Payload.Length > 0)
                nick = Encoding.UTF8.GetString(request.Payload).Trim();
        }
        catch
        {
            nick = "";
        }

        if (string.IsNullOrWhiteSpace(nick))
            nick = $"player_{request.ClientNetworkId}";

        _pendingNickByClientId[request.ClientNetworkId] = nick;

        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        players = new NetworkList<LobbyPlayerInfo>();
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;

        players.OnListChanged -= OnPlayersListChanged;
        players.OnListChanged += OnPlayersListChanged;

        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            nm.OnClientConnectedCallback -= HandleClientConnected;
            nm.OnClientConnectedCallback += HandleClientConnected;

            nm.OnClientDisconnectCallback -= HandleClientDisconnected;
            nm.OnClientDisconnectCallback += HandleClientDisconnected;

            if (nm.IsServer && !_approvalConfigured)
                EnsureConnectionApproval(nm);
        }

        if (IsServer && nm != null)
        {
            ulong hostId = nm.LocalClientId;

            string hostNick = LocalDesiredNickname;
            if (string.IsNullOrWhiteSpace(hostNick))
                hostNick = $"player_{hostId}";

            AddOrUpdatePlayer(hostId, hostNick);
            OnPlayersChanged?.Invoke();
        }

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            nm.OnClientConnectedCallback -= HandleClientConnected;
            nm.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        if (players != null)
            players.OnListChanged -= OnPlayersListChanged;

        if (Instance == this) Instance = null;

        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        if (players != null)
            players.Dispose();

        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    private void OnPlayersListChanged(NetworkListEvent<LobbyPlayerInfo> _)
    {
        OnPlayersChanged?.Invoke();
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        if (players.Count >= 4)
        {
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        if (!_pendingNickByClientId.TryGetValue(clientId, out string nick) || string.IsNullOrWhiteSpace(nick))
            nick = $"player_{clientId}";

        _pendingNickByClientId.Remove(clientId);

        AddOrUpdatePlayer(clientId, nick);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.LogWarning($"[LobbyManagerNGO] ClientDisconnected: {clientId}");
        if (!IsServer) return;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                players.RemoveAt(i);
                break;
            }
        }

        _pendingNickByClientId.Remove(clientId);
    }

    private void AddOrUpdatePlayer(ulong clientId, string nickname)
    {
        var info = new LobbyPlayerInfo(clientId, nickname);

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                players[i] = info;
                return;
            }
        }

        players.Add(info);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SubmitNicknameServerRpc(FixedString32Bytes nickname, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        string nick = nickname.ToString();
        if (string.IsNullOrWhiteSpace(nick))
            nick = $"player_{sender}";

        AddOrUpdatePlayer(sender, nick);
    }

    public void StartMatch()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return;

        if (!nm.IsHost)
        {
            Debug.LogWarning("[LobbyManagerNGO] StartMatch denied: not host.");
            return;
        }

        if (nm.SceneManager == null)
        {
            Debug.LogError("[LobbyManagerNGO] SceneManager is null. Enable 'Enable Scene Management' in NetworkManager.");
            return;
        }

        nm.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    public void LeaveLobby()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (nm.IsServer)
        {
            if (players != null)
                players.Clear();

            var ids = new List<ulong>(nm.ConnectedClientsIds);
            foreach (var id in ids)
            {
                if (id == NetworkManager.ServerClientId) continue;
                nm.DisconnectClient(id);
            }
        }

        if (nm.IsListening)
            nm.Shutdown();

        if (nm.IsServer && IsSpawned)
            NetworkObject.Despawn(true);

        OnPlayersChanged?.Invoke();
    }

    public string BuildPlayersListString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Players (max 4):");

        ulong hostId = NetworkManager.ServerClientId;

        var ordered = new List<LobbyPlayerInfo>(players.Count);

        for (int i = 0; i < players.Count; i++)
            if (players[i].ClientId == hostId) ordered.Add(players[i]);

        for (int i = 0; i < players.Count; i++)
            if (players[i].ClientId != hostId) ordered.Add(players[i]);

        for (int slot = 0; slot < 4; slot++)
        {
            if (slot < ordered.Count)
            {
                var p = ordered[slot];
                string hostTag = (p.ClientId == hostId) ? " [HOST]" : "";
                sb.AppendLine($"{slot + 1}) {p.Nickname}{hostTag}");
            }
            else
            {
                sb.AppendLine($"{slot + 1}) ---");
            }
        }

        return sb.ToString();
    }

    public string GetNicknameFor(ulong clientId)
    {
        if (players == null)
            return $"player_{clientId}";

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
                return players[i].Nickname.ToString();
        }

        return $"player_{clientId}";
    }
}