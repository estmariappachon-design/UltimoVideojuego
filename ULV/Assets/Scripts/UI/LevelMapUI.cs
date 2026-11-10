using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// MAPA DE NIVELES tipo camino (como Candy Crush), con scroll horizontal.
///  - Los niveles van en los puntos "Posicion_Nivel_XX" (MainMenu → MapaPanel → Scroll → Viewport → Contenido → Posiciones).
///  - Entre cada nivel se dibuja un caminito de puntos (más claro si ya lo pasaste).
///  - El marcador salta sobre el nivel donde vas.
///  - Al tocar un nivel se abre una ventana con su reto, estrellas y el botón ¡Jugar!
///
/// EN EL EDITOR se ve una VISTA PREVIA del mapa (niveles 1-3 completados, 4 actual, el resto bloqueado).
/// Mueve los puntos Posicion_Nivel_XX y el camino se redibuja solo. La vista previa no se guarda en la escena.
/// </summary>
[ExecuteAlways]
public class LevelMapUI : MonoBehaviour
{
    [Header("Partes")]
    public ScrollRect scroll;
    public RectTransform content;
    [Tooltip("Puntos donde va cada nivel. Si está vacío se calculan solos en zigzag")]
    public RectTransform[] levelPoints;
    public LevelNodeUI nodePrefab;
    public RectTransform nodesParent;
    public RectTransform pathParent;
    public RectTransform playerMarker;
    public LevelInfoPopup popup;

    [Header("Caminito")]
    public float dotSpacing = 46f;
    public Vector2 dotSize = new Vector2(24f, 24f);
    [Tooltip("Qué tanto se curva el camino entre niveles")]
    public float curve = 120f;
    public Color dotDoneColor = new Color(1f, 0.55f, 0.72f);
    public Color dotLockedColor = new Color(1f, 1f, 1f, 0.75f);
    [Tooltip("Altura del marcador sobre el nivel actual")]
    public float markerHeight = 150f;

    [Header("Zigzag automático (si no hay puntos)")]
    public float startX = 320f;
    public float stepX = 380f;
    public float amplitudeY = 230f;

    [Header("Editor")]
    [Tooltip("Mostrar en la escena una vista previa de los niveles y el camino (no se guarda)")]
    public bool vistaPreviaEnEditor = true;
    [Tooltip("Nivel donde se pone el marcador en la vista previa")]
    [Range(1, 10)] public int nivelActualVistaPrevia = 4;

    private readonly List<GameObject> spawned = new List<GameObject>();
    private Vector2 markerHome;
    private bool hasMarker;
    private string previewSignature = "";

    private void OnEnable()
    {
        if (Application.isPlaying) Build();
        else RefreshPreview(true);
    }

    private void OnDisable()
    {
        Clear();
        previewSignature = "";
    }

    // ================================================================== JUEGO
    public void Build()
    {
        PlayerProgress progress = FirebaseManager.Instance != null ? FirebaseManager.Instance.Progress : null;
        if (progress == null) progress = PlayerProgress.CreateNew("", "");   // para ver el mapa sin iniciar sesión
        int current = BuildWith(progress, true);
        if (Application.isPlaying) StartCoroutine(ScrollTo(PointOf(current)));
    }

    /// <returns>El nivel actual</returns>
    private int BuildWith(PlayerProgress progress, bool hidePoints)
    {
        Clear();
        ClearLeftovers();

        int count = Mathf.Min(PlayerProgress.TotalLevels, Mathf.Max(1, LevelCatalog.Count));
        Vector2[] pos = Positions(count);
        if (hidePoints && levelPoints != null)
            foreach (RectTransform p in levelPoints) if (p != null) p.gameObject.SetActive(false);

        // Nivel actual = el primero desbloqueado que no está completado
        int current = 1;
        for (int i = 1; i <= count; i++)
        {
            if (progress.IsUnlocked(i)) current = i;
            if (progress.GetLevel(i).state == LevelState.Unlocked) { current = i; break; }
        }

        // Caminito
        Sprite dot = UITheme.Get(ThemeKey.PuntoCamino);
        if (pathParent != null)
        {
            for (int i = 0; i < count - 1; i++)
            {
                bool done = progress.GetLevel(i + 1).state == LevelState.Completed;
                Vector2 a = pos[i], b = pos[i + 1];
                Vector2 mid = (a + b) / 2f;
                Vector2 dir = (b - a).normalized;
                Vector2 normal = new Vector2(-dir.y, dir.x) * (i % 2 == 0 ? curve : -curve);
                Vector2 control = mid + normal;
                float length = Vector2.Distance(a, b) * 1.15f;
                int dots = Mathf.Max(2, Mathf.RoundToInt(length / Mathf.Max(4f, dotSpacing)));
                for (int k = 1; k < dots; k++)
                {
                    float t = k / (float)dots;
                    Vector2 p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * control + t * t * b;
                    GameObject d = new GameObject("Punto", typeof(RectTransform), typeof(Image));
                    Track(d);
                    d.transform.SetParent(pathParent, false);
                    RectTransform rt = (RectTransform)d.transform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                    rt.sizeDelta = dotSize;
                    rt.anchoredPosition = p;
                    Image img = d.GetComponent<Image>();
                    img.sprite = dot;
                    img.raycastTarget = false;
                    img.color = done ? dotDoneColor : dotLockedColor;
                }
            }
        }

        // Nodos
        if (nodePrefab != null && nodesParent != null)
        {
            for (int i = 1; i <= count; i++)
            {
                LevelNodeUI node = Instantiate(nodePrefab, nodesParent);
                Track(node.gameObject);
                RectTransform rt = (RectTransform)node.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.anchoredPosition = pos[i - 1];
                node.Setup(i, progress.GetLevel(i), LevelCatalog.Get(i), i == current, OnNodeClicked);
            }
        }

        // Marcador del jugador
        if (playerMarker != null)
        {
            playerMarker.SetAsLastSibling();
            playerMarker.anchorMin = playerMarker.anchorMax = new Vector2(0f, 0.5f);
            markerHome = pos[current - 1] + new Vector2(0f, markerHeight);
            playerMarker.anchoredPosition = markerHome;
            hasMarker = true;
        }
        return current;
    }

