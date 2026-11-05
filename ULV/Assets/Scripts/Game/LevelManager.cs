using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla una partida en GameScene:
///  - lee qué nivel eligió el jugador y su configuración
///  - maneja la meta / condición de victoria, el puntaje y la velocidad
///  - al ganar o perder reporta el resultado a Firebase
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private enum State { Waiting, Playing, Paused, Ended }

    [Header("Referencias")]
    public PigeonController pigeon;
    public ProceduralSpawner spawner;

    [Header("HUD")]
    public TMP_Text levelTitleText;
    public TMP_Text goalText;
    public TMP_Text scoreText;
    public GameObject tapToStartLabel;
    public Button pauseButton;
    public GameObject pausePanel;
    public Button resumeButton;
    public Button pauseMenuButton;

    [Header("Panel de victoria")]
    public GameObject winPanel;
    public TMP_Text winScoreText;
    public TMP_Text winInfoText;
    public Button nextLevelButton;
    public Button winRetryButton;
    public Button winMenuButton;

    [Header("Panel de derrota")]
    public GameObject losePanel;
    public TMP_Text loseScoreText;
    public Button loseRetryButton;
    public Button loseMenuButton;

    [Header("Puntaje")]
    public int pointsPerObstacle = 10;
    public int pointsPerSecondSurvived = 5;
    public int completionBonusPerLevel = 50;   // bono = 50 * número de nivel

    [Header("Pruebas sin Firebase (al darle Play directo a GameScene)")]
    public int testLevel = 1;

    public float CurrentSpeed { get; private set; }

    private State state = State.Waiting;
    private int levelNumber;
    private LevelConfig cfg;
    private int score;
    private int obstaclesPassed;
    private int crumbsCollected;
    private float elapsed;
    private float survivalPointsAccumulator;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        bool online = FirebaseManager.Instance != null && FirebaseManager.Instance.Progress != null;
        levelNumber = online ? FirebaseManager.Instance.SelectedLevel : testLevel;
        cfg = LevelSettings.Get(levelNumber);

        levelTitleText.text = $"Nivel {levelNumber}: {cfg.title}";
        pigeon.OnDied += OnPigeonDied;

        // Botones
        pauseButton.onClick.AddListener(TogglePause);
        resumeButton.onClick.AddListener(TogglePause);
        pauseMenuButton.onClick.AddListener(GoToMenu);
        nextLevelButton.onClick.AddListener(NextLevel);
        winRetryButton.onClick.AddListener(Retry);
        winMenuButton.onClick.AddListener(GoToMenu);
        loseRetryButton.onClick.AddListener(Retry);
        loseMenuButton.onClick.AddListener(GoToMenu);

        winPanel.SetActive(false);
        losePanel.SetActive(false);
        pausePanel.SetActive(false);
        tapToStartLabel.SetActive(true);
        CurrentSpeed = 0f;
        UpdateHUD();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        switch (state)
        {
            case State.Waiting:
                if (TapInput.WasTapped()) StartPlaying();
                break;

            case State.Playing:
                elapsed += Time.deltaTime;

                // Dificultad progresiva: la velocidad sube con el tiempo
                CurrentSpeed = Mathf.Min(cfg.startSpeed + cfg.speedRamp * elapsed, cfg.maxSpeed);

                if (cfg.goal == GoalType.SurviveTime)
                {
                    survivalPointsAccumulator += pointsPerSecondSurvived * Time.deltaTime;
                    if (survivalPointsAccumulator >= 1f)
                    {
                        int whole = Mathf.FloorToInt(survivalPointsAccumulator);
                        survivalPointsAccumulator -= whole;
                        score += whole;
                    }
                    if (elapsed >= cfg.goalAmount) Win();
                }
                UpdateHUD();
                break;
        }
    }

    private void StartPlaying()
    {
        state = State.Playing;
        tapToStartLabel.SetActive(false);
        CurrentSpeed = cfg.startSpeed;
        spawner.Begin(cfg, levelNumber);
        pigeon.Begin();
    }

    // ------------------- Eventos del juego -------------------

    public void ObstaclePassed()
    {
        if (state != State.Playing) return;
        obstaclesPassed++;
        score += pointsPerObstacle;
        UpdateHUD();
        if (cfg.goal == GoalType.PassObstacles && obstaclesPassed >= cfg.goalAmount) Win();
    }

    public void CrumbCollected(int points)
    {
        if (state != State.Playing) return;
        crumbsCollected++;
        score += points;
        UpdateHUD();
        if (cfg.goal == GoalType.CollectCrumbs && crumbsCollected >= cfg.goalAmount) Win();
    }

    private void OnPigeonDied()
    {
        if (state != State.Playing) return;
        EndGame();
        StartCoroutine(ShowLoseAfterDelay());
    }

    // ------------------- Victoria / derrota -------------------

    private async void Win()
    {
        EndGame();
        pigeon.Freeze();

        int bonus = completionBonusPerLevel * levelNumber;
        score += bonus;
        UpdateHUD();

        winPanel.SetActive(true);
        winScoreText.text = $"Puntaje: {score}";
        winInfoText.text = $"Incluye bono de nivel: +{bonus}\nGuardando...";
        nextLevelButton.gameObject.SetActive(levelNumber < LevelSettings.Count);
        SetEndButtons(false);

        if (FirebaseManager.Instance != null && FirebaseManager.Instance.Progress != null)
        {
            bool unlocked = await FirebaseManager.Instance.ReportLevelResultAsync(levelNumber, score, true);
            if (this == null) return;
            int total = FirebaseManager.Instance.Progress.totalScore;
            winInfoText.text = $"Bono de nivel: +{bonus}\nPuntaje total: {total}"
                               + (unlocked ? $"\n¡Nivel {levelNumber + 1} desbloqueado!" : "")
                               + (levelNumber == LevelSettings.Count ? "\n¡Completaste el juego!" : "");
        }
        else
        {
            winInfoText.text = "(Modo prueba: no se guardó en Firebase)";
        }
        SetEndButtons(true);
    }

    private IEnumerator ShowLoseAfterDelay()
    {
        yield return new WaitForSeconds(0.8f);
        losePanel.SetActive(true);
        loseScoreText.text = $"Puntaje: {score}\n{GoalProgressText()}";
        loseRetryButton.interactable = false;
        loseMenuButton.interactable = false;
        SaveLoss();
    }

    private async void SaveLoss()
    {
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.Progress != null)
            await FirebaseManager.Instance.ReportLevelResultAsync(levelNumber, score, false);
        if (this == null) return;
        loseRetryButton.interactable = true;
        loseMenuButton.interactable = true;
    }

    private void EndGame()
    {
        state = State.Ended;
        CurrentSpeed = 0f;
        spawner.Stop();
        pauseButton.gameObject.SetActive(false);
    }

    private void SetEndButtons(bool value)
    {
        nextLevelButton.interactable = value;
        winRetryButton.interactable = value;
        winMenuButton.interactable = value;
    }

    // ------------------- HUD -------------------

    private void UpdateHUD()
    {
        scoreText.text = score.ToString();
        goalText.text = GoalProgressText();
    }

    private string GoalProgressText()
    {
        switch (cfg.goal)
        {
            case GoalType.PassObstacles:
                return $"Postes: {obstaclesPassed}/{cfg.goalAmount}";
            case GoalType.CollectCrumbs:
                return $"Migas: {crumbsCollected}/{cfg.goalAmount}";
            default:
                int left = Mathf.Max(0, Mathf.CeilToInt(cfg.goalAmount - elapsed));
                return $"Sobrevive: {left} s";
        }
    }

    // ------------------- Botones -------------------

    private void TogglePause()
    {
        if (state == State.Playing)
        {
            state = State.Paused;
            Time.timeScale = 0f;
            pausePanel.SetActive(true);
        }
        else if (state == State.Paused)
        {
            state = State.Playing;
            Time.timeScale = 1f;
            pausePanel.SetActive(false);
        }
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void NextLevel()
    {
        Time.timeScale = 1f;
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.Progress != null)
            FirebaseManager.Instance.PlayLevel(levelNumber + 1);
        else
        {
            testLevel = levelNumber + 1;
            Retry();
        }
    }

    private void GoToMenu()
    {
        Time.timeScale = 1f;
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.Progress != null)
        {
            // Si salió a mitad de partida, se cuenta como intento perdido
            if (state == State.Paused)
                _ = FirebaseManager.Instance.ReportLevelResultAsync(levelNumber, score, false);
            FirebaseManager.Instance.GoToMenu();
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
