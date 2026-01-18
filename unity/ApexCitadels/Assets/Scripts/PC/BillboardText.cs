using UnityEngine;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Makes a GameObject always face the camera (billboard effect).
    /// Useful for floating text labels that should always be readable.
    /// </summary>
    public class BillboardText : MonoBehaviour
    {
        private Camera _mainCamera;
        
        [SerializeField] private bool lockYAxis = true;
        
        private void Start()
        {
            _mainCamera = Camera.main;
        }
        
        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }
            
            // Face the camera
            Vector3 lookDirection = _mainCamera.transform.position - transform.position;
            
            if (lockYAxis)
            {
                lookDirection.y = 0;
            }
            
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(-lookDirection);
            }
        }
    }
}
