using UnityEngine;
// using UtilityToolkit.Editor;

namespace Components
{
    public class CameraSpinner : MonoBehaviour
    {
        [SerializeField] private Transform _lookAtTarget;
        [SerializeField] private float _rotationSpeed = 10f;

        private void Update()
        {
            transform.RotateAround(_lookAtTarget.position, Vector3.up, _rotationSpeed * Time.deltaTime);
            transform.LookAt(_lookAtTarget);
        }
    
        // [Button]
        public void ResetCamera()
        {
            transform.LookAt(_lookAtTarget);
        }
    }
}