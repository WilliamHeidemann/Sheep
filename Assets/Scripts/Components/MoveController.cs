using System;
using UnityEngine;

namespace Components
{
    public class MoveController : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed;
        [SerializeField] private float _moveSpeed;
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            var horizontal = Input.GetAxis("Horizontal");
            Rotate(horizontal);
            var vertical = Input.GetAxis("Vertical");
            Move(vertical);
        }
    
        private void Rotate(float horizontal)
        {
            var rotation = Quaternion.Euler(0, horizontal * _rotationSpeed * Time.deltaTime, 0);
            transform.rotation *= rotation;
        }
    
        private void Move(float vertical)
        {
            if (vertical == 0)
            {
                _animator.SetInteger("AnimationID", 0);
                return;
            } 
            _animator.SetInteger("AnimationID", 4);
            var forward = transform.forward;
            var move = forward * vertical * _moveSpeed * Time.deltaTime;
            transform.position += move;
        }
    }
}
