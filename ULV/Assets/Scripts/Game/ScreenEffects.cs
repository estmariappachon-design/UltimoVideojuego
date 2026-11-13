using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Efectos que tapan la pantalla (en el Canvas del juego):
///  - APAGÓN (nivel 7): todo se oscurece y solo se ve un círculo de luz sobre el pastel.
///  - NUBE DE HARINA (nivel 9): nubes blancas tapan la pantalla un momento.
/// </summary>
public class ScreenEffects : MonoBehaviour
{
    [Header("Apagón")]
    [Tooltip("Imagen negra con un hueco transparente en el centro (TemaVisual → Oscuridad)")]
    public RectTransform darkness;
    public CanvasGroup darknessGroup;
    [Range(0f, 1f)] public float darknessAlpha = 0.96f;
    [Tooltip("Capa negra que oscurece también el círculo de luz (luz tenue). Si está vacía se crea sola")]
    public Image lightDimmer;

    [Header("Harina")]
    public RectTransform flourRoot;
    public Image[] flourPuffs;
    [Range(0f, 1f)] public float flourAlpha = 0.93f;

    public bool IsBlackout { get; private set; }
    public bool IsFlour { get; private set; }

    private void Awake()
    {
        StopAll();
    }

    public void StopAll()
    {
        StopAllCoroutines();
        IsBlackout = IsFlour = false;
        if (darkness != null) darkness.gameObject.SetActive(false);
        if (lightDimmer != null) lightDimmer.gameObject.SetActive(false);
        if (flourRoot != null) flourRoot.gameObject.SetActive(false);
    }

    /// <summary>Crea la capa de "luz tenue" si no existe.</summary>
    private void EnsureDimmer()
    {
        if (lightDimmer != null || darkness == null) return;
        GameObject go = new GameObject("LuzTenue", typeof(RectTransform), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(darkness.parent, false);
        go.transform.SetSiblingIndex(darkness.GetSiblingIndex() + 1);
        lightDimmer = go.GetComponent<Image>();
        lightDimmer.color = new Color(0.03f, 0.02f, 0.08f, 0f);
        lightDimmer.raycastTarget = false;
        go.AddComponent<CubrirPantalla>();
        go.SetActive(false);
    }

    private void SetDim(float alpha)
    {
        if (lightDimmer == null) return;
        Color c = lightDimmer.color;
        c.a = alpha;
        lightDimmer.color = c;
    }

    /// <summary>Apagón: parpadea, se oscurece y deja un círculo de luz que sigue al pastel.</summary>
    /// <param name="lightDim">0 = el círculo de luz es normal; 0.7 = la luz es muy tenue</param>
    public IEnumerator Blackout(float duration, Func<Vector3> worldFocus, Camera cam, float lightDim = 0f)
    {
        if (darkness == null) yield break;
        IsBlackout = true;
        darkness.gameObject.SetActive(true);
        if (lightDim > 0f)
        {
            EnsureDimmer();
            if (lightDimmer != null) { lightDimmer.gameObject.SetActive(true); SetDim(0f); }
        }

        // Parpadeo
        for (int i = 0; i < 4; i++)
        {
            bool off = i % 2 == 0;
            SetDarkness(off ? darknessAlpha : 0.15f, worldFocus, cam);
            SetDim(off ? lightDim : 0f);
            yield return new WaitForSeconds(0.09f + i * 0.03f);
        }

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            SetDarkness(darknessAlpha, worldFocus, cam);
            SetDim(lightDim);
            yield return null;
        }
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            SetDarkness(darknessAlpha * (1f - t / 0.5f), worldFocus, cam);
            SetDim(lightDim * (1f - t / 0.5f));
            yield return null;
        }
        darkness.gameObject.SetActive(false);
        if (lightDimmer != null) lightDimmer.gameObject.SetActive(false);
        IsBlackout = false;
    }

    private void SetDarkness(float alpha, Func<Vector3> worldFocus, Camera cam)
    {
        if (darknessGroup != null) darknessGroup.alpha = alpha;
        if (cam != null && worldFocus != null)
            darkness.position = cam.WorldToScreenPoint(worldFocus());
    }

    /// <summary>Nube de harina que tapa la pantalla.</summary>
    public IEnumerator Flour(float duration)
    {
        if (flourRoot == null || flourPuffs == null || flourPuffs.Length == 0) yield break;
        IsFlour = true;
        flourRoot.gameObject.SetActive(true);
        Rect area = flourRoot.rect;

        Vector2[] start = new Vector2[flourPuffs.Length];
        Vector2[] drift = new Vector2[flourPuffs.Length];
        for (int i = 0; i < flourPuffs.Length; i++)
        {
            RectTransform rt = flourPuffs[i].rectTransform;
            start[i] = new Vector2(UnityEngine.Random.Range(area.xMin * 0.8f, area.xMax * 0.8f), UnityEngine.Random.Range(area.yMin * 0.7f, area.yMax * 0.7f));
            drift[i] = UnityEngine.Random.insideUnitCircle * 60f;
            rt.anchoredPosition = start[i];
            rt.localScale = Vector3.one * UnityEngine.Random.Range(0.6f, 1.0f);
            rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-20f, 20f));
        }

        float total = duration + 1.4f;
        for (float t = 0f; t < total; t += Time.deltaTime)
        {
            float a = t < 0.4f ? t / 0.4f : t > duration + 0.4f ? 1f - (t - duration - 0.4f) : 1f;
            for (int i = 0; i < flourPuffs.Length; i++)
            {
                RectTransform rt = flourPuffs[i].rectTransform;
                rt.anchoredPosition = start[i] + drift[i] * t;
                rt.localScale = Vector3.one * (rt.localScale.x + Time.deltaTime * 0.05f);
                Color c = flourPuffs[i].color;
                c.a = Mathf.Clamp01(a) * flourAlpha;
                flourPuffs[i].color = c;
            }
            yield return null;
        }
        flourRoot.gameObject.SetActive(false);
        IsFlour = false;
    }
}
