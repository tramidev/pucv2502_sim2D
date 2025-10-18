using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class Shape2D : MonoBehaviour
{
    public enum ShapeKind { Circle, Triangle, Rectangle }

    public struct CollisionInfo
    {
        public bool collided;
        public Vector2 point;    // Punto de contacto (mundo)
        public Vector2 normal;   // Normal desde "self" hacia "other"
        public Shape2D other;    // Otra figura

        public CollisionInfo(bool collided, Vector2 point, Vector2 normal, Shape2D other)
        {
            this.collided = collided;
            this.point = point;
            this.normal = normal.sqrMagnitude > 0f ? normal.normalized : Vector2.up;
            this.other = other;
        }
    }

    [Header("Forma")]
    public ShapeKind kind = ShapeKind.Rectangle;

    [Header("Triángulo (puntos en espacio local)")]
    public Vector2 triA = new Vector2(-0.5f, -0.3f);
    public Vector2 triB = new Vector2( 0.5f, -0.3f);
    public Vector2 triC = new Vector2( 0.0f,  0.6f);

    [Header("Overrides opcionales")]
    public float circleRadiusOverride = -1f;             // < 0 => del sprite
    public Vector2 rectSizeOverride = Vector2.one * -1f; // < 0 => del sprite

    private SpriteRenderer _sr;
    public SpriteRenderer SR => _sr ??= GetComponent<SpriteRenderer>();

    public Vector2 Center => (Vector2)transform.position;
    public float RotationDeg => transform.eulerAngles.z;
    public Quaternion Rot => transform.rotation;

    // ---- Suscripción automática al manager ----
    private bool _registered = false;

    private void Start()
    {
        // Suscribirse cuando inicie la escena
        var mgr = CollisionManager2D.Instance;
        if (mgr != null)
        {
            mgr.Register(this);
            _registered = true;
        }
        else
        {
            Debug.LogWarning($"[{name}] No hay CollisionManager2D en la escena. La figura no se registró.");
        }
    }

    private void OnEnable()
    {
        // En caso de volver a habilitarse, intenta suscribirse si aún no lo está
        if (!_registered && CollisionManager2D.Instance != null)
        {
            CollisionManager2D.Instance.Register(this);
            _registered = true;
        }
    }

    private void OnDisable()
    {
        if (_registered && CollisionManager2D.Instance != null)
        {
            CollisionManager2D.Instance.Unregister(this);
            _registered = false;
        }
    }

    private void OnDestroy()
    {
        if (_registered && CollisionManager2D.Instance != null)
        {
            CollisionManager2D.Instance.Unregister(this);
            _registered = false;
        }
    }
    // -------------------------------------------

    // Radio del círculo en mundo
    public float CircleRadius
    {
        get
        {
            if (kind != ShapeKind.Circle) return 0f;
            if (circleRadiusOverride > 0f) return circleRadiusOverride;

            var size = SR.sprite != null ? SR.sprite.bounds.size : Vector3.one;
            var s = transform.lossyScale;
            float rx = Mathf.Abs(size.x * s.x) * 0.5f;
            float ry = Mathf.Abs(size.y * s.y) * 0.5f;
            return (rx + ry) * 0.5f;
        }
    }

    // Half-extents del rectángulo (OBB)
    public Vector2 RectHalf
    {
        get
        {
            if (kind != ShapeKind.Rectangle) return Vector2.zero;
            if (rectSizeOverride.x > 0f && rectSizeOverride.y > 0f)
                return rectSizeOverride * 0.5f;

            var size = SR.sprite != null ? (Vector2)SR.sprite.bounds.size : Vector2.one;
            var s = (Vector2)transform.lossyScale;
            return new Vector2(Mathf.Abs(size.x * s.x) * 0.5f, Mathf.Abs(size.y * s.y) * 0.5f);
        }
    }

    // Vértices rectángulo (OBB) en mundo (CCW)
    public Vector2[] GetRectWorldVerts()
    {
        Vector2 h = RectHalf;
        Vector2 c = Center;
        Vector2 right = transform.right;
        Vector2 up = transform.up;

        return new Vector2[]
        {
            c +  right * h.x + up * h.y, // top-right
            c + -right * h.x + up * h.y, // top-left
            c + -right * h.x - up * h.y, // bottom-left
            c +  right * h.x - up * h.y  // bottom-right
        };
    }

    // Vértices triángulo en mundo (CCW)
    public Vector2[] GetTriangleWorldVerts()
    {
        var s = transform.lossyScale;
        Vector2 A = (Vector2)(Rot * Vector3.Scale(new Vector3(triA.x, triA.y, 0f), new Vector3(s.x, s.y, 1f))) + Center;
        Vector2 B = (Vector2)(Rot * Vector3.Scale(new Vector3(triB.x, triB.y, 0f), new Vector3(s.x, s.y, 1f))) + Center;
        Vector2 C = (Vector2)(Rot * Vector3.Scale(new Vector3(triC.x, triC.y, 0f), new Vector3(s.x, s.y, 1f))) + Center;
        return new Vector2[] { A, B, C };
    }

    // Callback de colisión
    public virtual void OnCollide(CollisionInfo info)
    {
        if (!info.collided) return;
        Debug.DrawLine(info.point, info.point + info.normal * 0.5f, Color.magenta, 0.02f, false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        switch (kind)
        {
            case ShapeKind.Circle:
                UnityEditor.Handles.color = Color.cyan;
                UnityEditor.Handles.DrawWireDisc(Center, Vector3.forward, CircleRadius);
                break;
            case ShapeKind.Rectangle:
                var r = GetRectWorldVerts();
                for (int i = 0; i < r.Length; i++)
                    Gizmos.DrawLine(r[i], r[(i + 1) % r.Length]);
                break;
            case ShapeKind.Triangle:
                var t = GetTriangleWorldVerts();
                for (int i = 0; i < 3; i++)
                    Gizmos.DrawLine(t[i], t[(i + 1) % 3]);
                break;
        }
    }
#endif
}

