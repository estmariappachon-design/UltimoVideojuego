using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TORRE DE PASTELES: lógica principal (GameScene).
///
/// Una capa se mueve de lado a lado sobre el pastel. Al tocar la pantalla cae:
///  - justo encima → PERFECTA (no se corta, suma combo)
///  - corrida → se corta lo que sobresale
///  - fuera del pastel → se cae (pierdes un corazón)
///  - quemada o de un sabor que no va → la rechaza el cliente (pierdes un corazón)
///    (esas hay que DEJARLAS PASAR: se van solas después de cruzar la pantalla)
/// Cada pedido es un pastel de N pisos con los sabores en orden. Hay que entregar 3 pedidos.
///
/// Retos según el nivel (se configuran en LevelCatalog): brisa, pasteles quemados, ventilador,
/// moscas, temblor, apagón, harina, superficie que se mueve y cliente que cambia el pedido.
/// </summary>
public class StackGame : MonoBehaviour
{
    public static StackGame Instance { get; private set; }

    /// <summary>Se dispara al terminar un pedido (las moscas se van).</summary>
    public static event System.Action OnOrderEnded;

    /// <summary>Nivel para probar sin Firebase (lo usa el mapa y el botón Siguiente).</summary>
    public static int OfflineLevel;

    private enum State { Intro, OrderIntro, Sliding, Dropping, Fixing, Busy, Paused, Ended }

    [Header("Escena")]
    public Camera cam;
    public SpriteRenderer backgroundRenderer;
    [Tooltip("Raíz del pastel (el plato y las capas son hijos). Muévelo para cambiar dónde se arma el pastel")]
    public Transform towerRoot;
    public CakeLayer layerPrefab;
    public CustomerView customer;
    public GameHud hud;
    public OrderIntroUI orderIntro;
    public ResultScreen results;
    public ScreenEffects effects;
    public WindEffect windEffect;
    public Confetti confetti;
    public FanHazard fan;
    public Fly flyPrefab;

    [Header("Posiciones (unidades, relativas a la Torre)")]
    [Tooltip("Altura de la superficie del plato, relativa a la Torre")]
    public float plateTopY = 0f;
    [Tooltip("A qué altura sobre el pastel se mueve la capa")]
    public float slideHeight = 1.3f;
    [Tooltip("Hasta dónde llega la capa a la IZQUIERDA del centro del pastel")]
    public float slideLeft = 3.4f;
    [Tooltip("Hasta dónde llega la capa a la DERECHA del centro del pastel")]
    public float slideRight = 3.0f;
    public float movingPlateRange = 1.2f;
    [Tooltip("Ancho mínimo para que una capa cortada siga contando")]
    public float minLayerWidth = 0.45f;
    public float dropTime = 0.3f;
    [Tooltip("Ancho visible mínimo del mundo (para celulares más cuadrados)")]
    public float minWorldWidth = 17f;

    [Header("Viento")]
    [Tooltip("Cuánto empuja el viento a la capa que se mueve")]
    public float windOnSlide = 1.1f;
    [Tooltip("Cuánto empuja el viento a la capa mientras CAE (lo que 'se lleva' el pastel)")]
    public float windOnDrop = 3.4f;
    [Tooltip("Cuánto se inclina la torre con el viento")]
    public float windSway = 0.06f;

    [Header("Combo")]
    [Tooltip("Con 3 perfectas seguidas la capa crece un poco")]
    public float comboGrow = 0.12f;

    [Header("Ventilador")]
    public float fanWarningTime = 1.6f;
    [Tooltip("Cuánto se mueve la torre con la ráfaga")]
    public float fanSway = 0.5f;

    [Header("Temblor")]
    [Tooltip("Inclinación máxima antes de que se caiga el piso de arriba")]
    public float maxTilt = 11f;
    [Tooltip("Cuánto endereza cada toque")]
    public float stabilizePerTap = 2.2f;

    [Header("Puntaje")]
    public int pointsPerLayer = 10;
    public int pointsPerPerfect = 25;
    public int pointsPerOrder = 150;
    public int levelBonusBase = 100;
    public int levelBonusPerLevel = 25;
    public int pointsPerHeartLeft = 50;
    public int pointsPerFly = 15;
    public int pointsPerDecoyAvoided = 20;
    [Tooltip("Puntos por quitar un piso que el cliente ya no quería")]
    public int pointsPerWrongRemoved = 10;

    [Header("Pruebas sin Firebase (Play directo en GameScene)")]
    [Range(1, 10)] public int testLevel = 1;
    [Tooltip("0 = pedidos distintos cada partida")]
    public int seed = 0;

    // ------------------------------------------------------------------ estado
    private State state = State.Intro;
    private State pausedFrom;
    private int levelNumber;
    private LevelCatalog catalog;
    private LevelCatalog.Entry level;
    private System.Random rng;

    private CakeOrder order;
    private LevelCatalog.Customer currentCustomer;
    private int orderIndex;
    private int ordersDone;
    private int lastCustomer = -1;
    private int floor;
    private int hearts;
    private int score;
    private int combo;
    private int perfectsThisOrder, perfectsTotal;
    private int mistakesThisOrder;
    private int fliesShooed, decoysAvoided;
    private float patienceLeft;
    private bool tickPlayed;
    private int orderChangeFloor = -1;
    private string loseReason;
    private readonly List<Sprite> servedCustomers = new List<Sprite>();

    private readonly List<CakeLayer> placed = new List<CakeLayer>();
    private CakeLayer moving;
    private int movingDir = 1;
    private float speed;
    private float topX;
    private float topWidth;
    private bool isDecoy, lastWasDecoy;
    private int decoyPassesLeft;
    private float shiftTimer;
    private int shiftIndex;
    private bool dropRequested;
    private bool lastDropGreen;
    private readonly List<CakeLayer> wrongLayers = new List<CakeLayer>();

    // Torre
    private float baseTowerX, baseTowerY;
    private float plateTime, swayOffset, shakeOffset, towerAngle;

    // Viento y retos
    private float wind, windTarget, windTimer, fanGust;
    private float fanTimer, tremorTimer, blackoutTimer, flourTimer, flyTimer;
    private bool bigEventActive, tremorActive, flyHintShown;
    private float tilt;
    private bool layerFellDuringTremor;
    private Coroutine eventRoutine;

