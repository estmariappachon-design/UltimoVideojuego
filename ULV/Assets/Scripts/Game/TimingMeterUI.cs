using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MEDIDOR DE PRECISIÓN (nivel 2): una barra con una zona VERDE y una línea que va y viene.
/// Si tocas cuando la línea está en el verde, el pastel cae PERFECTO en su lugar (el viento no lo mueve).
/// Está en GameScene → Canvas → SafeArea → MedidorPrecision (muévelo / cámbiale imágenes y colores como quieras).
/// </summary>
public class TimingMeterUI : MonoBehaviour
{
    [Header("Partes")]
    public RectTransform bar;
    public RectTransform greenZone;
    public RectTransform needle;
    public TMP_Text label;
    public Image greenImage;

    [Header("Colores")]
    public Color greenColor = new Color(0.35f, 0.82f, 0.45f);
    public Color hitColor = new Color(1f, 0.9f, 0.3f);
    public Color missColor = new Color(1f, 0.45f, 0.5f);

    [Header("Textos")]
    public string helpText = "¡Toca cuando la línea esté en el VERDE!";

    private float pos;          // 0..1
    private int dir = 1;
    private float speed = 0.9f; // recorridos por segundo
    private float greenCenter = 0.5f, greenSize = 0.22f;
    private Coroutine flash;

    public bool InGreen => Mathf.Abs(pos - greenCenter) <= greenSize / 2f;

    public void Show(bool show)
    {
        gameObject.SetActive(show);
        if (show && label != null) label.text = helpText;
    }

    /// <summary>Nueva ronda: la zona verde cambia de lugar.</summary>
    public void NewRound(System.Random rng, float size, float meterSpeed)
    {
        greenSize = Mathf.Clamp(size, 0.05f, 0.8f);
        speed = Mathf.Max(0.1f, meterSpeed);
        float half = greenSize / 2f;
        greenCenter = Mathf.Lerp(half + 0.05f, 1f - half - 0.05f, (float)rng.NextDouble());
        pos = (float)rng.NextDouble();
        dir = rng.Next(2) == 0 ? 1 : -1;
        if (greenImage != null) greenImage.color = greenColor;
        Layout();
    }

    public void Tick(float dt)
    {
        pos += dir * speed * dt;
        if (pos > 1f) { pos = 2f - pos; dir = -1; }
        else if (pos < 0f) { pos = -pos; dir = 1; }
        Layout();
    }

    public void Feedback(bool hit)
    {
        if (!gameObject.activeInHierarchy) return;
        if (flash != null) StopCoroutine(flash);
        flash = StartCoroutine(Flash(hit));
    }

    private IEnumerator Flash(bool hit)
    {
        if (greenImage == null) yield break;
        greenImage.color = hit ? hitColor : missColor;
        Vector3 s = bar != null ? bar.localScale : Vector3.one;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            if (bar != null) bar.localScale = s * (1f + Mathf.Sin(t / 0.3f * Mathf.PI) * 0.08f);
            yield return null;
        }
        if (bar != null) bar.localScale = Vector3.one;
        greenImage.color = greenColor;
    }

    private void Layout()
    {
        if (bar == null) return;
        float w = bar.rect.width;
        if (greenZone != null)
        {
            greenZone.anchorMin = new Vector2(greenCenter - greenSize / 2f, 0f);
            greenZone.anchorMax = new Vector2(greenCenter + greenSize / 2f, 1f);
            greenZone.offsetMin = greenZone.offsetMax = Vector2.zero;
        }
        if (needle != null)
        {
            needle.anchorMin = needle.anchorMax = new Vector2(pos, 0.5f);
            needle.anchoredPosition = Vector2.zero;
        }
    }

    // ================================================================== CREAR (lo usan el constructor y la actualización)
    /// <param name="ui">Imagen redondeada para la barra (en el editor se pasa la de Unity; al jugar se genera una)</param>
    public static TimingMeterUI Create(Transform parent, TMP_FontAsset font, Sprite ui = null)
    {
        if (ui == null) ui = RoundSprite();
        Sprite panel = UITheme.Get(ThemeKey.Panel);

        GameObject root = new GameObject("MedidorPrecision", typeof(RectTransform));
        root.layer = 5;
        RectTransform rt = (RectTransform)root.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);   // abajo a la derecha (no tapa el pastel)
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-24f, 24f);
        rt.sizeDelta = new Vector2(600f, 150f);

        Image bg = Img(rt, "Fondo", panel != null ? panel : ui, new Color(1f, 0.97f, 0.94f, 0.95f));
        Stretch(bg.rectTransform, 0, 0, 0, 0);

        TMP_Text label = Txt(rt, "Texto", "¡Toca cuando la línea esté en el VERDE!", 28, font);
        Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(560, 42));

        Image barBg = Img(rt, "Barra", ui, new Color(0.93f, 0.85f, 0.9f));
        Place(barBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(540, 56));

        Image green = Img(barBg.rectTransform, "ZonaVerde", ui, new Color(0.35f, 0.82f, 0.45f));
        Stretch(green.rectTransform, 0, 0, 0, 0);

        Image needle = Img(barBg.rectTransform, "Linea", ui, new Color(0.35f, 0.15f, 0.25f));
        needle.rectTransform.sizeDelta = new Vector2(12f, 84f);

        TimingMeterUI m = root.AddComponent<TimingMeterUI>();
        m.bar = barBg.rectTransform;
        m.greenZone = green.rectTransform;
        m.greenImage = green;
        m.needle = needle.rectTransform;
        m.label = label;
        m.Layout();
        root.SetActive(false);
        return m;
    }

    private static Sprite round;

    /// <summary>Círculo blanco con bordes para estirar (se crea al jugar si no hay otra imagen).</summary>
    internal static Sprite RoundSprite()
    {
        if (round != null) return round;
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float r = n / 2f;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d));
            }
        tex.SetPixels(px);
        tex.Apply();
        round = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(31, 31, 31, 31));
        round.name = "redondo";
        return round;
    }

    internal static Image Img(Transform parent, string name, Sprite s, Color c)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = s;
        img.type = s != null && s.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    internal static TMP_Text Txt(Transform parent, string name, string text, float size, TMP_FontAsset font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.47f, 0.27f, 0.33f);
        t.enableAutoSizing = true;
        t.fontSizeMin = 14;
        t.fontSizeMax = size;
        t.raycastTarget = false;
        return t;
    }

    internal static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, anchor.y);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    internal static void Stretch(RectTransform rt, float l, float r, float t, float b)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }
}
