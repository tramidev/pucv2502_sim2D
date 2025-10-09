using System;
using System.Collections.Generic;
using UnityEngine;

public class SpaceShip : MonoBehaviour
{
    
    public float speed = 10f; 
    public float mass = 1f;   
    private Vector3 velocity;
    private Vector3 currentGravityForce;

    public List<Planet> currPlanets;

    public void Start()
    {
        velocity = Vector3.right * speed;
    }

    void Update()
    {
        CalculateCurrGravity();
        velocity += currentGravityForce * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.up = velocity.normalized;
    }
    
    public void CalculateCurrGravity()
    {
        currentGravityForce = Vector2.zero;
        foreach (Planet x in currPlanets)
        {
            Vector3 gravityForce = x.GetGravityForce();
            Debug.Log(gravityForce);
            currentGravityForce += gravityForce;
        }
    }
}
