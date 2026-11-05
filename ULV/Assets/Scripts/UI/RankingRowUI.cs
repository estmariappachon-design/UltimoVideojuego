using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Una fila del ranking: posición, usuario, puntaje y nivel.
/// </summary>
public class RankingRowUI : MonoBehaviour
{
    public TMP_Text positionText;
    public TMP_Text usernameText;
    public TMP_Text scoreText;
    public TMP_Text levelText;
    public Image background;
    public Color normalColor = new Color(1f, 1f, 1f, 0.08f);
    public Color myRowColor = new Color(1f, 0.85f, 0.3f, 0.45f);

    public void Setup(int position, RankingEntry entry, bool isMe)
    {
        positionText.text = position.ToString();
        usernameText.text = entry.username + (isMe ? " (tú)" : "");
        scoreText.text = entry.totalScore.ToString();
        levelText.text = "Nv " + entry.maxUnlockedLevel;
        if (background != null) background.color = isMe ? myRowColor : normalColor;
    }
}
