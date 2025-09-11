using System.Collections.Generic;
using UnityEngine;

public class PoolGame : MonoBehaviour
{
    [Header("Referencias de escena")]
    [Tooltip("SpriteRenderer de la mesa en la escena")]
    public SpriteRenderer tableSprite;

    [Tooltip("Bolas en la mesa (todas con el script Ball)")]
    public List<Ball> balls = new List<Ball>();

    [Tooltip("Bola blanca")]
    public Ball cue;

    [Header("Parámetros de física y juego")]
    [Tooltip("Coeficiente de restitución contra las bandas")]
    public float cushionRestitution = 0.95f;

    [Tooltip("Coeficiente de restitución entre bolas")]
    public float ballRestitution = 0.98f;

    [Tooltip("Arrastre lineal (por segundo)")]
    public float linearDrag = 0.8f;

    [Tooltip("Velocidad mínima para ‘dormir’ la bola")]
    public float sleepSpeed = 0.05f;

    [Header("Disparo")]
    public float maxShotSpeed = 12f;
    public float shotPower = 1.8f;
    public Color aimColor = new Color(1, 1, 1, 0.7f);

    // ---- Internos ----
    private Rect tableRect;         // rectángulo de la mesa en coordenadas de mundo
    private float ballRadius; // tomado de la escena (extents del sprite de la cue)
    private bool aiming = false;
    private Vector2 aimStartWorld;
    private Vector2 aimCurrentWorld;

    void Start()
    {
        Application.targetFrameRate = 120;

        if (tableSprite == null)
        {
            Debug.LogError("[PoolGame] Falta asignar tableSprite.");
            enabled = false; return;
        }
        if (cue == null)
        {
            Debug.LogError("[PoolGame] Falta asignar la referencia 'cue'.");
            enabled = false; return;
        }
        if (balls == null || balls.Count == 0)
        {
            Debug.LogError("[PoolGame] La lista 'balls' está vacía. Asigna las bolas en el inspector.");
            enabled = false; return;
        }

        // Asegurar que cada Ball tenga referencias mínimas
        foreach (var b in balls)
        {
            if (b == null) continue;
            if (b.sr == null) b.sr = b.GetComponent<SpriteRenderer>();
            if (b.go == null) b.go = b.gameObject;
            if (b.pos == Vector2.zero && b.transform != null)
                b.pos = b.transform.position;
        }

        // Calcular el rectángulo de la mesa desde el SpriteRenderer (bounds en mundo)
        var tb = tableSprite.bounds;
        tableRect = new Rect(tb.min.x, tb.min.y, tb.size.x, tb.size.y);

        // Tomar radio desde la escena (tamaño constante) usando la bola blanca
        if (cue.sr == null) cue.sr = cue.GetComponent<SpriteRenderer>();
        if (cue.sr != null)
            ballRadius = Mathf.Max(cue.sr.bounds.size.x, cue.sr.bounds.size.y)/2;
    }

    void Update()
    {
        HandleInput();

        float dt = Mathf.Min(Time.deltaTime, 1f / 60f);
        StepPhysics(dt);
        SyncTransforms();
    }
    
    void HandleInput()
    {
        if (cue == null) return;
        Vector2 mouseWorld = ScreenToWorld(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            // Solo si clic cerca de la blanca y está casi quieta
            if ((mouseWorld - cue.pos).sqrMagnitude <= (ballRadius * 20f) * (ballRadius * 20f)
                && cue.vel.sqrMagnitude < 0.0001f)
            {
                aiming = true;
                aimStartWorld = mouseWorld;
                aimCurrentWorld = mouseWorld;
            }
        }
        if (aiming)
        {
            aimCurrentWorld = mouseWorld;
            if (Input.GetMouseButtonUp(0))
            {
                Vector2 dir = (aimCurrentWorld - aimStartWorld);
                Vector2 v = dir * shotPower;
                if (v.magnitude > maxShotSpeed) v = v.normalized * maxShotSpeed;
                cue.vel = v;
                aiming = false;
            }
        }
    }
    
    
    void StepPhysics(float dt)
    {
        StepPhysicsWallAndVelocity(cue,dt);
        
        foreach (var b in balls)
        {
            StepPhysicsWallAndVelocity(b,dt);
        }


        StepPhysicsBallColitions(dt);

    }

