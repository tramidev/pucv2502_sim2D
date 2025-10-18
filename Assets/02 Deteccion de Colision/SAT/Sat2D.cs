using UnityEngine;

public static class SAT2D
{
    public struct CollisionResult
    {
        public bool collided;
        public Vector2 normal;   // Normal de colisión, apunta de A -> B
        public float depth;      // Magnitud de penetración
        public Vector2 mtv;      // normal * depth
    }

    // ===================== PÚBLICO =====================

    // Polígono convexo vs Polígono convexo
    public static CollisionResult PolygonVsPolygon(Vector2[] polyA, Vector2[] polyB)
    {
        CollisionResult res = new CollisionResult { collided = true, depth = float.PositiveInfinity };
        if (polyA == null || polyB == null || polyA.Length < 3 || polyB.Length < 3)
        {
            res.collided = false;
            return res;
        }

        Vector2 centerA = Centroid(polyA);
        Vector2 centerB = Centroid(polyB);

        // Probar ejes de A
        for (int i = 0; i < polyA.Length; i++)
        {
            Vector2 axis = EdgeNormal(polyA, i);
            if (axis.sqrMagnitude < 1e-12f) continue;

            if (!OverlapOnAxis(polyA, polyB, axis, out float overlap)) {
                res.collided = false;
                return res;
            }
            if (overlap < res.depth) {
                // Asegurar dirección A->B
                Vector2 dir = (centerB - centerA);
                if (Vector2.Dot(dir, axis) < 0) axis = -axis;

                res.depth = overlap;
                res.normal = axis;
                res.mtv = axis * overlap;
            }
        }

        // Probar ejes de B
        for (int i = 0; i < polyB.Length; i++)
        {
            Vector2 axis = EdgeNormal(polyB, i);
            if (axis.sqrMagnitude < 1e-12f) continue;

            if (!OverlapOnAxis(polyA, polyB, axis, out float overlap)) {
                res.collided = false;
                return res;
            }
            if (overlap < res.depth) {
                Vector2 dir = (centerB - centerA);
                if (Vector2.Dot(dir, axis) < 0) axis = -axis;

                res.depth = overlap;
                res.normal = axis;
                res.mtv = axis * overlap;
            }
        }

        return res;
    }

