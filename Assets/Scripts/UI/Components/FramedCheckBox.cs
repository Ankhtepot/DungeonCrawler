using System;
using Scripts.System.MonoBases;
using TMPro;
using UnityEngine.UI;

namespace Scripts.UI.Components
{
    public class FramedCheckBox : UIElementBase
    {
        public event Action<bool> OnValueChanged; 

        private Toggle _checkbox;
        private TMP_Text _label;

        private void Awake()
        {
            _checkbox = body.transform.Find("CheckBox").GetComponent<Toggle>();
            _checkbox.onValueChanged.AddListener(OnValueChanged_internal);
            
            _label = _checkbox.transform.Find("Label").GetComponent<TMP_Text>();
        }

        private void OnDisable()
        {
            OnValueChanged = null;
        }

        public bool IsOn => _checkbox.isOn;

        public void SetLabel(string text) => _label.text = text ?? "";

        public void SetToggle(bool isOn) => _checkbox.isOn = isOn;

        private void OnValueChanged_internal(bool value)
        {
            OnValueChanged?.Invoke(value);
        }
    }
}
