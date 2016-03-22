using UnityEditor;
using UnityEngine;

namespace TileMapEditor
{
    /// <summary>
    /// Render custom gizmos
    /// </summary>
    //[CustomEditor(typeof(GameObject))]
    public class SceneGUIRenderer : Editor
    {
        /// <summary>
        /// Color of grid lines
        /// </summary>
        public static Color GridColor = Color.white;

        // The minimum allowable size of cell
        public const float CELL_SIZE_MIN = 0.05f;

        public static bool IsGridEnabled;
        public static Vector3 CellSize = new Vector2(1f, 1f);

        [DrawGizmo(GizmoType.NotInSelectionHierarchy)]
        private static void RenderGrid(Transform objectTransform, GizmoType gizmoType)
        {
            // Don't render grid if it is enabled
            if (!IsGridEnabled) return;
            // Don't render grid if specified incorrect cell size
            if (CellSize.x < CELL_SIZE_MIN || CellSize.y < CELL_SIZE_MIN) return;

            // Set grid color
            Gizmos.color = GridColor;
            // Get scene camera
            var camera = SceneView.currentDrawingSceneView.camera;

            // Get extreme screen points
            var minScreenPoint = camera.ScreenPointToRay(Vector2.zero).origin;
            var maxScreenPoint = camera.ScreenPointToRay(camera.pixelRect.size).origin;

            // Get extreme points of vertical lines.
            // To get stable position on screen it's necessary to correlate extreme screen points and shift center of grid. 
            var firstLinesPosition = CorrelateToGridCoord(minScreenPoint) - CellSize/2;
            var maxLinesPosition = CorrelateToGridCoord(maxScreenPoint) - CellSize/2;

            // Draw vertical lines
            for (var i = firstLinesPosition.x; i <= maxLinesPosition.x; i += CellSize.x)
            {
                Gizmos.DrawLine(
                    new Vector3(i, minScreenPoint.y),
                    new Vector3(i, maxScreenPoint.y));
            }

            //Draw horizontal lines
            for (var j = firstLinesPosition.y; j <= maxLinesPosition.y; j += CellSize.y)
            {
                Gizmos.DrawLine(
                    new Vector3(minScreenPoint.x, j),
                    new Vector3(maxScreenPoint.x, j));
            }

            SceneView.RepaintAll();

        }

        public static Vector3 CorrelateToGridCoord(Vector3 source)
        {
            return new Vector3(Mathf.Round(source.x/CellSize.x)*CellSize.x,
                Mathf.Round(source.y/CellSize.y)*CellSize.y);
        }
    }
}