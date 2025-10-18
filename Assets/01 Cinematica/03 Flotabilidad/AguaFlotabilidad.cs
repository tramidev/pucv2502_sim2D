using UnityEngine;

public class AguaFlotabilidad : MonoBehaviour
{
    private SpriteRenderer sr;
    public float density = 1f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }
    
    public Bounds WorldBounds => sr.bounds;
    
    public float SurfaceY => sr.bounds.max.y;
}
