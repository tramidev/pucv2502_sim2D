using UnityEngine;

public class CPUBallManagerSimple : MonoBehaviour
{
    [Header("Ball Settings")]
    public GameObject ballPrefab;
    public int ballCount = 200;
    public float ballRadius = 0.25f;
    public float minSpeed = 2f;
    public float maxSpeed = 5f;

    [Header("Simulation Area (World Space)")]
    public Vector2 areaMin = new Vector2(-9f, -5f);
    public Vector2 areaMax = new Vector2(9f, 5f);
    
    struct BallData
    {
        public Vector2 position;
        public Vector2 velocity;
    }

    private Transform[] _ballTransforms;
    private BallData[]  _ballDataCPU;


    void Start()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("Assign Ball Prefab in inspector.");
            enabled = false;
            return;
        }

        CreateBalls();
    }
    
    void CreateBalls()
    {
        _ballTransforms = new Transform[ballCount];
        _ballDataCPU    = new BallData[ballCount];

        for (int i = 0; i < ballCount; i++)
        {
            // Instantiate ball GameObject
            GameObject go = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
            go.name = "Ball_" + i;
            _ballTransforms[i] = go.transform;

            // Random position
            Vector2 pos = new Vector2(
                Random.Range(areaMin.x + ballRadius, areaMax.x - ballRadius),
                Random.Range(areaMin.y + ballRadius, areaMax.y - ballRadius)
            );

            // Random velocity
            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = Random.Range(minSpeed, maxSpeed);
            Vector2 vel = dir * speed;

            _ballDataCPU[i].position = pos;
            _ballDataCPU[i].velocity = vel;

            _ballTransforms[i].position = pos;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 1) Integrate positions and handle wall collisions (CPU)
        for (int i = 0; i < ballCount; i++)
        {
            BallData b = _ballDataCPU[i];

            // Integrate
            b.position += b.velocity * dt;

            // Walls - X axis
            if (b.position.x - ballRadius < areaMin.x)
            {
                b.position.x = areaMin.x + ballRadius;
                b.velocity.x *= -1f;
            }
            else if (b.position.x + ballRadius > areaMax.x)
            {
                b.position.x = areaMax.x - ballRadius;
                b.velocity.x *= -1f;
            }

            // Walls - Y axis
            if (b.position.y - ballRadius < areaMin.y)
            {
                b.position.y = areaMin.y + ballRadius;
                b.velocity.y *= -1f;
            }
            else if (b.position.y + ballRadius > areaMax.y)
            {
                b.position.y = areaMax.y - ballRadius;
                b.velocity.y *= -1f;
            }

            _ballDataCPU[i] = b;
        }

        // 2) Ball-ball collisions
        float doubleR = ballRadius * 2f;
        float maxDistSq = doubleR * doubleR;

        for (int i = 0; i < ballCount; i++)
        {
            for (int j = i + 1; j < ballCount; j++)
            {
                BallData a = _ballDataCPU[i];
                BallData b = _ballDataCPU[j];

                Vector2 delta = b.position - a.position;
                float distSq = delta.sqrMagnitude;

                if (distSq > 0f && distSq < maxDistSq)
                {
                    float dist = Mathf.Sqrt(distSq);
                    Vector2 n = delta / dist;   // normal from a to b
                    float penetration = doubleR - dist;

                    // Separate them half/half
                    Vector2 correction = n * (penetration * 0.5f);
                    a.position -= correction;
                    b.position += correction;

                    // Simple elastic response along the normal, equal mass
                    Vector2 relVel = a.velocity - b.velocity;
                    float vn = Vector2.Dot(relVel, n);

                    if (vn < 0f) // only if moving toward each other
                    {
                        // Exchange normal components (elastic, equal mass)
                        float impulse = -vn;
                        Vector2 impulseVec = impulse * n;

                        a.velocity += impulseVec;
                        b.velocity -= impulseVec;
                    }

                    _ballDataCPU[i] = a;
                    _ballDataCPU[j] = b;
                }
            }
        }

        // 3) Apply positions to transforms
        for (int i = 0; i < ballCount; i++)
        {
            _ballTransforms[i].position = _ballDataCPU[i].position;
        }
    }
}
