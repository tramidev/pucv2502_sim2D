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
        
        private List<Boid> _birds = new List<Boid>();

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
                
                // Crear componente Boid
                Boid boid = birdGO.AddComponent<Boid>();
                boid.Initialize(this, boidSettings);
                
                _birds.Add(boid);
            }
        }

        private void FixedUpdate()
        {
            if (_birds.Count == 0) return;
            
            // Actualizar cada pájaro
            foreach (var bird in _birds)
            {
                bird.UpdateBoid(_birds);
            }
        }

        public List<Boid> GetAllBirds()
        {
            return _birds;
        }

        public BoidSettings GetBoidSettings()
        {
            return boidSettings;
        }
    }
}
