using System.Collections.Generic;
using UnityEngine;

namespace PUCV.PhysicEngine2D
{
    public class Boid : MonoBehaviour
    {
        private FlockController _flockController;
        private FlockController.BoidSettings _settings;
        private Vector2 _velocity;
        private Vector2 _acceleration;
        public CustomRigidbody2D _rigidbody;

        public void Initialize(FlockController flockController, FlockController.BoidSettings settings)
        {
            _flockController = flockController;
            _settings = settings;
            _velocity = Random.insideUnitCircle * _settings.maxSpeed * 0.5f;
            _acceleration = Vector2.zero;
            
            // Obtener o crear CustomRigidbody2D
            _rigidbody = GetComponent<CustomRigidbody2D>();
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<CustomRigidbody2D>();
            }
            _rigidbody.velocity = _velocity;
        }

        public void UpdateBoid(List<Boid> allBirds)
        {
            // Calcular fuerzas
            Vector2 separationForce = CalculateSeparation(allBirds);
            Vector2 alignmentForce = CalculateAlignment(allBirds);
            Vector2 cohesionForce = CalculateCohesion(allBirds);
            
            // Aplicar pesos
            _acceleration = Vector2.zero;
            _acceleration += separationForce * _settings.separationWeight;
            _acceleration += alignmentForce * _settings.alignmentWeight;
            _acceleration += cohesionForce * _settings.cohesionWeight;
            
            // Calcular fuerza para evitar límites de mundo
            Vector2 avoidBoundsForce = CalculateAvoidBounds();
            _acceleration += avoidBoundsForce * _settings.separationWeight;
            
            // Limitar fuerza máxima
            if (_acceleration.magnitude > _settings.maxForce)
            {
                _acceleration = _acceleration.normalized * _settings.maxForce;
            }
            
            // Actualizar velocidad
            _velocity += _acceleration;
            
            // Limitar velocidad máxima
            if (_velocity.magnitude > _settings.maxSpeed)
            {
                _velocity = _velocity.normalized * _settings.maxSpeed;
            }
            
            // Actualizar posición
            Vector2 currentWorldPos = _rigidbody.GetWorldPosition();
            Vector2 newPos = currentWorldPos + _velocity * Time.fixedDeltaTime;
            
            // Aplicar límites de mundo suave (sin clamping duro)
            newPos.x = Mathf.Clamp(newPos.x, _settings.boundsMin.x, _settings.boundsMax.x);
            newPos.y = Mathf.Clamp(newPos.y, _settings.boundsMin.y, _settings.boundsMax.y);
            
            _rigidbody.SetWoldPosition(newPos);
            
            // Actualizar velocidad en rigidbody
            if (_rigidbody != null)
            {
                _rigidbody.velocity = _velocity;
            }
            
            // Rotar hacia la dirección de movimiento
            if (_velocity.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        private Vector2 CalculateSeparation(List<Boid> allBirds)
        {
            Vector2 steer = Vector2.zero;
            int count = 0;
            Vector2 myPos = _rigidbody.GetWorldPosition();
            
            foreach (var other in allBirds)
            {
                if (other == this) continue;
                
                Vector2 otherPos = other._rigidbody.GetWorldPosition();
                float distance = Vector2.Distance(myPos, otherPos);
                
                if (distance < _settings.separationRadius && distance > 0)
                {
                    Vector2 diff = (myPos - otherPos).normalized;
                    diff /= distance;
                    steer += diff;
                    count++;
                }
            }
            
            if (count > 0)
            {
                steer /= count;
                if (steer.magnitude > 0)
                {
                    steer = steer.normalized * _settings.maxSpeed - _velocity;
                    if (steer.magnitude > _settings.maxForce)
                    {
                        steer = steer.normalized * _settings.maxForce;
                    }
                }
            }
            
            return steer;
        }

        private Vector2 CalculateAlignment(List<Boid> allBirds)
        {
            Vector2 avgVelocity = Vector2.zero;
            int count = 0;
            Vector2 myPos = _rigidbody.GetWorldPosition();
            
            foreach (var other in allBirds)
            {
                if (other == this) continue;
                
                Vector2 otherPos = other._rigidbody.GetWorldPosition();
                float distance = Vector2.Distance(myPos, otherPos);
                
                if (distance < _settings.alignmentRadius)
                {
                    avgVelocity += other._velocity;
                    count++;
                }
            }
            
            if (count > 0)
            {
                avgVelocity /= count;
                avgVelocity = avgVelocity.normalized * _settings.maxSpeed;
                Vector2 steer = avgVelocity - _velocity;
                
                if (steer.magnitude > _settings.maxForce)
                {
                    steer = steer.normalized * _settings.maxForce;
                }
                return steer;
            }
            
            return Vector2.zero;
        }

        private Vector2 CalculateCohesion(List<Boid> allBirds)
        {
            Vector2 centerMass = Vector2.zero;
            int count = 0;
            Vector2 myPos = _rigidbody.GetWorldPosition();
            
            foreach (var other in allBirds)
            {
                if (other == this) continue;
                
                Vector2 otherPos = other._rigidbody.GetWorldPosition();
                float distance = Vector2.Distance(myPos, otherPos);
                
                if (distance < _settings.cohesionRadius)
                {
                    centerMass += otherPos;
                    count++;
                }
            }
            
            if (count > 0)
            {
                centerMass /= count;
                Vector2 direction = (centerMass - myPos).normalized;
                Vector2 steer = direction * _settings.maxSpeed - _velocity;
                
                if (steer.magnitude > _settings.maxForce)
                {
                    steer = steer.normalized * _settings.maxForce;
                }
                return steer;
            }
            
            return Vector2.zero;
        }

        public Vector2 GetVelocity()
        {
            return _velocity;
        }

        private Vector2 CalculateAvoidBounds()
        {
            Vector2 steer = Vector2.zero;
            float avoidDistance = 5f; // Distancia a la que empieza a evitar
            Vector2 currentPos = _rigidbody.GetWorldPosition();

            // Evitar límite izquierdo
            if (currentPos.x < _settings.boundsMin.x + avoidDistance)
            {
                steer.x += 1f;
            }
            // Evitar límite derecho
            if (currentPos.x > _settings.boundsMax.x - avoidDistance)
            {
                steer.x -= 1f;
            }

            // Evitar límite inferior
            if (currentPos.y < _settings.boundsMin.y + avoidDistance)
            {
                steer.y += 1f;
            }
            // Evitar límite superior
            if (currentPos.y > _settings.boundsMax.y - avoidDistance)
            {
                steer.y -= 1f;
            }

            if (steer.magnitude > 0)
            {
                steer = steer.normalized * _settings.maxSpeed - _velocity;
                if (steer.magnitude > _settings.maxForce)
                {
                    steer = steer.normalized * _settings.maxForce;
                }
            }

            return steer;
        }
    }
}

