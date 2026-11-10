using System.Collections;
using UnityEngine;

/// <summary>
/// El cliente (animalito) que pide el pastel. Cambia de cara según cómo vaya el pedido
/// y entra/sale de la escena con una pequeña animación. Muévelo en la escena donde quieras.
/// </summary>
public class CustomerView : MonoBehaviour
{
    public enum Mood { Normal, Happy, Angry }

    public SpriteRenderer sprite;
    public float bobAmount = 0.06f;
    public float bobSpeed = 2.5f;
    [Tooltip("Desde dónde entra (negativo = desde la izquierda)")]
    public float enterDistance = -6f;

    private LevelCatalog.Customer current;
    private Vector3 home;
    private float t;
    private Coroutine moveRoutine;

    public string CurrentName => current != null ? current.name : "";

    private float baseScale = 1f;

    private void Awake()
    {
        home = transform.position;
        if (sprite != null) baseScale = sprite.transform.localScale.x;
    }

    private void Update()
    {
        if (sprite == null) return;
        t += Time.deltaTime * bobSpeed;
        sprite.transform.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(t)) * bobAmount, 0f);
    }

    /// <summary>Llega un cliente nuevo (entra desde la derecha).</summary>
    public void Show(LevelCatalog.Customer customer)
    {
        current = customer;
        SetMood(Mood.Normal);
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(Move(home + Vector3.right * enterDistance, home, 0.45f));
    }

    /// <summary>El cliente se va (sale por la derecha).</summary>
    public void Leave()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(Move(transform.position, home + Vector3.right * enterDistance, 0.45f));
    }

    /// <summary>Saltito de alegría.</summary>
    public void Hop()
    {
        if (sprite != null) StartCoroutine(HopRoutine());
    }

    private IEnumerator HopRoutine()
    {
        Transform t = sprite.transform;
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.35f)
        {
            t.localScale = new Vector3(1f + Mathf.Sin(k * Mathf.PI) * 0.06f, 1f - Mathf.Sin(k * Mathf.PI * 2f) * 0.05f, 1f) * baseScale;
            yield return null;
        }
        t.localScale = Vector3.one * baseScale;
    }

    public void SetMood(Mood mood)
    {
        if (current == null || sprite == null) return;
        Sprite s = mood == Mood.Happy ? current.happy : mood == Mood.Angry ? current.angry : current.normal;
        sprite.sprite = s != null ? s : current.normal;
    }

    private IEnumerator Move(Vector3 from, Vector3 to, float duration)
    {
        float k = 0f;
        while (k < 1f)
        {
            k += Time.deltaTime / duration;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(k), 3f);
            transform.position = Vector3.LerpUnclamped(from, to, e);
            yield return null;
        }
        transform.position = to;
    }
}
