using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Conecta este objeto (Image de la UI o SpriteRenderer del escenario) con una casilla del TemaVisual.
///
/// Tienes DOS formas de cambiar la imagen y las dos funcionan:
///  1) En Assets/Resources/TemaVisual → cambia la casilla y se actualizan TODOS los objetos con esa casilla.
///  2) Directo en la escena: arrastra tu imagen al "Source Image" (o "Sprite") de este objeto.
///     El editor la guarda sola en el TemaVisual, así que todos los objetos iguales la usan
///     (por ejemplo, cambias un botón y cambian todos los botones).
///     Si quieres que SOLO este objeto tenga otra imagen, marca "Solo Este Objeto".
///
/// Al jugar NUNCA se reemplaza una imagen tuya: solo se cambian las imágenes de ejemplo del juego.
/// </summary>
[ExecuteAlways]
public class ThemedImage : MonoBehaviour
{
    public ThemeKey key;

    [Tooltip("Márcalo si quieres que este objeto tenga SU PROPIA imagen (no la del TemaVisual y sin cambiar los demás)")]
    public bool soloEsteObjeto = false;

    [Tooltip("Solo para SpriteRenderer: alto final en unidades del mundo (0 = no cambiar el tamaño)")]
    public float fitHeight = 0f;

    [SerializeField, HideInInspector] private Sprite applied;   // la última imagen que puso el tema

    private static readonly List<ThemedImage> all = new List<ThemedImage>();
    public static IReadOnlyList<ThemedImage> All => all;

    private void OnEnable()
    {
        if (!all.Contains(this)) all.Add(this);
        Apply();
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    public Sprite CurrentSprite
    {
        get
        {
            Image img = GetComponent<Image>();
            if (img != null) return img.sprite;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            return sr != null ? sr.sprite : null;
        }
    }

    /// <summary>¿Tiene una imagen puesta a mano (distinta de la del tema y que no es de ejemplo)?</summary>
    public bool HasOwnSprite
    {
        get
        {
            Sprite cur = CurrentSprite;
            if (cur == null) return false;
            if (cur == UITheme.Get(key) || cur == applied) return false;
            return !UITheme.IsBuiltInSprite(cur);
        }
    }

    /// <summary>Marca la imagen actual como "puesta por el tema" (la usa el editor después de guardarla en el tema).</summary>
    public void MarkApplied()
    {
        applied = CurrentSprite;
    }

    public void Apply()
    {
        if (soloEsteObjeto) return;
        Sprite s = UITheme.Get(key);
        if (s == null) return;
        if (HasOwnSprite) return;   // tu imagen se respeta

        Image img = GetComponent<Image>();
        if (img != null)
        {
            if (img.sprite != s) img.sprite = s;
            if (img.type == Image.Type.Simple || img.type == Image.Type.Sliced)
                img.type = s.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (sr.sprite != s) sr.sprite = s;
            if (fitHeight > 0f && sr.drawMode == SpriteDrawMode.Simple && s.bounds.size.y > 0.0001f)
            {
                float k = fitHeight / s.bounds.size.y;
                transform.localScale = new Vector3(k, k, 1f);
            }
        }
        applied = s;
        FitAspect();
    }

    /// <summary>Si es un fondo con AspectRatioFitter, ajusta la proporción a la imagen nueva (para que no se deforme).</summary>
    public void FitAspect()
    {
        Sprite s = CurrentSprite;
        AspectRatioFitter arf = GetComponent<AspectRatioFitter>();
        if (arf != null && s != null && s.rect.height > 0f) arf.aspectRatio = s.rect.width / s.rect.height;
    }

    public static void RefreshAll()
    {
        foreach (ThemedImage t in all.ToArray())
            if (t != null) t.Apply();
    }
}
