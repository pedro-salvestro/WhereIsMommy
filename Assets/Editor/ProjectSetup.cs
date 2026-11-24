using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Importa as imagens como sprites e gera as cenas Menu e Fase_Shopping.
public class SpriteImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Art/")) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Sprite;
        t.spriteImportMode = SpriteImportMode.Single;
        t.mipmapEnabled = false;
        t.alphaIsTransparency = true;
        t.maxTextureSize = 4096;

        bool character = assetPath.Contains("/Crianca/") || assetPath.Contains("/Mae/") || assetPath.Contains("/Policial/");
        t.spritePixelsPerUnit = character ? 55 : 100;
        t.filterMode = character ? FilterMode.Point : FilterMode.Bilinear;
        t.textureCompression = character ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
        // os personagens precisam ser legiveis para gerar as "mamaes parecidas" com outra roupa
        t.isReadable = character;
    }

    // mudar este numero faz o Unity reimportar as imagens com as regras acima
    public override uint GetVersion() => 2;
}

[InitializeOnLoad]
public static class ProjectSetup
{
    const string MenuPath = "Assets/Scenes/Menu.unity";
    const string LevelPath = "Assets/Scenes/Fase_Shopping.unity";

    static ProjectSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(MenuPath) || !File.Exists(LevelPath)) GenerateScenes();
        };
    }

    [MenuItem("Mamae/Apagar Recordes")]
    public static void ClearRecords()
    {
        foreach (GameMode m in System.Enum.GetValues(typeof(GameMode)))
        {
            PlayerPrefs.DeleteKey("recorde_" + m);
            foreach (Dificuldade d in System.Enum.GetValues(typeof(Dificuldade))) PlayerPrefs.DeleteKey(GameSettings.RecordKey(m, d));
        }
        PlayerPrefs.Save();
        Debug.Log("Recordes apagados.");
    }

    [MenuItem("Mamae/Gerar Cenas")]
    public static void GenerateScenes()
    {
        Directory.CreateDirectory("Assets/Scenes");
        CreateScene(MenuPath, "Menu", typeof(MenuController));
        CreateScene(LevelPath, "Fase", typeof(LevelController));
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuPath, true),
            new EditorBuildSettingsScene(LevelPath, true),
        };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(MenuPath);
        Debug.Log("Cenas geradas. Abra Menu e aperte Play.");
    }

    static void CreateScene(string path, string name, System.Type controller)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var go = new GameObject(name + "Controller");
        go.AddComponent(controller);
        if (controller == typeof(MenuController))
        {
            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            var c = cam.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = Color.black;
            cam.AddComponent<AudioListener>();
        }
        EditorSceneManager.SaveScene(scene, path);
    }
}
