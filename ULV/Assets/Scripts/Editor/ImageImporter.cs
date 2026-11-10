using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Configura AUTOMÁTICAMENTE como Sprite (2D y UI) cada imagen que pongas en
/// Assets/Sprites o en Assets/MisImagenes (la primera vez que se importa).
/// Según el nombre del archivo pone los bordes para que se estire bien:
///   capa_*        → bordes de 70 px a los lados (capas de pastel)
///   cinta*        → bordes de 130 px a los lados (listón de títulos)
///   boton*, panel*, input*, ticket*, burbuja*, barra* → bordes de 40 px (9-slice)
/// Para forzarlo de nuevo: Torre de Pasteles → 1. Configurar imágenes.
/// </summary>
public class SpriteImageImporter : AssetPostprocessor
{
    public static readonly string[] Roots = { "Assets/Sprites/", "Assets/MisImagenes/" };

    private static readonly string[] SlicedNames = { "boton", "panel", "input", "barra", "ticket", "burbuja_dialogo" };

    private static bool IsOurs(string path)
    {
        path = path.Replace('\\', '/');
        foreach (string r in Roots) if (path.StartsWith(r)) return true;
        return false;
    }

    private void OnPreprocessTexture()
    {
        if (!IsOurs(assetPath)) return;
        TextureImporter importer = (TextureImporter)assetImporter;
        // Solo la primera vez (si tú cambias algo a mano en el Inspector, se respeta)
        if (!importer.importSettingsMissing) return;
        Apply(importer, assetPath);
    }

    public static void Apply(TextureImporter importer, string path)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.spritePixelsPerUnit = 100;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;   // necesario para el modo mosaico (Tiled)
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);

        string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

        // Fondos grandes: se permite más resolución
        if (file.StartsWith("fondo")) importer.maxTextureSize = 4096;

        if (file.StartsWith("capa_")) { importer.spriteBorder = new Vector4(70, 0, 70, 0); return; }
        if (file.StartsWith("cinta")) { importer.spriteBorder = new Vector4(130, 0, 130, 0); return; }
        foreach (string n in SlicedNames)
        {
            if (file.StartsWith(n))
            {
                importer.spriteBorder = new Vector4(40, 40, 40, 40);
                return;
            }
        }
    }

    [MenuItem("Torre de Pasteles/1. Configurar imágenes", priority = 1)]
    public static void ConfigureAll()
    {
        int total = 0;
        foreach (string root in Roots)
        {
            string folder = root.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                Apply(importer, path);
                importer.SaveAndReimport();
                total++;
            }
        }
        Debug.Log($"Torre de Pasteles: {total} imágenes configuradas como Sprite.");
    }
}
