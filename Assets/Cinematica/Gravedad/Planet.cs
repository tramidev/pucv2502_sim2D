using UnityEngine;

public class Planet : MonoBehaviour
{
    public SpaceShip spaceShip;
    
    public float gravitationalConstant = 0.5f;
    public float mass = 100f;           
    public float gravityRadius = 50f;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = transform.position - spaceShip.transform.position;
        float distance = direction.magnitude;
        
        if (distance < gravityRadius && distance > 0.1f)
        {
            // Ley de la gravitación universal simplificada: F = G * (m1 * m2) / r^2
            float forceMagnitude = gravitationalConstant * (mass * spaceShip.mass) / (distance * distance);
            Vector3 force = direction.normalized * forceMagnitude;

            // Aplica la fuerza de gravedad a la nave
            spaceShip.ApplyGravity(force);
        }
    }
}