    private Vector2[] Positions(int count)
    {
        Vector2[] pos = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            if (levelPoints != null && i < levelPoints.Length && levelPoints[i] != null) pos[i] = levelPoints[i].anchoredPosition;
            else pos[i] = new Vector2(startX + i * stepX, (i % 2 == 0 ? -1f : 1f) * amplitudeY * (i % 4 < 2 ? 1f : 0.5f));
        }
        return pos;
    }

    private float PointOf(int level)
    {
        Vector2[] pos = Positions(Mathf.Min(PlayerProgress.TotalLevels, Mathf.Max(1, LevelCatalog.Count)));
        return pos[Mathf.Clamp(level - 1, 0, pos.Length - 1)].x;
    }

    private IEnumerator ScrollTo(float x)
    {
        yield return null;
        if (scroll == null || content == null) yield break;
        Canvas.ForceUpdateCanvases();
        float viewport = scroll.viewport != null ? scroll.viewport.rect.width : ((RectTransform)scroll.transform).rect.width;
        float range = content.rect.width - viewport;
        if (range > 0f) scroll.horizontalNormalizedPosition = Mathf.Clamp01((x - viewport / 2f) / range);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            RefreshPreview(false);
            return;
        }
        if (hasMarker && playerMarker != null)
            playerMarker.anchoredPosition = markerHome + Vector2.up * Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 22f;
    }

    private void OnNodeClicked(int levelNumber)
    {
        if (!Application.isPlaying) return;
        PlayerProgress progress = FirebaseManager.Instance != null ? FirebaseManager.Instance.Progress : null;
        LevelData data = progress != null ? progress.GetLevel(levelNumber) : new LevelData { state = LevelState.Unlocked };
        if (popup != null) popup.Show(levelNumber, data, LevelCatalog.Get(levelNumber), () => Play(levelNumber));
        else Play(levelNumber);
    }

    private void Play(int levelNumber)
    {
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.Progress != null)
            FirebaseManager.Instance.PlayLevel(levelNumber);
        else
        {
            StackGame.OfflineLevel = levelNumber;
            SceneManager.LoadScene(LevelCatalog.SceneFor(levelNumber));
        }
    }

    // ================================================================== VISTA PREVIA (EDITOR)
    private void RefreshPreview(bool force)
    {
        if (!vistaPreviaEnEditor)
        {
            if (spawned.Count > 0) Clear();
            previewSignature = "";
            return;
        }
        string sig = Signature();
        if (!force && sig == previewSignature && spawned.Count > 0) return;
        previewSignature = sig;

        PlayerProgress demo = PlayerProgress.CreateNew("", "");
        int current = Mathf.Clamp(nivelActualVistaPrevia, 1, PlayerProgress.TotalLevels);
        int[] demoStars = { 3, 2, 3, 1, 2, 3, 2, 1, 3 };
        for (int i = 1; i <= PlayerProgress.TotalLevels; i++)
        {
            LevelData d = demo.GetLevel(i);
            if (d == null) continue;
            d.state = i < current ? LevelState.Completed : i == current ? LevelState.Unlocked : LevelState.Locked;
            d.stars = i < current ? demoStars[(i - 1) % demoStars.Length] : 0;
            d.bestScore = i < current ? 1000 + i * 150 : 0;
        }
        BuildWith(demo, false);
    }

    private string Signature()
    {
        var sb = new System.Text.StringBuilder();
        if (levelPoints != null)
            foreach (RectTransform p in levelPoints)
                if (p != null) sb.Append(p.anchoredPosition.x.ToString("0")).Append(',').Append(p.anchoredPosition.y.ToString("0")).Append(';');
        sb.Append(curve).Append(dotSpacing).Append(dotSize).Append(nivelActualVistaPrevia).Append(markerHeight)
          .Append(dotDoneColor).Append(dotLockedColor).Append(UITheme.Get(ThemeKey.PuntoCamino) != null ? UITheme.Get(ThemeKey.PuntoCamino).name : "");
        return sb.ToString();
    }

    /// <summary>Lo creado en el editor no se guarda en la escena ni se puede seleccionar por accidente.</summary>
    private void Track(GameObject go)
    {
        spawned.Add(go);
        if (Application.isPlaying) return;
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
#if UNITY_EDITOR
        UnityEditor.SceneVisibilityManager.instance.DisablePicking(go, true);
#endif
    }

    private void Clear()
    {
        foreach (GameObject g in spawned)
        {
            if (g == null) continue;
            if (Application.isPlaying) Destroy(g);
            else DestroyImmediate(g);
        }
        spawned.Clear();
    }

    /// <summary>Por si quedó algo de una vista previa anterior (por ejemplo, después de recompilar).</summary>
    private void ClearLeftovers()
    {
        foreach (RectTransform parent in new[] { nodesParent, pathParent })
        {
            if (parent == null) continue;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject c = parent.GetChild(i).gameObject;
                if ((c.hideFlags & HideFlags.DontSave) == 0) continue;
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
        }
    }
}
