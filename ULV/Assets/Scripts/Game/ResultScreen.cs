using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantallas de VICTORIA y DERROTA animadas:
///  - Victoria: rayos de luz girando, cinta con el título, 3 estrellas que aparecen una por una,
///    puntaje que cuenta hacia arriba, detalle con íconos, clientes felices, sello de "¡Nuevo récord!".
///  - Derrota: cliente enojado, motivo, barra de progreso de pedidos y un consejo.
/// Todo está en GameScene → Canvas → Resultados (muévelo y cámbialo a tu gusto).
/// </summary>
public class ResultScreen : MonoBehaviour
{
    public class Stat
    {
        public Sprite icon;
        public string label;
        public int value;
        public bool plus;
    }

    public class WinData
    {
        public int level;
        public int score;
        public int stars;
        public bool newRecord;
        public List<Stat> stats = new List<Stat>();
        public List<Sprite> happyCustomers = new List<Sprite>();
    }

    public class LoseData
    {
        public int level;
        public string reason;
        public int score;
        public int ordersDone;
        public int ordersToWin;
        public Sprite angryCustomer;
    }

    [Header("Común")]
    public CanvasGroup group;
    public RectTransform card;
    public RectTransform rays;
    public Image ribbon;
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    [Tooltip("Capa (encima de todo) donde salta el confeti de la pantalla de resultados")]
    public RectTransform confettiLayer;
    public Sprite[] confettiSprites;

    [Header("Victoria")]
    public GameObject winContent;
    public Image[] stars;
    public TMP_Text scoreText;
    public GameObject newRecordBadge;
    public Transform statsParent;
    [Tooltip("Fila de ejemplo con hijos: Icono (Image), Texto (TMP), Valor (TMP)")]
    public GameObject statRowTemplate;
    public Transform customersRow;
    public Image customerTemplate;
    public TMP_Text footerText;
    public Button nextButton;
    public Button retryButton;
    public Button menuButton;

    [Header("Derrota")]
    public GameObject loseContent;
    public Image loseCustomer;
    public TMP_Text loseReasonText;
    public TMP_Text loseProgressText;
    public Image loseProgressFill;
    public TMP_Text tipText;
    public Button loseRetryButton;
    public Button loseMenuButton;

    [Header("Colores")]
    public Color winRibbonColor = Color.white;
    public Color loseRibbonColor = new Color(0.75f, 0.7f, 0.85f);

    [Header("TEXTOS")]
    public string[] winTitles = { "¡Delicioso!", "¡Pastelería exitosa!", "¡Qué rico!", "¡Eres una gran pastelera!" };
    public string winSubtitleFormat = "Nivel {0} completado";
    public string loseTitle = "¡Oh no!";
    public string loseSubtitleFormat = "Nivel {0}";
    public string loseProgressFormat = "Pedidos entregados: {0} / {1}";
    public string scoreFormat = "{0}";
    [TextArea]
    public string[] tips =
    {
        "Consejo: espera a que la capa esté justo encima antes de tocar.",
        "Consejo: ¡no pongas los pasteles quemados! Déjalos pasar.",
        "Consejo: cuando el ventilador se enciende, espera a que pare.",
        "Consejo: toca las moscas apenas aparezcan.",
        "Consejo: en el temblor toca muy rápido para sujetar la torre.",
        "Consejo: con viento, suelta la capa un poquito antes, en contra del viento.",
        "Consejo: mira el ticket: la flecha te dice qué piso sigue."
    };

    private readonly List<GameObject> spawned = new List<GameObject>();

    private void Update()
    {
        if (rays != null && rays.gameObject.activeInHierarchy) rays.Rotate(0f, 0f, -18f * Time.deltaTime);
    }

    public void SetButtonsInteractable(bool value)
    {
        foreach (Button b in new[] { nextButton, retryButton, menuButton, loseRetryButton, loseMenuButton })
            if (b != null) b.interactable = value;
    }

    public void SetFooter(string text)
    {
        if (footerText != null) footerText.text = text;
    }

    // ================================================================== VICTORIA
    public void ShowWin(WinData d, bool hasNextLevel)
    {
        Prepare(true);
        if (nextButton != null) nextButton.gameObject.SetActive(hasNextLevel);
        StartCoroutine(WinRoutine(d));
    }

    private IEnumerator WinRoutine(WinData d)
    {
        if (titleText != null) titleText.text = winTitles.Length > 0 ? winTitles[Random.Range(0, winTitles.Length)] : "";
        if (subtitleText != null) subtitleText.text = string.Format(winSubtitleFormat, d.level);
        if (scoreText != null) scoreText.text = string.Format(scoreFormat, 0);
        if (newRecordBadge != null) newRecordBadge.SetActive(false);

        Sprite full = UITheme.Get(ThemeKey.Estrella);
        Sprite empty = UITheme.Get(ThemeKey.EstrellaVacia);
        foreach (Image s in stars)
        {
            if (s == null) continue;
            if (empty != null) s.sprite = empty;
            s.color = empty != null ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.4f);
            s.transform.localScale = Vector3.one;
        }

