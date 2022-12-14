using Scripts.Helpers.Extensions;
using UnityEngine;

namespace Scripts.MapEditor
{
    public class Cursor3D : MonoBehaviour
    {
        [SerializeField] private GameObject cursor;
        [SerializeField] private GameObject copy;

        public static Vector3 EditorWallCursorScale;

        static Cursor3D()
        {
            EditorWallCursorScale = new Vector3(0.15f, 1.2f, 1.2f);
        }

        private void OnEnable()
        {
            cursor.gameObject.SetActive(false);
        }

        public void ShowAt(Vector3 position, Vector3 scale, Quaternion rotation)
        {
            Transform ownTransform = transform;
            ownTransform.position = position;
            ownTransform.localRotation = rotation;
            ownTransform.localScale = scale;
            
            cursor.SetActive(true);
        }

        public void ShowAt(Vector3Int gridPosition, bool withCopyAbove = false, bool withCopyBellow = false)
        {
            Vector3 worldPosition = gridPosition.ToWorldPosition();
            ShowAt(worldPosition);

            if (withCopyAbove)
            {
                copy.transform.position = worldPosition + Vector3.up;
                copy.SetActive(true);
                return;
            }
            
            if (withCopyBellow)
            {
                copy.transform.position = worldPosition + Vector3.down;
                copy.SetActive(true);
                return;
            }
            
            copy.SetActive(false);
        }

        private void ShowAt(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            // Logger.Log($"Activating cursor on worldPosition: {worldPosition}");
            cursor.SetActive(true);
        }

        public void Hide()
        {
            copy.SetActive(false);

            Transform ownTransform = transform;
            ownTransform.localScale = Vector3.one;
            ownTransform.position = Vector3.zero;
            transform.localRotation = Quaternion.Euler(Vector3.zero);
            cursor.SetActive(false);
        }
    }
}