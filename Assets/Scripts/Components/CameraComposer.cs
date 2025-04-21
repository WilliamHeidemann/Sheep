using UnityEngine;

namespace Components
{
    public class CameraComposer : MonoBehaviour
    {
        [SerializeField] private GameObject _dogCamera;
        [SerializeField] private GameObject _rotatingCamera;

        private bool _isUsingDogCamera = true;
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleCamera();
            }
        }

        private void ToggleCamera()
        {
            _isUsingDogCamera = !_isUsingDogCamera;
            if (_isUsingDogCamera)
            {
                _dogCamera.SetActive(true);
                _rotatingCamera.SetActive(false);
            }
            else
            {
                _dogCamera.SetActive(false);
                _rotatingCamera.SetActive(true);
            }
        }
    }
}
