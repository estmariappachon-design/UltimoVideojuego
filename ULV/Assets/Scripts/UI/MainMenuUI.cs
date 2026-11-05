using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú principal (MainMenu): datos del jugador + navegación entre paneles.
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

    [Header("Botones")]
    public Button playButton;
    public Button rankingButton;
    public Button logoutButton;
    public Button[] backButtons;   // botones "Volver" de los paneles de niveles y ranking

    [Header("Otros")]
    public LevelSelectUI levelSelect;

    private void Start()
    {
        playButton.onClick.AddListener(() => ShowPanel(levelsPanel));
        rankingButton.onClick.AddListener(() => ShowPanel(rankingPanel));
        logoutButton.onClick.AddListener(() => FirebaseManager.Instance.Logout());
        foreach (Button b in backButtons) b.onClick.AddListener(() => ShowPanel(homePanel));

        ShowPanel(homePanel);
        RefreshFromFirebase();
    }

    /// <summary>Vuelve a leer el progreso desde Firebase para mostrar datos reales.</summary>
    private async void RefreshFromFirebase()
    {
        if (FirebaseManager.Instance == null || FirebaseManager.Instance.CurrentUser == null)
        {
            Debug.LogWarning("Abre el juego desde LoginScene para tener sesión iniciada.");
            return;
        }

        RefreshTexts(); // muestra lo que ya hay en memoria mientras llega la respuesta
        try
        {
            await FirebaseManager.Instance.LoadProgressAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("No se pudo recargar el progreso: " + e.Message);
        }
        RefreshTexts();
        if (levelSelect != null) levelSelect.Build();
    }

    private void RefreshTexts()
    {
        PlayerProgress p = FirebaseManager.Instance.Progress;
        if (p == null) return;

        usernameText.text = p.username;
        totalScoreText.text = "Puntaje total: " + p.totalScore;
        lastScoreText.text = "Última partida: " + p.currentRunScore;
        maxLevelText.text = $"Nivel máximo: {p.maxUnlockedLevel} / {PlayerProgress.TotalLevels}";
    }

    private void ShowPanel(GameObject panel)
    {
        homePanel.SetActive(panel == homePanel);
        levelsPanel.SetActive(panel == levelsPanel);
        rankingPanel.SetActive(panel == rankingPanel);
    }
}
