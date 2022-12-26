using System.Globalization;
using Scripts.Localization;
using Scripts.System.Pooling;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using NotImplementedException = System.NotImplementedException;

namespace Scripts.UI.Components
{
    public class WaypointControl : MonoBehaviour, IPoolInitializable
    {
        private Vector3Control _position;
        private InputField _speedInput;
        private TMP_Text _stepLabel;
        private TMP_Text _stepValue;

        private UnityEvent<WaypointControl, Vector3> OnPositionChanged { get; set; } = new();
        private UnityEvent<WaypointControl, float> OnSpeedChanged { get; set; } = new();

        private void Awake()
        {
            _position = transform.Find("Vector3Control").GetComponent<Vector3Control>();
            _speedInput = transform.Find("AdditionalSettings/SpeedInput").GetComponent<InputField>();

            _stepLabel = transform.Find("AdditionalSettings/StepLabel").GetComponent<TMP_Text>();
            _stepLabel.text = t.Get(Keys.Step);
        
            _stepValue = transform.Find("AdditionalSettings/StepValue").GetComponent<TMP_Text>();
        }

        public void Set(
            string title,
            float step,
            Vector3 position,
            float speed,
            UnityAction<WaypointControl, Vector3> onPositionChanged,
            UnityAction<WaypointControl, float> onSpeedChanged,
            string xLabel = null, string yLabel = null, string zLabel = null)
        {
            OnPositionChanged.RemoveAllListeners();
            _position.OnValueChanged.RemoveAllListeners();
            _position.Label.text = title;
            _position.XLabel.text = string.IsNullOrEmpty(xLabel) ? "x" : xLabel;
            _position.YLabel.text = string.IsNullOrEmpty(yLabel) ? "y" : yLabel;
            _position.ZLabel.text = string.IsNullOrEmpty(zLabel) ? "z" : zLabel;
            _position.Value = position;
            _position.Step = step;
            _position.OnValueChanged.AddListener(OnPositionChanged_internal);
            OnPositionChanged.AddListener(onPositionChanged);

            OnSpeedChanged.RemoveAllListeners();
            _speedInput.OnValueChanged.RemoveAllListeners();
            _speedInput.SetTitleText(t.Get(Keys.SpeedTowardsPoint));
            _speedInput.SetInputText(speed.ToString(CultureInfo.InvariantCulture));
            _speedInput.OnValueChanged.AddListener(OnSpeedChanged_internal);
            OnSpeedChanged.AddListener(onSpeedChanged);

            _stepValue.text = step.ToString(CultureInfo.InvariantCulture);
        }

        private void OnPositionChanged_internal(Vector3 newPosition)
        {
            OnPositionChanged.Invoke(this, newPosition);
        }
    
        private void OnSpeedChanged_internal(string newSpeed)
        {
            if (float.TryParse(newSpeed, out float parsedValue))
            {
                OnSpeedChanged.Invoke(this, parsedValue);
            }
        }

        public void Initialize()
        {
            OnPositionChanged.RemoveAllListeners();
            OnSpeedChanged.RemoveAllListeners();
        }
    }
}