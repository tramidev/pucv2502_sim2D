using UnityEngine;
using System.Collections.Generic;
using PUCV.PhysicEngine2D;
using CustomCollider2D = PUCV.PhysicEngine2D.CustomCollider2D;

public static class SimpleCollisionMath2D
{
    // -------- Helpers geométricos
    // cross product 2D
    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    // Intersección de segmentos (helper)
    private static bool SegmentsIntersect(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        Vector2 r = p2 - p;
        Vector2 s = q2 - q;
        float rxs = Cross(r, s);
        float q_pxr = Cross(q - p, r);

        if (Mathf.Abs(rxs) < 1e-12f)
        {
            // Paralelos o colineales
            if (Mathf.Abs(q_pxr) < 1e-12f)
            {
                float rdotr = Dot(r, r);
                if (rdotr < 1e-12f) return false;
                float t0 = Dot(q - p, r) / rdotr;
                float t1 = t0 + Dot(s, r) / rdotr;
                float tmin = Mathf.Max(0f, Mathf.Min(t0, t1));
                float tmax = Mathf.Min(1f, Mathf.Max(t0, t1));
                if (tmax >= tmin)
                {
                    float tm = (tmin + tmax) * 0.5f;
                    intersection = p + r * tm;
                    return true;
                }
            }
            return false;
        }

        float t = Cross(q - p, s) / rxs;
        float u = Cross(q - p, r) / rxs;
        if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
        {
            intersection = p + t * r;
            return true;
        }
        return false;
    }

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
    public static bool CollideCircleCircle(Vector2 c1, float r1, Vector2 c2, float r2, out Vector2 point, out Vector2 normal, out float depth)
    {
        Vector2 d = c2 - c1;
        float dist = d.magnitude;
        float sum = r1 + r2;

        point = Vector2.zero; normal = Vector2.zero; depth = 0f;
        if (dist >= sum) return false;

        normal = dist > 1e-6f ? d / dist : Vector2.right;
        depth = sum - dist;
        float t1 = Mathf.Clamp(r1, 0f, sum);
        point = c1 + normal * t1;
        return true;
    }

    // Circle-Polygon (rect o tri)
    public static bool CollideCirclePolygon(Vector2 cc, float r, IList<Vector2> poly, out Vector2 point, out Vector2 normal, out float depth)
    {
        // 1) Punto más cercano del polígono al centro del círculo
        int edgeIdx;
        Vector2 closest = ClosestPointOnPolygon(cc, poly, out edgeIdx);
        Vector2 v = cc - closest;
        float dist = v.magnitude;

        bool inside = PointInPolygon(cc, poly);
        point = Vector2.zero; normal = Vector2.zero; depth = 0f;

        if (inside)
        {
            Vector2 a = poly[edgeIdx];
            Vector2 b = poly[(edgeIdx + 1) % poly.Count];
            Vector2 e = (b - a).normalized;
            Vector2 n = new Vector2(-e.y, e.x);
            if (Dot(n, cc - closest) < 0f) n = -n;

            // distancia desde centro hasta la arista
            float dToEdge = dist; // distancia desde centro al borde (si está dentro)
            depth = r + dToEdge; // para sacar el círculo fuera: mover r + d
            point = closest;
            normal = n;
            return true;
        }

        if (dist <= r)
        {
            normal = dist > 1e-6f ? v / dist : (cc - PolygonCentroid(poly)).normalized;
            depth = r - dist;
            point = closest;
            return true;
        }

        return false;
    }

    // Polygon-Polygon (sin SAT) - usa intersección de segmentos y test punto-en-polígono
    public static bool CollidePolygonPolygon(IList<Vector2> A, IList<Vector2> B, out Vector2 point, out Vector2 normal)
    {
        point = Vector2.zero; normal = Vector2.zero;
        if (A == null || B == null || A.Count < 3 || B.Count < 3) return false;

        // 1) Buscar intersección entre aristas
        for (int i = 0; i < A.Count; i++)
        {
            Vector2 a1 = A[i];
            Vector2 a2 = A[(i + 1) % A.Count];
            for (int j = 0; j < B.Count; j++)
            {
                Vector2 b1 = B[j];
                Vector2 b2 = B[(j + 1) % B.Count];
                if (SegmentsIntersect(a1, a2, b1, b2, out Vector2 ip))
                {
                    // Normal aproximada: normal de la arista de A
                    Vector2 ca = PolygonCentroid(A);
                    Vector2 cb = PolygonCentroid(B);
                    Vector2 dir = cb - ca;
                    Vector2 e = a2 - a1;
                    if (e.sqrMagnitude < 1e-12f) continue;
                    Vector2 n = new Vector2(-e.y, e.x).normalized;
                    if (Dot(n, dir) < 0f) n = -n;

                    point = ip;
                    normal = n;
                    return true;
                }
            }
        }

        // 2) Si no hay intersecciones, puede que un polígono esté completamente dentro del otro
        // Comprobar un vértice de A dentro de B
        for (int i = 0; i < A.Count; i++)
        {
            if (PointInPolygon(A[i], B))
            {
                point = A[i];
                // Normal: tomar la arista más cercana en B
                int edgeIdx;
                Vector2 closest = ClosestPointOnPolygon(point, B, out edgeIdx);
                if (edgeIdx >= 0)
                {
                    Vector2 ba = B[(edgeIdx + 1) % B.Count] - B[edgeIdx];
                    Vector2 n = new Vector2(-ba.y, ba.x).normalized;
                    Vector2 dir = PolygonCentroid(B) - PolygonCentroid(A); // from A to B
                    if (Dot(n, dir) < 0f) n = -n;
                    normal = n;
                }
                return true;
            }
        }

        // Comprobar un vértice de B dentro de A
        for (int i = 0; i < B.Count; i++)
        {
            if (PointInPolygon(B[i], A))
            {
                point = B[i];
                int edgeIdx;
                Vector2 closest = ClosestPointOnPolygon(point, A, out edgeIdx);
                if (edgeIdx >= 0)
                {
                    Vector2 aa = A[(edgeIdx + 1) % A.Count] - A[edgeIdx];
                    Vector2 n = new Vector2(-aa.y, aa.x).normalized;
                    Vector2 dir = PolygonCentroid(A) - PolygonCentroid(B); // from B to A
                    if (Dot(n, dir) < 0f) n = -n;
                    normal = n;
                }
                return true;
            }
        }

        return false;
    }

