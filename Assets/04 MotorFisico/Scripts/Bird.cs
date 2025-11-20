using UnityEngine;

namespace PUCV.PhysicEngine2D
{
    public class Bird : MonoBehaviour
    {
        private Vector2 _acceleration;
        public CustomRigidbody2D _rigidbody;

        public void Initialize(CustomRigidbody2D rigidbody)
        {
            _rigidbody = rigidbody;
            _rigidbody.velocity = Random.insideUnitCircle * 2.5f;
            _acceleration = Vector2.zero;
        }

        /// <summary>
        /// Aplica las fuerzas calculadas por el FlockController al pájaro
        /// </summary>
        public void ApplyForces(Vector2 separationForce, Vector2 alignmentForce, Vector2 cohesionForce, Vector2 avoidBoundsForce, FlockController.BoidSettings settings)
        {
            // Resetear aceleración
            _acceleration = Vector2.zero;
            
            // Aplicar pesos
            _acceleration += separationForce * settings.separationWeight;
            _acceleration += alignmentForce * settings.alignmentWeight;
            _acceleration += cohesionForce * settings.cohesionWeight;
            _acceleration += avoidBoundsForce * settings.separationWeight;
            
            // Limitar fuerza máxima
            if (_acceleration.magnitude > settings.maxForce)
            {
                _acceleration = _acceleration.normalized * settings.maxForce;
            }
            
            // Actualizar velocidad usando CustomRigidbody2D
            _rigidbody.velocity += _acceleration;
            
            // Limitar velocidad máxima
            if (_rigidbody.velocity.magnitude > settings.maxSpeed)
            {
                _rigidbody.velocity = _rigidbody.velocity.normalized * settings.maxSpeed;
            }
        }

        /// <summary>
        /// Actualiza la posición del pájaro en el mundo
        /// </summary>
        public void UpdatePosition(FlockController.BoidSettings settings)
        {
            // Obtener posición actual usando CustomRigidbody2D
            Vector2 currentWorldPos = _rigidbody.GetWorldPosition();
            Vector2 newPos = currentWorldPos + _rigidbody.velocity * Time.fixedDeltaTime;
            
            // Aplicar límites de mundo
            newPos.x = Mathf.Clamp(newPos.x, settings.boundsMin.x, settings.boundsMax.x);
            newPos.y = Mathf.Clamp(newPos.y, settings.boundsMin.y, settings.boundsMax.y);
            
            // Establecer nueva posición usando CustomRigidbody2D
            _rigidbody.SetWoldPosition(newPos);
            
            // Rotar hacia la dirección de movimiento
            if (_rigidbody.velocity.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(_rigidbody.velocity.y, _rigidbody.velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        public Vector2 GetWorldPosition()
        {
            return _rigidbody.GetWorldPosition();
        }

        public Vector2 GetVelocity()
        {
            return _rigidbody.velocity;
        }

        public void SetVelocity(Vector2 velocity)
        {
            _rigidbody.velocity = velocity;
        }
    }
}

