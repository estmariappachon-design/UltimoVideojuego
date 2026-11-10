using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ventana que sale al tocar un nivel del mapa: nombre, miniatura, reto, estrellas, mejor puntaje y ¡Jugar!
/// Está en MainMenu → Canvas → VentanaNivel.
/// </summary>
public class LevelInfoPopup : MonoBehaviour
{
    public RectTransform card;
    public TMP_Text numberText;
    public TMP_Text titleText;
    public TMP_Text challengeText;
    public TMP_Text bestText;
    public Image thumbnail;
    public Image[] stars;
    public Button playButton;
    public Button closeButton;

    [Header("Textos")]
    public string levelFormat = "Nivel {0}";
    public string bestFormat = "Mejor puntaje: {0}";
    public string notPlayedYet = "¡Todavía no lo has superado!";

    private Action onPlay;
    private bool wired;

    private void Wire()
    {
        if (wired) return;
        wired = true;
        playButton.onClick.AddListener(() =>
        {
            Action a = onPlay;
            Hide();
            a?.Invoke();
        });
        closeButton.onClick.AddListener(Hide);
    }

    public void Show(int levelNumber, LevelData data, LevelCatalog.Entry entry, Action play)
    {
        gameObject.SetActive(true);
        Wire();
        onPlay = play;

        if (numberText != null) numberText.text = string.Format(levelFormat, levelNumber);
        if (titleText != null) titleText.text = entry != null ? entry.title : "";
        if (challengeText != null) challengeText.text = entry != null ? entry.challengeDescription : "";
        if (thumbnail != null)
        {
            thumbnail.sprite = entry != null ? entry.thumbnail : null;
            thumbnail.enabled = thumbnail.sprite != null;
        }
        if (bestText != null)
            bestText.text = data.state == LevelState.Completed ? string.Format(bestFormat, data.bestScore) : notPlayedYet;

        Sprite full = UITheme.Get(ThemeKey.Estrella);
        Sprite empty = UITheme.Get(ThemeKey.EstrellaVacia);
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            bool on = i < data.stars;
            if (full != null && empty != null) stars[i].sprite = on ? full : empty;
            else stars[i].color = on ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }
        StartCoroutine(PopIn());
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private IEnumerator PopIn()
    {
        if (card == null) yield break;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            float k = t / 0.3f;
            float s = k < 0.7f ? Mathf.Lerp(0.6f, 1.06f, k / 0.7f) : Mathf.Lerp(1.06f, 1f, (k - 0.7f) / 0.3f);
            card.localScale = Vector3.one * s;
            yield return null;
        }
        card.localScale = Vector3.one;
    }
}
