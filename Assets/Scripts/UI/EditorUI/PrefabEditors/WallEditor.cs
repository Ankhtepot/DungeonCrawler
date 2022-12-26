﻿using System.Collections.Generic;
using System.Linq;
using Scripts.Building.PrefabsSpawning.Configurations;
using Scripts.Building.PrefabsSpawning.Walls;
using Scripts.Helpers.Extensions;
using Scripts.Localization;
using Scripts.MapEditor.Services;
using Scripts.ScriptableObjects;
using Scripts.System;
using Scripts.UI.Components;
using Scripts.UI.EditorUI.PrefabEditors;
using UnityEngine;
using static Scripts.Enums;

namespace Scripts.UI.EditorUI
{
    public class WallEditor : PrefabEditorBase<WallConfiguration, WallPrefabBase>
    {
        [SerializeField] private LabeledSlider offsetSlider;
        
        private WaypointEditor _waypointEditor;

        public static List<Waypoint> _debugWaypoints;

        protected override WallConfiguration GetNewConfiguration(string prefabName)
        {
            return new WallConfiguration
            {
                PrefabType = EditedPrefabType,
                PrefabName = AvailablePrefabs.FirstOrDefault(prefab => prefab.name == prefabName)?.name,
                TransformData = new PositionRotation(Placeholder.transform.position, Placeholder.transform.rotation),
                WayPoints = new List<Waypoint>(),
                Offset = 0f
            };
        }

        protected override WallConfiguration CopyConfiguration(WallConfiguration sourceConfiguration) => new(EditedConfiguration);

        protected override Vector3 Cursor3DScale => new(0.15f, 1f, 1f);

        public override void SetActive(bool isActive)
        {
            base.SetActive(isActive);

            _waypointEditor = body.transform.Find("WaypointsEditor").GetComponent<WaypointEditor>();
        }

        public override void Open(WallConfiguration configuration)
        {
            if (!CanOpen) return;
            
            if (configuration == null)
            {
                Close();
                return;
            }
            
            base.Open(configuration);

            if (PhysicalPrefabBody)
            {
                offsetSlider.SetActive(true);
                offsetSlider.Value = configuration.Offset;
                offsetSlider.slider.onValueChanged.RemoveAllListeners();
                offsetSlider.slider.onValueChanged.AddListener(OnOffsetSliderValueChanged);
            }

            VisualizeOtherComponents();
        }

        protected override string SetupWindow(EPrefabType prefabType, bool deleteButtonActive)
        {
            offsetSlider.SetLabel(t.Get(Keys.Offset));
            offsetSlider.SetActive(false);
            
            return base.SetupWindow(prefabType, deleteButtonActive);
        }

        protected override void SetPrefab(string prefabName)
        {
            base.SetPrefab(prefabName);
            
            _waypointEditor.SetActive(false);
            
            if (PhysicalPrefabBody)
            {
                offsetSlider.SetActive(true);
                offsetSlider.Value = EditedConfiguration.Offset;
                offsetSlider.slider.onValueChanged.AddListener(OnOffsetSliderValueChanged);
            }
            
            VisualizeOtherComponents();
        }

        protected override void Delete()
        {
            if (EditedConfiguration.WayPoints.Any())
            {
                WayPointService.DestroyPath(EditedConfiguration.WayPoints);
            }
            
            base.Delete();
        }

        protected override void SaveMapAndClose()
        {
            WayPointService.HighlightPath(EditedConfiguration.WayPoints, false);
            base.SaveMapAndClose();
        }

        protected override void RemoveAndClose()
        {
            if (EditedConfiguration.WayPoints.Any())
            {
                WayPointService.DestroyPath(EditedConfiguration.WayPoints);
            }
            
            base.RemoveAndClose();
        }

        private void OnOffsetSliderValueChanged(float value)
        {
            SetEdited();
            Vector3 newPosition = PhysicalPrefabBody.transform.localPosition;
            newPosition.x = value;
            EditedConfiguration.Offset = value;
            PhysicalPrefabBody.transform.localPosition = newPosition;
        }

        private void VisualizeOtherComponents()
        {
            WallPrefabBase script = PhysicalPrefab.GetComponentInParent<WallPrefabBase>();

            if (!script) return;
            
            if (script.presentedInEditor)
            {
                script.transform.Find("EditorPresentation").gameObject.SetActive(true);
            }

            if (script is WallMovementBetween movementScript)
            {
                if (EditedConfiguration.WayPoints.Count < 2 && movementScript.waypointsPreset)
                {
                    List<Waypoint> translatedWaypoints = new();
                    
                    foreach (Waypoint waypoint in movementScript.waypointsPreset.waypoints)
                    {
                        Waypoint newWaypoint = new()
                        {
                            position = EditedConfiguration.TransformData.Position + waypoint.position,
                            moveSpeedModifier = waypoint.moveSpeedModifier
                        };
                        translatedWaypoints.Add(newWaypoint);
                    }
                    
                    EditedConfiguration.WayPoints = translatedWaypoints;
                }
                else if (EditedConfiguration.WayPoints.Count == 0)
                {
                    EditedConfiguration.WayPoints.Add(
                        new Waypoint(
                            EditorMouseService.Instance.LastLeftButtonUpWorldPosition,
                            0.3f));
                }

                _debugWaypoints = EditedConfiguration.WayPoints;
                _waypointEditor.SetActive(true, EditedConfiguration.WayPoints, OnPathChanged);
                WayPointService.AddPath(EditedConfiguration.WayPoints,true);
                EditorCameraService.Instance.ResetCamera();
            }
        }

        private void OnPathChanged(IEnumerable<Waypoint> path)
         { 
             SetEdited();
            WayPointService.DestroyPath(EditedConfiguration.WayPoints);
            EditedConfiguration.WayPoints = path.ToList();
            WayPointService.AddPath(path, true);
        }
    }
}