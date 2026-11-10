using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz del juego (GameScene → Canvas). Mueve, cambia tamaño, fuente o imagen
/// de cualquier parte con la herramienta Rect (T).
/// En "TEXTOS" están todos los mensajes; {0}, {1}… son datos que pone el juego (no los borres).
/// </summary>
public class GameHud : MonoBehaviour
{
    [Header("Arriba a la izquierda")]
    public TMP_Text levelText;
    public TMP_Text scoreText;
    public TMP_Text ordersText;
    public Image[] hearts;
    public Button pauseButton;

    [Header("RETO (siempre visible arriba)")]
    public GameObject challengeBanner;
    public TMP_Text levelChallengeText;
    public TMP_Text orderChallengeText;

    [Header("Viento")]
    public GameObject windRoot;
    public RectTransform windArrow;
    public TMP_Text windText;

    [Header("Pedido")]
    public OrderTicketUI ticket;

    [Header("Mensajes")]
    public TMP_Text messageText;
    public TMP_Text popupText;
    public GameObject tapHint;

    [Header("Avisos de eventos (ventilador, temblor, apagón…)")]
    public GameObject eventBanner;
    public TMP_Text eventText;

    [Header("Temblor: barra de estabilidad")]
    public GameObject stabilityRoot;
    public Image stabilityFill;
    public RectTransform stabilityHand;
    public Color stableColor = new Color(0.55f, 0.85f, 0.6f);
    public Color unstableColor = new Color(1f, 0.45f, 0.5f);

    [Header("Pausa")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button pauseRetryButton;
    public Button pauseMenuButton;

    [Header("TEXTOS (cámbialos aquí)")]
    public string levelFormat = "Nivel {0}: {1}";
    public string ordersFormat = "Pedidos {0}/{1}";
    public string ticketTitleFormat = "Pedido de {0}";
    public string challengePerfectFormat = "Extra: {0} capas perfectas";
    public string challengeWidthFormat = "Extra: que quede de {0}% de ancho";
    public string challengeNoMistakes = "Extra: ¡sin errores!";
    public string windFormat = "Viento {0}";
    [TextArea] public string introFormat = "Nivel {0}\n<size=55%>{1}</size>";
    public string introGoal = "Prepara {0} pasteles de {1} pisos";
    public string perfectMessage = "¡Perfecto!";
    public string perfectComboFormat = "¡Perfecto x{0}!";
    public string goodMessage = "¡Bien!";
    public string missMessage = "¡Ups, se cayó!";
    public string wrongFlavorFormat = "¡Ese piso era de {0}!";
    public string notInOrderMessage = "¡Ese sabor no está en el pedido!";
    public string burntMessage = "¡Era un pastel quemado!";
    public string decoyAvoidedMessage = "¡Bien hecho, lo dejaste pasar!";
    public string orderDoneMessage = "¡Pedido listo!";
    public string challengeFailedMessage = "¡No cumpliste el extra del pedido!";
    public string patienceOutMessage = "¡El cliente se cansó de esperar!";
    public string fanWarningMessage = "¡El ventilador se va a encender!";
    public string fanBlowingMessage = "¡RÁFAGA! Espera para soltar";
    public string flyIncomingMessage = "¡Una mosca! Tócala para espantarla";
    public string flyShooedMessage = "¡Fuera, mosca!";
    public string flyDamageMessage = "¡La mosca dañó el pastel!";
    public string tremorMessage = "¡La mesa tiembla! Toca rápido para sujetar la torre";
    public string tremorFallMessage = "¡Se cayó un piso!";
    public string blackoutMessage = "¡Se fueron las luces!";
    public string flourMessage = "¡Nube de harina!";
    public string orderChangeFormat = "¡Cambié de opinión! El piso {0} ahora es de {1}";
    public string noHeartsTitle = "¡Te quedaste sin corazones!";

    private Coroutine messageRoutine;
    private Coroutine popupRoutine;
    private Coroutine eventRoutine;

    // ------------------------------------------------------------------ ARRIBA
    public void SetHearts(int current, int max)
    {
        if (hearts == null) return;
        Sprite full = UITheme.Get(ThemeKey.CorazonLleno);
        Sprite empty = UITheme.Get(ThemeKey.CorazonVacio);
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;
            hearts[i].gameObject.SetActive(i < max);
            if (full != null && empty != null)
            {
                hearts[i].sprite = i < current ? full : empty;
                hearts[i].color = Color.white;
            }
            else hearts[i].color = i < current ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }
    }

