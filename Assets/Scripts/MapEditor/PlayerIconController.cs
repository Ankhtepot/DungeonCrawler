using Scripts.Helpers;
using UnityEngine;

namespace Scripts.MapEditor
{
    public class PlayerIconController : MonoBehaviour
    {
        [SerializeField] private GameObject body;
        [SerializeField] private GameObject arrow;

        public void SetPositionByGrid(Vector3Int gridPosition)
        {
            gridPosition = gridPosition.SwapXY();
            gridPosition.y = -gridPosition.y;
            gameObject.transform.position = gridPosition;
        }
        
        public void SetArrowRotation(Quaternion rotation) => SetArrowRotation(rotation.eulerAngles);

        private void SetArrowRotation(Vector3 rotation)
        {
            Vector3 arrowRotation = arrow.transform.localRotation.eulerAngles;
            arrowRotation.z = rotation.y - 90;
            arrow.transform.localRotation = Quaternion.Euler(arrowRotation);
        }

        public void SetActive(bool isActive) => body.SetActive(isActive);
    }
}
