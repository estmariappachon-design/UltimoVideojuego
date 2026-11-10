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
        if (flourRoot != null) flourRoot.gameObject.SetActive(false);
    }

    /// <summary>Apagón: parpadea, se oscurece y deja un círculo de luz que sigue al pastel.</summary>
    public IEnumerator Blackout(float duration, Func<Vector3> worldFocus, Camera cam)
    {
        if (darkness == null) yield break;
        IsBlackout = true;
        darkness.gameObject.SetActive(true);

        // Parpadeo
        for (int i = 0; i < 4; i++)
        {
            SetDarkness(i % 2 == 0 ? darknessAlpha : 0.15f, worldFocus, cam);
            yield return new WaitForSeconds(0.09f + i * 0.03f);
        }

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            SetDarkness(darknessAlpha, worldFocus, cam);
            yield return null;
        }
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            SetDarkness(darknessAlpha * (1f - t / 0.5f), worldFocus, cam);
            yield return null;
        }
        darkness.gameObject.SetActive(false);
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
