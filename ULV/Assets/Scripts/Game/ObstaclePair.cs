using UnityEngine;

/// <summary>
/// Poste doble (arriba y abajo) con un hueco en medio por donde pasa la paloma.
/// La altura del hueco, su tamaño y si se mueve los decide el generador procedural.
///
/// Prefab:
///   ObstaclePair (este script + Scroller)
///     ├─ Top     (SpriteRenderer + BoxCollider2D isTrigger, tag "Obstacle")
///     ├─ Bottom  (SpriteRenderer + BoxCollider2D isTrigger, tag "Obstacle")
///     └─ ScoreZone (BoxCollider2D isTrigger + ScoreZone.cs, SIN tag)
/// </summary>
[RequireComponent(typeof(Scroller))]
public class ObstaclePair : MonoBehaviour
{
    public Transform top;
    public Transform bottom;
    public Transform scoreZone;

    private float baseY;
    private float amplitude;
    private float frequency;
    private float phase;

    /// <param name="gapCenterY">Altura del centro del hueco</param>
    /// <param name="gapSize">Alto del hueco</param>
    /// <param name="moveAmplitude">0 = quieto; >0 = sube y baja</param>
    public void Setup(float gapCenterY, float gapSize, float moveAmplitude, float moveFrequency, float startPhase)
    {
        baseY = gapCenterY;
        amplitude = moveAmplitude;
        frequency = moveFrequency;
        phase = startPhase;

        transform.position = new Vector3(transform.position.x, gapCenterY, transform.position.z);

        float topHeight = PieceHeight(top);
        float bottomHeight = PieceHeight(bottom);

        top.localPosition = new Vector3(0, gapSize / 2f + topHeight / 2f, 0);
        bottom.localPosition = new Vector3(0, -gapSize / 2f - bottomHeight / 2f, 0);

        // La zona de puntaje ocupa exactamente el hueco
        scoreZone.localPosition = Vector3.zero;
        scoreZone.localScale = new Vector3(0.3f, gapSize, 1f);
    }

    private void Update()
    {
        if (amplitude <= 0f) return;
        if (LevelManager.Instance == null || LevelManager.Instance.CurrentSpeed <= 0f) return;

        phase += Time.deltaTime * frequency;
        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin(phase) * amplitude;
        transform.position = p;
    }

    private static float PieceHeight(Transform piece)
    {
        SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
        return sr != null ? sr.bounds.size.y : piece.localScale.y;
    }
}
