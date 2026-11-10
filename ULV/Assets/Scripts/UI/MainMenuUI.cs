using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú principal (MainMenu): datos del jugador + navegación entre paneles.
/// Los textos con {0} se pueden cambiar en la sección "Textos".
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Datos del jugador")]
    public TMP_Text usernameText;
    public TMP_Text totalScoreText;
    public TMP_Text lastScoreText;
    public TMP_Text maxLevelText;

    [Header("Paneles")]
    public GameObject homePanel;
    public GameObject levelsPanel;
    public GameObject rankingPanel;
    public GameObject settingsPanel;

    [Header("Botones")]
    public Button playButton;
    public Button rankingButton;
    public Button settingsButton;
    public Button logoutButton;
    [Tooltip("Botones 'Volver' de los otros paneles")]
    public Button[] backButtons;

    [Header("Otros")]
    public LevelMapUI levelSelect;

    [Header("Textos")]
    public string totalScoreFormat = "Puntaje total: {0}";
    public string lastScoreFormat = "Última partida: {0}";
    public string maxLevelFormat = "Nivel máximo: {0} / {1}";

    private void Start()
    {
        playButton.onClick.AddListener(() => ShowPanel(levelsPanel));
        rankingButton.onClick.AddListener(() => ShowPanel(rankingPanel));
        if (settingsButton != null) settingsButton.onClick.AddListener(() => ShowPanel(settingsPanel));
        logoutButton.onClick.AddListener(() => FirebaseManager.Instance.Logout());
        foreach (Button b in backButtons) b.onClick.AddListener(() => ShowPanel(homePanel));

        ShowPanel(homePanel);
        RefreshFromFirebase();
    }

    private async void RefreshFromFirebase()
    {
        if (FirebaseManager.Instance == null || FirebaseManager.Instance.CurrentUser == null)
        {
            Debug.LogWarning("Abre el juego desde LoginScene para tener sesión iniciada.");
            return;
        }

        RefreshTexts();
        try
        {
            await FirebaseManager.Instance.LoadProgressAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("No se pudo recargar el progreso: " + e.Message);
        }
        if (this == null) return;
        RefreshTexts();
        if (levelSelect != null && levelSelect.isActiveAndEnabled) levelSelect.Build();
    }

    private void RefreshTexts()
    {
        PlayerProgress p = FirebaseManager.Instance.Progress;
        if (p == null) return;

        usernameText.text = p.username;
        totalScoreText.text = string.Format(totalScoreFormat, p.totalScore);
        lastScoreText.text = string.Format(lastScoreFormat, p.currentRunScore);
        maxLevelText.text = string.Format(maxLevelFormat, p.maxUnlockedLevel, PlayerProgress.TotalLevels);
    }

    private void ShowPanel(GameObject panel)
    {
        homePanel.SetActive(panel == homePanel);
        levelsPanel.SetActive(panel == levelsPanel);
        rankingPanel.SetActive(panel == rankingPanel);
        if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
    }
}
