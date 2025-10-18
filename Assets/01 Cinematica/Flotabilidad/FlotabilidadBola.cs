using UnityEngine;

public class FlotabilidadBola : MonoBehaviour
{

    public float mass = 1f;               // m: masa de la pelota
    public float volume = 0.5f;           // V: volumen del objeto (elige un valor acorde a su tamaño)
    public float initialSpeed;       // Velocidad inicial
    
    public float gravityMagnitude = 9.81f;   // g
    public float dampingInWater = 0.9f;      // amortiguación al estar en agua (0..1)
    public float dampingInAir = 0.999f;      // amortiguación en aire (0..1)

    [Header("Referencias")]
    public AguaFlotabilidad water;

    private SpriteRenderer sr;
    private Vector2 velocity;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        velocity = initialSpeed * Vector2.down;
    }

    void Update()
    {
        if (!water) return;

        float dt = Time.deltaTime;
        Bounds ball = sr.bounds;
        Bounds waterBounds = water.WorldBounds;

        // Fuerza de gravedad
        Vector2 gravity = new Vector2(0f, -gravityMagnitude);
        Vector2 downForce = mass * gravity; // Fg = m * g (hacia abajo)

        // ¿Hay intersección entre agua y pelota?
        if (ball.Intersects(waterBounds))
        {
            // Solapamiento vertical
            float overlapBottom = Mathf.Max(ball.min.y, waterBounds.min.y);
            float overlapTop    = Mathf.Min(ball.max.y, waterBounds.max.y);
            float overlapHeight = Mathf.Max(0f, overlapTop - overlapBottom);

            // Solapamiento horizontal
            float overlapLeft   = Mathf.Max(ball.min.x, waterBounds.min.x);
            float overlapRight  = Mathf.Min(ball.max.x, waterBounds.max.x);
            float overlapWidth  = Mathf.Max(0f, overlapRight - overlapLeft);

            float fracVertical   = Mathf.Clamp01(overlapHeight / ball.size.y);
            float fracHorizontal = Mathf.Clamp01(overlapWidth  / ball.size.x);

            // Fracción sumergida aproximada por área de bounds (mejor que solo vertical)
            float submergedFraction = Mathf.Clamp01(fracVertical * fracHorizontal);

            // Volumen desplazado (Arquímedes)
            float displacedVolume = volume * submergedFraction;

            // Empuje: Fb = ρ * V_desplazado * g  (hacia arriba)
            float buoyantMag = water.density * displacedVolume * gravityMagnitude;
            Vector2 buoyantForce = new Vector2(0f, buoyantMag);

            downForce += buoyantForce;

            // Amortiguación dentro del agua (aplicada a la velocidad)
            velocity *= Mathf.Clamp01(Mathf.Lerp(1f, dampingInWater, submergedFraction));
        }
        else
        {
            // Amortiguación en aire
            velocity *= Mathf.Clamp01(dampingInAir);
        }
        
        Vector2 acceleration = downForce / mass;
        velocity += acceleration * dt;
        transform.position += (Vector3)(velocity * dt);
    }
}
