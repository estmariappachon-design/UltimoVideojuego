using UnityEngine;

/// <summary>
/// Movimiento suave y tierno: sube y baja, "respira" (se agranda un poquito) y se balancea.
/// Se usa en el logo y en los personajes que se asoman sobre las ventanas. Ajusta los valores a tu gusto.
/// </summary>
public class Flotar : MonoBehaviour
{
    [Tooltip("Cuántos píxeles sube y baja")]
    public float altura = 12f;
    [Tooltip("Qué tan rápido")]
    public float velocidad = 1.6f;
    [Tooltip("Cuánto se agranda al 'respirar' (0.03 = 3%)")]
    public float respiro = 0.03f;
    [Tooltip("Grados que se balancea")]
    public float balanceo = 2f;
    [Tooltip("Al aparecer, crece con un rebote")]
    public bool rebotarAlAparecer = true;

    private Vector2 home;
    private Vector3 baseScale;
    private RectTransform rt;
    private float start;

    private void Awake()
    {
        rt = transform as RectTransform;
        baseScale = transform.localScale;
        if (rt != null) home = rt.anchoredPosition;
    }

    private void OnEnable()
    {
        start = Time.unscaledTime;
    }

    private void Update()
    {
        float t = Time.unscaledTime - start;
        float pop = 1f;
        if (rebotarAlAparecer && t < 0.45f)
        {
            float k = t / 0.45f;
            pop = k < 0.7f ? Mathf.Lerp(0.2f, 1.12f, k / 0.7f) : Mathf.Lerp(1.12f, 1f, (k - 0.7f) / 0.3f);
        }
        float s = 1f + Mathf.Sin(t * velocidad * 2f) * respiro;
        transform.localScale = baseScale * s * pop;
        transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * velocidad * 0.8f) * balanceo);
        if (rt != null) rt.anchoredPosition = home + Vector2.up * Mathf.Sin(t * velocidad) * altura;
    }
}