        yield return CardIn();

        // Estrellas una por una
        for (int i = 0; i < stars.Length && i < d.stars; i++)
        {
            yield return new WaitForSeconds(0.3f);
            Image s = stars[i];
            if (s == null) continue;
            if (full != null) s.sprite = full;
            s.color = Color.white;
            StartCoroutine(Pop(s.transform, 1.45f, 0.3f));
            AudioManager.PlayPitched(AudioManager.Sfx.StarPop, 1f + 0.15f * i);
            BurstUI(s.rectTransform.position, 14);
        }

        // Filas de detalle
        foreach (Stat st in d.stats)
        {
            GameObject row = Instantiate(statRowTemplate, statsParent);
            row.SetActive(true);
            spawned.Add(row);
            Image icon = FindComp<Image>(row, "Icono");
            TMP_Text label = FindComp<TMP_Text>(row, "Texto");
            TMP_Text value = FindComp<TMP_Text>(row, "Valor");
            if (icon != null) { icon.sprite = st.icon; icon.enabled = st.icon != null; }
            if (label != null) label.text = st.label;
            if (value != null) value.text = (st.plus ? "+" : "") + st.value;
            StartCoroutine(SlideIn(row.transform as RectTransform));
            AudioManager.PlayPitched(AudioManager.Sfx.Count, 1.1f);
            yield return new WaitForSeconds(0.12f);
        }

        // Clientes felices
        foreach (Sprite c in d.happyCustomers)
        {
            if (customerTemplate == null || c == null) break;
            Image img = Instantiate(customerTemplate, customersRow);
            img.gameObject.SetActive(true);
            img.sprite = c;
            spawned.Add(img.gameObject);
            StartCoroutine(Pop(img.transform, 1.2f, 0.25f));
            yield return new WaitForSeconds(0.1f);
        }

