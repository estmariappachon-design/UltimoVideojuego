using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PRESENTACIÓN DEL PEDIDO: cuando llega un cliente aparece una burbuja de diálogo grande con
/// su saludo y el pastel que quiere (los pisos aparecen uno por uno). Al tocar la pantalla
/// (o después de unos segundos) la burbuja "vuela" hasta el ticket del pedido.
/// Muévela y cámbiale el tamaño en GameScene → Canvas → PresentacionPedido.
/// </summary>
public class OrderIntroUI : MonoBehaviour
{
    public RectTransform card;
    public CanvasGroup group;
    public TMP_Text nameText;
    public TMP_Text greetingText;
    public TMP_Text floorsText;
    public TMP_Text challengeText;
    public TMP_Text hintText;
    [Tooltip("Contenedor de las capas (VerticalLayoutGroup)")]
    public Transform layersParent;
    [Tooltip("Capa de ejemplo (Image, desactivada)")]
    public GameObject layerTemplate;
    public float autoContinueSeconds = 6f;

    [Header("Textos")]
    public string nameFormat = "{0} dice:";
    public string floorsFormat = "Pastel de {0} pisos";
    public string hintMessage = "Toca para empezar";

    public bool IsShowing { get; private set; }

    private readonly List<GameObject> spawned = new List<GameObject>();
    private bool skip;
    private bool canSkip;
    private Vector3 homePosition;
    private bool homeSaved;

    private void Awake()
    {
        if (!IsShowing) gameObject.SetActive(false);
    }

    public void Skip()
    {
        if (canSkip) skip = true;
    }

    public IEnumerator Play(CakeOrder order, LevelCatalog.Customer customer, LevelCatalog.Flavor[] flavors,
                            string challenge, RectTransform flyTarget)
    {
        IsShowing = true;
        gameObject.SetActive(true);
        if (!homeSaved) { homePosition = card.position; homeSaved = true; }
        card.position = homePosition;
        card.localScale = Vector3.zero;
        if (group != null) group.alpha = 1f;
        skip = false;
        canSkip = false;

        foreach (GameObject g in spawned) if (g != null) Destroy(g);
        spawned.Clear();

        if (nameText != null) nameText.text = string.Format(nameFormat, customer != null ? customer.name : "");
        if (greetingText != null) greetingText.text = customer != null ? customer.greeting : "";
        if (floorsText != null) floorsText.text = string.Format(floorsFormat, order.flavors.Length);
        if (challengeText != null)
        {
            challengeText.gameObject.SetActive(!string.IsNullOrEmpty(challenge));
            challengeText.text = challenge;
        }
        if (hintText != null) { hintText.text = hintMessage; hintText.alpha = 0f; }

        // Aparece la burbuja con rebote
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            float k = t / 0.35f;
            float s = k < 0.7f ? Mathf.Lerp(0f, 1.1f, k / 0.7f) : Mathf.Lerp(1.1f, 1f, (k - 0.7f) / 0.3f);
            card.localScale = Vector3.one * s;
            yield return null;
        }
        card.localScale = Vector3.one;

        // Los pisos aparecen uno por uno (de abajo hacia arriba)
        for (int i = 0; i < order.flavors.Length; i++)
        {
            GameObject row = Instantiate(layerTemplate, layersParent);
            row.SetActive(true);
            row.transform.SetAsFirstSibling();
            spawned.Add(row);
            Image img = row.GetComponent<Image>();
            if (img == null) img = row.GetComponentInChildren<Image>();
            if (img != null) { img.sprite = flavors[order.flavors[i]].layerSprite; img.preserveAspect = true; }
            TMP_Text label = row.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = (i + 1).ToString();
            StartCoroutine(PopIn(row.transform));
            AudioManager.PlayPitched(AudioManager.Sfx.Drop, 1f + i * 0.06f);
            yield return new WaitForSeconds(0.09f);
        }

        canSkip = true;
        float wait = 0f;
        while (!skip && wait < autoContinueSeconds)
        {
            wait += Time.deltaTime;
            if (hintText != null) hintText.alpha = 0.55f + Mathf.Sin(wait * 5f) * 0.45f;
            yield return null;
        }
        canSkip = false;
        AudioManager.Play(AudioManager.Sfx.Whoosh);

        // Vuela hasta el ticket
        Vector3 from = card.position;
        Vector3 to = flyTarget != null ? flyTarget.position : from;
        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            float k = t / 0.45f;
            float e = k * k * (3f - 2f * k);
            card.position = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 80f;
            card.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, e);
            if (group != null) group.alpha = 1f - k * 0.7f;
            yield return null;
        }

        gameObject.SetActive(false);
        card.position = homePosition;
        card.localScale = Vector3.one;
        IsShowing = false;
    }

    private IEnumerator PopIn(Transform t)
    {
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.18f)
        {
            if (t == null) yield break;
            float s = k < 0.7f ? Mathf.Lerp(0.2f, 1.15f, k / 0.7f) : Mathf.Lerp(1.15f, 1f, (k - 0.7f) / 0.3f);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one;
    }
}
