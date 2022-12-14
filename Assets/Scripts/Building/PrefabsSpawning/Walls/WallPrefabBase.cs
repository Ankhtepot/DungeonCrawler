using System;
using Scripts.Building.PrefabsSpawning.Configurations;
using Scripts.Building.Walls;
using Scripts.MapEditor;
using Scripts.System;
using Scripts.System.Pooling;
using Scripts.UI.EditorUI;
using UnityEngine;

namespace Scripts.Building.PrefabsSpawning.Walls
{
    public abstract class WallPrefabBase : PrefabBase, IPoolInitializable
    {
        public GameObject presentedInEditor;

        private static MapEditorManager Manager => MapEditorManager.Instance;
        
        private Cursor3D _cursor3D;
        [NonSerialized] public bool WallEligibleForEditing;
        private WallConfiguration _ownConfiguration;

        public void Initialize()
        {
            WallEligibleForEditing = false;
            _ownConfiguration = null;
            
            if (presentedInEditor) presentedInEditor.SetActive(false);
        }

        public void OnClickInEditor()
        {
            EditorUIManager.Instance.OpenEditorWindow(_ownConfiguration);
            WallEligibleForEditing = false;
        }

        /// <summary>
        /// Editor uses this method for editing existing walls in editor. 
        /// </summary>
        public void OnMouseEntered()
        {
            Transform ownTransform = transform;
            Vector3 position = ownTransform.position;
            Quaternion rot = ownTransform.rotation;

            FindObjectOfType<Cursor3D>().ShowAt(position, Cursor3D.EditorWallCursorScale, rot);
                
            _ownConfiguration ??= Manager.MapBuilder
                .GetPrefabConfigurationByTransformData(new PositionRotation(position, rot)) as WallConfiguration;
            WallEligibleForEditing = true;
        }
    }
}