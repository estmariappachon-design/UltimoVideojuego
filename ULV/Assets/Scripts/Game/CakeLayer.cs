using System.Collections;
using UnityEngine;

/// <summary>
/// Una capa de pastel. La imagen se alarga a lo ancho (modo Tiled con bordes) sin deformar
/// los bordes redondeados, y la carita se mantiene del mismo tamaño en el centro.
/// Prefab: Assets/Prefabs/CapaPastel
/// </summary>
public class CakeLayer : MonoBehaviour
{
    public SpriteRenderer body;
    public SpriteRenderer face;
    [Tooltip("Alto de cada capa (unidades)")]
    public float height = 0.62f;
    [Tooltip("Si la capa es más angosta que esto, se esconde la carita")]
    public float faceMinWidth = 0.8f;
    public Sprite scaredFace;
    [Tooltip("Color de la capa cuando una mosca la daña")]
    public Color dirtyColor = new Color(0.72f, 0.82f, 0.55f);

    public float Width { get; private set; }
    public int FlavorIndex { get; private set; }
    public bool IsBurnt { get; private set; }
    public bool IsDirty { get; private set; }

    private Sprite normalFace;

    private void Awake()
    {
        if (face != null) normalFace = face.sprite;
    }

    public void Setup(Sprite sprite, int flavorIndex, float width)
    {
        IsBurnt = false;
        IsDirty = false;
        if (body != null) body.color = Color.white;
        if (face != null && normalFace != null) face.sprite = normalFace;
        SetFlavor(sprite, flavorIndex);
        SetWidth(width);
    }

    public void SetFlavor(Sprite sprite, int flavorIndex)
    {
        FlavorIndex = flavorIndex;
        if (body != null && sprite != null)
        {
            body.sprite = sprite;
            SetWidth(Width > 0f ? Width : 1f);
        }
    }

    /// <summary>Convierte la capa en un pastel QUEMADO (no se debe colocar).</summary>
    public void SetBurnt(Sprite burntSprite, Sprite burntFace)
    {
        IsBurnt = true;
        FlavorIndex = -1;
        if (body != null)
        {
            if (burntSprite != null) body.sprite = burntSprite;
            else body.color = new Color(0.35f, 0.25f, 0.2f);
            SetWidth(Width);
        }
        if (face != null && burntFace != null) face.sprite = burntFace;
    }

    /// <summary>Una mosca dañó la capa.</summary>
    public void SetDirty()
    {
        IsDirty = true;
        if (body != null) body.color = dirtyColor;
        Scare();
    }

    /// <summary>El cliente ya no quiere este piso: parpadea en rojo hasta que lo toques para quitarlo.</summary>
    public bool IsWrong { get; private set; }
    [Tooltip("Color con el que parpadea un piso que el cliente ya no quiere")]
    public Color wrongColor = new Color(1f, 0.45f, 0.45f);
    private Coroutine wrongRoutine;

    public void SetWrong(bool wrong)
    {
        IsWrong = wrong;
        if (wrongRoutine != null) StopCoroutine(wrongRoutine);
        wrongRoutine = null;
        if (wrong)
        {
            Scare();
            wrongRoutine = StartCoroutine(WrongBlink());
        }
        else if (body != null) body.color = IsDirty ? dirtyColor : Color.white;
    }

    private IEnumerator WrongBlink()
    {
        Vector3 home = transform.localPosition;
        float t = 0f;
        while (IsWrong)
        {
            t += Time.deltaTime;
            float k = (Mathf.Sin(t * 10f) + 1f) / 2f;
            if (body != null) body.color = Color.Lerp(Color.white, wrongColor, k);
            transform.localPosition = home + new Vector3(Mathf.Sin(t * 40f) * 0.03f, 0f, 0f);
            yield return null;
        }
    }

    /// <summary>¿El punto (mundo) toca esta capa?</summary>
    public bool ContainsPoint(Vector3 worldPoint, float extra = 0.15f)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        return Mathf.Abs(local.x) <= Width / 2f + extra && Mathf.Abs(local.y) <= height / 2f + extra;
    }

    public void SetWidth(float width)
    {
        Width = width;
        if (body != null && body.sprite != null)
        {
            Vector2 s = body.sprite.bounds.size;
            float k = height / s.y;
            body.drawMode = SpriteDrawMode.Tiled;           // los bordes no se deforman y el centro se repite
            body.tileMode = SpriteTileMode.Continuous;
            body.transform.localScale = new Vector3(k, k, 1f);
            Vector4 b = body.sprite.border / body.sprite.pixelsPerUnit;
            float minW = b.x + b.z;
            body.size = new Vector2(Mathf.Max(width / k, minW), s.y);
        }
        if (face != null) face.enabled = width >= faceMinWidth;
    }

    public void SetAlpha(float a)
    {
        if (body != null) { Color c = body.color; c.a = a; body.color = c; }
        if (face != null) { Color c = face.color; c.a = a; face.color = c; }
    }

    /// <summary>Carita asustada (para capas que se caen).</summary>
    public void Scare()
    {
        if (face != null && scaredFace != null && !IsBurnt) face.sprite = scaredFace;
    }

    /// <summary>Pequeño "aplastón" al caer sobre el pastel.</summary>
    public void Squish()
    {
        StartCoroutine(SquishRoutine());
    }

    private IEnumerator SquishRoutine()
    {
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(t / 0.18f * Mathf.PI) * 0.18f;
            transform.localScale = new Vector3(1f + k * 0.5f, 1f - k, 1f);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    /// <summary>La capa sale volando y cae fuera de la pantalla.</summary>
    public void Fall(float pushX)
    {
        if (IsWrong) { IsWrong = false; if (wrongRoutine != null) StopCoroutine(wrongRoutine); }
        Scare();
        transform.SetParent(null, true);
        Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.SetVelocity(new Vector2(pushX, 2f));
        rb.angularVelocity = -pushX * 60f + Random.Range(-80f, 80f);
        Destroy(gameObject, 3f);
    }
}
