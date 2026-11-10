using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MOSCA (nivel 5 y 10): vuela hacia una capa del pastel, se posa y si nadie la espanta
/// la daña (se pierde un corazón). Tócala para espantarla.
/// Prefab: Assets/Prefabs/Mosca (las imágenes salen de TemaVisual → Mosca).
/// </summary>
public class Fly : MonoBehaviour
{
    public static readonly List<Fly> All = new List<Fly>();

    public SpriteRenderer sprite;
    public Sprite[] frames;
    public float frameRate = 16f;
    [Tooltip("Segundos que se queda posada antes de dañar el pastel")]
    public float landedTime = 2.2f;
    [Tooltip("Qué tan cerca hay que tocar para espantarla (unidades)")]
    public float tapRadius = 0.8f;

    private enum S { Flying, Landed, Leaving }
    private S state;
    private Transform target;
    private Vector3 offset;
    private Vector3 basePos;
    private Vector3 leaveVelocity;
    private float speed;
    private float timer, phase, animTimer;
    private int frame;
    private Action<Fly, Transform> onDamage;

    public bool IsActive => state != S.Leaving;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    private void Start()
    {
        // Las imágenes del TemaVisual (casilla Mosca) tienen prioridad, así cambias la mosca sin tocar el prefab
        if (UITheme.Instance != null && UITheme.Instance.mosca != null && UITheme.Instance.mosca.Length > 0 && UITheme.Instance.mosca[0] != null)
            frames = UITheme.Instance.mosca;
        if (frames != null && frames.Length > 0 && sprite != null && frames[0] != null) sprite.sprite = frames[0];
        StackGame.OnOrderEnded += Leave;
    }

    private void OnDestroy()
    {
        StackGame.OnOrderEnded -= Leave;
    }

    public void Setup(Transform targetLayer, Vector3 worldOffset, float flySpeed, Action<Fly, Transform> damageCallback)
    {
        target = targetLayer;
        offset = worldOffset;
        speed = flySpeed;
        onDamage = damageCallback;
        basePos = transform.position;
        phase = UnityEngine.Random.value * 10f;
        state = S.Flying;
    }

    public bool ContainsPoint(Vector3 worldPoint)
    {
        Vector2 d = worldPoint - transform.position;
        return d.sqrMagnitude <= tapRadius * tapRadius;
    }

    /// <summary>Espantar la mosca. Devuelve true si estaba activa.</summary>
    public bool Shoo()
    {
        if (state == S.Leaving) return false;
        Leave();
        return true;
    }

    public void Leave()
    {
        if (this == null || state == S.Leaving) return;
        state = S.Leaving;
        if (sprite != null) sprite.color = Color.white;
        float side = transform.position.x >= 0f ? 1f : -1f;
        leaveVelocity = new Vector3(side * UnityEngine.Random.Range(4f, 7f), UnityEngine.Random.Range(4f, 7f), 0f);
        Destroy(gameObject, 2.5f);
    }

    private void Update()
    {
        Animate();
        phase += Time.deltaTime;

        switch (state)
        {
            case S.Flying:
                if (target == null || !target.gameObject.activeInHierarchy) { Leave(); return; }
                Vector3 goal = target.position + offset;
                basePos = Vector3.MoveTowards(basePos, goal, speed * Time.deltaTime);
                float k = Mathf.Clamp01(Vector3.Distance(basePos, goal) / 2f);   // menos zigzag al llegar
                transform.position = basePos + new Vector3(Mathf.Sin(phase * 7f) * 0.3f * k, Mathf.Cos(phase * 9f) * 0.25f * k, 0f);
                Face(goal.x - transform.position.x);
                if (Vector3.Distance(basePos, goal) < 0.05f)
                {
                    state = S.Landed;
                    timer = landedTime;
                }
                break;

            case S.Landed:
                if (target == null) { Leave(); return; }
                transform.position = target.position + offset + new Vector3(Mathf.Sin(phase * 30f) * 0.03f, 0f, 0f);
                timer -= Time.deltaTime;
                if (sprite != null)
                    sprite.color = timer < 0.9f && Mathf.FloorToInt(timer * 10f) % 2 == 0 ? new Color(1f, 0.55f, 0.55f) : Color.white;
                if (timer <= 0f)
                {
                    onDamage?.Invoke(this, target);
                    Leave();
                }
                break;

            case S.Leaving:
                transform.position += leaveVelocity * Time.deltaTime;
                break;
        }
    }

    private void Face(float dx)
    {
        if (sprite != null && Mathf.Abs(dx) > 0.01f) sprite.flipX = dx < 0f;
    }

    private void Animate()
    {
        if (sprite == null || frames == null || frames.Length == 0) return;
        animTimer += Time.deltaTime;
        if (animTimer >= 1f / frameRate)
        {
            animTimer = 0f;
            frame = (frame + 1) % frames.Length;
            sprite.sprite = frames[frame];
        }
    }
}