    public void SetChallenge(string levelChallenge, string orderChallenge)
    {
        if (challengeBanner != null) challengeBanner.SetActive(!string.IsNullOrEmpty(levelChallenge) || !string.IsNullOrEmpty(orderChallenge));
        if (levelChallengeText != null) levelChallengeText.text = levelChallenge;
        if (orderChallengeText != null)
        {
            orderChallengeText.gameObject.SetActive(!string.IsNullOrEmpty(orderChallenge));
            orderChallengeText.text = orderChallenge;
        }
    }

    public void SetWind(float wind, float max)
    {
        if (windRoot == null) return;
        bool show = max > 0f && Mathf.Abs(wind) > max * 0.08f;
        windRoot.SetActive(show);
        if (!show) return;
        float k = Mathf.Clamp01(Mathf.Abs(wind) / max);
        if (windArrow != null)
            windArrow.localScale = new Vector3(wind < 0 ? -1f : 1f, 1f, 1f) * (0.6f + 0.5f * k);
        if (windText != null)
            windText.text = string.Format(windFormat, new string('»', Mathf.Max(1, Mathf.RoundToInt(k * 4f))));
    }

    // ------------------------------------------------------------------ MENSAJES
    public void ShowMessage(string text, float seconds)
    {
        if (messageText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text, seconds));
    }

    private IEnumerator MessageRoutine(string text, float seconds)
    {
        messageText.gameObject.SetActive(true);
        messageText.text = text;
        RectTransform rt = messageText.rectTransform;
        for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            rt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, t / 0.2f);
            yield return null;
        }
        rt.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(seconds);
        messageText.gameObject.SetActive(false);
    }

    public void HideMessage()
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        if (messageText != null) messageText.gameObject.SetActive(false);
    }

    /// <summary>Texto que salta (¡Perfecto!, ¡Bien!) con un pequeño rebote.</summary>
    public void Popup(string text, Color color)
    {
        if (popupText == null) return;
        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(PopupRoutine(text, color));
    }

    private IEnumerator PopupRoutine(string text, Color color)
    {
        popupText.gameObject.SetActive(true);
        popupText.text = text;
        popupText.color = color;
        RectTransform rt = popupText.rectTransform;
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            float pop = t < 0.15f ? Mathf.Lerp(0.4f, 1.2f, t / 0.15f) : Mathf.Lerp(1.2f, 1f, (t - 0.15f) / 0.2f);
            rt.localScale = Vector3.one * pop;
            popupText.alpha = t > 0.55f ? 1f - (t - 0.55f) / 0.25f : 1f;
            yield return null;
        }
        popupText.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ EVENTOS
    public void ShowEvent(string text, float seconds)
    {
        if (eventBanner == null) return;
        if (eventRoutine != null) StopCoroutine(eventRoutine);
        eventRoutine = StartCoroutine(EventRoutine(text, seconds));
    }

    private IEnumerator EventRoutine(string text, float seconds)
    {
        eventBanner.SetActive(true);
        if (eventText != null) eventText.text = text;
        Transform t = eventBanner.transform;
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.25f)
        {
            t.localScale = new Vector3(1f, Mathf.Lerp(0f, 1f, k), 1f);
            yield return null;
        }
        t.localScale = Vector3.one;
        for (float k = 0f; k < seconds; k += Time.deltaTime)
        {
            t.localScale = Vector3.one * (1f + Mathf.Sin(k * 10f) * 0.02f);
            yield return null;
        }
        HideEvent();
    }

    public void HideEvent()
    {
        if (eventRoutine != null) StopCoroutine(eventRoutine);
        if (eventBanner != null) eventBanner.SetActive(false);
    }

    public void ShowStability(bool show)
    {
        if (stabilityRoot != null) stabilityRoot.SetActive(show);
    }

    public void SetStability(float fraction)
    {
        if (stabilityFill == null) return;
        stabilityFill.fillAmount = Mathf.Clamp01(fraction);
        stabilityFill.color = Color.Lerp(unstableColor, stableColor, fraction);
    }

    public void PulseStabilityHand()
    {
        if (stabilityHand != null) StartCoroutine(Pulse(stabilityHand));
    }

    private static IEnumerator Pulse(Transform t)
    {
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.12f)
        {
            t.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, k);
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}
