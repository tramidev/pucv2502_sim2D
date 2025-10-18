using UnityEngine;

public class Planet : MonoBehaviour
{
    public SpaceShip spaceShip;
    
    public float gravitationalConstant = 0.5f;
    public float mass = 100f;
    public float gravityRadius = 50f;

    bool inGravity;
    
    
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
            if (!inGravity)
            {
                inGravity = true;
                spaceShip.currPlanets.Add(this);
            }
        }
        else
        {
            if (inGravity)
            {
                inGravity = false;
                spaceShip.currPlanets.Remove(this);
            }
        }
    }
    
    public Vector3 GetGravityForce()
    {
        Vector3 direction = transform.position - spaceShip.transform.position;
        float distance = direction.magnitude;
        float forceMagnitude = gravitationalConstant * (mass * spaceShip.mass) / (distance * distance);
        Vector3 force = direction.normalized * forceMagnitude;
        return force;
    }
}
