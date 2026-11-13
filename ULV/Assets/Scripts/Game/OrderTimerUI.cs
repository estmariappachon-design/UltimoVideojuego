using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CRONÓMETRO del pedido (nivel 8): cuenta los segundos que quedan para entregar el pastel.
/// Se activa cuando el nivel tiene "Patience Seconds" mayor que 0 en el LevelCatalog.
/// Está en GameScene → Canvas → SafeArea → Cronometro.
/// </summary>
public class OrderTimerUI : MonoBehaviour
{
    public TMP_Text timeText;
    public Image icon;
    public Image fill;

    [Header("Colores")]
    public Color normalColor = new Color(0.47f, 0.27f, 0.33f);
    public Color hurryColor = new Color(0.9f, 0.2f, 0.3f);
    [Tooltip("Desde cuántos segundos se pone rojo y late")]
    public float hurrySeconds = 10f;

    public string format = "{0}:{1:00}";

    public void Show(bool show)
    {
        if (gameObject.activeSelf != show) gameObject.SetActive(show);
        if (!show) transform.localScale = Vector3.one;
    }

    public void Set(float secondsLeft, float total)
    {
        secondsLeft = Mathf.Max(0f, secondsLeft);
        int s = Mathf.CeilToInt(secondsLeft);
        if (timeText != null)
        {
            timeText.text = string.Format(format, s / 60, s % 60);
            timeText.color = secondsLeft <= hurrySeconds ? hurryColor : normalColor;
        }
        if (fill != null) fill.fillAmount = total > 0f ? secondsLeft / total : 0f;
        if (fill != null) fill.color = secondsLeft <= hurrySeconds ? hurryColor : new Color(0.45f, 0.8f, 0.55f);
        float pulse = secondsLeft <= hurrySeconds && secondsLeft > 0f ? 1f + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.12f : 1f;
        transform.localScale = Vector3.one * pulse;
    }

    // ================================================================== CREAR
    public static OrderTimerUI Create(Transform parent, TMP_FontAsset font, Sprite ui = null, Sprite circle = null)
    {
        if (ui == null) ui = TimingMeterUI.RoundSprite();
        if (circle == null) circle = TimingMeterUI.RoundSprite();
        Sprite panel = UITheme.Get(ThemeKey.Panel);

        GameObject root = new GameObject("Cronometro", typeof(RectTransform));
        root.layer = 5;
        RectTransform rt = (RectTransform)root.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(150f, -345f);
        rt.sizeDelta = new Vector2(300f, 96f);

        Image bg = TimingMeterUI.Img(rt, "Fondo", panel != null ? panel : ui, new Color(1f, 0.97f, 0.94f, 0.97f));
        TimingMeterUI.Stretch(bg.rectTransform, 0, 0, 0, 0);

        // Reloj: círculo que se va vaciando
        Image ring = TimingMeterUI.Img(rt, "Reloj", circle, new Color(0.9f, 0.85f, 0.88f));
        ring.type = Image.Type.Simple;
        ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        ring.rectTransform.anchoredPosition = new Vector2(52f, 0f);
        ring.rectTransform.sizeDelta = new Vector2(68f, 68f);
        Image fill = TimingMeterUI.Img(ring.rectTransform, "Relleno", circle, new Color(0.45f, 0.8f, 0.55f));
        TimingMeterUI.Stretch(fill.rectTransform, 6, 6, 6, 6);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = false;

        TMP_Text t = TimingMeterUI.Txt(rt, "Tiempo", "1:15", 52, font);
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        t.rectTransform.pivot = new Vector2(0f, 0.5f);
        t.rectTransform.anchoredPosition = new Vector2(100f, 0f);
        t.rectTransform.sizeDelta = new Vector2(190f, 80f);

        OrderTimerUI timer = root.AddComponent<OrderTimerUI>();
        timer.timeText = t;
        timer.icon = ring;
        timer.fill = fill;
        root.SetActive(false);
        return timer;
    }
}
