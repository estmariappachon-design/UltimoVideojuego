using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Panel de ranking: lee "ranking" de Firebase y crea una fila por jugador.
/// </summary>
public class RankingUI : MonoBehaviour
{
    public Transform rowsContainer;    // objeto con VerticalLayoutGroup (dentro de un ScrollView)
    public RankingRowUI rowPrefab;
    public TMP_Text statusText;
    public int maxPlayers = 20;

    private void OnEnable()
    {
        Load();
    }

    private async void Load()
    {
        foreach (Transform child in rowsContainer)
            Destroy(child.gameObject);

        if (FirebaseManager.Instance == null) return;

        statusText.text = "Cargando ranking...";
        List<RankingEntry> list;
        try
        {
            list = await FirebaseManager.Instance.GetRankingAsync(maxPlayers);
        }
        catch (System.Exception e)
        {
            statusText.text = "No se pudo cargar el ranking.";
            Debug.LogError(e);
            return;
        }

        if (this == null || !gameObject.activeInHierarchy) return; // el panel se cerró mientras cargaba

        statusText.text = list.Count == 0 ? "Aún no hay jugadores." : "";
        string myUid = FirebaseManager.Instance.CurrentUser != null ? FirebaseManager.Instance.CurrentUser.UserId : "";

        for (int i = 0; i < list.Count; i++)
        {
            RankingRowUI row = Instantiate(rowPrefab, rowsContainer);
            row.Setup(i + 1, list[i], list[i].uid == myUid);
        }
    }
}
