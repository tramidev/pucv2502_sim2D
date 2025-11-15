using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace PUCV.PhysicEngine2D
{
    public class CustomRigidbody2D : MonoBehaviour
    {
        public Vector2 velocity;

        private bool _registered;
        private CustomCollider2D _customCollider;

        private void Start()
        {
            RegisterToPhysicsManager();
            _customCollider = GetComponent<CustomCollider2D>();
            _customCollider.AddRigidbodyReference(this);
        }
        
        
        private void RegisterToPhysicsManager()
        {
            if (_registered) return;
            PhysicsManager2D.RegisterRigidbody(this);
            _registered = true;
        }

        private void UnregisterFromPhysicsManager()
        {
            if (!_registered) return;
            PhysicsManager2D.UnregisterRigidbody(this);
            _registered = false;
        }

        public Vector2 GetWorldPosition()
        {
            return transform.position;
        }
        
        public void SetWoldPosition(Vector2 newPos)
        {
            transform.position = newPos;
        }

        public CustomCollider2D GetCollider()
        {
            return _customCollider;
        }
    }
}
