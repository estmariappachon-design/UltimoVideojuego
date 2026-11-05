using UnityEngine;

/// <summary>
/// Crea los 10 botones de nivel dentro de un contenedor con GridLayoutGroup.
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    public Transform buttonsContainer;     // objeto con GridLayoutGroup
    public LevelButtonUI levelButtonPrefab;

    private void OnEnable()
    {
        Build();
    }

    public void Build()
    {
        if (FirebaseManager.Instance == null || FirebaseManager.Instance.Progress == null) return;

        foreach (Transform child in buttonsContainer)
            Destroy(child.gameObject);

        PlayerProgress p = FirebaseManager.Instance.Progress;
        for (int i = 1; i <= PlayerProgress.TotalLevels; i++)
        {
            LevelButtonUI button = Instantiate(levelButtonPrefab, buttonsContainer);
            button.Setup(i, p.GetLevel(i), LevelSettings.Get(i).title, OnLevelClicked);
        }
    }

    private void OnLevelClicked(int levelNumber)
    {
        FirebaseManager.Instance.PlayLevel(levelNumber);
    }
}
