using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchManagerNGO : NetworkBehaviour
{
    public static MatchManagerNGO Instance { get; private set; }

    [Header("Rules")]
    [SerializeField] private float soloMatchSeconds = 120f;   
    [SerializeField] private float multiMatchSeconds = 180f;  

    private float matchDurationSeconds; 

    [Header("UI (optional)")]
    [SerializeField] private EndMatchUI endMatchUI;

    public NetworkVariable<bool> MatchEnded = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> WinnerName = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> WinnerCoins = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly List<PlayerState> players = new List<PlayerState>();
    private float serverStartTime;

    private void Awake()
    {
        GameFreeze.MatchEnded = false; 
        
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        GameFreeze.MatchEnded = false;

        if (endMatchUI == null)
            endMatchUI = FindFirstObjectByType<EndMatchUI>(FindObjectsInactive.Include);

        MatchEnded.OnValueChanged += OnMatchEndedChanged;
        WinnerName.OnValueChanged += (_, __) => TryRefreshUI();
        WinnerCoins.OnValueChanged += (_, __) => TryRefreshUI();

        if (IsServer)
        {
            serverStartTime = Time.time;

            int totalNow = GetTotalPlayersNowServer();

            matchDurationSeconds = (totalNow <= 1) ? soloMatchSeconds : multiMatchSeconds;

            MatchEnded.Value = false;
            WinnerName.Value = default;
            WinnerCoins.Value = 0;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        MatchEnded.OnValueChanged -= OnMatchEndedChanged;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        if (MatchEnded.Value) return;

        if (matchDurationSeconds > 0f && Time.time - serverStartTime >= matchDurationSeconds)
        {
            EndMatch_Server();
        }
    }


    public void RegisterPlayer(PlayerState ps) => RegisterPlayerServer(ps);
    public void UnregisterPlayer(PlayerState ps) => UnregisterPlayerServer(ps);

    public void RegisterPlayerServer(PlayerState ps)
    {
        if (!IsServer || ps == null) return;
        if (!players.Contains(ps)) players.Add(ps);
    }

    public void UnregisterPlayerServer(PlayerState ps)
    {
        if (!IsServer || ps == null) return;
        players.Remove(ps);
    }


    public void NotifyPlayerDiedServer(PlayerState ps)
    {
        if (!IsServer) return;
        if (MatchEnded.Value) return;

        EnsurePlayersListServer();

        int totalNow = GetTotalPlayersNowServer();
        if (totalNow < 2)
            return; 

        int aliveCount = GetAliveCountServer(out var lastAlive);
        if (aliveCount == 1 && lastAlive != null)
        {
            EndMatch_Server(forcedWinner: lastAlive);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        EnsurePlayersListServer();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        if (MatchEnded.Value) return;

        EnsurePlayersListServer();

        int totalNow = GetTotalPlayersNowServer();
        if (totalNow < 2)
            return;

        int aliveCount = GetAliveCountServer(out var lastAlive);
        if (aliveCount == 1 && lastAlive != null)
        {
            EndMatch_Server(forcedWinner: lastAlive);
        }
    }


    private void EndMatch_Server(PlayerState forcedWinner = null)
    {
        if (!IsServer) return;
        if (MatchEnded.Value) return;

        EnsurePlayersListServer();

        PlayerState winner = forcedWinner != null ? forcedWinner : DetermineWinner_Server();

        if (winner != null)
        {
            WinnerName.Value = new FixedString64Bytes(winner.PlayerName.Value.ToString());

            WinnerCoins.Value = winner.GetTotalCoinsCollected();
        }
        else
        {
            WinnerName.Value = new FixedString64Bytes("NOBODY");
            WinnerCoins.Value = 0;
        }

        MatchEnded.Value = true;
        ShowEndMatchUIClientRpc();
    }

    private PlayerState DetermineWinner_Server()
    {
        PlayerState bestAlive = null;

        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p == null) continue;
            if (!p.IsAliveServer()) continue;

            if (bestAlive == null || p.GetTotalCoinsCollected() > bestAlive.GetTotalCoinsCollected())
                bestAlive = p;
        }

        if (bestAlive != null) return bestAlive;

        PlayerState bestAny = null;
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p == null) continue;

            if (bestAny == null || p.GetTotalCoinsCollected() > bestAny.GetTotalCoinsCollected())
                bestAny = p;
        }

        return bestAny;
    }

    private void OnMatchEndedChanged(bool prev, bool next)
    {
        if (next)
        {
            GameFreeze.MatchEnded = true;
            TryRefreshUI();
        }
        else
        {
            GameFreeze.MatchEnded = false;
        }
    }

    private void TryRefreshUI()
    {
        if (endMatchUI == null)
            endMatchUI = FindFirstObjectByType<EndMatchUI>(FindObjectsInactive.Include);

        if (endMatchUI != null)
            endMatchUI.RefreshFromMatchManager(this);
    }

    [ClientRpc]
    private void ShowEndMatchUIClientRpc()
    {
        TryRefreshUI();
        if (endMatchUI != null) endMatchUI.Show();
    }


    public void RequestRematch()
    {
        if (IsServer) StartRematchServer();
        else RequestRematchServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestRematchServerRpc(RpcParams rpcParams = default)
    {
        StartRematchServer();
    }

    private void StartRematchServer()
    {
        if (!IsServer) return;


        var sceneName = SceneManager.GetActiveScene().name;
        NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }


    private int GetTotalPlayersNowServer()
    {
        if (!IsServer) return 0;
        if (NetworkManager.Singleton == null) return players.Count;

        if (NetworkManager.Singleton.ConnectedClientsList != null)
            return NetworkManager.Singleton.ConnectedClientsList.Count;

        return NetworkManager.Singleton.ConnectedClients.Count;
    }

    private int GetAliveCountServer(out PlayerState lastAlive)
    {
        int alive = 0;
        lastAlive = null;

        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p == null) continue;
            if (!p.IsAliveServer()) continue;

            alive++;
            lastAlive = p;
        }

        return alive;
    }

    private void EnsurePlayersListServer()
    {
        if (!IsServer) return;
        if (NetworkManager.Singleton == null) return;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var cc = kv.Value;
            if (cc == null || cc.PlayerObject == null) continue;

            var ps = cc.PlayerObject.GetComponent<PlayerState>();
            if (ps == null) continue;

            if (!players.Contains(ps))
                players.Add(ps);
        }
    }
}
