using UnityEngine;

/// <summary>
/// Zona invisible dentro del hueco de un poste. Cuando la paloma la atraviesa,
/// cuenta como "poste pasado". Solo cuenta una vez.
/// </summary>
public class ScoreZone : MonoBehaviour
{
    private bool passed;

    public void Pass()
    {
        if (passed) return;
        passed = true;
        if (LevelManager.Instance != null) LevelManager.Instance.ObstaclePassed();
    }
}
