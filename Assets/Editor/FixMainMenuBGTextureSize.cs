using UnityEditor;
using UnityEngine;

public static class FixMainMenuBGTextureSize
{
    const string Path = "Assets/Sprites/Light/MainMenuScreen.png";

    [MenuItem("InboxZero/Fix MainMenu BG Texture Size")]
    static void Fix()
    {
        var importer = AssetImporter.GetAtPath(Path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[FixMainMenuBGTextureSize] TextureImporter not found at " + Path);
            return;
        }

        importer.maxTextureSize = 8192;

        // Also set per-platform overrides to 8192
        foreach (var platform in new[] { "DefaultTexturePlatform", "Standalone", "WebGL" })
        {
            var s = importer.GetPlatformTextureSettings(platform);
            s.maxTextureSize = 8192;
            s.overridden = true;
            importer.SetPlatformTextureSettings(s);
        }

        importer.SaveAndReimport();
        Debug.Log("[FixMainMenuBGTextureSize] Done — maxTextureSize set to 8192 and reimported.");
    }
}