    // Círculo vs Círculo
    public static CollisionResult CircleVsCircle(Vector2 cA, float rA, Vector2 cB, float rB)
    {
        CollisionResult res = new CollisionResult { collided = false, depth = 0f, normal = Vector2.zero, mtv = Vector2.zero };
        Vector2 ab = cB - cA;
        float dist = ab.magnitude;
        float r = rA + rB;

        if (dist >= r)
            return res;

        res.collided = true;
        // Si los centros coinciden, elige una normal estable (e.g., eje X)
        Vector2 n = dist > 1e-6f ? (ab / Mathf.Max(dist, 1e-6f)) : new Vector2(1f, 0f);
        float depth = r - dist;
        res.normal = n;
        res.depth = depth;
        res.mtv = n * depth;
        return res;
    }

 
    public static CollisionResult CircleVsPolygon(Vector2 c, float r, Vector2[] poly)
    {
        CollisionResult res = new CollisionResult { collided = true, depth = float.PositiveInfinity };
        if (poly == null || poly.Length < 3) { res.collided = false; return res; }

        Vector2 centerPoly = Centroid(poly);

        // 1) Probar ejes (normales) de cada arista del polígono
        for (int i = 0; i < poly.Length; i++)
        {
            Vector2 axis = EdgeNormal(poly, i);
            if (axis.sqrMagnitude < 1e-12f) continue;

            // Proyección del polígono
            ProjectPolygon(poly, axis, out float minP, out float maxP);
            // Proyección del círculo
            float centerProj = Vector2.Dot(c, axis);
            float minC = centerProj - r;
            float maxC = centerProj + r;

            float overlap = GetOverlap(minP, maxP, minC, maxC);
            if (overlap <= 0f) { res.collided = false; return res; }

            if (overlap < res.depth)
            {
                // Dirección del MTV A->B (aquí A = polígono, B = círculo)
                Vector2 dir = (c - centerPoly);
                if (Vector2.Dot(dir, axis) < 0) axis = -axis;

                res.depth = overlap;
                res.normal = axis;
                res.mtv = axis * overlap;
            }
        }

        // 2) Eje adicional: desde el centro del círculo al punto MÁS CERCANO del polígono
        Vector2 closest = ClosestPointOnPolygon(c, poly, out int _);
        Vector2 axisExtra = (closest - c);
        if (axisExtra.sqrMagnitude > 1e-12f)
        {
            axisExtra.Normalize();

            ProjectPolygon(poly, axisExtra, out float minP2, out float maxP2);
            float centerProj2 = Vector2.Dot(c, axisExtra);
            float minC2 = centerProj2 - r;
            float maxC2 = centerProj2 + r;

            float overlap2 = GetOverlap(minP2, maxP2, minC2, maxC2);
            if (overlap2 <= 0f) { res.collided = false; return res; }

            if (overlap2 < res.depth)
            {
                Vector2 dir = (c - centerPoly);
                if (Vector2.Dot(dir, axisExtra) < 0) axisExtra = -axisExtra;

                res.depth = overlap2;
                res.normal = axisExtra;
                res.mtv = axisExtra * overlap2;
            }
        }

        return res;
    }

 
    public static Vector2[] BuildOrientedRect(Vector2 center, Vector2 halfExtents, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        Vector2 hx = new Vector2(halfExtents.x, 0);
        Vector2 hy = new Vector2(0, halfExtents.y);

        // Esquinas locales antes de rotar: (+/-hx, +/-hy)
        Vector2[] local = new Vector2[4];
        local[0] = hx + hy; // ( +x, +y)
        local[1] = -hx + hy; // ( -x, +y)
        local[2] = -hx - hy; // ( -x, -y)
        local[3] = hx - hy; // ( +x, -y)

        Vector2[] world = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            Vector2 p = local[i];
            Vector2 r = new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);
            world[i] = center + r;
        }
        return world;
    }

    public static Vector2[] BuildTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        return new Vector2[] { a, b, c };
    }

    // ===================== PRIVADO (utilidades) =====================

    private static Vector2 Centroid(Vector2[] poly)
    {
        Vector2 sum = Vector2.zero;
        for (int i = 0; i < poly.Length; i++) sum += poly[i];
        return sum / poly.Length;
    }

    private static Vector2 EdgeNormal(Vector2[] poly, int i)
    {
        int j = (i + 1) % poly.Length;
        Vector2 edge = poly[j] - poly[i];
        if (edge.sqrMagnitude < 1e-12f) return Vector2.zero;
        // normal perpendicular (x, y) -> ( -y, x ) ó ( y, -x ), ambas sirven
        Vector2 n = new Vector2(-edge.y, edge.x).normalized;
        return n;
    }

    private static void ProjectPolygon(Vector2[] poly, Vector2 axis, out float min, out float max)
    {
        float d = Vector2.Dot(poly[0], axis);
        min = d; max = d;
        for (int i = 1; i < poly.Length; i++)
        {
            d = Vector2.Dot(poly[i], axis);
            if (d < min) min = d;
            if (d > max) max = d;
        }
    }

    private static float GetOverlap(float minA, float maxA, float minB, float maxB)
    {
        float left = Mathf.Max(minA, minB);
        float right = Mathf.Min(maxA, maxB);
        return right - left; // <= 0 => no hay solapamiento
    }

    private static bool OverlapOnAxis(Vector2[] polyA, Vector2[] polyB, Vector2 axis, out float overlap)
    {
        ProjectPolygon(polyA, axis, out float minA, out float maxA);
        ProjectPolygon(polyB, axis, out float minB, out float maxB);
        overlap = GetOverlap(minA, maxA, minB, maxB);
        return overlap > 0f;
    }

    // Punto más cercano del polígono a un punto externo. Devuelve también el índice del segmento más cercano.
    private static Vector2 ClosestPointOnPolygon(Vector2 p, Vector2[] poly, out int edgeIndex)
    {
        float bestDist2 = float.PositiveInfinity;
        Vector2 best = Vector2.zero;
        int bestIdx = 0;

        for (int i = 0; i < poly.Length; i++)
        {
            int j = (i + 1) % poly.Length;
            Vector2 a = poly[i];
            Vector2 b = poly[j];
            Vector2 closest = ClosestPointOnSegment(p, a, b);
            float d2 = (p - closest).sqrMagnitude;
            if (d2 < bestDist2)
            {
                bestDist2 = d2;
                best = closest;
                bestIdx = i;
            }
        }
        edgeIndex = bestIdx;
        return best;
    }

    private static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float abLen2 = Vector2.SqrMagnitude(ab);
        if (abLen2 < 1e-12f) return a;
        float t = Vector2.Dot(p - a, ab) / abLen2;
        t = Mathf.Clamp01(t);
        return a + t * ab;
    }
}
