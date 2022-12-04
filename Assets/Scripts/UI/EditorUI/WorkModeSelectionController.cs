﻿using System;
using System.Collections.Generic;
using Scripts.EventsManagement;
using Scripts.Helpers;
using Scripts.MapEditor;
using Scripts.System;
using UnityEngine;
using static Scripts.MapEditor.Enums;

namespace Scripts.UI.EditorUI
{
    public class WorkModeSelectionController : UIWindowBase
    {
        [SerializeField] private ImageButton buildModeButton;
        [SerializeField] private ImageButton selectModeButton;

        private Dictionary<ImageButton, EWorkMode> _workModesMap;

        private void Awake()
        {
            _workModesMap = new()
            {
                {buildModeButton, EWorkMode.Build},
                {selectModeButton, EWorkMode.Select},
            };
        }

        private void OnEnable()
        {
            EditorEvents.OnWorkModeChanged += OnWorkModeChanged;
            buildModeButton.OnClick += WorkModeButtonClicked;
            selectModeButton.OnClick += WorkModeButtonClicked;
        }

        private void OnDisable()
        {
            EditorEvents.OnWorkModeChanged -= OnWorkModeChanged;
            buildModeButton.OnClick -= WorkModeButtonClicked;
            selectModeButton.OnClick -= WorkModeButtonClicked;
        }

        private void OnWorkModeChanged(EWorkMode newWorkMode)
        {
            foreach (KeyValuePair<ImageButton, EWorkMode> record in _workModesMap)
            {
                if (record.Value == newWorkMode)
                {
                    record.Key.SetSelected(true);
                    continue;
                }
                
                record.Key.SetSelected(false);
            }
        }

        private void WorkModeButtonClicked(MonoBehaviour sender)
        {
            ImageButton button = sender as ImageButton;

            if (!button) return;
            
            MapEditorManager.Instance.SetWorkMode(_workModesMap[button]);
        }
    }
}