        // Puntaje contando
        float dur = 1.1f;
        int lastTick = -1;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / dur, 3f);
            int v = Mathf.RoundToInt(d.score * k);
            if (scoreText != null) scoreText.text = string.Format(scoreFormat, v);
            int tick = Mathf.FloorToInt(t * 14f);
            if (tick != lastTick) { lastTick = tick; AudioManager.PlayPitched(AudioManager.Sfx.Count, 0.9f + k * 0.6f); }
            yield return null;
        }
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, d.score);
            StartCoroutine(Pop(scoreText.transform, 1.25f, 0.25f));
        }

        if (d.newRecord && newRecordBadge != null)
        {
            yield return new WaitForSeconds(0.2f);
            newRecordBadge.SetActive(true);
            StartCoroutine(Pop(newRecordBadge.transform, 1.5f, 0.35f));
            AudioManager.Play(AudioManager.Sfx.NewRecord);
            BurstUI(newRecordBadge.transform.position, 30);
        }
    }

    // ================================================================== DERROTA
    public void ShowLose(LoseData d)
    {
        Prepare(false);
        StartCoroutine(LoseRoutine(d));
    }

    private IEnumerator LoseRoutine(LoseData d)
    {
        if (titleText != null) titleText.text = loseTitle;
        if (subtitleText != null) subtitleText.text = string.Format(loseSubtitleFormat, d.level);
        if (loseReasonText != null) loseReasonText.text = d.reason;
        if (loseProgressText != null) loseProgressText.text = string.Format(loseProgressFormat, d.ordersDone, d.ordersToWin);
        if (loseProgressFill != null) loseProgressFill.fillAmount = 0f;
        if (tipText != null) tipText.text = tips.Length > 0 ? tips[Random.Range(0, tips.Length)] : "";
        if (loseCustomer != null)
        {
            loseCustomer.sprite = d.angryCustomer;
            loseCustomer.enabled = d.angryCustomer != null;
        }

        yield return CardIn(true);

        // El cliente se sacude
        if (loseCustomer != null)
        {
            Vector3 home = loseCustomer.rectTransform.anchoredPosition;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                loseCustomer.rectTransform.anchoredPosition = home + new Vector3(Mathf.Sin(t * 60f) * 12f * (1f - t / 0.5f), 0f, 0f);
                yield return null;
            }
            loseCustomer.rectTransform.anchoredPosition = home;
        }

        // Barra de progreso
        float target = d.ordersToWin > 0 ? (float)d.ordersDone / d.ordersToWin : 0f;
        for (float t = 0f; t < 0.7f; t += Time.deltaTime)
        {
            if (loseProgressFill != null) loseProgressFill.fillAmount = Mathf.Lerp(0f, target, t / 0.7f);
            yield return null;
        }
        if (loseProgressFill != null) loseProgressFill.fillAmount = target;
    }

    // ================================================================== AYUDAS
    private void Prepare(bool win)
    {
        foreach (GameObject g in spawned) if (g != null) Destroy(g);
        spawned.Clear();
        gameObject.SetActive(true);
        if (winContent != null) winContent.SetActive(win);
        if (loseContent != null) loseContent.SetActive(!win);
        if (rays != null) rays.gameObject.SetActive(win);
        if (ribbon != null) ribbon.color = win ? winRibbonColor : loseRibbonColor;
        SetButtonsInteractable(false);
    }

    private IEnumerator CardIn(bool fromTop = false)
    {
        if (group != null) group.alpha = 0f;
        Vector2 home = card.anchoredPosition;
        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            float k = t / 0.45f;
            if (group != null) group.alpha = Mathf.Clamp01(k * 2f);
            if (fromTop)
            {
                float e = 1f - Mathf.Pow(1f - k, 3f);
                card.anchoredPosition = home + Vector2.up * (1-e) * 700f + Vector2.up * Mathf.Sin(k * Mathf.PI) * 30f;
                card.localScale = Vector3.one;
            }
            else
            {
                float s = k < 0.7f ? Mathf.Lerp(0.5f, 1.08f, k / 0.7f) : Mathf.Lerp(1.08f, 1f, (k - 0.7f) / 0.3f);
                card.localScale = Vector3.one * s;
            }
            yield return null;
        }
        if (group != null) group.alpha = 1f;
        card.anchoredPosition = home;
        card.localScale = Vector3.one;
    }

    private static IEnumerator Pop(Transform t, float peak, float duration)
    {
        for (float k = 0f; k < 1f; k += Time.deltaTime / duration)
        {
            if (t == null) yield break;
            float s = k < 0.6f ? Mathf.Lerp(0f, peak, k / 0.6f) : Mathf.Lerp(peak, 1f, (k - 0.6f) / 0.4f);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one;
    }

    private static IEnumerator SlideIn(RectTransform rt)
    {
        if (rt == null) yield break;
        CanvasGroup cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.25f)
        {
            if (rt == null) yield break;
            cg.alpha = k;
            rt.localScale = new Vector3(Mathf.Lerp(0.6f, 1f, k), 1f, 1f);
            yield return null;
        }
        if (rt != null) { cg.alpha = 1f; rt.localScale = Vector3.one; }
    }

    /// <summary>Confeti de la interfaz (sale desde un punto de la pantalla).</summary>
    public void BurstUI(Vector3 screenPosition, int count)
    {
        if (confettiLayer == null || confettiSprites == null || confettiSprites.Length == 0) return;
        for (int i = 0; i < count; i++) StartCoroutine(UIPiece(screenPosition));
    }

    private static readonly Color[] ConfettiColors =
    {
        new Color(1f, 0.6f, 0.75f), new Color(0.6f, 0.85f, 1f), new Color(1f, 0.9f, 0.5f),
        new Color(0.7f, 1f, 0.75f), new Color(0.85f, 0.7f, 1f)
    };

    private IEnumerator UIPiece(Vector3 screenPosition)
    {
        GameObject go = new GameObject("Confeti", typeof(RectTransform));
        go.transform.SetParent(confettiLayer, false);
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.sprite = confettiSprites[Random.Range(0, confettiSprites.Length)];
        Color c = ConfettiColors[Random.Range(0, ConfettiColors.Length)];
        img.color = c;
        RectTransform rt = (RectTransform)go.transform;
        float size = Random.Range(18f, 34f);
        rt.sizeDelta = new Vector2(size, size);
        rt.position = screenPosition;
        float scale = confettiLayer.lossyScale.x > 0f ? confettiLayer.lossyScale.x : 1f;
        Vector2 v = Random.insideUnitCircle.normalized * Random.Range(350f, 750f);
        v.y = Mathf.Abs(v.y) + 300f;
        float spin = Random.Range(-500f, 500f);
        float life = Random.Range(0.9f, 1.4f);
        for (float t = 0f; t < life; t += Time.deltaTime)
        {
            v.y -= 1400f * Time.deltaTime;
            rt.position += (Vector3)(v * scale * Time.deltaTime);
            rt.Rotate(0f, 0f, spin * Time.deltaTime);
            img.color = new Color(c.r, c.g, c.b, 1f - Mathf.Clamp01((t - life * 0.6f) / (life * 0.4f)));
            yield return null;
        }
        Destroy(go);
    }

    private static T FindComp<T>(GameObject root, string name) where T : Component
    {
        Transform t = root.transform.Find(name);
        return t != null ? t.GetComponent<T>() : null;
    }
}