    private bool Online => FirebaseManager.Instance != null && FirebaseManager.Instance.Progress != null;
    private float LayerH => layerPrefab != null ? layerPrefab.height : 0.62f;
    private float EffectiveWind => wind + fanGust;
    private float LeftLimit => baseTowerX - slideLeft;
    private float RightLimit => baseTowerX + slideRight;

    // ================================================================== INICIO
    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        if (cam == null) cam = Camera.main;
        if (cam != null) cam.orthographicSize = Mathf.Max(cam.orthographicSize, minWorldWidth / (2f * cam.aspect));
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        catalog = LevelCatalog.Instance;
        levelNumber = Online ? FirebaseManager.Instance.SelectedLevel : (OfflineLevel > 0 ? OfflineLevel : testLevel);
        level = LevelCatalog.Get(levelNumber);
        if (catalog == null || level == null)
        {
            Debug.LogError("Falta Assets/Resources/LevelCatalog. Usa el menú Torre de Pasteles → CREAR TODO.");
            return;
        }

        rng = new System.Random(seed != 0 ? seed : System.Environment.TickCount ^ (levelNumber * 7919));
        hearts = level.hearts;
        baseTowerX = towerRoot.position.x;
        baseTowerY = towerRoot.position.y;

        if (backgroundRenderer != null && level.background != null) backgroundRenderer.sprite = level.background;
        if (AudioManager.Instance != null)
            AudioManager.PlayMusic(level.music != null ? level.music : AudioManager.Instance.defaultLevelMusic);

        // Botones
        hud.pauseButton.onClick.AddListener(TogglePause);
        hud.resumeButton.onClick.AddListener(TogglePause);
        hud.pauseRetryButton.onClick.AddListener(Retry);
        hud.pauseMenuButton.onClick.AddListener(GoToMenu);
        results.nextButton.onClick.AddListener(NextLevel);
        results.retryButton.onClick.AddListener(Retry);
        results.menuButton.onClick.AddListener(GoToMenu);
        results.loseRetryButton.onClick.AddListener(Retry);
        results.loseMenuButton.onClick.AddListener(GoToMenu);
        hud.pausePanel.SetActive(false);
        results.gameObject.SetActive(false);
        hud.ShowStability(false);
        hud.HideEvent();
        hud.ticket.SetVisible(false);
        if (effects != null) effects.StopAll();
        hud.EnsureExtras();
        if (hud.timingMeter != null) hud.timingMeter.Show(level.timingMeter);
        if (hud.orderTimer != null) hud.orderTimer.Show(false);

        // Retos del nivel
        if (fan != null) fan.gameObject.SetActive(level.fanEvery > 0f);
        if (windEffect != null) windEffect.maxWind = Mathf.Max(0.01f, Mathf.Max(level.windStrength, level.fanEvery > 0f ? level.fanForce : 0f));
        fanTimer = FirstDelay(level.fanEvery);
        tremorTimer = FirstDelay(level.tremorEvery);
        blackoutTimer = level.blackoutFirstDelay > 0f ? level.blackoutFirstDelay : FirstDelay(level.blackoutEvery);
        flourTimer = FirstDelay(level.flourEvery);
        flyTimer = FirstDelay(level.flyEvery);

