using UnityEngine;

public class LanzamientoParabolicoFisica : MonoBehaviour
{
    // Velocidad inicial
    [Header("Parámetros de disparo")]
    public float launchSpeed = 10f;                
    // Ángulo de lanzamiento en grados
    [Range(-89f, 89f)] public float launchAngle = 45f; 
    // Magnitud de la gravedad
    public float gravity = 9.81f;                  
    
    //Segundos de simulacion para dibujar
    public float simTime = 2f; 
    //Pasos a dibujar
    public int steps = 30;
    
    public Transform bala;
    
    // Estado interno
    private Vector2 _velocity;
    private float _life;
    private Vector2? initPos = null;

    public void Fire()
    {
        float rad = launchAngle * Mathf.Deg2Rad;

        // Calcula la velocidad inicial
        float vx = launchSpeed * Mathf.Cos(rad);
        float vy = launchSpeed * Mathf.Sin(rad);

        _velocity = new Vector2(vx, vy);
        _life = 0f;
    }

    private void Start()
    {
        initPos = bala.position;
        Fire(); // dispara automáticamente al iniciar
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        Vector2 prevPos = bala.position;

        // Aplicar gravedad manualmente
        _velocity += Vector2.down * gravity * dt;

        // Nueva posición
        Vector2 newPos = prevPos + _velocity * dt;

        // Actualizar posición
        bala.position = newPos;

        // Orientar sprite según la dirección del movimiento
        if (_velocity.sqrMagnitude > 0.0001f)
            bala.right = _velocity.normalized;
    }

    // Gizmos para ver la trayectoria
    private void OnDrawGizmos()
    {
        if (initPos == null)
        {
            initPos = bala.position;
        }
        Gizmos.color = Color.blue;
        Vector2 pos = initPos.Value;
        
        float rad = launchAngle * Mathf.Deg2Rad;
        Vector2 angleVector = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 vel = angleVector * launchSpeed;
        
        Gizmos.DrawLine(pos,pos+(angleVector*(launchSpeed/3)));
        Gizmos.color = Color.yellow;
        float dt = simTime / steps;

        Vector2 prev = pos;
        for (int i = 0; i < steps; i++)
        {
            vel += Vector2.down * gravity * dt;
            Vector2 next = prev + vel * dt;
            Gizmos.DrawLine(prev, next);
            
            prev = next;
        }
    }
}
