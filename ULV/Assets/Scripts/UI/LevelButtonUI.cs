using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Botón de un nivel. Muestra número, nombre, mejor puntaje y si está bloqueado o completado.
/// </summary>
public class LevelButtonUI : MonoBehaviour
{
    public Button button;
    public TMP_Text numberText;
    public TMP_Text titleText;
    public TMP_Text bestScoreText;
    public GameObject lockIcon;        // candado (visible si está bloqueado)
    public GameObject completedIcon;   // estrella/check (visible si está completado)
    public Image background;

    public Color lockedColor = new Color(0.45f, 0.45f, 0.45f);
    public Color unlockedColor = new Color(1f, 0.85f, 0.35f);
    public Color completedColor = new Color(0.45f, 0.85f, 0.45f);

    public void Setup(int levelNumber, LevelData data, string title, Action<int> onClick)
    {
        bool locked = data.state == LevelState.Locked;
        bool completed = data.state == LevelState.Completed;

        numberText.text = levelNumber.ToString();
        if (titleText != null) titleText.text = locked ? "Bloqueado" : title;
        if (bestScoreText != null) bestScoreText.text = completed ? "Mejor: " + data.bestScore : "";
        if (lockIcon != null) lockIcon.SetActive(locked);
        if (completedIcon != null) completedIcon.SetActive(completed);
        if (background != null)
            background.color = locked ? lockedColor : (completed ? completedColor : unlockedColor);

        button.interactable = !locked;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(levelNumber));
    }
}
