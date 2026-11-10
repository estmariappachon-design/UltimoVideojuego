using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Muestra el viento con pétalos / chispas de azúcar que cruzan la pantalla.
/// Más viento = más partículas y más rápidas.
/// </summary>
public class WindEffect : MonoBehaviour
{
    public Sprite[] particleSprites;
    public Color[] colors = { new Color(1f, 0.75f, 0.85f), new Color(1f, 1f, 1f), new Color(0.75f, 0.9f, 1f), new Color(1f, 0.95f, 0.7f) };
    public float halfWidth = 11f;
    public float minY = -5f, maxY = 5f;
    public float particlesPerSecondAtMax = 25f;
    public float maxWind = 2.5f;
    public int sortingOrder = 20;

    private class Particle { public Transform t; public float speedFactor; public float wobble; public float phase; }
    private readonly List<Particle> particles = new List<Particle>();
    private float wind;
    private float spawnAccumulator;

    public void SetWind(float value) => wind = value;

    private void Update()
    {
        float strength = maxWind > 0f ? Mathf.Abs(wind) / maxWind : 0f;
        if (particleSprites != null && particleSprites.Length > 0 && strength > 0.05f)
        {
            spawnAccumulator += strength * particlesPerSecondAtMax * Time.deltaTime;
            while (spawnAccumulator >= 1f)
            {
                spawnAccumulator -= 1f;
                Spawn();
            }
        }

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            Particle p = particles[i];
            if (p.t == null) { particles.RemoveAt(i); continue; }
            p.phase += Time.deltaTime * 3f;
            Vector3 pos = p.t.position;
            pos.x += wind * 3f * p.speedFactor * Time.deltaTime;
            pos.y += Mathf.Sin(p.phase) * p.wobble * Time.deltaTime - 0.3f * Time.deltaTime;
            p.t.position = pos;
            p.t.Rotate(0f, 0f, 120f * p.speedFactor * Time.deltaTime);
            if (Mathf.Abs(pos.x) > halfWidth + 1f || Mathf.Abs(wind) < 0.05f && p.speedFactor < 0.7f)
            {
                Destroy(p.t.gameObject);
                particles.RemoveAt(i);
            }
        }
    }

    private void Spawn()
    {
        GameObject go = new GameObject("Particula viento");
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = particleSprites[Random.Range(0, particleSprites.Length)];
        sr.color = colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;
        sr.sortingOrder = sortingOrder;
        float size = Random.Range(0.15f, 0.35f);
        if (sr.sprite != null) size /= sr.sprite.bounds.size.y;
        go.transform.localScale = Vector3.one * size;
        float startX = wind > 0 ? -halfWidth - 0.5f : halfWidth + 0.5f;
        go.transform.position = new Vector3(startX, Random.Range(minY, maxY), 0f);
        particles.Add(new Particle { t = go.transform, speedFactor = Random.Range(0.6f, 1.4f), wobble = Random.Range(0.3f, 1.2f), phase = Random.value * 6f });
    }
}