    void StepPhysicsWallAndVelocity(Ball b,float dt)
    {
        if (b == null) return;
        
        // Drag lineal
        float damp = Mathf.Clamp01(1f - linearDrag * dt);
        b.vel *= damp;

        if (b.vel.magnitude < sleepSpeed) b.vel = Vector2.zero;

        ResolveWallCollision(b);
        
        b.pos += b.vel * dt;
    }
    
    void StepPhysicsBallColitions(float dt)
    {
        List<Ball> allBalls = new List<Ball>();
        allBalls.AddRange(balls);
        allBalls.Add(cue);
        
        int n = allBalls.Count;
        
        
        for (int i = 0; i < n; i++)
        {
            var a = allBalls[i];
            if (a == null) continue;

            for (int j = i + 1; j < n; j++)
            {
                var b = allBalls[j];
                if (b == null) continue;

                ResolveBallCollision(a, b);
            }
        }
    }
    

    // --- Rebote en paredes con reflexión vectorial ---
    void ResolveWallCollision(Ball b)
    {
        float r = ballRadius;

    
        if (b.pos.x - r < tableRect.xMin)
        {
            b.pos.x = tableRect.xMin + r;
            Reflect(b, Vector2.right); // normal hacia dentro de la mesa
        }
        // Derecha
        else if (b.pos.x + r > tableRect.xMax)
        {
            b.pos.x = tableRect.xMax - r;
            Reflect(b, Vector2.left);
        }

        // Abajo
        if (b.pos.y - r < tableRect.yMin)
        {
            b.pos.y = tableRect.yMin + r;
            Reflect(b, Vector2.up);
        }
        // Arriba
        else if (b.pos.y + r > tableRect.yMax)
        {
            b.pos.y = tableRect.yMax - r;
            Reflect(b, Vector2.down);
        }
    }

    void Reflect(Ball b, Vector2 normal)
    {
        normal.Normalize();
        b.vel = b.vel - 2f * Vector2.Dot(b.vel, normal) * normal;
        b.vel *= cushionRestitution;
    }

    // --- Colisión elástica simple entre bolas (masas iguales) ---
    void ResolveBallCollision(Ball a, Ball b)
    {
        Vector2 delta = b.pos - a.pos;
        float sqrDistance = delta.sqrMagnitude;
        float minDist = ballRadius*2f;

        if (sqrDistance > 0f && sqrDistance < (minDist*minDist))
        {
            float dist = Mathf.Sqrt(sqrDistance);
            Vector2 n = delta / dist; // normal desde a hacia b

            // Separación para corregir la penetración
            float penetration = minDist - dist;
            float move = penetration * 0.5f;
            a.pos -= n * move;
            b.pos += n * move;

            // Velocidad relativa a lo largo de la normal
            Vector2 relVel = b.vel - a.vel;
            float relNormal = Vector2.Dot(relVel, n);

            // Si ya se separan, no aplicar impulso
            if (relNormal > 0f) return;
            
            // masas iguales (m=1) -> impulso simplificado
            float j = -(1f + ballRestitution) * relNormal / 2f;

            Vector2 impulse = j * n;
            a.vel -= impulse;
            b.vel += impulse;
        }
    }

    void SyncTransforms()
    {
        //Sync cue
        if (cue != null)
        {
            cue.transform.position = new Vector3(cue.pos.x, cue.pos.y, cue.transform.position.z);
        }

        //Sync Balls
        foreach (var b in balls)
        {
            if (b == null) continue;
            b.transform.position = new Vector3(b.pos.x, b.pos.y, b.transform.position.z);
        }
    }

    Vector2 ScreenToWorld(Vector3 mousePos)
    {
        var cam = Camera.main;
        if (cam == null) return Vector2.zero;
        Vector3 w = cam.ScreenToWorldPoint(mousePos);
        return new Vector2(w.x, w.y);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Contorno de mesa
        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(new Vector3(tableRect.center.x, tableRect.center.y, 0f),
                            new Vector3(tableRect.width, tableRect.height, 0f));

        // Línea de apuntado
        if (aiming && cue != null)
        {
            Gizmos.color = aimColor;
            Vector3 from = new Vector3(cue.pos.x, cue.pos.y, 0f);
            Vector3 to   = new Vector3(aimCurrentWorld.x, aimCurrentWorld.y, 0f);
            Gizmos.DrawLine(from, to);
        }
    }
}