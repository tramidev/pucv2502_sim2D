using System.Collections.Generic;
using UnityEngine;

public class CollisionManager2D : MonoBehaviour
{
    public static CollisionManager2D Instance { get; private set; }

    public List<Shape2D> shapes = new List<Shape2D>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Hay más de un CollisionManager2D. Se reemplaza el Instance.");
        }
        Instance = this;
    }

    public void Register(Shape2D s)
    {
        if (s == null) return;
        if (!shapes.Contains(s)) shapes.Add(s);
    }

    public void Unregister(Shape2D s)
    {
        if (s == null) return;
        shapes.Remove(s);
    }

    private void Update()
    {
        int n = shapes.Count;
        for (int i = 0; i < n; i++)
        {
            var A = shapes[i];
            if (A == null) continue;

            for (int j = i + 1; j < n; j++)
            {
                var B = shapes[j];
                if (B == null) continue;

                if (TryCollide(A, B, out Vector2 pointA, out Vector2 normalA))
                {
                    A.OnCollide(new Shape2D.CollisionInfo(true, pointA, normalA, B));
                    B.OnCollide(new Shape2D.CollisionInfo(true, pointA, -normalA, A));
                }
            }
        }
    }

    bool TryCollide(Shape2D A, Shape2D B, out Vector2 contactPoint, out Vector2 normalFromAtoB)
    {
        contactPoint = Vector2.zero;
        normalFromAtoB = Vector2.zero;

        var aKind = A.kind;
        var bKind = B.kind;

        // Circle - Circle
        if (aKind == Shape2D.ShapeKind.Circle && bKind == Shape2D.ShapeKind.Circle)
        {
            if (SimpleCollisionMath2D.CollideCircleCircle(A.Center, A.CircleRadius, B.Center, B.CircleRadius, out var p, out var n))
            {
                contactPoint = p; normalFromAtoB = n; return true;
            }
            return false;
        }

        // Circle - Polygon
        if (aKind == Shape2D.ShapeKind.Circle && (bKind == Shape2D.ShapeKind.Rectangle || bKind == Shape2D.ShapeKind.Triangle))
        {
            var polyB = (bKind == Shape2D.ShapeKind.Rectangle) ? (IList<Vector2>)B.GetRectWorldVerts()
                                                               : (IList<Vector2>)B.GetTriangleWorldVerts();
            if (SimpleCollisionMath2D.CollideCirclePolygon(A.Center, A.CircleRadius, polyB, out var p, out var n))
            { contactPoint = p; normalFromAtoB = n; return true; }
            return false;
        }
        if (bKind == Shape2D.ShapeKind.Circle && (aKind == Shape2D.ShapeKind.Rectangle || aKind == Shape2D.ShapeKind.Triangle))
        {
            var polyA = (aKind == Shape2D.ShapeKind.Rectangle) ? (IList<Vector2>)A.GetRectWorldVerts()
                                                               : (IList<Vector2>)A.GetTriangleWorldVerts();
            if (SimpleCollisionMath2D.CollideCirclePolygon(B.Center, B.CircleRadius, polyA, out var p, out var n))
            { contactPoint = p; normalFromAtoB = n; return true; }
            return false;
        }

        // Polygon - Polygon
        if ((aKind == Shape2D.ShapeKind.Rectangle || aKind == Shape2D.ShapeKind.Triangle) &&
            (bKind == Shape2D.ShapeKind.Rectangle || bKind == Shape2D.ShapeKind.Triangle))
        {
            var polyA = (aKind == Shape2D.ShapeKind.Rectangle) ? (IList<Vector2>)A.GetRectWorldVerts()
                                                               : (IList<Vector2>)A.GetTriangleWorldVerts();
            var polyB = (bKind == Shape2D.ShapeKind.Rectangle) ? (IList<Vector2>)B.GetRectWorldVerts()
                                                               : (IList<Vector2>)B.GetTriangleWorldVerts();

            if (SimpleCollisionMath2D.CollidePolygonPolygon(polyA, polyB, out var p, out var n))
            { contactPoint = p; normalFromAtoB = n; return true; }
            return false;
        }

        return false;
    }
}
