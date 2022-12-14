using System;
using Scripts.EventsManagement;
using Scripts.Helpers;
using Scripts.System;
using Scripts.System.MonoBases;
using UnityEngine;

namespace Scripts.MapEditor.Services
{
    public class EditorCameraService : SingletonNotPersisting<EditorCameraService>
    {
        public float cameraPanSpeed = 100f;
        [SerializeField] private float cameraZoomSpeed = 100f;
        [SerializeField] private float maxZoomHeight = 20f;
        [SerializeField] private float cameraRotationSpeed = 100f;
        [SerializeField] private float prefabMoveCameraDistance = 8f;
        [SerializeField] private float prefabMoveCameraXOffset = -2f;
        [SerializeField] private Transform cameraHolder;

        [NonSerialized] public static bool IsOrthographic = true;

        private EditorMouseService Mouse => EditorMouseService.Instance;
        private Vector3 _cameraMoveVector = Vector3.zero;

        internal void ResetCamera()
        {
            cameraHolder.localRotation = Quaternion.Euler(Vector3.zero);
        }
        
        internal void HandleMouseMovement()
        {
            if (Mouse.LeftClickExpired && Input.GetMouseButtonUp(0)
                || Mouse.RightClickExpired && Input.GetMouseButtonUp(1))
            {
                Mouse.IsManipulatingCameraPosition = false;
                Mouse.RefreshMousePosition(true);
            }
            
            if (!Mouse.LeftClickedOnUI && Mouse.LeftClickExpired && Input.GetMouseButton(0))
            {
                Mouse.IsManipulatingCameraPosition = true;
                Mouse.SetCursorToCameraMovement();

                float xDelta = Input.GetAxis(Strings.MouseXAxis);
                float yDelta = Input.GetAxis(Strings.MouseYAxis);

                if (xDelta == 0f && yDelta == 0f) return;
                
                Matrix4x4 worldToLocalMatrix = transform.worldToLocalMatrix;
                Vector3 localForward = worldToLocalMatrix.MultiplyVector(-cameraHolder.forward);
                Vector3 localRight = worldToLocalMatrix.MultiplyVector(cameraHolder.right);
                Vector3 moveVector = localRight * (yDelta * Time.deltaTime * cameraPanSpeed);
                moveVector += localForward * (xDelta * Time.deltaTime * cameraPanSpeed);
                
                cameraHolder.position += moveVector;
            }

            if (Mouse.RightClickExpired && Input.GetMouseButton(1))
            {
                Mouse.IsManipulatingCameraPosition = true;
                Mouse.SetCursorToCameraMovement();

                float xDelta = Input.GetAxis(Strings.MouseXAxis);
                float yDelta = Input.GetAxis(Strings.MouseYAxis);

                if (xDelta == 0f && yDelta == 0f) return;

                Vector3 cameraRotation = cameraHolder.localRotation.eulerAngles;
                _cameraMoveVector.x = cameraRotation.x;
                _cameraMoveVector.y = cameraRotation.y + (xDelta * Time.deltaTime * cameraRotationSpeed);
                _cameraMoveVector.z = cameraRotation.z - (yDelta * Time.deltaTime * cameraRotationSpeed);
                
                cameraHolder.localRotation = Quaternion.Euler(_cameraMoveVector);
            }
        }
        
        internal void HandleMouseWheel()
        {
            float wheelDelta = Input.GetAxis(Strings.MouseWheel);
            if (wheelDelta != 0)
            {
                TranslateCamera(0, -wheelDelta * Time.deltaTime * cameraZoomSpeed, 0);
            }
        }

        public void MoveCameraToPrefab(Vector3 worldPosition) => MoveCameraTo(
            worldPosition.x,
            worldPosition.y + prefabMoveCameraDistance,
            worldPosition.z - prefabMoveCameraXOffset);
        
        internal void MoveCameraTo(float x, float y, float z)
        {
            _cameraMoveVector.x = x;
            _cameraMoveVector.y = y;
            _cameraMoveVector.z = z;

            cameraHolder.position = _cameraMoveVector;
        }

        internal void TranslateCamera(Vector3 positionDelta) => TranslateCamera(positionDelta.x, positionDelta.y, positionDelta.z);

        internal static void ToggleCameraPerspective()
        {
            IsOrthographic = !CameraManager.Instance.mainCamera.orthographic;
            CameraManager.Instance.mainCamera.orthographic = IsOrthographic;
            EditorEvents.TriggerOnCameraPerspectiveChanged(IsOrthographic);
        }
        
        private void TranslateCamera(float x, float y, float z)
        {
            _cameraMoveVector.x = z;
            _cameraMoveVector.y = y;
            _cameraMoveVector.z = -x;

            Vector3 newPosition = cameraHolder.position + _cameraMoveVector;

            newPosition.y = Mathf.Clamp(
                newPosition.y,
                -MapEditorManager.Instance.EditedLayout.Count + 3, maxZoomHeight);
            
            cameraHolder.position = newPosition;
        }
    }
}