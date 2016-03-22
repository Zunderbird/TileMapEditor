using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace TileMapEditor
{
    public class TileMapEditor : EditorWindow
    {
        private int _labelsWidth;
        private int _fieldsWidth;

        private int _currentTileMapIndex;
        private Vector2 _tilesPopupScrollPos;

        private Sprite[] _sprites;
        private Sprite _activeSprite;

        private bool _isGrouppingByMask;

        // Foldouts
        private bool _isGridPreferencesVisible = true;
        private bool _isResourcePreferencesVisible = true;
        private bool _isDrawingPreferencesVisible = true;

        // Icon Sizes
        private IconsSize _currentIconsSize;
        
        private readonly Dictionary<IconsSize, int> _spriteSizes = new Dictionary<IconsSize, int>
    {
        { IconsSize.SmallIcons, 32 },
        { IconsSize.MediumIcons, 64 },
        { IconsSize.LargeIcons, 128 },
        { IconsSize.ExtraLargeIcons, 256}
    };

        [MenuItem("Tools/TileMapEditor")]
        private static void TileMapEditorMain()
        {
            GetWindow(typeof(TileMapEditor));
        }

        void OnEnable()
        {
            DrawingModeEventHandler.IsEnabled = true;
            _currentIconsSize = IconsSize.MediumIcons;
            minSize = new Vector2(250, 260);
        }

        void OnDestroy()
        {
            DrawingModeEventHandler.IsEnabled = false;
        }

        void OnGUI()
        {
            _labelsWidth = (int)(position.width*0.4f);
            _fieldsWidth = (int)(position.width * 0.6f - 10);

            GridPreferencesField();

            ResourcePreferences();

            DrawingPreferences();

            DisplayTilesScrollView();

            SceneView.RepaintAll();
        }

        private void GridPreferencesField()
        {
            _isGridPreferencesVisible = EditorGUILayout.Foldout(_isGridPreferencesVisible, "Grid Preferences");
            if (!_isGridPreferencesVisible) return;

            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("   Enable grid", GUILayout.Width(_labelsWidth));
            SceneGUIRenderer.IsGridEnabled = EditorGUILayout.Toggle(SceneGUIRenderer.IsGridEnabled, GUILayout.Width(16));

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("   Cell size", GUILayout.Width(_labelsWidth));
            SceneGUIRenderer.CellSize = EditorGUILayout.Vector2Field("", SceneGUIRenderer.CellSize, GUILayout.Width(_fieldsWidth), GUILayout.Height(10));

            if (SceneGUIRenderer.CellSize.x < SceneGUIRenderer.CELL_SIZE_MIN)
            {
                Debug.Log("Incorrectly stated cell width!\nCan't set size x less than " + SceneGUIRenderer.CELL_SIZE_MIN);
                SceneGUIRenderer.CellSize.x = SceneGUIRenderer.CELL_SIZE_MIN;
            }
            if (SceneGUIRenderer.CellSize.y < SceneGUIRenderer.CELL_SIZE_MIN)
            {
                Debug.Log("Incorrectly stated cell height!\nCan't set size y less than " + SceneGUIRenderer.CELL_SIZE_MIN);
                SceneGUIRenderer.CellSize.y = SceneGUIRenderer.CELL_SIZE_MIN;
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("   Grid color", GUILayout.Width(_labelsWidth));
            SceneGUIRenderer.GridColor = EditorGUILayout.ColorField(SceneGUIRenderer.GridColor, GUILayout.Width(_fieldsWidth));

            GUILayout.EndHorizontal();
        }

        private void ResourcePreferences()
        {
            _isResourcePreferencesVisible = EditorGUILayout.Foldout(_isResourcePreferencesVisible, "Resource Preferences");

            var tileMaps = ResourceManager.GetTileMapNames();

            if (tileMaps.Length > _currentTileMapIndex)
                _sprites = ResourceManager.LoadSpritesFromTileMap(tileMaps[_currentTileMapIndex]);

            if (_isResourcePreferencesVisible)
            {
                TileMapPopup(tileMaps);
            }
        }

        private void TileMapPopup(string[] tileMaps)
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("   Tile Map", GUILayout.Width(_labelsWidth));

            _currentTileMapIndex = EditorGUILayout.Popup(_currentTileMapIndex, tileMaps, GUILayout.Width(_fieldsWidth));

            if (tileMaps.Length > _currentTileMapIndex)
                DrawingModeEventHandler.CurrentTileMapName = tileMaps[_currentTileMapIndex].Split('.').First();

            GUILayout.EndHorizontal();
        }

        private void DrawingPreferences()
        {
            _isDrawingPreferencesVisible = EditorGUILayout.Foldout(_isDrawingPreferencesVisible, "Drawing Preferences");
            if (!_isDrawingPreferencesVisible) return;

            ParentObjectField();

            DrawingOptionField();

            GroupByMaskField();

            TileIconsSizeField();

        }

        private void ParentObjectField()
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("   Parent Object", GUILayout.Width(_labelsWidth));
            DrawingModeEventHandler.ParentObject = (GameObject)EditorGUILayout.ObjectField(DrawingModeEventHandler.ParentObject, typeof(GameObject), true, GUILayout.Width(_fieldsWidth));

            GUILayout.EndHorizontal();
        }

        private void DrawingOptionField()
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("   Drawing Mode", GUILayout.Width(_labelsWidth));
            DrawingModeEventHandler.SelectedDrawingOption = (DrawingOption)EditorGUILayout.EnumPopup(DrawingModeEventHandler.SelectedDrawingOption, GUILayout.Width(_fieldsWidth));

            GUILayout.EndHorizontal();
        }

        private void GroupByMaskField()
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("   GroupByName", GUILayout.Width(_labelsWidth));
            _isGrouppingByMask = EditorGUILayout.Toggle(_isGrouppingByMask, GUILayout.Width(16));

            GUILayout.EndHorizontal();
        }

        private void TileIconsSizeField()
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("   Tiles", GUILayout.Width(_labelsWidth));
            _currentIconsSize = (IconsSize)EditorGUILayout.EnumPopup(_currentIconsSize, GUILayout.Width(_fieldsWidth));

            GUILayout.EndHorizontal();
        }

        private void DisplayTilesScrollView()
        {
            _tilesPopupScrollPos = EditorGUILayout.BeginScrollView(_tilesPopupScrollPos);
            GUILayout.BeginHorizontal();

            if (_sprites != null) DisplaySprites();

            GUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private void DisplaySprites()
        {
            float currentWidth = 0;

            var grouped = (_isGrouppingByMask)? 
                _sprites.GroupBy(tile => new string(tile.name.Where(char.IsLetter).ToArray()))
                : _sprites.GroupBy(tile => tile.name);

            foreach (var group in grouped)
            {
                currentWidth += _spriteSizes[_currentIconsSize];

                if (currentWidth > position.width)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    currentWidth = _spriteSizes[_currentIconsSize];
                }

                DisplaySprite(group.First());

                if (_activeSprite == group.First())
                {
                    DrawingModeEventHandler.ActiveGroup = group;
                }
            }
        }

        private void DisplaySprite(Sprite sprite)
        {
            // Determine new gui style for icons button
            var textureStyle = new GUIStyle(GUI.skin.button);
            textureStyle.margin = new RectOffset(0, 0, 0, 0);

            float textureScaleInButton;

            if (_activeSprite != null && _activeSprite.name == sprite.name)
            {
                textureStyle.normal.background = textureStyle.active.background;
                textureScaleInButton = 0.9f;
            }
            else
            {
                textureStyle.normal.background = null;
                textureScaleInButton = 0.8f;
            }

            DisplayButton(textureStyle, sprite, textureScaleInButton);
        }

        private void DisplayButton(GUIStyle textureStyle, Sprite sprite, float textureOffset)
        {
            // Display the button with current icons size
            // Set sprite as active if button is chosen
            if (GUILayout.Button("", textureStyle,
            GUILayout.Width(_spriteSizes[_currentIconsSize]),
            GUILayout.Height(_spriteSizes[_currentIconsSize])))
            {
                _activeSprite = sprite;
                //DrawingModeEventHandler.CurrentSprite = sprite;
            }

            // Determine the appropriate scale according to the size of the sprite
            var scale = (sprite.textureRect.width > sprite.textureRect.height)
                ? _spriteSizes[_currentIconsSize] / sprite.textureRect.width
                : _spriteSizes[_currentIconsSize] / sprite.textureRect.height;

            // Calculate new sprite width and height
            var textureWidth = sprite.textureRect.width * scale * textureOffset;
            var textureHeight = sprite.textureRect.height * scale * textureOffset;

            //Calculate difference between button size and sprite size
            var diffX = _spriteSizes[_currentIconsSize] - textureWidth;
            var diffY = _spriteSizes[_currentIconsSize] - textureHeight;

            // Draw Texture on button
            GUI.DrawTextureWithTexCoords(new Rect(GUILayoutUtility.GetLastRect().x + diffX / 2,
                                                  GUILayoutUtility.GetLastRect().y + diffY / 2,
                                                  textureWidth,
                                                  textureHeight),
                                         sprite.texture,
                                         new Rect(sprite.textureRect.x / sprite.texture.width,
                                                     sprite.textureRect.y / sprite.texture.height,
                                                     sprite.textureRect.width / sprite.texture.width,
                                                     sprite.textureRect.height / sprite.texture.height));
        }

    }

}


