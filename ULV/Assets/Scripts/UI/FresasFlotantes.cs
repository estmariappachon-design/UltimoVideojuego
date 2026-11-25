using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FONDO ANIMADO del login y el menú: fresas (y estrellitas/corazones) que caen despacio,
/// se balancean y giran, como una lluvia dulce. Va en un objeto que cubre la pantalla, detrás de los paneles.
/// Cambia las imágenes en "Imagenes" (arrastra tus fresas, corazones, chispitas...).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FresasFlotantes : MonoBehaviour
{
    [Tooltip("Imágenes que caen (se elige una al azar para cada una)")]
    public Sprite[] imagenes;
    [Tooltip("Cuántas hay en pantalla al mismo tiempo")]
    [Range(1, 60)] public int cantidad = 18;
    public Vector2 tamano = new Vector2(70f, 150f);
    [Tooltip("Velocidad de caída (pixeles por segundo)")]
    public Vector2 velocidad = new Vector2(60f, 140f);
    [Tooltip("Qué tanto se balancean de lado a lado")]
    public float balanceo = 40f;
    [Tooltip("Qué tan transparentes (0 = invisibles, 1 = sólidas)")]
    public Vector2 opacidad = new Vector2(0.55f, 1f);
    [Tooltip("Las más pequeñas se ven más transparentes (efecto de profundidad)")]
    public bool profundidad = true;

    private class Item
    {
        public RectTransform rt; public Image img;
        public float x, y, speed, sway, phase, spin, size;
    }

    private readonly List<Item> items = new List<Item>();
    private RectTransform area;

    private void OnEnable()
    {
        area = (RectTransform)transform;
        if (items.Count == 0) Build();
    }

    private void Build()
    {
        if (imagenes == null || imagenes.Length == 0) return;
        for (int i = 0; i < cantidad; i++)
        {
            GameObject go = new GameObject("Fresa", typeof(RectTransform), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(transform, false);
            Item it = new Item { rt = (RectTransform)go.transform, img = go.GetComponent<Image>() };
            it.img.raycastTarget = false;
            it.img.preserveAspect = true;
            it.rt.anchorMin = it.rt.anchorMax = new Vector2(0.5f, 0.5f);
            Respawn(it, true);
            items.Add(it);
        }
    }

    private void Respawn(Item it, bool anywhere)
    {
        Rect r = area.rect;
        it.img.sprite = imagenes[Random.Range(0, imagenes.Length)];
        float k = Random.value;
        it.size = Mathf.Lerp(tamano.x, tamano.y, k);
        it.rt.sizeDelta = new Vector2(it.size, it.size);
        float a = profundidad ? Mathf.Lerp(opacidad.x, opacidad.y, k) : Random.Range(opacidad.x, opacidad.y);
        it.img.color = new Color(1f, 1f, 1f, a);
        it.speed = Mathf.Lerp(velocidad.x, velocidad.y, profundidad ? k : Random.value);
        it.sway = Random.Range(0.4f, 1f) * balanceo;
        it.phase = Random.value * 10f;
        it.spin = Random.Range(-40f, 40f);
        it.x = Random.Range(r.xMin, r.xMax);
        it.y = anywhere ? Random.Range(r.yMin, r.yMax) : r.yMax + it.size;
        it.rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(-30f, 30f));
    }

    private void Update()
    {
        if (items.Count == 0) return;
        Rect r = area.rect;
        float t = Time.unscaledTime;
        foreach (Item it in items)
        {
            it.y -= it.speed * Time.unscaledDeltaTime;
            it.rt.anchoredPosition = new Vector2(it.x + Mathf.Sin(t * 1.3f + it.phase) * it.sway, it.y);
            it.rt.Rotate(0f, 0f, it.spin * Time.unscaledDeltaTime);
            if (it.y < r.yMin - it.size) Respawn(it, false);
        }
    }
}
