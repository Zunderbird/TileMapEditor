using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TileMapEditor
{
    public static class ResourceManager
    {
        private const string ASSETS_FOLDER = "Assets";
        private const string TILEMAPS_FOLDER = "Tilemaps";

        public static void CreateDirectoryIfNotExists(string directory)
        {
            if (System.IO.Directory.Exists(directory)) return;

            System.IO.Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
            Debug.Log("Created directory: " + directory);

        }

        private static IEnumerable<string> LoadFileNamesFromAssets(string folderName, string filefFormat)
        {
            CreateDirectoryIfNotExists(Application.dataPath + '/' + folderName);

            var files = System.IO.Directory.GetFiles(Application.dataPath + '/' + folderName + '/', "*." + filefFormat);
            return files;
        }

        public static string[] GetTileMapNames()
        {
            var files = LoadFileNamesFromAssets(TILEMAPS_FOLDER, "png");
            return files.Select(file => file.Replace(Application.dataPath + '/' + TILEMAPS_FOLDER + '/', "")).ToArray();
        }

        public static Sprite[] LoadSpritesFromTileMap(string tileMapName)
        {
            return AssetDatabase.LoadAllAssetsAtPath(ASSETS_FOLDER + '/' + TILEMAPS_FOLDER + '/' + tileMapName)
                .Select(sprite => sprite as Sprite)
                .Where(sprite => sprite != null).ToArray();
        }

    }
}
