using UnityEngine;

public class LanzamientoParabolaFija : MonoBehaviour
{
    [Header("Puntos de referencia")]
    public Transform startPoint;    // Punto inicial
    public Transform endPoint;      // Punto final
    
    public Transform bala;

    [Header("Parábola")]
    public float height = 3f;       // Altura extra en el vértice
    public float duration = 2f;     // Tiempo total de recorrido

    [Header("Gizmos")]
    public Color gizmoColor = Color.yellow;
    public int gizmoSegments = 30;

    private float _time;

    private void Start()
    {
        if (startPoint != null)
            bala.position = startPoint.position;
        _time = 0f;
    }

    private void Update()
    {
        if (startPoint == null || endPoint == null) return;

        _time += Time.deltaTime;
        float t = Mathf.Clamp01(_time / duration);

        // Definir el vértice en el medio entre start y end, elevado en "height"
        Vector2 mid = (startPoint.position + endPoint.position) * 0.5f;
        mid.y += height;

        // Fórmula cuadrática (parábola exacta por 3 puntos)
        Vector2 pos = Mathf.Pow(1 - t, 2) * (Vector2)startPoint.position +
                      2 * (1 - t) * t * mid +
                      Mathf.Pow(t, 2) * (Vector2)endPoint.position;

        bala.position = pos;
    }

    private void OnDrawGizmos()
    {
        if (startPoint == null || endPoint == null) return;

        Vector2 p0 = startPoint.position;
        Vector2 p2 = endPoint.position;
        Vector2 mid = (p0 + p2) * 0.5f;
        mid.y += height; // vértice

        Gizmos.color = gizmoColor;

        Vector2 prev = p0;
        for (int i = 1; i <= gizmoSegments; i++)
        {
            float t = i / (float)gizmoSegments;
            Vector2 pos = Mathf.Pow(1 - t, 2) * p0 +
                          2 * (1 - t) * t * mid +
                          Mathf.Pow(t, 2) * p2;

            Gizmos.DrawLine(prev, pos);
            prev = pos;
        }
    }
}
