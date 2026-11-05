using UnityEngine;

/// <summary>
/// Ajusta un panel al "área segura" del celular para que el notch o
/// la barra de gestos no tapen botones. Ponlo en un panel hijo del Canvas
/// que ocupe toda la pantalla, y mete toda la UI dentro de él.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    private RectTransform rect;
    private Rect lastSafeArea;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea) Apply();
    }

    private void Apply()
    {
        Rect safe = Screen.safeArea;
        lastSafeArea = safe;

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        min.x /= Screen.width;  min.y /= Screen.height;
        max.x /= Screen.width;  max.y /= Screen.height;

        rect.anchorMin = min;
        rect.anchorMax = max;
    }
}
