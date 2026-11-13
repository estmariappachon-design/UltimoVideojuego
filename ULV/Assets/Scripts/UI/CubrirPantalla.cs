using UnityEngine;

/// <summary>
/// Hace que este objeto cubra TODA la pantalla aunque esté dentro del SafeArea
/// (en celulares con notch el SafeArea es más chico que la pantalla y se veían bordes sin cubrir).
/// Se usa en los fondos oscuros de las ventanas y en el fondo/scroll del mapa.
/// Los paneles que están ENCIMA (tarjetas, botones) no cambian de tamaño.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class CubrirPantalla : MonoBehaviour
{
    [Tooltip("Píxeles extra hacia afuera en cada borde (para que nunca quede una rayita)")]
    public float margen = 6f;
    [Tooltip("Píxeles extra SOLO a los lados (izquierda y derecha)")]
    public float extraLados = 0f;

    private static readonly Vector3[] corners = new Vector3[4];
    private RectTransform rt;
    private Canvas canvas;

    private void OnEnable() => Ajustar();
    private void LateUpdate() => Ajustar();

    public void Ajustar()
    {
        if (rt == null) rt = (RectTransform)transform;
        RectTransform parent = rt.parent as RectTransform;
        if (parent == null) return;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        RectTransform root = (RectTransform)canvas.rootCanvas.transform;

        // Esquinas de la pantalla (el Canvas) en el espacio del padre
        root.GetWorldCorners(corners);
        Vector2 min = parent.InverseTransformPoint(corners[0]);
        Vector2 max = parent.InverseTransformPoint(corners[2]);
        Rect pr = parent.rect;

        Vector2 offMin = new Vector2(min.x - pr.xMin - margen - extraLados, min.y - pr.yMin - margen);
        Vector2 offMax = new Vector2(max.x - pr.xMax + margen + extraLados, max.y - pr.yMax + margen);

        if (rt.anchorMin != Vector2.zero) rt.anchorMin = Vector2.zero;
        if (rt.anchorMax != Vector2.one) rt.anchorMax = Vector2.one;
        if ((rt.offsetMin - offMin).sqrMagnitude > 0.01f) rt.offsetMin = offMin;
        if ((rt.offsetMax - offMax).sqrMagnitude > 0.01f) rt.offsetMax = offMax;
    }
}
