﻿using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.Building;
using Scripts.Building.PrefabsSpawning.Configurations;
using Scripts.Building.Walls;
using Scripts.EventsManagement;
using Scripts.Helpers;
using Scripts.Helpers.Extensions;
using Scripts.Localization;
using Scripts.MapEditor;
using Scripts.MapEditor.Services;
using Scripts.System;
using Scripts.System.MonoBases;
using Scripts.UI.Components;
using Scripts.UI.EditorUI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Scripts.Enums;
using static Scripts.MapEditor.Enums;

namespace Scripts.UI.EditorUI.PrefabEditors
{
    public abstract class PrefabEditorBase<TC, TPrefab> : EditorWindowBase, IPrefabEditor
        where TC : PrefabConfiguration
        where TPrefab : PrefabBase
    {
        protected GameObject Placeholder;

        private PrefabList _prefabList;
        private ConfigurationList _existingList;
        private GameObject _mainWindow;
        private Button _saveButton;
        private Button _cancelButton;
        private Button _deleteButton;
        private Button _closeButton;
        private ImageButton _prefabsFinderButton;
        private Title _prefabTitle;
        private TMP_Text _statusText;
        private EditorUIManager Manager => EditorUIManager.Instance;

        protected MapBuilder MapBuilder => MapEditorManager.Instance.MapBuilder;
        protected TC EditedConfiguration;
        protected EPrefabType EditedPrefabType;
        protected GameObject PhysicalPrefabBody;
        protected GameObject PhysicalPrefab;
        protected bool IsCurrentConfigurationChanged;
        
        private TC _originalConfiguration;
        private HashSet<TPrefab> _availablePrefabs;
        private Cursor3D _cursor3D;
        private bool _isEditingExistingPrefab;
        
        protected bool CanOpen => !IsCurrentConfigurationChanged;

        private void Awake()
        {
            AssignComponents();

            _closeButton.onClick.AddListener(RemoveAndReopen);
            _saveButton.onClick.AddListener(SaveMap);
            _deleteButton.onClick.AddListener(Delete);
            _cancelButton.onClick.AddListener(RemoveChanges);

            _cursor3D = FindObjectOfType<Cursor3D>();
        }

        private void OnEnable()
        {
            EditorEvents.OnWorkModeChanged += Close;
        }

        private void OnDisable()
        {
            EditorEvents.OnWorkModeChanged -= Close;
            StopAllCoroutines();
        }

        protected abstract TC GetNewConfiguration(string prefabName);
        protected abstract TC CloneConfiguration(TC sourceConfiguration);
        protected abstract void VisualizeOtherComponents();
        protected abstract void InitializeOtherComponents();

        protected virtual Vector3 Cursor3DScale => Vector3.one;

        /// <summary>
        /// Opens Existing Prefabs list. Next steps are - clicking in map to add/edit prefabs or open prefabs finder.
        /// </summary>
        public void Open()
        {
            _mainWindow.SetActive(false);
            _prefabList.Close();
            _existingList.Close();
            _prefabTitle.SetActive(false);
            EditedConfiguration = _originalConfiguration = null;
            SetEdited(false);

            SetButtons();
            
            SetExistingList(true,
                t.Get(Keys.ExistingPrefabs),
                MapBuilder.GetConfigurationsByPrefabClass<TC, TPrefab>(),
                OnExistingItemClick);
            
            SetActive(true);
        }

        public virtual void Open(TC configuration)
        {
            if (!CanOpen) return;
            
            if (configuration == null)
            {
                // Close();
                return;
            }
            
            _isEditingExistingPrefab = true;
            IsCurrentConfigurationChanged = false;
            EditedPrefabType = configuration.PrefabType;
            EditedConfiguration = configuration;
            _originalConfiguration = CloneConfiguration(configuration);
            
            string prefabListTitle = SetupWindow(configuration.PrefabType);

            MoveCameraToPrefab(configuration.TransformData.Position);
            
            _cursor3D.ShowAt(configuration.TransformData.Position,
                Cursor3DScale,
                configuration.TransformData.Rotation);

            _prefabTitle.SetActive(true);
            _prefabTitle.SetTitle(configuration.PrefabName);

            SetExistingList(false);
            _prefabList.Open(prefabListTitle, _availablePrefabs!, prefab => SetPrefab(prefab.gameObject.name));

            PhysicalPrefab = MapBuilder.GetPrefabByConfiguration(configuration);
            PhysicalPrefabBody = PhysicalPrefab.GetBody()?.gameObject;
            
            VisualizeOtherComponents();
        }

        public void Open(EPrefabType prefabType, PositionRotation placeholderTransformData)
        {
            if (!CanOpen) return;

            MoveCameraToPrefab(placeholderTransformData.Position);

            _isEditingExistingPrefab = false;
            
            string prefabListTitle = SetupWindow(prefabType);

            Placeholder.transform.position = placeholderTransformData.Position;
            Placeholder.transform.rotation = placeholderTransformData.Rotation;
            Placeholder.transform.parent = null;
            Placeholder.SetActive(true);

            if (_availablePrefabs == null || !_availablePrefabs.Any())
            {
                EditedPrefabType = EPrefabType.Invalid;
                SetStatusText(t.Get(Keys.NoPrefabsAvailable));
                return;
            }
            
            _cursor3D.ShowAt(placeholderTransformData.Position,
                Cursor3DScale,
                placeholderTransformData.Rotation);

            EditedPrefabType = prefabType;

            SetStatusText(t.Get(Keys.SelectPrefab));

            SetExistingList(false);
            _prefabList.Open(prefabListTitle, _availablePrefabs, prefab => SetPrefab(prefab.gameObject.name));
        }
        
        /// <summary>
        /// IPrefabEditor method
        /// </summary>
        public virtual void CloseWithRemovingChanges()
        {
            RemoveAndClose();
        }

        protected virtual void SetPrefab(string prefabName)
        {
            SetStatusText();
            _isEditingExistingPrefab = false;

            if (EditedConfiguration != null)
            {
                MapBuilder.RemovePrefab(EditedConfiguration);
                EditedConfiguration.PrefabName = prefabName;
                EditedConfiguration = CloneConfiguration(EditedConfiguration);
            }
            else
            {
                EditedConfiguration = GetNewConfiguration(prefabName);
            }
            

            if (!MapBuilder.BuildPrefab(EditedConfiguration))
            {
                SetStatusText(t.Get(Keys.ErrorBuildingPrefab));
                Placeholder.SetActive(false);
                return;
            }

            _prefabTitle.SetActive(true);
            _prefabTitle.SetTitle(EditedConfiguration.PrefabName);

            PhysicalPrefab = MapBuilder.GetPrefabByConfiguration(EditedConfiguration);
            
            if (PhysicalPrefab)
            {
                PhysicalPrefabBody = PhysicalPrefab.GetBody()?.gameObject;
            }

            SetEdited();
            SetButtons();
            Placeholder.SetActive(false);
            
            VisualizeOtherComponents();
        }
        
        protected void MoveCameraToPrefab(Vector3 targetPosition) => 
            EditorCameraService.Instance.MoveCameraToPrefab(targetPosition);

        protected void SetEdited(bool isEdited = true)
        {
            IsCurrentConfigurationChanged = isEdited;
            Manager.isAnyObjectEdited = isEdited;
            SetButtons();
            
            if (isEdited)
            {
                EditorEvents.TriggerOnMapEdited();
            }
        }

        protected virtual void Delete()
        {
            MapBuilder.RemovePrefab(EditedConfiguration);

            MapEditorManager.Instance.SaveMap();
            _prefabList.DeselectButtons();
            _isEditingExistingPrefab = false;
            EditedConfiguration = null;
            _originalConfiguration = null;
            SetEdited(false);
            _prefabList.DeselectButtons();
            
            SetButtons();
            VisualizeOtherComponents();
        }

        private void RemoveAndReopen()
        {
            if (IsCurrentConfigurationChanged)
            {
                RemoveChanges();
            }
            
            Open();
        }

        protected virtual void RemoveAndClose()
        {
            if (IsCurrentConfigurationChanged)
            {
                RemoveChanges();
            }
            
            Close();
        }
        
        private void RemoveChanges()
        {
            if (_isEditingExistingPrefab)
            {
                MapBuilder.RemovePrefab(EditedConfiguration);
                MapBuilder.BuildPrefab(_originalConfiguration);
                EditedConfiguration = CloneConfiguration(_originalConfiguration);
            }
            
            if (!_isEditingExistingPrefab && EditedConfiguration != null)
            {
                MapBuilder.RemovePrefab(EditedConfiguration);
                EditedConfiguration = null;
                _prefabList.DeselectButtons();
            }
            
            SetEdited(false);
            SetButtons();
            VisualizeOtherComponents();
        }

        protected virtual void SaveMap()
        {
            SetEdited(false);
            
            _isEditingExistingPrefab = true;
            MapBuilder.AddReplacePrefabConfiguration(EditedConfiguration);
            MapEditorManager.Instance.SaveMap();

            SetButtons();
        }

        protected void Close()
        {
            EditedConfiguration = null;
            _originalConfiguration = null;
            _isEditingExistingPrefab = false;
            EditedPrefabType = EPrefabType.Invalid;

            Placeholder.transform.position = Vector3.zero;
            Placeholder.transform.parent = body.transform;
            Placeholder.SetActive(false);

            _prefabTitle.SetActive(false);
            PhysicalPrefabBody = null;
            PhysicalPrefab = null;

            _cursor3D.Hide();

            Manager.isAnyObjectEdited = false;
            Manager.WallGizmo.Reset();
            EditorMouseService.Instance.RefreshMousePosition();

            SetActive(false);
        }
        
        
        private void SetButtons()
        {
            _cancelButton.gameObject.SetActive(IsCurrentConfigurationChanged);
            _deleteButton.gameObject.SetActive(_isEditingExistingPrefab || EditedConfiguration is {SpawnPrefabOnBuild: true});
            _saveButton.gameObject.SetActive(IsCurrentConfigurationChanged);
            _closeButton.SetTextColor(IsCurrentConfigurationChanged ? Colors.Negative : Colors.White);
            _prefabsFinderButton.SetActive(!IsCurrentConfigurationChanged);
        }

        private void SetStatusText(string text = null)
        {
            _statusText.text = text ?? "";

            if (string.IsNullOrEmpty(text))
            {
                _statusText.gameObject.SetActive(false);
                return;
            }

            _statusText.gameObject.SetActive(true);
        }
        
        private string SetupWindow(EPrefabType prefabType)
        {
            InitializeOtherComponents();
            
            SetActive(true);
            _prefabList.SetActive(false);
            SetExistingList(false);
            _mainWindow.SetActive(true);
            SetStatusText();

            _closeButton.GetComponentInChildren<TMP_Text>().text = t.Get(Keys.Close);
            _cancelButton.GetComponentInChildren<TMP_Text>().text = t.Get(Keys.Cancel);

            _saveButton.GetComponentInChildren<TMP_Text>().text = t.Get(Keys.Save);

            _deleteButton.GetComponentInChildren<TMP_Text>().text = t.Get(Keys.Delete);
            
            SetButtons();

            string prefabListTitle = t.Get(Keys.AvailablePrefabs);

            _availablePrefabs = PrefabStore.GetPrefabsOfType(prefabType)?
                .Select(prefab => prefab.GetComponent<TPrefab>())
                .Where(prefab => prefab.prefabType == prefabType)
                .ToHashSet();

            return prefabListTitle;
        }
        
        private void Close(EWorkMode _)
        {
            if (!CanOpen) RemoveAndClose();
        }

        private void SetExistingList(bool isOpen,
            string title = null,
            IEnumerable<TC> items = null,
            Action<TC> onClick = null,
            Action onClose = null)
        {
            if (!isOpen)
            {
                _existingList.Close();
                return;
            }

            if (string.IsNullOrEmpty(title) || items == null || onClick == null) return;
            
            _existingList.Open(title, items, configuration => Open(configuration as TC), onClose);
        }

        private void OnExistingItemClick(TC clickedPrefab)
        {
            Open(MapBuilder.GetConfigurationByGuid<TC>(clickedPrefab.Guid));
        }
        
        private void AssignComponents()
        {
            Transform bodyTransform = body.transform; 
            Placeholder = bodyTransform.Find("Placeholder").gameObject;
            _prefabList = bodyTransform.Find("AvailablePrefabs").GetComponent<PrefabList>();
            _existingList = bodyTransform.Find("ExistingPrefabs").GetComponent<ConfigurationList>();
            Transform frame = bodyTransform.GetChild(0).GetChild(0);
            _prefabTitle = frame.Find("Header/PrefabTitle").GetComponent<Title>();
            _prefabsFinderButton = frame.Find("Header/PrefabFinderButton").GetComponent<ImageButton>();
            _statusText = frame.Find("StatusText").GetComponent<TMP_Text>();
            Transform buttons = frame.Find("Buttons");
            _mainWindow = bodyTransform.Find("Background").gameObject;
            _cancelButton = buttons.Find("CancelButton").GetComponent<Button>();
            _cancelButton.SetTextColor(Colors.Warning);
            _saveButton = buttons.Find("SaveButton").GetComponent<Button>();
            _saveButton.SetTextColor(Colors.Positive);
            _deleteButton = buttons.Find("DeleteButton").GetComponent<Button>();
            _deleteButton.SetTextColor(Colors.Negative);
            _closeButton = buttons.Find("CloseButton").GetComponent<Button>();
        }
    }
}