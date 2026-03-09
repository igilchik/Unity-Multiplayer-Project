using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetUI : MonoBehaviour
{
    private const string PrefNickKey = "player_nickname";

    [Header("UI")]
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private TMP_InputField nicknameInput;

    [Header("Transport")]
    [SerializeField] private UnityTransport transport;

    private void Awake()
    {
        if (transport == null)
            transport = FindFirstObjectByType<UnityTransport>();

        if (nicknameInput != null)
            nicknameInput.text = PlayerPrefs.GetString(PrefNickKey, "Player");
    }

    public void StartHost()
    {
        ApplyAddress();
        SaveNickname();
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        ApplyAddress();
        SaveNickname();
        NetworkManager.Singleton.StartClient();
    }

    private void ApplyAddress()
    {
        if (transport == null) return;

        string addr = addressInput != null ? addressInput.text : "127.0.0.1";
        transport.ConnectionData.Address = string.IsNullOrWhiteSpace(addr) ? "127.0.0.1" : addr;
    }

    private void SaveNickname()
    {
        string nick = nicknameInput != null ? nicknameInput.text : "Player";
        nick = SanitizeNick(nick);

        PlayerPrefs.SetString(PrefNickKey, nick);
        PlayerPrefs.Save();
    }

    private string SanitizeNick(string nick)
    {
        if (string.IsNullOrWhiteSpace(nick)) return "Player";
        nick = nick.Trim();
        if (nick.Length > 16) nick = nick.Substring(0, 16);
        return nick;
    }

    public static string GetSavedNickname()
    {
        return PlayerPrefs.GetString(PrefNickKey, "Player");
    }

    public void ConnectAsClientOnEndEdit(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        ApplyAddress();
        SaveNickname();
        NetworkManager.Singleton.StartClient();
    }
}
