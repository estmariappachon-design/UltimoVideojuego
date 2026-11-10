using UnityEngine;

/// <summary>
/// Pone la imagen de fondo cubriendo toda la cámara (sea cual sea el tamaño del celular).
/// Va en un hijo de la cámara con SpriteRenderer.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFit : MonoBehaviour
{
    private SpriteRenderer sr;
    private Camera cam;
    private float lastAspect = -1f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        cam = GetComponentInParent<Camera>();
    }

    private void LateUpdate()
    {
        if (cam == null || sr.sprite == null) return;
        if (Mathf.Approximately(cam.aspect, lastAspect)) return;
        lastAspect = cam.aspect;

        float camH = cam.orthographicSize * 2f;
        float camW = camH * cam.aspect;
        Vector2 size = sr.sprite.bounds.size;
        float k = Mathf.Max(camW / size.x, camH / size.y) * 1.02f;
        transform.localScale = new Vector3(k, k, 1f);
        transform.localPosition = new Vector3(0f, 0f, 20f);
    }
}