    // Colisiones especializadas
    public static bool CollideTriangleTriangle(IList<Vector2> A, IList<Vector2> B, out Vector2 point, out Vector2 normal)
    {
        // Reusar la lógica general pero optimizada para 3 vértices
        point = Vector2.zero; normal = Vector2.zero;
        if (A == null || B == null || A.Count != 3 || B.Count != 3) return false;

        // 1) Intersecciones entre aristas
        for (int i = 0; i < 3; i++)
        {
            Vector2 a1 = A[i]; Vector2 a2 = A[(i + 1) % 3];
            for (int j = 0; j < 3; j++)
            {
                Vector2 b1 = B[j]; Vector2 b2 = B[(j + 1) % 3];
                if (SegmentsIntersect(a1, a2, b1, b2, out Vector2 ip))
                {
                    Vector2 e = a2 - a1; if (e.sqrMagnitude < 1e-12f) continue;
                    Vector2 n = new Vector2(-e.y, e.x).normalized;
                    Vector2 dir = PolygonCentroid(B) - PolygonCentroid(A);
                    if (Dot(n, dir) < 0f) n = -n;
                    point = ip; normal = n; return true;
                }
            }
        }

        // 2) Vértice de A dentro de B
        for (int i = 0; i < 3; i++) if (PointInTriangle(A[i], B[0], B[1], B[2]))
        {
            point = A[i];
            int edgeIdx; Vector2 closest = ClosestPointOnPolygon(point, B, out edgeIdx);
            if (edgeIdx >= 0)
            {
                Vector2 ba = B[(edgeIdx + 1) % B.Count] - B[edgeIdx];
                Vector2 n = new Vector2(-ba.y, ba.x).normalized;
                Vector2 dir = PolygonCentroid(B) - PolygonCentroid(A);
                if (Dot(n, dir) < 0f) n = -n;
                normal = n;
            }
            return true;
        }

        // 3) Vértice de B dentro de A
        for (int i = 0; i < 3; i++) if (PointInTriangle(B[i], A[0], A[1], A[2]))
        {
            point = B[i];
            int edgeIdx; Vector2 closest = ClosestPointOnPolygon(point, A, out edgeIdx);
            if (edgeIdx >= 0)
            {
                Vector2 aa = A[(edgeIdx + 1) % A.Count] - A[edgeIdx];
                Vector2 n = new Vector2(-aa.y, aa.x).normalized;
                Vector2 dir = PolygonCentroid(A) - PolygonCentroid(B);
                if (Dot(n, dir) < 0f) n = -n;
                normal = n;
            }
            return true;
        }

        return false;
    }

    public static bool CollideTriangleRectangle(IList<Vector2> tri, IList<Vector2> rect, out Vector2 point, out Vector2 normal)
    {
        // Triángulo contra rectángulo: reutiliza la lógica de polígonos pero con tamaños fijos
        return CollidePolygonPolygon(tri, rect, out point, out normal);
    }

    public static bool CollideRectangleRectangle(IList<Vector2> A, IList<Vector2> B, out Vector2 point, out Vector2 normal)
    {
        // Rectángulo contra rectángulo: usar intersección de aristas + contención
        return CollidePolygonPolygon(A, B, out point, out normal);
    }

