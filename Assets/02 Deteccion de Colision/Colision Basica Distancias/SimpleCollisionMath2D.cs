using UnityEngine;
using System.Collections.Generic;
using PUCV.PhysicEngine2D;
using Unity.VisualScripting;
using CustomCollider2D = PUCV.PhysicEngine2D.CustomCollider2D;

public static class SimpleCollisionMath2D
{
    // -------- Helpers geométricos
    public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;

    public static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f);
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    public static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // Barycentric
        Vector2 v0 = c - a, v1 = b - a, v2 = p - a;
        float d00 = Dot(v0, v0);
        float d01 = Dot(v0, v1);
        float d11 = Dot(v1, v1);
        float d20 = Dot(v2, v0);
        float d21 = Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        if (Mathf.Abs(denom) < 1e-8f) return false;
        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;
        return (u >= 0f && v >= 0f && w >= 0f);
    }

    public static bool PointInOBB(Vector2 p, Vector2 center, Vector2 half, Vector2 right, Vector2 up)
    {
        Vector2 d = p - center;
        float pr = Mathf.Abs(Dot(d, right));
        float pu = Mathf.Abs(Dot(d, up));
        return pr <= half.x + 1e-6f && pu <= half.y + 1e-6f;
    }

    public static Vector2 ClosestPointOnPolygon(Vector2 p, IList<Vector2> poly, out int edgeIndex)
    {
        float minDist2 = float.PositiveInfinity;
        Vector2 closest = p;
        edgeIndex = -1;
        for (int i = 0; i < poly.Count; i++)
        {
            var a = poly[i];
            var b = poly[(i + 1) % poly.Count];
            var q = ClosestPointOnSegment(p, a, b);
            float d2 = (q - p).sqrMagnitude;
            if (d2 < minDist2)
            {
                minDist2 = d2;
                closest = q;
                edgeIndex = i;
            }
        }
        return closest;
    }

    // -------- Proyección de polígono sobre eje
    private static void Project(IList<Vector2> poly, Vector2 axis, out float min, out float max)
    {
        float first = Dot(poly[0], axis);
        min = max = first;
        for (int i = 1; i < poly.Count; i++)
        {
            float p = Dot(poly[i], axis);
            if (p < min) min = p;
            if (p > max) max = p;
        }
    }

    private static bool OverlapOnAxis(IList<Vector2> A, IList<Vector2> B, Vector2 axis, ref float minOverlap, ref Vector2 smallestAxis)
    {
        if (axis.sqrMagnitude < 1e-10f) return true;
        axis.Normalize();
        Project(A, axis, out float minA, out float maxA);
        Project(B, axis, out float minB, out float maxB);

        float overlap = Mathf.Min(maxA, maxB) - Mathf.Max(minA, minB);
        if (overlap < 0f) return false;
        if (overlap < minOverlap)
        {
            minOverlap = overlap;
            smallestAxis = axis;
        }
        return true;
    }

    // -------- Colisiones

    // Circle-Circle
    public static bool CollideCircleCircle(Vector2 c1, float r1, Vector2 c2, float r2, out Vector2 point, out Vector2 normal)
    {
        Vector2 d = c2 - c1;
        float dist = d.magnitude;
        float sum = r1 + r2;

        if (dist > sum && dist > 1e-6f)
        {
            point = Vector2.zero; normal = Vector2.zero;
            return false;
        }

        // Si están exactamente superpuestos, elegimos una normal arbitraria
        normal = dist > 1e-6f ? d / dist : Vector2.right;
        float t1 = Mathf.Clamp(r1, 0f, sum);
        point = c1 + normal * t1; // punto de contacto del lado del primer círculo
        return true;
    }

    // Circle-Polygon (rect o tri)
    public static bool CollideCirclePolygon(Vector2 cc, float r, IList<Vector2> poly, out Vector2 point, out Vector2 normal)
    {
        // 1) Punto más cercano del polígono al centro del círculo
        int edgeIdx;
        Vector2 closest = ClosestPointOnPolygon(cc, poly, out edgeIdx);
        Vector2 v = cc - closest;
        float dist = v.magnitude;

        // 2) ¿el centro está dentro del polígono? => penetración "desde dentro"
        bool inside = PointInPolygon(cc, poly);
        if (inside)
        {
            // Normal = normal de la arista más cercana (hacia afuera), punto = proyección del centro sobre esa arista
            Vector2 a = poly[edgeIdx];
            Vector2 b = poly[(edgeIdx + 1) % poly.Count];
            Vector2 e = (b - a).normalized;
            Vector2 n = new Vector2(-e.y, e.x); // normal "izquierda"
            // Asegurar que la normal apunte desde polígono hacia el círculo (del polígono al centro)
            if (Dot(n, cc - closest) < 0f) n = -n;

            point = closest;
            normal = n;
            return true;
        }

        if (dist <= r)
        {
            normal = dist > 1e-6f ? v / dist : (cc - PolygonCentroid(poly)).normalized;
            point = closest;
            return true;
        }

        point = Vector2.zero;
        normal = Vector2.zero;
        return false;
    }

    // Polygon-Polygon (SAT básico)
    public static bool CollidePolygonPolygon(IList<Vector2> A, IList<Vector2> B, out Vector2 point, out Vector2 normal)
    {
        float minOverlap = float.PositiveInfinity;
        Vector2 smallestAxis = Vector2.zero;

        // Probar ejes de A
        for (int i = 0; i < A.Count; i++)
        {
            Vector2 edge = A[(i + 1) % A.Count] - A[i];
            Vector2 axis = new Vector2(-edge.y, edge.x);
            if (!OverlapOnAxis(A, B, axis, ref minOverlap, ref smallestAxis))
            {
                point = Vector2.zero; normal = Vector2.zero;
                return false;
            }
        }
        // Probar ejes de B
        for (int i = 0; i < B.Count; i++)
        {
            Vector2 edge = B[(i + 1) % B.Count] - B[i];
            Vector2 axis = new Vector2(-edge.y, edge.x);
            if (!OverlapOnAxis(A, B, axis, ref minOverlap, ref smallestAxis))
            {
                point = Vector2.zero; normal = Vector2.zero;
                return false;
            }
        }

        // Hay intersección. Normal = eje de menor solape, dirección desde A hacia B
        Vector2 ca = PolygonCentroid(A);
        Vector2 cb = PolygonCentroid(B);
        normal = smallestAxis;
        if (Dot(normal, cb - ca) < 0f) normal = -normal;

        // Punto de contacto aproximado: punto más cercano entre polígonos sobre la normal
        int ia; Vector2 pa = ClosestPointOnPolygonToAxis(A, normal, out ia);
        int ib; Vector2 pb = ClosestPointOnPolygonToAxis(B, -normal, out ib);
        point = (pa + pb) * 0.5f;

        return true;
    }

    // -------- Utilidades de polígono

    public static bool PointInPolygon(Vector2 p, IList<Vector2> poly)
    {
        // Ray casting
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector2 pi = poly[i];
            Vector2 pj = poly[j];
            bool intersect = ((pi.y > p.y) != (pj.y > p.y)) &&
                             (p.x < (pj.x - pi.x) * (p.y - pi.y) / ((pj.y - pi.y) + 1e-12f) + pi.x);
            if (intersect) inside = !inside;
        }
        return inside;
    }

    public static Vector2 PolygonCentroid(IList<Vector2> poly)
    {
        float signedArea = 0f;
        float cx = 0f, cy = 0f;
        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[(i + 1) % poly.Count];
            float cross = a.x * b.y - b.x * a.y;
            signedArea += cross;
            cx += (a.x + b.x) * cross;
            cy += (a.y + b.y) * cross;
        }
        signedArea *= 0.5f;
        if (Mathf.Abs(signedArea) < 1e-8f) return poly[0];
        return new Vector2(cx / (6f * signedArea), cy / (6f * signedArea));
    }

    private static Vector2 ClosestPointOnPolygonToAxis(IList<Vector2> poly, Vector2 axis, out int idx)
    {
        float min = float.PositiveInfinity;
        idx = -1;
        Vector2 best = poly[0];
        for (int i = 0; i < poly.Count; i++)
        {
            float d = Mathf.Abs(Dot(poly[i], axis));
            if (d < min)
            {
                min = d;
                best = poly[i];
                idx = i;
            }
        }
        return best;
    }
    
    public static List<InternalCollisionInfo> DetectCollisions(List<CustomCollider2D> colliders)
    {
        List<InternalCollisionInfo> collisions = new List<InternalCollisionInfo>();

        for (int i = 0; i < colliders.Count; i++)
        {
            for (int j = i + 1; j < colliders.Count; j++)
            {
                CustomCollider2D colA = colliders[i];
                CustomCollider2D colB = colliders[j];
                
                bool collisionFound = false;
                Vector2 point;
                Vector2 normal;

                if (colA.type == CustomCollider2D.ShapeType.Circle && colB.type == CustomCollider2D.ShapeType.Circle)
                {
                    if (CollideCircleCircle(
                            colA.Center, 
                            colA.CircleRadius, 
                            colB.Center, 
                            colB.CircleRadius, 
                            out point, 
                            out normal
                            ))
                    {
                        collisionFound = true;
                    }
                }
                else if(colA.type != CustomCollider2D.ShapeType.Circle && colB.type != CustomCollider2D.ShapeType.Circle)
                {
                    IList<Vector2> polyA = colA.GetPolygonVertices();
                    IList<Vector2> polyB = colB.GetPolygonVertices();

                    if (CollidePolygonPolygon(polyA, polyB, out point, out normal))
                    {
                        collisionFound = true;
                    }
                }
                else
                {
                    if (colA.type == CustomCollider2D.ShapeType.Circle)
                    {
                        IList<Vector2> polyB = colB.GetPolygonVertices();
                        if (CollideCirclePolygon(
                                colA.Center, 
                                colA.CircleRadius, 
                                polyB, 
                                out point,
                                out normal)
                            )
                        {
                            collisionFound = true;
                        }
                    }
                    else
                    {
                        IList<Vector2> polyA = colA.GetPolygonVertices();
                        if(CollideCirclePolygon(
                               colB.Center, 
                               colB.CircleRadius, 
                               polyA,
                                out point,
                                out normal)
                           )
                        {
                            collisionFound = true;
                        }
                    }
                }
                
                if (collisionFound)
                {
                    var collision = new InternalCollisionInfo(colA,colB,point,normal);
                    collisions.Add(collision);
                }
            }
        }

        return collisions;
    }
}
