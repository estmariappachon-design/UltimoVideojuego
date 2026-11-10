using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// El TICKET del pedido: quién pide, los pisos (arriba el último, abajo el primero),
/// cuáles ya están hechos, la flecha del piso que sigue, el extra y la barra de paciencia.
/// </summary>
public class OrderTicketUI : MonoBehaviour
{
    [Header("Partes")]
    public TMP_Text titleText;
    public TMP_Text challengeText;
    [Tooltip("Contenedor de las filas (con VerticalLayoutGroup)")]
    public Transform rowsParent;
    [Tooltip("Fila de ejemplo (desactivada). Hijos: Icono (Image), Nombre (TMP), Check (Image), Flecha (Image)")]
    public GameObject rowTemplate;
    public GameObject patienceRoot;
    public Image patienceFill;
    public CanvasGroup group;

    [Header("Colores")]
    public Color doneColor = new Color(1f, 1f, 1f, 0.45f);
    public Color patienceOk = new Color(0.55f, 0.85f, 0.55f);
    public Color patienceLow = new Color(1f, 0.45f, 0.5f);
    public Color changedColor = new Color(1f, 0.85f, 0.4f);

    private class Row { public GameObject go; public Image icon; public TMP_Text label; public GameObject check; public GameObject arrow; public Image background; }
    private readonly List<Row> rows = new List<Row>();   // rows[0] = piso de abajo
    private int current;

    public void SetVisible(bool visible)
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null) group.alpha = visible ? 1f : 0f;
    }

    public void Show(CakeOrder order, LevelCatalog.Flavor[] flavors, string title, string challenge)
    {
        foreach (Row r in rows) if (r.go != null) Destroy(r.go);
        rows.Clear();
        if (titleText != null) titleText.text = title;
        if (challengeText != null)
        {
            challengeText.gameObject.SetActive(!string.IsNullOrEmpty(challenge));
            challengeText.text = challenge;
        }

        for (int i = 0; i < order.flavors.Length; i++)
        {
            GameObject go = Instantiate(rowTemplate, rowsParent);
            go.SetActive(true);
            go.transform.SetAsFirstSibling();
            Row row = new Row
            {
                go = go,
                icon = FindComp<Image>(go, "Icono"),
                label = FindComp<TMP_Text>(go, "Nombre"),
                check = FindChild(go, "Check"),
                arrow = FindChild(go, "Flecha"),
                background = go.GetComponent<Image>()
            };
            rows.Add(row);
            SetRowFlavor(i, flavors[order.flavors[i]]);
            if (row.check != null) row.check.SetActive(false);
            if (row.arrow != null) row.arrow.SetActive(false);
        }
        SetCurrentFloor(0);
        SetPatience(order.patience > 0f ? 1f : -1f);
    }

    private void SetRowFlavor(int i, LevelCatalog.Flavor f)
    {
        Row row = rows[i];
        if (row.icon != null) { row.icon.sprite = f.layerSprite; row.icon.preserveAspect = true; }
        if (row.label != null) row.label.text = (i + 1) + ". " + f.name;
    }

    public void SetCurrentFloor(int floor)
    {
        current = floor;
        for (int i = 0; i < rows.Count; i++)
        {
            bool done = i < floor;
            if (rows[i].check != null) rows[i].check.SetActive(done);
            if (rows[i].arrow != null) rows[i].arrow.SetActive(i == floor);
            if (rows[i].icon != null) rows[i].icon.color = done ? doneColor : Color.white;
            if (rows[i].label != null) rows[i].label.alpha = done ? 0.5f : 1f;
        }
    }

    /// <summary>El cliente cambió el sabor de un piso: la fila brilla.</summary>
    public void ChangeRow(int floor, LevelCatalog.Flavor f)
    {
        if (floor < 0 || floor >= rows.Count) return;
        SetRowFlavor(floor, f);
        StartCoroutine(Flash(rows[floor]));
    }

    private IEnumerator Flash(Row row)
    {
        if (row.background == null)
        {
            row.background = row.go.AddComponent<Image>();
            row.background.raycastTarget = false;
        }
        Color baseColor = new Color(1f, 1f, 1f, 0f);
        for (float t = 0f; t < 2f; t += Time.deltaTime)
        {
            float k = (Mathf.Sin(t * 12f) + 1f) * 0.5f;
            row.background.color = Color.Lerp(baseColor, changedColor, k);
            row.go.transform.localScale = Vector3.one * (1f + k * 0.06f);
            yield return null;
        }
        row.background.color = baseColor;
        row.go.transform.localScale = Vector3.one;
    }

    /// <summary>El ticket aparece con un rebote.</summary>
    public void Pop()
    {
        StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        Transform t = transform;
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.3f)
        {
            float s = k < 0.6f ? Mathf.Lerp(0.85f, 1.08f, k / 0.6f) : Mathf.Lerp(1.08f, 1f, (k - 0.6f) / 0.4f);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private void Update()
    {
        // La flecha del piso que sigue late
        if (current >= 0 && current < rows.Count && rows[current].arrow != null)
        {
            Transform a = rows[current].arrow.transform;
            a.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 8f) * 0.15f);
        }
    }

    /// <summary>fraction &lt; 0 = sin paciencia (se oculta la barra).</summary>
    public void SetPatience(float fraction)
    {
        if (patienceRoot != null) patienceRoot.SetActive(fraction >= 0f);
        if (patienceFill == null || fraction < 0f) return;
        patienceFill.fillAmount = Mathf.Clamp01(fraction);
        patienceFill.color = fraction < 0.3f ? patienceLow : patienceOk;
    }

    private static GameObject FindChild(GameObject root, string name)
    {
        Transform t = root.transform.Find(name);
        return t != null ? t.gameObject : null;
    }

    private static T FindComp<T>(GameObject root, string name) where T : Component
    {
        Transform t = root.transform.Find(name);
        return t != null ? t.GetComponent<T>() : null;
    }
}
