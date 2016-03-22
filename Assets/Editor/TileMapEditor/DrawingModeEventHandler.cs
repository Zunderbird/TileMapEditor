using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TileMapEditor
{
    /// <summary>
    /// Handle events from the tile window
    /// </summary>
    public class DrawingModeEventHandler : Editor
    {
        public static bool IsEnabled;
        public static DrawingOption SelectedDrawingOption;
        public static GameObject ParentObject;
        public static string CurrentTileMapName;

        public static IGrouping<string, Sprite> ActiveGroup;

        private static GameObject _activeGameObject;
        private static Vector3 _mouseWorldPos;
        private static IEnumerable<GameObject> _foundGameObjects;

        private static bool _isTheSamePosition;

        private static readonly Dictionary<DrawingOption, Action> DrawingModes = new Dictionary<DrawingOption, Action>
        {
            {DrawingOption.PaintOver, PaintOver},
            {DrawingOption.PaintOnlyOnEmpty, PaintOnlyOnEmpty},
            {DrawingOption.PaintOrReplace, PaintOrReplace},
            {DrawingOption.Erase, Erase},
            {DrawingOption.Select, SelectGameObject}
        };

        static DrawingModeEventHandler()
        {
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView aView)
        {
            if (!IsEnabled) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (!IsLeftButtonPressed()) return;

            var mouseWorldPos = GetMouseWorldPos();

            _isTheSamePosition = (mouseWorldPos == _mouseWorldPos);

            _mouseWorldPos = mouseWorldPos;
            _foundGameObjects = GetGameObjectsInPosition(_mouseWorldPos);

            DrawingModes[SelectedDrawingOption]();

        }

        private static void PaintOver()
        {
            if (ActiveGroup == null || 
                _isTheSamePosition && Event.current.type != EventType.MouseDown) return;

            GameObject parentForCurrent = null;

            if (_foundGameObjects != null && ParentObject != null)
            {
                parentForCurrent = _foundGameObjects.FirstOrDefault(go => go.transform.parent == ParentObject.transform);
            }

            var gameObject = NewGameObject();
            if (parentForCurrent != null) gameObject.transform.parent = parentForCurrent.transform;
        }

        private static void PaintOnlyOnEmpty()
        {
            if (ActiveGroup == null) return;

            if (_foundGameObjects == null || !_foundGameObjects.Any()) NewGameObject();
        }

        private static void PaintOrReplace()
        {
            if (ActiveGroup == null 
                || _isTheSamePosition && Event.current.type != EventType.MouseDown) return;

            Erase();
            NewGameObject();
        }

        private static void Erase()
        {
            if (_foundGameObjects == null) return;

            foreach (var gameObject in _foundGameObjects)
            {
                DestroyImmediate(gameObject);
            }
        }

        private static void SelectGameObject()
        {
            if (Event.current.type == EventType.MouseDown)
            {
                if (ParentObject == null)
                {
                    Debug.Log("First set parent game object!");
                    return;
                }
                if (_foundGameObjects == null)
                {
                    Debug.Log("Parent game object hasn't any child to select!");
                    return;
                }

                Selection.activeGameObject = null;

                _activeGameObject = _foundGameObjects.FirstOrDefault(go =>
                    go.GetComponent<SpriteRenderer>() != null &&
                    go.transform.parent == ParentObject.transform);
            }
            if (Event.current.type == EventType.MouseDrag && _activeGameObject != null)
            {
                _activeGameObject.transform.position = _mouseWorldPos;
            }
        }

        private static GameObject NewGameObject()
        {
            var currentSprite = ActiveGroup.ToArray()[Random.Range(0, ActiveGroup.Count())]; ;

            var prefab = GetSpritePrefab(currentSprite);

            var gameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            InitializeGameObject(gameObject, currentSprite);

            return gameObject;
        }

        private static GameObject GetSpritePrefab(Sprite sprite)
        {
            var name = new string(sprite.name.Where(char.IsLetter).ToArray());

            var prefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/" + CurrentTileMapName + "/" + name + ".prefab",
                typeof(GameObject));

            if (prefab != null) return prefab;

            ResourceManager.CreateDirectoryIfNotExists(Application.dataPath + "/Prefabs" + '/' + CurrentTileMapName);

            var template = new GameObject(sprite.name, typeof(SpriteRenderer));
            InitializeGameObject(template, sprite);

            prefab = PrefabUtility.CreatePrefab("Assets/Prefabs/" + CurrentTileMapName + "/" + name + ".prefab", template);

            DestroyImmediate(template);

            Debug.Log("Created new prefab " + name + "!");

            return prefab;
        }

        private static void InitializeGameObject(GameObject gameObject, Sprite sprite)
        {
            gameObject.name = sprite.name;
            gameObject.transform.position = _mouseWorldPos;
            gameObject.GetComponent<SpriteRenderer>().sprite = sprite;

            if (ParentObject == null)
            {
                ParentObject = new GameObject();
                Debug.Log("New Game Object created!");
            }
            gameObject.transform.parent = ParentObject.transform;
        }

        private static IEnumerable<GameObject> GetGameObjectsInPosition(Vector3 position)
        {
            if (ParentObject == null || ParentObject.transform.childCount == 0) return null;

            var allGameObjects = new GameObject[ParentObject.transform.childCount];
            for (var i = 0; i < ParentObject.transform.childCount; i++)
                allGameObjects[i] = ParentObject.transform.GetChild(i).gameObject;

            return allGameObjects.Where(go => Approximately(go.transform.position, position));
        }

        private static Vector3 GetMouseWorldPos()
        {
            var mousePos = Event.current.mousePosition;
            mousePos.y = SceneView.currentDrawingSceneView.camera.pixelHeight - mousePos.y;

            var mouseWorldPos = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(mousePos).origin;

            mouseWorldPos = SceneGUIRenderer.CorrelateToGridCoord(mouseWorldPos);

            return mouseWorldPos;
        }

        private static bool IsLeftButtonPressed()
        {
            return (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown) &&
                   Event.current.button == 0;
        }

        public static bool Approximately(Vector3 first, Vector3 second)
        {
            return Mathf.Approximately(first.x, second.x) &&
                   Mathf.Approximately(first.y, second.y) &&
                   Mathf.Approximately(first.z, second.z);
        }
    }
}
