using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Un nivel en el mapa: galleta/macaron con el número, estrellas ganadas y candado.
/// Prefab: Assets/Prefabs/UI/NodoNivel (ábrelo para cambiar su diseño).
/// Las imágenes salen de TemaVisual (Nodo Nivel / Nodo Bloqueado / Nodo Completado).
/// </summary>
public class LevelNodeUI : MonoBehaviour
{
    public Button button;
    public Image baseImage;
    public TMP_Text numberText;
    public TMP_Text titleText;
    public Image[] stars;
    public GameObject lockIcon;
    [Tooltip("Aro que late alrededor del nivel actual")]
    public RectTransform glow;

    [Header("Colores")]
    public Color lockedTint = new Color(0.8f, 0.8f, 0.85f);
    public Color lockedTextColor = new Color(0.55f, 0.5f, 0.6f);

    private bool isCurrent;

    public void Setup(int levelNumber, LevelData data, LevelCatalog.Entry entry, bool current, Action<int> onClick)
    {
        bool locked = data.state == LevelState.Locked;
        bool completed = data.state == LevelState.Completed;
        isCurrent = current && !locked;

        Sprite s = UITheme.Get(locked ? ThemeKey.NodoBloqueado : completed ? ThemeKey.NodoCompletado : ThemeKey.NodoNivel);
        if (baseImage != null)
        {
            if (s != null) baseImage.sprite = s;
            baseImage.color = locked ? lockedTint : Color.white;
        }
        if (numberText != null)
        {
            numberText.text = levelNumber.ToString();
            numberText.gameObject.SetActive(!locked);
        }
        if (titleText != null)
        {
            titleText.text = entry != null ? entry.title : "";
            titleText.color = locked ? lockedTextColor : titleText.color;
        }
        if (lockIcon != null) lockIcon.SetActive(locked);

        Sprite full = UITheme.Get(ThemeKey.Estrella);
        Sprite empty = UITheme.Get(ThemeKey.EstrellaVacia);
        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                stars[i].gameObject.SetActive(completed);
                bool on = i < data.stars;
                if (full != null && empty != null) stars[i].sprite = on ? full : empty;
                else stars[i].color = on ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }
        if (glow != null) glow.gameObject.SetActive(isCurrent);

        button.interactable = !locked;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(levelNumber));
    }

    private void Update()
    {
        if (!isCurrent) return;
        float k = Mathf.Sin(Time.time * 4f);
        transform.localScale = Vector3.one * (1f + k * 0.04f);
        if (glow != null)
        {
            glow.Rotate(0f, 0f, -40f * Time.deltaTime);
            glow.localScale = Vector3.one * (1.05f + k * 0.05f);
        }
    }
}
