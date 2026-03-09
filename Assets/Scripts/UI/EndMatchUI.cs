using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class EndMatchUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Texts")]
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private TMP_Text coinsText;

    [Header("Buttons")]
    [SerializeField] private Button rematchButton;
    [Header("Buttons")]
    [SerializeField] private Button mainMenuButton;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";


    private void Awake()
    {
        if (root == null) root = gameObject;
        Hide();

        if (rematchButton != null)
            rematchButton.onClick.AddListener(OnClickRematch);

            if (mainMenuButton != null)
        mainMenuButton.onClick.AddListener(OnClickMainMenu);
    }

    public void RefreshFromMatchManager(MatchManagerNGO mm)
    {
        if (mm == null) return;

        string name = mm.WinnerName.Value.ToString();
        int coins = mm.WinnerCoins.Value;

        if (string.IsNullOrWhiteSpace(name))
            name = "NOBODY";

        if (winnerText != null) winnerText.text = $"WINNER: {name}";
        if (coinsText != null)  coinsText.text = $"Total coins: {coins}";
    }

    public void Show()
    {
        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void OnClickRematch()
    {
        GameFreeze.MatchEnded = false; 
        Hide();

        var mm = MatchManagerNGO.Instance != null ? MatchManagerNGO.Instance : FindFirstObjectByType<MatchManagerNGO>();
        if (mm != null)
            mm.RequestRematch();
    }

    private void OnClickMainMenu()
    {
        GameFreeze.MatchEnded = false; 
        Hide();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }
}