    // Circle-Triangle específico
    public static bool CollideCircleTriangle(Vector2 cc, float r, IList<Vector2> tri, out Vector2 point, out Vector2 normal, out float depth)
    {
        point = Vector2.zero; normal = Vector2.zero; depth = 0f;
        if (tri == null || tri.Count != 3) return false;

        // 1) Si el centro está dentro del triángulo
        if (PointInTriangle(cc, tri[0], tri[1], tri[2]))
        {
            int edgeIdx; Vector2 closest = ClosestPointOnPolygon(cc, tri, out edgeIdx);
            Vector2 a = tri[edgeIdx]; Vector2 b = tri[(edgeIdx + 1) % tri.Count];
            Vector2 e = (b - a).normalized; Vector2 n = new Vector2(-e.y, e.x);
            if (Dot(n, cc - closest) < 0f) n = -n;
            float dToEdge = (cc - closest).magnitude;
            depth = r + dToEdge;
            point = closest; normal = n; return true;
        }

        // 2) Comprobar distancia a cada arista
        float bestDist2 = float.PositiveInfinity; Vector2 bestP = Vector2.zero; int bestIdx = -1;
        for (int i = 0; i < 3; i++)
        {
            Vector2 a = tri[i]; Vector2 b = tri[(i + 1) % 3];
            Vector2 q = ClosestPointOnSegment(cc, a, b);
            float d2 = (q - cc).sqrMagnitude;
            if (d2 < bestDist2)
            {
                bestDist2 = d2; bestP = q; bestIdx = i;
            }
        }
        if (bestDist2 <= r * r + 1e-6f)
        {
            float d = Mathf.Sqrt(Mathf.Max(0f, bestDist2));
            normal = d > 1e-6f ? (cc - bestP) / d : (cc - PolygonCentroid(tri)).normalized;
            depth = r - d;
            point = bestP; return true;
        }

        return false;
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
                Vector2 point = Vector2.zero;
                Vector2 normal = Vector2.zero;
                float depth = 0f;

                if (colA.type == CustomCollider2D.ShapeType.Circle && colB.type == CustomCollider2D.ShapeType.Circle)
                {
                    if (CollideCircleCircle(
                            colA.Center,
                            colA.CircleRadius,
                            colB.Center,
                            colB.CircleRadius,
                            out point,
                            out normal,
                            out depth
                            ))
                    {
                        collisionFound = true;
                    }
                }
                else if(colA.type != CustomCollider2D.ShapeType.Circle && colB.type != CustomCollider2D.ShapeType.Circle)
                {
                    IList<Vector2> polyA = colA.GetPolygonVertices();
                    IList<Vector2> polyB = colB.GetPolygonVertices();

                    // Usar rutinas especializadas según cantidad de vértices
                    if (polyA != null && polyB != null)
                    {
                        if (polyA.Count == 3 && polyB.Count == 3)
                        {
                            if (CollideTriangleTriangle(polyA, polyB, out point, out normal)) collisionFound = true;
                        }
                        else if (polyA.Count == 3 && polyB.Count == 4)
                        {
                            if (CollideTriangleRectangle(polyA, polyB, out point, out normal)) collisionFound = true;
                        }
                        else if (polyA.Count == 4 && polyB.Count == 3)
                        {
                            if (CollideTriangleRectangle(polyB, polyA, out point, out normal)) collisionFound = true;
                        }
                        else if (polyA.Count == 4 && polyB.Count == 4)
                        {
                            if (CollideRectangleRectangle(polyA, polyB, out point, out normal)) collisionFound = true;
                        }
                        else
                        {
                            if (CollidePolygonPolygon(polyA, polyB, out point, out normal)) collisionFound = true;
                        }
                    }
                }
                else
                {
                    if (colA.type == CustomCollider2D.ShapeType.Circle)
                    {
                        IList<Vector2> polyB = colB.GetPolygonVertices();
                        if (polyB != null)
                        {
                            if (polyB.Count == 3)
                            {
                                if (CollideCircleTriangle(colA.Center, colA.CircleRadius, polyB, out point, out normal, out depth)) collisionFound = true;
                            }
                            else
                            {
                                if (CollideCirclePolygon(colA.Center, colA.CircleRadius, polyB, out point, out normal, out depth)) collisionFound = true;
                            }
                        }
                    }
                    else
                    {
                        IList<Vector2> polyA = colA.GetPolygonVertices();
                        if (polyA != null)
                        {
                            if (polyA.Count == 3)
                            {
                                if (CollideCircleTriangle(colB.Center, colB.CircleRadius, polyA, out point, out normal, out depth)) collisionFound = true;
                            }
                            else
                            {
                                if (CollideCirclePolygon(colB.Center, colB.CircleRadius, polyA, out point, out normal, out depth)) collisionFound = true;
                            }
                        }
                    }
                }
                
                if (collisionFound)
                {
                    var collision = new InternalCollisionInfo(colA, colB, point, normal);
                    // Asignar MTV si depth > 0 (solo para colisiones con círculos calculadas)
                    if (depth > 1e-6f)
                    {
                        collision.hasMTV = true;
                        collision.mtvA = -normal * depth * 0.5f;
                        collision.mtvB = normal * depth * 0.5f;
                    }
                    else
                    {
                        collision.hasMTV = false;
                    }

                    collisions.Add(collision);
                    depth = 0f; // reset
                }
             }
         }

         return collisions;
     }
 }
