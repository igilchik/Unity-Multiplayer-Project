using System.Collections;
using System.Text;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [FormerlySerializedAs("MainMenuPanel")]
    [SerializeField] private GameObject mainMenuPanel;

    [FormerlySerializedAs("LobbyPanel")]
    [SerializeField] private GameObject lobbyPanel;

    [Header("Main Menu UI")]
    [FormerlySerializedAs("NicknameInput")]
    [SerializeField] private TMP_InputField nicknameInput;

    [FormerlySerializedAs("IPInput")]
    [SerializeField] private TMP_InputField ipInput;

    [FormerlySerializedAs("PortInput")]
    [SerializeField] private TMP_InputField portInput;

    [FormerlySerializedAs("StatusText")]
    [SerializeField] private TMP_Text statusText;

    [Header("Lobby UI")]
    [FormerlySerializedAs("PlayersListText")]
    [SerializeField] private TMP_Text playersListText;

    [FormerlySerializedAs("StartMatchButton")]
    [SerializeField] private Button startMatchButton;

    [FormerlySerializedAs("LeaveLobbyButton")]
    [SerializeField] private Button leaveLobbyButton;

    [Header("Defaults")]
    [SerializeField] private string defaultNickname = "player";
    [SerializeField] private string defaultIp = "127.0.0.1";
    [SerializeField] private ushort defaultPort = 7778;

    private Coroutine _sendNickRoutine;
    private Coroutine _startRoutine;
    private bool _nicknameSent;
    private bool _callbacksHooked;

    private const string PREF_NICK = "player_nickname";
    private string _pendingNickname = "";

    private void Awake()
    {
        if (nicknameInput != null)
        {
            string saved = PlayerPrefs.GetString(PREF_NICK, "");
            if (!string.IsNullOrWhiteSpace(saved))
                nicknameInput.text = saved;

            nicknameInput.onValueChanged.AddListener(_ => SaveNickname());
            nicknameInput.onEndEdit.AddListener(_ => SaveNickname());
        }

        if (nicknameInput != null && string.IsNullOrWhiteSpace(nicknameInput.text))
            nicknameInput.text = defaultNickname;

        SaveNickname();

        if (ipInput != null && string.IsNullOrWhiteSpace(ipInput.text))
            ipInput.text = defaultIp;

        if (portInput != null && string.IsNullOrWhiteSpace(portInput.text))
            portInput.text = defaultPort.ToString();

        ShowMainMenu();
        SetStatus("Not connected");

        if (startMatchButton != null)
            startMatchButton.interactable = false;

        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsListening || nm.IsClient || nm.IsServer))
        {
            Debug.LogWarning("[MainMenuUI] Network already running on menu load -> Shutdown()");
            nm.Shutdown();
        }
    }

    private void OnEnable()
    {
        HookNetCallbacks();
    }

    private void OnDisable()
    {
        UnhookLobbyEvents();
        UnhookNetCallbacks();
    }


    public void OnClickHost()
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null)
        {
            SetStatus("Missing NetworkManager");
            return;
        }

        if (!ushort.TryParse(portInput != null ? portInput.text : "", out ushort port))
        {
            SetStatus("Wrong port");
            return;
        }

        SaveNickname();
        string nick = GetCommittedNickname(); 
        LobbyManagerNGO.LocalDesiredNickname = nick;
        ApplyNicknameToConnectionData(nick);

        if (nm.IsListening || nm.IsClient || nm.IsServer)
            nm.Shutdown();

        var utp = nm.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (utp == null)
        {
            SetStatus("Missing UnityTransport");
            return;
        }

        utp.SetConnectionData("127.0.0.1", port, "0.0.0.0");

        LobbyManagerNGO.EnsureConnectionApproval(nm);

        bool ok = nm.StartHost();
        if (!ok)
        {
            SetStatus("StartHost FAILED");
            return;
        }

        ShowLobby();
        SetStatus("Hosting...");

        if (startMatchButton != null)
            startMatchButton.interactable = true;

        RefreshPlayersList();
    }

    public void OnClickJoin()
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null)
        {
            SetStatus("Missing NetworkManager");
            return;
        }

        string ip = ipInput != null ? ipInput.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(ip))
        {
            SetStatus("Enter the host IP address");
            return;
        }

        if (!ushort.TryParse(portInput != null ? portInput.text : "", out ushort port))
        {
            SetStatus("Wrong port");
            return;
        }

        SaveNickname();
        string nick = GetCommittedNickname(); 
        LobbyManagerNGO.LocalDesiredNickname = nick;
        ApplyNicknameToConnectionData(nick);

        if (nm.IsListening || nm.IsClient || nm.IsServer)
            nm.Shutdown();

        var utp = nm.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (utp == null)
        {
            SetStatus("Missing UnityTransport");
            return;
        }

        utp.SetConnectionData(ip, port);

        LobbyManagerNGO.EnsureConnectionApproval(nm);

        bool ok = nm.StartClient();
        if (!ok)
        {
            SetStatus("StartClient FAILED");
            return;
        }

        ShowLobby();
        SetStatus("Joining...");

        if (startMatchButton != null)
            startMatchButton.interactable = false;

        RefreshPlayersList();
    }

    public void OnClickStartMatch()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
        {
            SetStatus("Network is not working");
            return;
        }

        if (!nm.IsHost)
        {
            SetStatus("Only HOST can start the match");
            return;
        }

        if (LobbyManagerNGO.Instance == null)
        {
            SetStatus("LobbyManager not ready");
            return;
        }

        LobbyManagerNGO.Instance.StartMatch();
        SetStatus("Loading Game...");
    }

    public void OnClickLeaveLobby()
    {
        StopNickRoutine();
        _nicknameSent = false;

        UnhookLobbyEvents();

        if (LobbyManagerNGO.Instance != null)
            LobbyManagerNGO.Instance.LeaveLobby();
        else
            ForceShutdownOnly();

        ShowMainMenu();
        SetStatus("Not connected");

        if (startMatchButton != null)
            startMatchButton.interactable = false;

        if (playersListText != null)
            playersListText.text = "";
    }


    private void SaveNickname()
    {
        if (nicknameInput == null) return;

        string nick = nicknameInput.text;
        if (string.IsNullOrWhiteSpace(nick))
            nick = defaultNickname;

        PlayerPrefs.SetString(PREF_NICK, nick.Trim());
        PlayerPrefs.Save();
    }

    private string GetCommittedNickname()
    {
        if (nicknameInput != null)
        {
            nicknameInput.DeactivateInputField();
            nicknameInput.ForceLabelUpdate();
        }

        string nick = nicknameInput != null ? nicknameInput.text : "";
        if (string.IsNullOrWhiteSpace(nick))
            nick = defaultNickname;

        return nick.Trim();
    }

    private void ApplyNicknameToConnectionData(string nick)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (string.IsNullOrWhiteSpace(nick))
            nick = defaultNickname;

        nick = nick.Trim();
        if (nick.Length > 24) nick = nick.Substring(0, 24);

        nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(nick);
    }


    private void ApplyTransport(string connectAddress, ushort port, string listenAddress = "0.0.0.0")
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        var utp = nm.GetComponent<UnityTransport>();
        if (utp == null)
        {
            SetStatus("Missing UnityTransport");
            return;
        }

        utp.SetConnectionData(connectAddress, port, listenAddress);
        Debug.Log($"[MainMenuUI] Transport => connect={connectAddress}:{port} listen={listenAddress}");
    }

    private void ForceShutdownOnly()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (nm.IsListening || nm.IsClient || nm.IsServer)
            nm.Shutdown();
    }

    private IEnumerator EnsureStoppedThen(System.Action startAction)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) yield break;

        if (nm.IsListening || nm.IsClient || nm.IsServer)
        {
            nm.Shutdown();
            yield return null;
            yield return null;
        }

        _nicknameSent = false;
        StopNickRoutine();

        startAction?.Invoke();
    }


    private void HookNetCallbacks()
    {
        if (_callbacksHooked) return;

        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
        nm.OnTransportFailure += OnTransportFailure;

        _callbacksHooked = true;
    }

    private void UnhookNetCallbacks()
    {
        if (!_callbacksHooked) return;

        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnClientConnectedCallback -= OnClientConnected;
        nm.OnClientDisconnectCallback -= OnClientDisconnected;
        nm.OnTransportFailure -= OnTransportFailure;

        _callbacksHooked = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (clientId != nm.LocalClientId) return;

        ShowLobby();
        SetStatus(nm.IsHost ? "Host connected" : "Client connected");

        if (startMatchButton != null)
            startMatchButton.interactable = nm.IsHost;

        RefreshPlayersList();

        if (!_nicknameSent)
        {
            StopNickRoutine();
            _sendNickRoutine = StartCoroutine(SendNicknameWhenLobbyReady());
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (clientId != nm.LocalClientId) return;

        StopNickRoutine();
        _nicknameSent = false;

        UnhookLobbyEvents();

        ShowMainMenu();
        SetStatus("Disconnected");

        if (startMatchButton != null)
            startMatchButton.interactable = false;

        if (playersListText != null)
            playersListText.text = "";
    }

    private void OnTransportFailure()
    {
        Debug.LogError("[MainMenuUI] Transport failure");
        StopNickRoutine();
        _nicknameSent = false;

        UnhookLobbyEvents();
        ShowMainMenu();
        SetStatus("Transport failure");
    }


    private void HookLobbyEvents()
    {
        if (LobbyManagerNGO.Instance != null)
            LobbyManagerNGO.Instance.OnPlayersChanged += RefreshPlayersList;
    }

    private void UnhookLobbyEvents()
    {
        if (LobbyManagerNGO.Instance != null)
            LobbyManagerNGO.Instance.OnPlayersChanged -= RefreshPlayersList;
    }

    private IEnumerator SendNicknameWhenLobbyReady()
    {
        float t = 0f;
        const float timeout = 8f;

        while (t < timeout)
        {
            if (LobbyManagerNGO.Instance != null && LobbyManagerNGO.Instance.IsSpawned)
                break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (LobbyManagerNGO.Instance == null || !LobbyManagerNGO.Instance.IsSpawned)
        {
            Debug.LogWarning("[MainMenuUI] LobbyManager not ready (timeout), nickname not sent yet.");
            yield break;
        }

        UnhookLobbyEvents();
        HookLobbyEvents();

        RefreshPlayersList();

        if (_nicknameSent) yield break;

        string nick = string.IsNullOrWhiteSpace(_pendingNickname) ? defaultNickname : _pendingNickname;
        LobbyManagerNGO.LocalDesiredNickname = nick;

        LobbyManagerNGO.Instance.SubmitNicknameServerRpc(new Unity.Collections.FixedString32Bytes(nick));
        _nicknameSent = true;

        yield return null;
        yield return null;

        RefreshPlayersList();
    }

    private void StopNickRoutine()
    {
        if (_sendNickRoutine != null)
        {
            StopCoroutine(_sendNickRoutine);
            _sendNickRoutine = null;
        }
    }


    private void RefreshPlayersList()
    {
        if (playersListText == null) return;

        if (LobbyManagerNGO.Instance == null)
        {
            playersListText.text =
                "Players (max 4):\n" +
                "1) ---\n" +
                "2) ---\n" +
                "3) ---\n" +
                "4) ---\n";
            return;
        }

        playersListText.text = LobbyManagerNGO.Instance.BuildPlayersListString();
    }

    private void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
    }

    private void ShowLobby()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
    }

    private void SetStatus(string s)
    {
        if (statusText != null)
            statusText.text = "Status: " + s;
    }
}