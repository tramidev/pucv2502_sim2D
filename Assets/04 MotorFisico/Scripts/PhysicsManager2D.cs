using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace PUCV.PhysicEngine2D
{
    public class PhysicsManager2D : MonoBehaviour
    {
        private static PhysicsManager2D _instance;
        private bool _registered;
        private List<CustomRigidbody2D> _rigidbodies = new List<CustomRigidbody2D>();
        
        //Per FixedUpdateList
        private List<InternalCollisionInfo> _currentCollisionList = new List<InternalCollisionInfo>();
        private Dictionary<CustomRigidbody2D,InternalCollisionInfo> _collisionDict = new Dictionary<CustomRigidbody2D, InternalCollisionInfo>();

        private void Awake()
        {
            if (_instance != null)
            {
                DestroyImmediate(this);
                return;
            }
            
            //Singleton
            _instance = this;
            DontDestroyOnLoad(_instance);
        }

        public static void RegisterRigidbody(CustomRigidbody2D customRigidbody2D)
        {
            if(_instance) _instance._rigidbodies.Add(customRigidbody2D);
        }

        public static void UnregisterRigidbody(CustomRigidbody2D customRigidbody2D)
        {
            if(_instance) _instance._rigidbodies.Remove(customRigidbody2D);
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            StepCalculateCollisions(deltaTime);
            StepInformCollisions(deltaTime);
            StepApplyMovementToRigidbodies(deltaTime);
            
            
        }

        private void StepCalculateCollisions(float deltaTime)
        {
            List<CustomCollider2D> colliders = new List<CustomCollider2D>();
            foreach (CustomRigidbody2D rigidbody in _rigidbodies)
            {
                CustomCollider2D customCollider = rigidbody.GetCollider();
                colliders.Add(customCollider);
            }
            var currCollisionList = CollisionMath2D.DetectCollisions(colliders);
            
            //Check if collision was present last frame
            foreach (var currCollisionInfo in currCollisionList)
            {
                currCollisionInfo.wasCollidedLastFrame = false;
                foreach (var prevCollisionInfo in _currentCollisionList)
                {
                    if ((prevCollisionInfo.bodyACollider == currCollisionInfo.bodyACollider &&
                         prevCollisionInfo.bodyBCollider == currCollisionInfo.bodyBCollider) ||
                        (prevCollisionInfo.bodyACollider == currCollisionInfo.bodyBCollider &&
                         prevCollisionInfo.bodyBCollider == currCollisionInfo.bodyACollider))
                    {
                        currCollisionInfo.wasCollidedLastFrame = true;
                        break;
                    }
                }
            }

            _currentCollisionList = currCollisionList;
        }
        
        private void StepInformCollisions(float deltaTime)
        {
            foreach (var currCollisionInfo in _currentCollisionList)
            {
                if (currCollisionInfo.wasCollidedLastFrame) continue;
                CollisionInfo a = currCollisionInfo.GetCollInfoForBodyA();
                CollisionInfo b = currCollisionInfo.GetCollInfoForBodyB();
                currCollisionInfo.bodyACollider.InformOnCollisionEnter2D(a);
                currCollisionInfo.bodyBCollider.InformOnCollisionEnter2D(b);
            }
        }

        void StepApplyMovementToRigidbodies(float deltaTime)
        {
            if (_rigidbodies == null || _rigidbodies.Count == 0) return;
            foreach (CustomRigidbody2D rigidbody in _rigidbodies)
            {
                Vector2 rigidbodyPos = rigidbody.GetWorldPosition();
                rigidbodyPos += rigidbody.velocity*deltaTime;
                rigidbody.SetWoldPosition(rigidbodyPos);
            }
        }
    }
    
    public class InternalCollisionInfo
    {
        
        public CustomCollider2D bodyACollider;
        public CustomRigidbody2D bodyARigidbody;
        public CustomCollider2D bodyBCollider;
        public CustomRigidbody2D bodyBRigidbody;
        public bool wasCollidedLastFrame;

        public Vector2 contactPoint;
        public Vector2 contactNormalAB;
        public Vector2 contactNormalBA;

        public InternalCollisionInfo(
            CustomCollider2D colA, 
            CustomCollider2D colB, 
            Vector2 point, 
            Vector2 normal
            )
        {
            bodyACollider = colA;
            bodyARigidbody = colA.rigidBody;
            bodyBCollider = colB;
            bodyBRigidbody = colB.rigidBody;
            contactPoint = point;
            contactNormalAB = normal;
            contactNormalBA = -normal;
        }

        public CollisionInfo GetCollInfoForBodyA()
        {
            return new CollisionInfo()
            {
                otherCollider = bodyBCollider,
                otherRigidbody = bodyBRigidbody,
                contactPoint = contactPoint,
                contactNormal = contactNormalAB
            };
        }

        public CollisionInfo GetCollInfoForBodyB()
        {
            return new CollisionInfo()
            {
                otherCollider = bodyACollider,
                otherRigidbody = bodyARigidbody,
                contactPoint = contactPoint,
                contactNormal = contactNormalBA
            };
        }
    }

    public class CollisionInfo
    {
        public CustomCollider2D otherCollider;
        public CustomRigidbody2D otherRigidbody;

        public Vector2 contactPoint;
        public Vector2 contactNormal;
    }
}

