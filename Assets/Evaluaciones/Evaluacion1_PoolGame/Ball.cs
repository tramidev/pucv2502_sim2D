using System;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public Vector2 pos;
    public Vector2 vel;
    public GameObject go;
    public SpriteRenderer sr;


    public void Awake()
    {
        pos = new Vector2(transform.position.x, transform.position.y);
    }
}
