using System;
using UnityEngine;

public class SpaceShip : MonoBehaviour
{
    
    public float speed = 10f; 
    public float mass = 1f;   
    private Vector3 velocity;
    private Vector3 currentGravityForce;

    public void Start()
    {
        velocity = Vector3.right * speed;
    }
    
    void Update()
    {
        velocity += currentGravityForce * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.up = velocity.normalized;
    }
    
    public void ApplyGravity(Vector3 gravityForce)
    {
        currentGravityForce = gravityForce.normalized + currentGravityForce.normalized;
    }
}