        hud.levelText.text = string.Format(hud.levelFormat, levelNumber, level.title);
        hud.SetChallenge(level.challengeDescription, "");
        UpdateHud();
        StartCoroutine(IntroRoutine());
    }

    private float FirstDelay(float every) => every > 0f ? every * Range(0.5f, 0.9f) : 0f;

    private IEnumerator IntroRoutine()
    {
        state = State.Intro;
        string goal = string.Format(hud.introGoal, level.ordersToWin, level.floorsPerOrder);
        hud.ShowMessage(string.Format(hud.introFormat, levelNumber, goal), 1.8f);
        yield return new WaitForSeconds(1.8f);
        StartOrder();
    }

    // ================================================================== PEDIDOS
    private void StartOrder()
    {
        order = OrderGenerator.Create(rng, level, catalog.flavors.Length, catalog.customers.Length, orderIndex, lastCustomer);
        lastCustomer = order.customer;

        foreach (CakeLayer l in placed) if (l != null) Destroy(l.gameObject);
        placed.Clear();
        floor = 0;
        topX = 0f;
        topWidth = level.baseWidth;
        combo = 0;
        perfectsThisOrder = 0;
        mistakesThisOrder = 0;
        lastWasDecoy = false;
        patienceLeft = order.patience;
        tickPlayed = false;
        wrongLayers.Clear();
        if (level.blackoutEvery > 0f && level.blackoutFirstDelay > 0f) blackoutTimer = level.blackoutFirstDelay;
        orderChangeFloor = level.orderChangeChance > 0f && rng.NextDouble() < level.orderChangeChance && level.floorsPerOrder > 4
            ? rng.Next(2, level.floorsPerOrder - 2) : -1;

        currentCustomer = catalog.customers.Length > 0 ? catalog.customers[order.customer] : null;
        if (customer != null && currentCustomer != null) customer.Show(currentCustomer);
        hud.ticket.SetVisible(false);
        hud.SetChallenge(level.challengeDescription, ChallengeText());
        UpdateHud();
        StartCoroutine(OrderIntroRoutine());
    }

    private IEnumerator OrderIntroRoutine()
    {
        state = State.OrderIntro;
        yield return new WaitForSeconds(0.5f);   // el cliente entra
        if (orderIntro != null)
            yield return orderIntro.Play(order, currentCustomer, catalog.flavors, ChallengeText(), (RectTransform)hud.ticket.transform);
        if (state == State.Ended) yield break;

        string name = currentCustomer != null ? currentCustomer.name : "";
        hud.ticket.Show(order, catalog.flavors, string.Format(hud.ticketTitleFormat, name), ChallengeText());
        hud.ticket.SetVisible(true);
        hud.ticket.Pop();
        if (hud.orderTimer != null)
        {
            hud.orderTimer.Show(order.patience > 0f);
            if (order.patience > 0f) hud.orderTimer.Set(patienceLeft, order.patience);
        }
        SpawnMovingLayer();
    }

    private string ChallengeText()
    {
        if (order == null) return "";
        switch (order.challenge)
        {
            case ChallengeType.PerfectDrops: return string.Format(hud.challengePerfectFormat, Mathf.RoundToInt(order.challengeValue));
            case ChallengeType.MinFinalWidth: return string.Format(hud.challengeWidthFormat, Mathf.RoundToInt(order.challengeValue * 100f));
            case ChallengeType.NoMistakes: return hud.challengeNoMistakes;
            default: return "";
        }
    }

    private bool ChallengeMet()
    {
        switch (order.challenge)
        {
            case ChallengeType.PerfectDrops: return perfectsThisOrder >= Mathf.RoundToInt(order.challengeValue);
            case ChallengeType.MinFinalWidth: return topWidth >= order.challengeValue * level.baseWidth - 0.001f;
            case ChallengeType.NoMistakes: return mistakesThisOrder == 0;
            default: return true;
        }
    }

    // ================================================================== CAPA QUE SE MUEVE
    private void SpawnMovingLayer()
    {
        if (state == State.Ended) return;
        int flavor = order.flavors[floor];
        bool burnt = false;
        isDecoy = false;

        // ¿Sale una capa que NO se debe poner? (quemada o de un sabor que no está en el pedido)
        if (!lastWasDecoy && level.decoyChance > 0f && rng.NextDouble() < level.decoyChance)
        {
            bool wantBurnt = rng.NextDouble() < level.burntRatio;
            int other = OrderGenerator.RandomFlavorNotInOrder(rng, order, catalog.flavors.Length);
            if (!wantBurnt && other >= 0) { flavor = other; isDecoy = true; }
            else { burnt = true; isDecoy = true; }
        }
        lastWasDecoy = isDecoy;
        decoyPassesLeft = level.decoyPasses;

        if (!isDecoy && level.flavorShiftInterval > 0f)
        {
            shiftIndex = rng.Next(order.pool.Count);
            flavor = order.pool[shiftIndex];
            shiftTimer = level.flavorShiftInterval;
        }

        moving = Instantiate(layerPrefab);
        moving.Setup(catalog.flavors[flavor].layerSprite, flavor, topWidth);
        if (burnt) moving.SetBurnt(catalog.burntLayerSprite, catalog.burntFaceSprite);

        movingDir = rng.Next(2) == 0 ? 1 : -1;
        float x = movingDir > 0 ? LeftLimit : RightLimit;
        moving.transform.position = new Vector3(x, SlideY(), 0f);

        speed = Mathf.Min(level.maxSpeed, level.startSpeed + level.speedPerFloor * floor + level.speedPerOrder * orderIndex);
        dropRequested = false;
        if (level.timingMeter && hud.timingMeter != null) hud.timingMeter.NewRound(rng, level.meterGreenSize, level.meterSpeed);
        state = State.Sliding;
        if (hud.tapHint != null) hud.tapHint.SetActive(orderIndex == 0 && floor == 0 && levelNumber == 1);
    }

    private float FloorCenterLocalY(int f) => plateTopY + LayerH * (f + 0.5f);
    private float SlideY() => baseTowerY + FloorCenterLocalY(floor) + slideHeight;

    /// <summary>Teclado / compatibilidad.</summary>
    public void RequestDrop()
    {
        if (state == State.Sliding && !tremorActive) dropRequested = true;
    }

    /// <summary>Un toque en la pantalla (lo llama TapZone).</summary>
    public void OnTap(Vector2 screenPosition)
    {
        if (level == null || state == State.Paused || state == State.Ended) return;

        if (state == State.OrderIntro)
        {
            if (orderIntro != null) orderIntro.Skip();
            return;
        }

        Vector3 w = cam != null ? cam.ScreenToWorldPoint(screenPosition) : Vector3.zero;
        w.z = 0f;

        // ¿Tocó una mosca?
        if (cam != null && Fly.All.Count > 0)
        {
            foreach (Fly f in Fly.All.ToArray())
            {
                if (f == null || !f.IsActive || !f.ContainsPoint(w)) continue;
                f.Shoo();
                fliesShooed++;
                score += pointsPerFly;
                AudioManager.Play(AudioManager.Sfx.Shoo);
                hud.Popup(hud.flyShooedMessage + " +" + pointsPerFly, new Color(0.3f, 0.7f, 0.4f));
                UpdateHud();
                return;
            }
        }

        // El cliente cambió el pedido: hay que tocar los pisos que parpadean para quitarlos
        if (state == State.Fixing)
        {
            if (cam != null && TryRemoveWrong(w)) return;
            hud.ShowEvent(hud.removeWrongMessage, 999f);
            return;
        }

        // Tocar un pastel que NO va lo bota (niveles con "Tap To Discard")
        if (state == State.Sliding && isDecoy && level.tapToDiscard && moving != null && cam != null && moving.ContainsPoint(w, 0.35f))
        {
            StartCoroutine(DecoyLeaves(true));
            return;
        }

        // Temblor: cada toque sujeta la torre
        if (tremorActive)
        {
            tilt = Mathf.Max(0f, tilt - stabilizePerTap);
            hud.PulseStabilityHand();
            AudioManager.PlayPitched(AudioManager.Sfx.Stabilize, Range(0.9f, 1.2f));
            return;
        }

        if (state == State.Sliding) dropRequested = true;
    }

    // ================================================================== CICLO
    private void Update()
    {
        if (level == null || state == State.Paused || state == State.Ended) return;

        UpdateWind();
        UpdateHazards();
        UpdateTowerTransform();
        UpdateTimer();

        if (state == State.Fixing)
        {
            // Por si un piso marcado se cayó por otra razón (temblor)
            wrongLayers.RemoveAll(l => l == null || !placed.Contains(l));
            if (wrongLayers.Count == 0) StartCoroutine(AfterFix());
            return;
        }

        if (state != State.Sliding || moving == null) return;

        if (tremorActive)
        {
            moving.transform.position = new Vector3(moving.transform.position.x, SlideY(), 0f);
            return;
        }

        // Medidor de precisión
        if (level.timingMeter && hud.timingMeter != null) hud.timingMeter.Tick(Time.deltaTime);

        // Movimiento de lado a lado (+ viento)
        Vector3 p = moving.transform.position;
        p.x += (movingDir * speed + EffectiveWind * windOnSlide) * Time.deltaTime;
        bool bounced = false;
        if (p.x > RightLimit) { p.x = RightLimit; movingDir = -1; bounced = true; }
        else if (p.x < LeftLimit) { p.x = LeftLimit; movingDir = 1; bounced = true; }
        p.y = SlideY();
        moving.transform.position = p;

        // Las capas que no van se van solas después de cruzar
        if (bounced && isDecoy)
        {
            decoyPassesLeft--;
            if (decoyPassesLeft <= 0)
            {
                StartCoroutine(DecoyLeaves());
                return;
            }
        }

        // Sabor cambiante (opcional)
        if (!isDecoy && level.flavorShiftInterval > 0f)
        {
            shiftTimer -= Time.deltaTime;
            if (shiftTimer <= 0f)
            {
                shiftTimer = level.flavorShiftInterval;
                shiftIndex = (shiftIndex + 1) % order.pool.Count;
                int f = order.pool[shiftIndex];
                moving.SetFlavor(catalog.flavors[f].layerSprite, f);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) dropRequested = true;
        if (dropRequested)
        {
            dropRequested = false;
            if (hud.tapHint != null) hud.tapHint.SetActive(false);
            bool green = level.timingMeter && hud.timingMeter != null && hud.timingMeter.InGreen;
            if (level.timingMeter && hud.timingMeter != null) hud.timingMeter.Feedback(green);
            StartCoroutine(DropRoutine(green));
        }
    }

    /// <summary>Cronómetro del pedido (corre mientras se juega, también mientras quitas pisos).</summary>
    private void UpdateTimer()
    {
        if (order == null || order.patience <= 0f) return;
        if (state != State.Sliding && state != State.Dropping && state != State.Fixing) return;

        patienceLeft -= Time.deltaTime;
        hud.ticket.SetPatience(patienceLeft / order.patience);
        if (hud.orderTimer != null) hud.orderTimer.Set(patienceLeft, order.patience);
        if (!tickPlayed && patienceLeft < Mathf.Min(10f, order.patience * 0.25f))
        {
            tickPlayed = true;
            AudioManager.Play(AudioManager.Sfx.Tick);
            if (customer != null) customer.SetMood(CustomerView.Mood.Angry);
        }
        if (patienceLeft <= 0f)
        {
            if (moving != null) { Destroy(moving.gameObject); moving = null; }
            foreach (CakeLayer l in wrongLayers) if (l != null) l.SetWrong(false);
            wrongLayers.Clear();
            StartCoroutine(OrderFailed(hud.timeUpMessage));
        }
    }

    private IEnumerator DecoyLeaves(bool tapped = false)
    {
        state = State.Busy;
        CakeLayer l = moving;
        moving = null;
        decoysAvoided++;
        score += pointsPerDecoyAvoided;
        UpdateHud();
        hud.Popup(tapped ? hud.discardedMessage : hud.decoyAvoidedMessage, new Color(0.3f, 0.7f, 0.4f));
        AudioManager.Play(AudioManager.Sfx.DecoyAvoided);

        float dir = l.transform.position.x >= baseTowerX ? 1f : -1f;
        Vector3 from = l.transform.position;
        if (tapped) l.Scare();
        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            l.transform.position = tapped
                ? from + new Vector3(dir * t * 10f, Mathf.Sin(t / 0.45f * Mathf.PI) * 1.4f - t * t * 20f, 0f)
                : from + new Vector3(dir * t * 28f, Mathf.Sin(t / 0.45f * Mathf.PI) * 0.6f, 0f);
            l.SetAlpha(1f - t / 0.45f);
            yield return null;
        }
        Destroy(l.gameObject);
        if (state == State.Ended) yield break;
        SpawnMovingLayer();
    }

    // ================================================================== VIENTO Y TORRE
    private void UpdateWind()
    {
        if (level.windStrength > 0f)
        {
            windTimer -= Time.deltaTime;
            if (windTimer <= 0f)
            {
                // Procedural: nueva dirección y fuerza al azar (casi nunca suave)
                windTimer = level.windChangeEvery * Range(0.7f, 1.3f);
                float sign = rng.Next(2) == 0 ? -1f : 1f;
                windTarget = sign * level.windStrength * Range(0.55f, 1f);
                AudioManager.Play(AudioManager.Sfx.Wind);
            }
            wind = Mathf.MoveTowards(wind, windTarget, level.windStrength * 1.5f * Time.deltaTime);
        }
        else wind = 0f;

        float shownMax = Mathf.Max(level.windStrength, level.fanEvery > 0f ? level.fanForce : 0f);
        hud.SetWind(EffectiveWind, shownMax);
        if (windEffect != null) windEffect.SetWind(EffectiveWind);
    }

    private void UpdateTowerTransform()
    {
        if (towerRoot == null) return;
        float plate = 0f;
        if (level.movingPlateSpeed > 0f)
        {
            plateTime += Time.deltaTime * level.movingPlateSpeed;
            plate = Mathf.Sin(plateTime) * movingPlateRange;
        }
        // La brisa inclina un poquito la torre
        float breezeSway = EffectiveWind * windSway * Mathf.Min(1f, placed.Count / 4f);
        towerRoot.position = new Vector3(baseTowerX + plate + swayOffset + shakeOffset + breezeSway, baseTowerY, 0f);
        towerRoot.rotation = Quaternion.Euler(0f, 0f, towerAngle);
    }

    // ================================================================== RETOS (eventos)
    private void UpdateHazards()
    {
        bool playing = state == State.Sliding || state == State.Dropping;
        if (!playing) return;

        // Moscas
        if (level.flyEvery > 0f && flyPrefab != null)
        {
            flyTimer -= Time.deltaTime;
            if (flyTimer <= 0f)
            {
                flyTimer = level.flyEvery * Range(0.7f, 1.3f);
                if (placed.Count > 0 && Fly.All.Count < level.maxFlies) SpawnFly();
            }
        }

        // Eventos grandes (solo uno a la vez)
        if (Tick(ref fanTimer, level.fanEvery) && fan != null) eventRoutine = StartCoroutine(FanRoutine());
        else if (Tick(ref tremorTimer, level.tremorEvery))
        {
            if (placed.Count >= 2) eventRoutine = StartCoroutine(TremorRoutine());
            else tremorTimer = 2f;
        }
        else if (Tick(ref blackoutTimer, level.blackoutEvery) && effects != null) eventRoutine = StartCoroutine(BlackoutRoutine());
        else if (Tick(ref flourTimer, level.flourEvery) && effects != null) eventRoutine = StartCoroutine(FlourRoutine());
    }

    private bool Tick(ref float timer, float every)
    {
        if (every <= 0f) return false;
        timer -= Time.deltaTime;
        if (timer > 0f) return false;
        if (bigEventActive || state != State.Sliding) { timer = 0.6f; return false; }
        timer = every * Range(0.75f, 1.25f);
        return true;
    }

    // ---------- Ventilador ----------
    private IEnumerator FanRoutine()
    {
        bigEventActive = true;
        fan.SetMode(FanHazard.Mode.Warning);
        hud.ShowEvent(hud.fanWarningMessage, fanWarningTime);
        AudioManager.Play(AudioManager.Sfx.Alert);
        yield return new WaitForSeconds(fanWarningTime);

        fan.SetMode(FanHazard.Mode.Blowing);
        AudioManager.Play(AudioManager.Sfx.Fan);
        hud.ShowEvent(hud.fanBlowingMessage, level.fanDuration);
        float dir = fan.Direction(baseTowerX);
        for (float t = 0f; t < level.fanDuration; t += Time.deltaTime)
        {
            float k = Mathf.Min(1f, t / 0.3f) * Mathf.Min(1f, (level.fanDuration - t) / 0.4f);
            fanGust = dir * level.fanForce * k;
            swayOffset = dir * fanSway * k + Mathf.Sin(t * 9f) * 0.15f * k;
            yield return null;
        }
        fanGust = 0f;
        fan.SetMode(FanHazard.Mode.Idle);
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            swayOffset = Mathf.Lerp(swayOffset, 0f, t / 0.4f);
            yield return null;
        }
        swayOffset = 0f;
        bigEventActive = false;
    }

    // ---------- Temblor ----------
    private IEnumerator TremorRoutine()
    {
        bigEventActive = true;
        tremorActive = true;
        layerFellDuringTremor = false;
        tilt = 1.5f;
        if (moving != null) moving.SetAlpha(0.5f);
        hud.ShowEvent(hud.tremorMessage, level.tremorDuration);
        hud.ShowStability(true);
        AudioManager.Play(AudioManager.Sfx.Tremor);

        for (float t = 0f; t < level.tremorDuration; t += Time.deltaTime)
        {
            if (state == State.Ended) break;
            tilt += level.tremorStrength * (1f + placed.Count * 0.08f) * Time.deltaTime;
            towerAngle = Mathf.Sin(t * 14f) * tilt;
            shakeOffset = Mathf.Sin(t * 31f) * 0.05f;
            if (cam != null) cam.transform.localPosition = new Vector3(Mathf.Sin(t * 47f) * 0.04f, cam.transform.localPosition.y, cam.transform.localPosition.z);
            hud.SetStability(1f - tilt / maxTilt);
            if (tilt >= maxTilt)
            {
                TopLayerFalls();
                tilt = maxTilt * 0.35f;
                if (hearts <= 0) break;
            }
            yield return null;
        }

        float a0 = towerAngle;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            towerAngle = Mathf.Lerp(a0, 0f, t / 0.3f);
            shakeOffset = 0f;
            yield return null;
        }
        towerAngle = 0f;
        if (cam != null) cam.transform.localPosition = new Vector3(0f, cam.transform.localPosition.y, cam.transform.localPosition.z);
        hud.ShowStability(false);
        hud.HideEvent();
        tremorActive = false;
        bigEventActive = false;

        if (state == State.Sliding && moving != null)
        {
            moving.SetAlpha(1f);
            // Si se cayó un piso, la capa que esperaba ya no corresponde: sale una nueva
            if (layerFellDuringTremor)
            {
                Destroy(moving.gameObject);
                moving = null;
                SpawnMovingLayer();
            }
        }
    }

    private void TopLayerFalls()
    {
        if (placed.Count == 0) return;
        CakeLayer top = placed[placed.Count - 1];
        placed.RemoveAt(placed.Count - 1);
        top.Fall(towerAngle > 0f ? -3f : 3f);
        floor = placed.Count;
        RecomputeTop();
        hud.ticket.SetCurrentFloor(floor);
        layerFellDuringTremor = true;

        hearts--;
        mistakesThisOrder++;
        combo = 0;
        UpdateHud();
        hud.ShowMessage(hud.tremorFallMessage, 1.2f);
        AudioManager.Play(AudioManager.Sfx.Miss);
        if (customer != null) customer.SetMood(CustomerView.Mood.Angry);
        if (hearts <= 0)
        {
            loseReason = hud.tremorFallMessage;
            StartCoroutine(LoseRoutine());
        }
    }

    private void RecomputeTop()
    {
        if (placed.Count > 0)
        {
            CakeLayer last = placed[placed.Count - 1];
            topX = last.transform.localPosition.x;
            topWidth = last.Width;
        }
        else
        {
            topX = 0f;
            topWidth = level.baseWidth;
        }
    }

    // ---------- Apagón y harina ----------
    private IEnumerator BlackoutRoutine()
    {
        bigEventActive = true;
        hud.ShowEvent(hud.blackoutMessage, 1.4f);
        AudioManager.Play(AudioManager.Sfx.LightsOff);
        yield return effects.Blackout(level.blackoutDuration, BlackoutFocus, cam, level.blackoutLightDim);
        bigEventActive = false;
    }

    private Vector3 BlackoutFocus()
    {
        return new Vector3(towerRoot.position.x + topX, baseTowerY + FloorCenterLocalY(floor) + slideHeight * 0.4f, 0f);
    }

    private IEnumerator FlourRoutine()
    {
        bigEventActive = true;
        hud.ShowEvent(hud.flourMessage, 1.2f);
        AudioManager.Play(AudioManager.Sfx.Flour);
        yield return effects.Flour(level.flourDuration);
        bigEventActive = false;
    }

    // ---------- Moscas ----------
    private void SpawnFly()
    {
        CakeLayer target = placed[rng.Next(placed.Count)];
        float side = rng.Next(2) == 0 ? -1f : 1f;
        Vector3 start;
        if (level.flySpawnDistance > 0f)
        {
            // Aparece CERCA del pastel (a un lado y un poco arriba)
            Vector3 tp = target.transform.position;
            float d = level.flySpawnDistance;
            start = new Vector3(tp.x + side * Range(target.Width * 0.5f + 0.4f, target.Width * 0.5f + d), tp.y + Range(0.5f, d), 0f);
            if (cam != null)
            {
                float top = cam.transform.position.y + cam.orthographicSize - 0.8f;
                start.y = Mathf.Min(start.y, top);
            }
        }
        else
        {
            float halfW = cam != null ? cam.orthographicSize * cam.aspect : 9f;
            start = new Vector3((cam != null ? cam.transform.position.x : 0f) + side * (halfW + 1f), baseTowerY + Range(1f, 5f), 0f);
        }
        Fly f = Instantiate(flyPrefab, start, Quaternion.identity);
        Vector3 offset = new Vector3(Range(-target.Width * 0.3f, target.Width * 0.3f), LayerH * 0.55f, 0f);
        f.Setup(target.transform, offset, level.flySpeed * Range(0.85f, 1.15f), OnFlyDamage);
        AudioManager.Play(AudioManager.Sfx.FlyBuzz);
        if (!flyHintShown)
        {
            flyHintShown = true;
            hud.ShowEvent(hud.flyIncomingMessage, 2f);
        }
    }

    private void OnFlyDamage(Fly fly, Transform target)
    {
        if (state == State.Ended) return;
        CakeLayer l = target != null ? target.GetComponent<CakeLayer>() : null;
        if (l != null && placed.Contains(l)) l.SetDirty();
        hearts--;
        mistakesThisOrder++;
        combo = 0;
        UpdateHud();
        hud.ShowMessage(hud.flyDamageMessage, 1.2f);
        AudioManager.Play(AudioManager.Sfx.WrongFlavor);
        StartCoroutine(Shake());
        if (customer != null) customer.SetMood(CustomerView.Mood.Angry);
        if (hearts <= 0)
        {
            loseReason = hud.flyDamageMessage;
            StartCoroutine(LoseRoutine());
        }
    }

    // ================================================================== SOLTAR
    /// <param name="snap">true = se tocó en el VERDE del medidor: cae justo en su lugar, el viento no la mueve</param>
    private IEnumerator DropRoutine(bool snap = false)
    {
        state = State.Dropping;
        lastDropGreen = snap;
        CakeLayer layer = moving;
        Vector3 start = layer.transform.position;
        float x = start.x;

        for (float t = 0f; t < dropTime; t += Time.deltaTime)
        {
            if (layer == null || state == State.Ended || state == State.Busy) yield break;
            float k = t / dropTime;
            if (snap) x = Mathf.Lerp(start.x, towerRoot.position.x + topX, Mathf.SmoothStep(0f, 1f, k));
            else x += EffectiveWind * windOnDrop * Time.deltaTime;
            float targetY = baseTowerY + FloorCenterLocalY(floor);
            layer.transform.position = new Vector3(x, Mathf.Lerp(start.y, targetY, k * k), 0f);
            yield return null;
        }
        if (layer == null || state == State.Ended || state == State.Busy) yield break;
        if (snap) x = towerRoot.position.x + topX;
        layer.transform.position = new Vector3(x, baseTowerY + FloorCenterLocalY(floor), 0f);
        Land(layer);
    }

    private void Land(CakeLayer layer)
    {
        moving = null;
        float pushSide = layer.transform.position.x > towerRoot.position.x ? 4f : -4f;

        // Pastel quemado
        if (layer.IsBurnt)
        {
            layer.Fall(pushSide);
            AudioManager.Play(AudioManager.Sfx.WrongFlavor);
            Mistake(hud.burntMessage);
            return;
        }

        // Sabor equivocado / que no está en el pedido
        int required = order.flavors[floor];
        if (layer.FlavorIndex != required)
        {
            bool inOrder = System.Array.IndexOf(order.flavors, layer.FlavorIndex) >= 0;
            layer.Fall(pushSide);
            AudioManager.Play(AudioManager.Sfx.WrongFlavor);
            Mistake(inOrder ? string.Format(hud.wrongFlavorFormat, catalog.flavors[required].name) : hud.notInOrderMessage);
            return;
        }

        float topWorldX = towerRoot.position.x + topX;
        float w = layer.Width;
        float x = layer.transform.position.x;
        float delta = x - topWorldX;

        if (Mathf.Abs(delta) <= level.perfectTolerance)
        {
            // PERFECTA
            combo++;
            perfectsThisOrder++;
            perfectsTotal++;
            if (combo >= 3) w = Mathf.Min(level.baseWidth, w + comboGrow);
            Place(layer, topX, w);
            score += pointsPerLayer + pointsPerPerfect * combo;
            AudioManager.PlayPitched(AudioManager.Sfx.Perfect, 1f + 0.08f * Mathf.Min(combo - 1, 8));
            AudioManager.Vibrate();
            string msg = combo > 1 ? string.Format(hud.perfectComboFormat, combo) : hud.perfectMessage;
            if (lastDropGreen) msg = hud.meterGreenMessage + (combo > 1 ? " x" + combo : "");
            hud.Popup(msg, new Color(1f, 0.3f, 0.6f));
            if (confetti != null) confetti.Burst(layer.transform.position + Vector3.up * 0.3f, 8, 4f, 0.2f);
            if (customer != null) customer.Hop();
        }
        else
        {
            float left = Mathf.Max(x - w / 2f, topWorldX - topWidth / 2f);
            float right = Mathf.Min(x + w / 2f, topWorldX + topWidth / 2f);
            float overlap = right - left;

            if (overlap < minLayerWidth)
            {
                combo = 0;
                layer.Fall(delta > 0 ? 3f : -3f);
                AudioManager.Play(AudioManager.Sfx.Miss);
                Mistake(hud.missMessage);
                return;
            }

            combo = 0;
            float cutWidth = w - overlap;
            float cutCenter = delta > 0 ? right + cutWidth / 2f : left - cutWidth / 2f;
            if (cutWidth > 0.05f)
            {
                CakeLayer piece = Instantiate(layerPrefab, new Vector3(cutCenter, layer.transform.position.y, 0f), Quaternion.identity);
                piece.Setup(layer.body.sprite, layer.FlavorIndex, cutWidth);
                piece.Fall(delta > 0 ? 2f : -2f);
            }
            float center = (left + right) / 2f;
            Place(layer, center - towerRoot.position.x, overlap);
            score += pointsPerLayer;
            AudioManager.Play(AudioManager.Sfx.Cut);
            hud.Popup(hud.goodMessage, new Color(0.2f, 0.5f, 1f));
        }

        floor++;
        hud.ticket.SetCurrentFloor(floor);
        UpdateHud();

        if (floor >= level.floorsPerOrder) StartCoroutine(OrderFinished());
        else if (floor == orderChangeFloor) StartCoroutine(OrderChangeRoutine());
        else SpawnMovingLayer();
    }

    private void Place(CakeLayer layer, float localX, float width)
    {
        layer.transform.SetParent(towerRoot, true);
        layer.transform.localRotation = Quaternion.identity;
        layer.transform.localPosition = new Vector3(localX, FloorCenterLocalY(floor), 0f);
        layer.SetWidth(width);
        layer.Squish();
        placed.Add(layer);
        topX = localX;
        topWidth = width;
        AudioManager.Play(AudioManager.Sfx.Drop);
    }

    private void Mistake(string message)
    {
        hearts--;
        mistakesThisOrder++;
        combo = 0;
        UpdateHud();
        hud.ShowMessage(message, 1.2f);
        StartCoroutine(Shake());
        if (customer != null) customer.SetMood(CustomerView.Mood.Angry);

        if (hearts <= 0)
        {
            loseReason = message;
            StartCoroutine(LoseRoutine());
        }
        else StartCoroutine(ContinueAfterMistake());
    }

    private IEnumerator ContinueAfterMistake()
    {
        state = State.Busy;
        yield return new WaitForSeconds(0.7f);
        if (state == State.Ended) yield break;
        if (customer != null) customer.SetMood(CustomerView.Mood.Normal);
        SpawnMovingLayer();
    }

    // ---------- El cliente cambia el pedido ----------
    private IEnumerator OrderChangeRoutine()
    {
        state = State.Busy;
        orderChangeFloor = -1;

        // Versión nueva: el cliente cambia pisos YA PUESTOS → hay que tocarlos para quitarlos y rehacerlos
        if (level.orderChangeFloors > 0 && placed.Count > 0)
        {
            int n = Mathf.Min(placed.Count, rng.Next(1, level.orderChangeFloors + 1));
            List<int> changed = OrderGenerator.ChangePlacedFloors(rng, order, placed.Count - n, placed.Count);
            if (changed.Count > 0)
            {
                if (customer != null) { customer.SetMood(CustomerView.Mood.Normal); customer.Hop(); }
                AudioManager.Play(AudioManager.Sfx.OrderChange);
                foreach (int j2 in changed)
                {
                    hud.ticket.ChangeRow(j2, catalog.flavors[order.flavors[j2]]);
                    placed[j2].SetWrong(true);
                    wrongLayers.Add(placed[j2]);
                }
                hud.ticket.SetCurrentFloor(changed[0]);
                string what = changed.Count == 1 ? hud.oneFloorWord : string.Format(hud.manyFloorsWord, changed.Count);
                hud.ShowMessage(string.Format(hud.orderChangePlacedFormat, what), 2.2f);
                yield return new WaitForSeconds(1.2f);
                if (state == State.Ended) yield break;
                state = State.Fixing;
                hud.ShowEvent(hud.removeWrongMessage, 999f);
                yield break;
            }
        }

        int j = OrderGenerator.ChangeFloor(rng, order, floor);
        if (j >= 0)
        {
            if (customer != null) customer.SetMood(CustomerView.Mood.Normal);
            AudioManager.Play(AudioManager.Sfx.OrderChange);
            LevelCatalog.Flavor f = catalog.flavors[order.flavors[j]];
            hud.ShowEvent(string.Format(hud.orderChangeFormat, j + 1, f.name), 2.4f);
            hud.ticket.ChangeRow(j, f);
            yield return new WaitForSeconds(1.8f);
        }
        if (state == State.Ended) yield break;
        SpawnMovingLayer();
    }

    /// <summary>Toque durante el cambio de pedido: si tocó un piso que parpadea, lo quita (y los de encima).</summary>
    private bool TryRemoveWrong(Vector3 worldPoint)
    {
        for (int i = placed.Count - 1; i >= 0; i--)
        {
            CakeLayer l = placed[i];
            if (l == null || !l.IsWrong || !l.ContainsPoint(worldPoint)) continue;
            for (int k = placed.Count - 1; k >= i; k--)
            {
                CakeLayer r = placed[k];
                placed.RemoveAt(k);
                wrongLayers.Remove(r);
                if (r == null) continue;
                r.SetWrong(false);
                r.Fall(k % 2 == 0 ? -3.5f : 3.5f);
                score += pointsPerWrongRemoved;
            }
            floor = placed.Count;
            RecomputeTop();
            hud.ticket.SetCurrentFloor(floor);
            UpdateHud();
            hud.Popup(hud.wrongRemovedMessage + " +" + pointsPerWrongRemoved, new Color(0.3f, 0.7f, 0.4f));
            AudioManager.Play(AudioManager.Sfx.Shoo);
            wrongLayers.RemoveAll(x => x == null || !placed.Contains(x));
            if (wrongLayers.Count == 0) StartCoroutine(AfterFix());
            return true;
        }
        return false;
    }

    private IEnumerator AfterFix()
    {
        if (state != State.Fixing) yield break;
        state = State.Busy;
        hud.HideEvent();
        if (customer != null) customer.SetMood(CustomerView.Mood.Happy);
        yield return new WaitForSeconds(0.4f);
        if (state == State.Ended) yield break;
        if (customer != null) customer.SetMood(CustomerView.Mood.Normal);
        SpawnMovingLayer();
    }

    // ================================================================== FIN DE PEDIDO
    private void StopEvents()
    {
        if (eventRoutine != null) StopCoroutine(eventRoutine);
        eventRoutine = null;
        bigEventActive = tremorActive = false;
        fanGust = 0f;
        swayOffset = shakeOffset = towerAngle = 0f;
        if (fan != null) fan.SetMode(FanHazard.Mode.Idle);
        if (effects != null) effects.StopAll();
        hud.ShowStability(false);
        hud.HideEvent();
        if (hud.orderTimer != null) hud.orderTimer.Show(false);
        foreach (CakeLayer l in wrongLayers) if (l != null) l.SetWrong(false);
        wrongLayers.Clear();
        OnOrderEnded?.Invoke();
    }

    private IEnumerator OrderFinished()
    {
        state = State.Busy;
        StopEvents();
        yield return new WaitForSeconds(0.3f);

        if (!ChallengeMet())
        {
            yield return OrderFailed(hud.challengeFailedMessage);
            yield break;
        }

        ordersDone++;
        orderIndex++;
        int widthBonus = Mathf.RoundToInt(topWidth / level.baseWidth * 100f);
        score += pointsPerOrder + widthBonus;
        UpdateHud();
        if (currentCustomer != null) servedCustomers.Add(currentCustomer.happy != null ? currentCustomer.happy : currentCustomer.normal);

        if (customer != null) { customer.SetMood(CustomerView.Mood.Happy); customer.Hop(); }
        hud.ShowMessage(hud.orderDoneMessage, 1.4f);
        AudioManager.Play(AudioManager.Sfx.OrderDone);
        if (confetti != null) confetti.Burst(towerRoot.position + Vector3.up * 2f, 40, 7f);
        yield return new WaitForSeconds(1.6f);

        if (customer != null) customer.Leave();
        hud.ticket.SetVisible(false);
        yield return new WaitForSeconds(0.4f);

        if (ordersDone >= level.ordersToWin) Win();
        else StartOrder();
    }

    private IEnumerator OrderFailed(string message)
    {
        state = State.Busy;
        StopEvents();
        hearts--;
        orderIndex++;
        UpdateHud();
        if (customer != null) customer.SetMood(CustomerView.Mood.Angry);
        hud.ShowMessage(message, 1.5f);
        AudioManager.Play(AudioManager.Sfx.OrderFailed);
        StartCoroutine(Shake());
        yield return new WaitForSeconds(1.6f);

        if (hearts <= 0) { loseReason = message; yield return LoseRoutine(); yield break; }
        if (customer != null) customer.Leave();
        foreach (CakeLayer l in placed) if (l != null) l.Fall(Range(-3f, 3f));
        placed.Clear();
        yield return new WaitForSeconds(0.5f);
        StartOrder();
    }

    // ================================================================== VICTORIA / DERROTA
    private async void Win()
    {
        state = State.Ended;
        StopEvents();
        AudioManager.PlayJingle(true);
        if (confetti != null) confetti.Burst(towerRoot.position + Vector3.up * 2f, 70, 9f);

        int bonusLevel = levelBonusBase + levelBonusPerLevel * levelNumber;
        int bonusHearts = Mathf.Max(0, hearts) * pointsPerHeartLeft;
        score += bonusLevel + bonusHearts;
        UpdateHud();

        int lost = level.hearts - hearts;
        int stars = lost <= 0 ? 3 : lost == 1 ? 2 : 1;
        int previousBest = Online ? FirebaseManager.Instance.Progress.GetLevel(levelNumber).bestScore : 0;

        ResultScreen.WinData d = new ResultScreen.WinData
        {
            level = levelNumber,
            score = score,
            stars = stars,
            newRecord = previousBest > 0 && score > previousBest,
            happyCustomers = new List<Sprite>(servedCustomers)
        };
        d.stats.Add(new ResultScreen.Stat { icon = UITheme.Get(ThemeKey.IconoPedido), label = "Pedidos entregados", value = ordersDone });
        d.stats.Add(new ResultScreen.Stat { icon = UITheme.Get(ThemeKey.IconoPerfecto), label = "Capas perfectas", value = perfectsTotal });
        if (fliesShooed > 0) d.stats.Add(new ResultScreen.Stat { icon = UITheme.Get(ThemeKey.IconoMosca), label = "Moscas espantadas", value = fliesShooed });
        if (decoysAvoided > 0) d.stats.Add(new ResultScreen.Stat { icon = UITheme.Get(ThemeKey.IconoQuemado), label = "Pasteles evitados", value = decoysAvoided });
        d.stats.Add(new ResultScreen.Stat { icon = UITheme.Get(ThemeKey.Estrella), label = "Bono de nivel", value = bonusLevel, plus = true });
        d.stats.Add(new ResultScreen.Stat { icon = UITheme.Get(ThemeKey.IconoCorazon), label = "Bono de corazones", value = bonusHearts, plus = true });

        results.ShowWin(d, levelNumber < LevelCatalog.Count);
        results.SetFooter("Guardando...");

        if (Online)
        {
            bool unlocked = await FirebaseManager.Instance.ReportLevelResultAsync(levelNumber, score, true, stars);
            if (this == null) return;
            string footer = "Puntaje total: " + FirebaseManager.Instance.Progress.totalScore;
            if (unlocked) footer += "   ·   ¡Nivel " + (levelNumber + 1) + " desbloqueado!";
            if (levelNumber >= LevelCatalog.Count) footer += "   ·   ¡Eres la mejor pastelera!";
            results.SetFooter(footer);
        }
        else results.SetFooter("(Modo prueba: no se guardó)");
        results.SetButtonsInteractable(true);
    }

    private IEnumerator LoseRoutine()
    {
        if (state == State.Ended) yield break;
        state = State.Ended;
        StopEvents();
        if (moving != null) { Destroy(moving.gameObject); moving = null; }
        yield return new WaitForSeconds(0.8f);
        AudioManager.PlayJingle(false);

        results.ShowLose(new ResultScreen.LoseData
        {
            level = levelNumber,
            reason = hud.noHeartsTitle + "\n<size=75%>" + loseReason + "</size>",
            score = score,
            ordersDone = ordersDone,
            ordersToWin = level.ordersToWin,
            angryCustomer = currentCustomer != null ? currentCustomer.angry : null
        });
        SaveLoss();
    }

    private async void SaveLoss()
    {
        if (Online) await FirebaseManager.Instance.ReportLevelResultAsync(levelNumber, score, false);
        if (this == null) return;
        results.SetButtonsInteractable(true);
    }

    // ================================================================== AYUDAS
    private void UpdateHud()
    {
        hud.scoreText.text = score.ToString();
        hud.ordersText.text = string.Format(hud.ordersFormat, ordersDone, level.ordersToWin);
        hud.SetHearts(Mathf.Max(0, hearts), level.hearts);
    }

    private IEnumerator Shake()
    {
        if (cam == null) yield break;
        Vector3 home = cam.transform.position;
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            cam.transform.position = home + (Vector3)Random.insideUnitCircle * 0.12f;
            yield return null;
        }
        cam.transform.position = home;
    }

    private float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);

    private void TogglePause()
    {
        if (state == State.Ended) return;
        if (state != State.Paused)
        {
            pausedFrom = state;
            state = State.Paused;
            Time.timeScale = 0f;
            hud.pausePanel.SetActive(true);
        }
        else
        {
            state = pausedFrom;
            Time.timeScale = 1f;
            hud.pausePanel.SetActive(false);
        }
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        OfflineLevel = levelNumber;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void NextLevel()
    {
        Time.timeScale = 1f;
        if (Online) FirebaseManager.Instance.PlayLevel(levelNumber + 1);
        else
        {
            OfflineLevel = Mathf.Min(levelNumber + 1, LevelCatalog.Count);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void GoToMenu()
    {
        Time.timeScale = 1f;
        if (Online)
        {
            if (state == State.Paused)
                _ = FirebaseManager.Instance.ReportLevelResultAsync(levelNumber, score, false);
            FirebaseManager.Instance.GoToMenu();
        }
        else SceneManager.LoadScene("MainMenu");
    }
}
