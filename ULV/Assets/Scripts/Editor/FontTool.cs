using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// TIPOGRAFÍA: pon tu fuente (.ttf u .otf) en Assets/Fuentes y usa
/// "Torre de Pasteles → 5. Aplicar tipografía". Se crea la fuente para TextMeshPro y se aplica
/// a todos los textos de las escenas y prefabs, y como fuente por defecto de los textos nuevos.
/// </summary>
public static class FontTool
{
    private const string Folder = "Assets/Fuentes";

    [MenuItem("Torre de Pasteles/5. Aplicar tipografía (Assets/Fuentes)", priority = 5)]
    public static void ApplyMenu()
    {
        TMP_FontAsset fa = GetOrCreateFontAsset();
        if (fa == null)
        {
            EditorUtility.DisplayDialog("Tipografía",
                "No encontré ninguna fuente.\n\nPon un archivo .ttf u .otf dentro de la carpeta Assets/Fuentes " +
                "(por ejemplo Fredoka, Baloo 2 o Sniglet de fonts.google.com) y vuelve a intentarlo.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        int count = ApplyEverywhere(fa);
        EditorUtility.DisplayDialog("Tipografía", $"Listo: la fuente '{fa.name}' se aplicó a {count} textos.", "OK");
    }

    /// <summary>Si hay una fuente en Assets/Fuentes la aplica sin preguntar (lo usa CREAR TODO).</summary>
    public static void ApplyIfAvailable()
    {
        TMP_FontAsset fa = GetOrCreateFontAsset();
        if (fa != null) ApplyEverywhere(fa);
    }

    public static TMP_FontAsset GetOrCreateFontAsset()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Fuentes");

        string[] guids = AssetDatabase.FindAssets("t:Font", new[] { Folder });
        if (guids.Length == 0) return null;
        string fontPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null) return null;

        string assetPath = Folder + "/" + Path.GetFileNameWithoutExtension(fontPath) + " SDF.asset";
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        // Fuente dinámica: acepta cualquier letra (ñ, tildes, ¡¿)
        TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(font);
        if (fa == null) return null;
        fa.name = font.name + " SDF";
        AssetDatabase.CreateAsset(fa, assetPath);
        if (fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null)
        {
            fa.atlasTextures[0].name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        }
        if (fa.material != null)
        {
            fa.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
        }
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(assetPath);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
    }

    public static int ApplyEverywhere(TMP_FontAsset fa)
    {
        int count = 0;

        // Prefabs
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int n = SetFont(root, fa);
            if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            count += n;
        }

        // Escenas de Build Settings
        string current = EditorSceneManager.GetActiveScene().path;
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (!File.Exists(s.path)) continue;
            var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            int n = 0;
            foreach (GameObject go in scene.GetRootGameObjects()) n += SetFont(go, fa);
            if (n > 0) EditorSceneManager.SaveScene(scene);
            count += n;
        }
        if (!string.IsNullOrEmpty(current) && File.Exists(current)) EditorSceneManager.OpenScene(current, OpenSceneMode.Single);

        // Fuente por defecto de TextMeshPro (para los textos nuevos)
        if (TMP_Settings.instance != null)
        {
            SerializedObject so = new SerializedObject(TMP_Settings.instance);
            SerializedProperty prop = so.FindProperty("m_defaultFontAsset");
            if (prop != null)
            {
                prop.objectReferenceValue = fa;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(TMP_Settings.instance);
            }
        }
        AssetDatabase.SaveAssets();
        return count;
    }

    private static int SetFont(GameObject root, TMP_FontAsset fa)
    {
        int n = 0;
        foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.font == fa) continue;
            t.font = fa;
            EditorUtility.SetDirty(t);
            n++;
        }
        return n;
    }
}
