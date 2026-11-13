using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// "Torre de Pasteles → 8. Actualizar mi proyecto (retos nuevos y pantallas)".
/// Aplica los cambios nuevos a TUS escenas sin volver a crearlas (no se pierde nada de lo que moviste o cambiaste):
///  • Retos nuevos de los niveles 2, 5, 7, 8 y 10 en el LevelCatalog (se respetan imágenes, música y títulos propios).
///  • GameScene: agrega el medidor de precisión (nivel 2) y el cronómetro (nivel 8).
///  • Los fondos oscuros de Pausa, ¡Ganaste!/¡Perdiste! y la ventana del nivel cubren TODA la pantalla.
///  • El mapa llega hasta los bordes de la pantalla y su fondo es más ancho a los lados.
/// </summary>
public static class ProjectUpdater
{
    private static readonly Color Dim = new Color(0.35f, 0.18f, 0.28f, 0.6f);

    [MenuItem("Torre de Pasteles/8. Actualizar mi proyecto (retos nuevos y pantallas)", priority = 8)]
    public static void UpdateProject()
    {
        if (!EditorUtility.DisplayDialog("Actualizar proyecto",
                "Se van a aplicar los cambios nuevos a tus escenas y al LevelCatalog.\n\n" +
                "NO se borra nada de lo que cambiaste (posiciones, imágenes, textos, fuente).\n" +
                "Los valores de dificultad de los niveles sí se ponen como los nuevos retos.\n\n¿Continuar?", "Sí, actualizar", "Cancelar")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        StringBuilder log = new StringBuilder();
        string current = SceneManager.GetActiveScene().path;

        // 1) Retos nuevos
        CakeBuilder.BuildCatalog();
        log.AppendLine("• LevelCatalog: retos nuevos (2 medidor + brisa suave, 5 moscas cerca, 7 apagón a los 5 s, 8 cliente exigente con cronómetro, 10 tormenta).");

        // 2) GameScene
        if (Open("Assets/Scenes/GameScene.unity"))
        {
            GameHud hud = Object.FindFirstObjectByType<GameHud>(FindObjectsInactive.Include);
            if (hud != null)
            {
                Transform parent = hud.transform.Find("SafeArea");
                if (parent == null) parent = hud.transform;
                TMP_FontAsset font = hud.scoreText != null ? hud.scoreText.font : null;
                Sprite round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                if (hud.timingMeter == null)
                {
                    hud.timingMeter = TimingMeterUI.Create(parent, font, round);
                    log.AppendLine("• GameScene: medidor de precisión agregado (abajo a la derecha).");
                }
                if (hud.orderTimer == null)
                {
                    hud.orderTimer = OrderTimerUI.Create(parent, font, round, knob);
                    log.AppendLine("• GameScene: cronómetro agregado (debajo del viento).");
                }
                EditorUtility.SetDirty(hud);
                if (FixOverlay(hud.pausePanel)) log.AppendLine("• Pausa: el fondo oscuro cubre toda la pantalla.");
            }
            ResultScreen rs = Object.FindFirstObjectByType<ResultScreen>(FindObjectsInactive.Include);
            if (rs != null && FixOverlay(rs.gameObject)) log.AppendLine("• ¡Ganaste!/¡Perdiste!: el fondo oscuro cubre toda la pantalla.");
            ScreenEffects fx = Object.FindFirstObjectByType<ScreenEffects>(FindObjectsInactive.Include);
            if (fx != null && fx.flourRoot != null && fx.flourRoot.GetComponent<CubrirPantalla>() == null)
                fx.flourRoot.gameObject.AddComponent<CubrirPantalla>();
            PanelTools.SetupOpenScene();
            Save();
        }

        // 3) MainMenu
        if (Open("Assets/Scenes/MainMenu.unity"))
        {
            LevelInfoPopup popup = Object.FindFirstObjectByType<LevelInfoPopup>(FindObjectsInactive.Include);
            if (popup != null && FixOverlay(popup.gameObject)) log.AppendLine("• Ventana del nivel: el fondo oscuro cubre toda la pantalla.");

            LevelMapUI map = Object.FindFirstObjectByType<LevelMapUI>(FindObjectsInactive.Include);
            if (map != null)
            {
                if (FixOverlay(map.gameObject)) log.AppendLine("• Mapa: el color de fondo cubre toda la pantalla.");
                if (map.scroll != null && map.scroll.GetComponent<CubrirPantalla>() == null)
                {
                    map.scroll.gameObject.AddComponent<CubrirPantalla>().margen = 0f;
                    log.AppendLine("• Mapa: el scroll llega hasta los bordes de la pantalla.");
                }
                if (map.content != null)
                {
                    Transform bg = map.content.Find("FondoMapa");
                    if (bg != null)
                    {
                        RectTransform rt = (RectTransform)bg;
                        float extra = CakeBuilder.MapSideExtra;
                        rt.offsetMin = new Vector2(Mathf.Min(rt.offsetMin.x, -extra), rt.offsetMin.y);
                        rt.offsetMax = new Vector2(Mathf.Max(rt.offsetMax.x, extra), rt.offsetMax.y);
                        EditorUtility.SetDirty(rt);
                        log.AppendLine("• Mapa: la imagen de fondo es más ancha a los lados.");
                    }
                }
            }
            PanelTools.SetupOpenScene();
            Save();
        }

        if (!string.IsNullOrEmpty(current) && File.Exists(current)) EditorSceneManager.OpenScene(current);
        Debug.Log("Torre de Pasteles – Actualización:\n" + log);
        EditorUtility.DisplayDialog("Actualizar proyecto", "¡Listo!\n\n" + log, "OK");
    }

    private static bool Open(string path)
    {
        if (!File.Exists(path)) return false;
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        return true;
    }

    private static void Save()
    {
        Scene s = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
    }

    /// <summary>
    /// El fondo oscuro de una ventana pasa a un hijo "FondoPantalla" que cubre TODA la pantalla.
    /// La ventana (y sus tarjetas y botones) se quedan del mismo tamaño y en el mismo lugar.
    /// </summary>
    private static bool FixOverlay(GameObject root)
    {
        if (root == null) return false;
        Transform existing = root.transform.Find("FondoPantalla");
        if (existing != null)
        {
            if (existing.GetComponent<CubrirPantalla>() == null) existing.gameObject.AddComponent<CubrirPantalla>();
            return false;
        }

        Image old = root.GetComponent<Image>();
        GameObject go = new GameObject("FondoPantalla", typeof(RectTransform), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(root.transform, false);
        go.transform.SetAsFirstSibling();
        Image back = go.GetComponent<Image>();
        if (old != null)
        {
            back.sprite = old.sprite;
            back.type = old.type;
            back.color = old.color;
            back.material = old.material;
            old.enabled = false;   // la ventana ya no pinta el fondo (lo hace el hijo)
            EditorUtility.SetDirty(old);
        }
        else back.color = Dim;
        back.raycastTarget = true;
        go.AddComponent<CubrirPantalla>().Ajustar();
        EditorUtility.SetDirty(root);
        return true;
    }
}
