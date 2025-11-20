using System.Collections.Generic;
using UnityEngine;

namespace PUCV.PhysicEngine2D
{
    public class FlockController : MonoBehaviour
    {
        [System.Serializable]
        public class BoidSettings
        {
            public float separationRadius = 1f;
            public float alignmentRadius = 2f;
            public float cohesionRadius = 3f;
            
            public float separationWeight = 1.5f;
            public float alignmentWeight = 1f;
            public float cohesionWeight = 1f;
            
            public float maxSpeed = 5f;
            public float maxForce = 0.2f;
            
            public Vector2 boundsMin = new Vector2(-50, -50);
            public Vector2 boundsMax = new Vector2(50, 50);
        }

        [SerializeField]
        private int flockSize = 300;
        
        [SerializeField]
        private float birdSize = 0.1f;
        
        [SerializeField]
        private GameObject birdPrefab;
        
        [SerializeField]
        private BoidSettings boidSettings = new BoidSettings();
        
        private List<Bird> _birds = new List<Bird>();

        private void Awake()
        {
            // Si no hay un prefab asignado, usar un gameobject vacío
            if (birdPrefab == null)
            {
                birdPrefab = new GameObject("BirdPrefab");
                birdPrefab.AddComponent<SpriteRenderer>();
            }
        }

        private void Start()
        {
            CreateFlock();
        }

        public void Initialize(GameObject initialGameObject)
        {
            birdPrefab = initialGameObject;
        }

        private void CreateFlock()
        {
            // Crear los pájaros de la bandada
            for (int i = 0; i < flockSize; i++)
            {
                GameObject birdGO = Instantiate(birdPrefab, transform);
                birdGO.name = $"Bird_{i}";
                
                // Configurar escala
                birdGO.transform.localScale = Vector3.one * birdSize;
                
                // Posición aleatoria
                Vector3 randomPos = new Vector3(
                    Random.Range(-10f, 10f),
                    Random.Range(-10f, 10f),
                    0
                );
                birdGO.transform.position = randomPos;
                
                // Obtener o crear CustomRigidbody2D
                CustomRigidbody2D rigidbody = birdGO.GetComponent<CustomRigidbody2D>();
                if (rigidbody == null)
                {
                    rigidbody = birdGO.AddComponent<CustomRigidbody2D>();
                }
                
                // Crear componente Bird
                Bird bird = birdGO.AddComponent<Bird>();
                bird.Initialize(rigidbody);
                
                _birds.Add(bird);
            }
        }

        private void FixedUpdate()
        {
            if (_birds.Count == 0) return;
            
            // Calcular fuerzas para todos los pájaros
            foreach (var bird in _birds)
            {
                Vector2 separationForce = CalculateSeparation(bird);
                Vector2 alignmentForce = CalculateAlignment(bird);
                Vector2 cohesionForce = CalculateCohesion(bird);
                Vector2 avoidBoundsForce = CalculateAvoidBounds(bird);
                
                // Aplicar fuerzas
                bird.ApplyForces(separationForce, alignmentForce, cohesionForce, avoidBoundsForce, boidSettings);
            }
            
            // Aplicar movimiento a todos los pájaros
            foreach (var bird in _birds)
            {
                bird.UpdatePosition(boidSettings);
            }
        }

        private Vector2 CalculateSeparation(Bird bird)
        {
            Vector2 steer = Vector2.zero;
            int count = 0;
            Vector2 myPos = bird.GetWorldPosition();
            
            foreach (var other in _birds)
            {
                if (other == bird) continue;
                
                Vector2 otherPos = other.GetWorldPosition();
                float distance = Vector2.Distance(myPos, otherPos);
                
                if (distance < boidSettings.separationRadius && distance > 0)
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
                    steer = steer.normalized * boidSettings.maxSpeed - bird.GetVelocity();
                    if (steer.magnitude > boidSettings.maxForce)
                    {
                        steer = steer.normalized * boidSettings.maxForce;
                    }
                }
            }
            
            return steer;
        }

        private Vector2 CalculateAlignment(Bird bird)
        {
            Vector2 avgVelocity = Vector2.zero;
            int count = 0;
            Vector2 myPos = bird.GetWorldPosition();
            
            foreach (var other in _birds)
            {
                if (other == bird) continue;
                
                Vector2 otherPos = other.GetWorldPosition();
                float distance = Vector2.Distance(myPos, otherPos);
                
                if (distance < boidSettings.alignmentRadius)
                {
                    avgVelocity += other.GetVelocity();
                    count++;
                }
            }
            
            if (count > 0)
            {
                avgVelocity /= count;
                avgVelocity = avgVelocity.normalized * boidSettings.maxSpeed;
                Vector2 steer = avgVelocity - bird.GetVelocity();
                
                if (steer.magnitude > boidSettings.maxForce)
                {
                    steer = steer.normalized * boidSettings.maxForce;
                }
                return steer;
            }
            
            return Vector2.zero;
        }

        private Vector2 CalculateCohesion(Bird bird)
        {
            Vector2 centerMass = Vector2.zero;
            int count = 0;
            Vector2 myPos = bird.GetWorldPosition();
            
            foreach (var other in _birds)
            {
                if (other == bird) continue;
                
                Vector2 otherPos = other.GetWorldPosition();
                float distance = Vector2.Distance(myPos, otherPos);
                
                if (distance < boidSettings.cohesionRadius)
                {
                    centerMass += otherPos;
                    count++;
                }
            }
            
            if (count > 0)
            {
                centerMass /= count;
                Vector2 direction = (centerMass - myPos).normalized;
                Vector2 steer = direction * boidSettings.maxSpeed - bird.GetVelocity();
                
                if (steer.magnitude > boidSettings.maxForce)
                {
                    steer = steer.normalized * boidSettings.maxForce;
                }
                return steer;
            }
            
            return Vector2.zero;
        }

        private Vector2 CalculateAvoidBounds(Bird bird)
        {
            Vector2 steer = Vector2.zero;
            float avoidDistance = 5f;
            Vector2 currentPos = bird.GetWorldPosition();

            if (currentPos.x < boidSettings.boundsMin.x + avoidDistance)
            {
                steer.x += 1f;
            }
            if (currentPos.x > boidSettings.boundsMax.x - avoidDistance)
            {
                steer.x -= 1f;
            }

            if (currentPos.y < boidSettings.boundsMin.y + avoidDistance)
            {
                steer.y += 1f;
            }
            if (currentPos.y > boidSettings.boundsMax.y - avoidDistance)
            {
                steer.y -= 1f;
            }

            if (steer.magnitude > 0)
            {
                steer = steer.normalized * boidSettings.maxSpeed - bird.GetVelocity();
                if (steer.magnitude > boidSettings.maxForce)
                {
                    steer = steer.normalized * boidSettings.maxForce;
                }
            }

            return steer;
        }

        public List<Bird> GetAllBirds()
        {
            return _birds;
        }

        public BoidSettings GetBoidSettings()
        {
            return boidSettings;
        }
    }
}
