using System.Collections;
using UnityEngine;

/// <summary>
/// Explosión de confeti / brillitos (al entregar un pedido o al hacer una capa perfecta).
/// </summary>
public class Confetti : MonoBehaviour
{
    public Sprite[] sprites;
    public Color[] colors =
    {
        new Color(1f, 0.6f, 0.75f), new Color(0.6f, 0.85f, 1f), new Color(1f, 0.9f, 0.5f),
        new Color(0.7f, 1f, 0.75f), new Color(0.85f, 0.7f, 1f)
    };
    public int sortingOrder = 25;

    public void Burst(Vector3 position, int count, float force, float size = 0.25f)
    {
        if (sprites == null || sprites.Length == 0) return;
        for (int i = 0; i < count; i++) StartCoroutine(Piece(position, force, size));
    }

    private IEnumerator Piece(Vector3 position, float force, float size)
    {
        GameObject go = new GameObject("Confeti");
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[Random.Range(0, sprites.Length)];
        Color c = colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;
        sr.color = c;
        sr.sortingOrder = sortingOrder;
        float s = size * Random.Range(0.6f, 1.3f);
        if (sr.sprite != null) s /= sr.sprite.bounds.size.y;
        go.transform.localScale = Vector3.one * s;

        Vector2 v = Random.insideUnitCircle.normalized * force * Random.Range(0.5f, 1f);
        v.y = Mathf.Abs(v.y) + force * 0.4f;
        float spin = Random.Range(-400f, 400f);
        float life = Random.Range(0.9f, 1.4f);

        for (float t = 0f; t < life; t += Time.deltaTime)
        {
            v.y -= 9f * Time.deltaTime;
            v *= 1f - 1.5f * Time.deltaTime;
            go.transform.position += (Vector3)v * Time.deltaTime;
            go.transform.Rotate(0f, 0f, spin * Time.deltaTime);
            sr.color = new Color(c.r, c.g, c.b, 1f - Mathf.Clamp01((t - life * 0.6f) / (life * 0.4f)));
            yield return null;
        }
        Destroy(go);
    }
}
