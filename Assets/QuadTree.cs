using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AABB // axis-aligned bounding box
{
    public float centerX;
    public float centerY;
    public float halfWidth;
    public float halfHeight;

    public AABB(float x, float y, float hw, float hh)
    {
        centerX = x;
        centerY = y;
        halfWidth = hw;
        halfHeight = hh;
    }
}

public class QuadTree
{
    AABB self;
    List<Vector2> particles = new List<Vector2>();
    int quadVolumne;

    public QuadTree(float x, float y, float hw, float hh, int vol)
    {
        self = new AABB(x, y, hw, hh);
        quadVolumne = vol;
    }

    QuadTree topRight = null;
    QuadTree bottomRight = null;
    QuadTree bottomLeft = null;
    QuadTree topLeft = null;

    bool Contain(Vector2 particle)
    {
        return !(particle.x < self.centerX - self.halfWidth ||
                particle.x > self.centerX + self.halfWidth ||
                particle.y < self.centerY - self.halfHeight ||
                particle.y > self.centerY + self.halfHeight);
    }

    void Subdivide()
    {
        float cx = self.centerX;
        float cy = self.centerY;
        float qw = self.halfWidth/2; // quater width
        float qh = self.halfHeight/2;
        topRight = new QuadTree(cx + qw, cy + qh, qw, qh, quadVolumne);
        bottomRight = new QuadTree(cx + qw, cy - qh, qw, qh, quadVolumne);
        bottomLeft = new QuadTree(cx - qw, cy - qh, qw, qh, quadVolumne);
        topLeft = new QuadTree(cx - qw, cy + qh, qw, qh, quadVolumne);
        Debug.Log("subdived");

    }

    public bool Insert(Vector2 particle)
    {
        // check if particle is inside of this aabb boundry
        if (!Contain(particle)) return false;

        // add particle to list if list is not full and return true
        if (particles.Count < quadVolumne)
        {
            particles.Add(particle);
            return true;
        }

        // else if list is full, subdivide 
        else if(topRight == null)
        {
            Subdivide();
        }
        // try insert to children nodes
        if (topRight.Insert(particle)) return true;
        if (bottomRight.Insert(particle)) return true;
        if (bottomLeft.Insert(particle)) return true;
        if (topLeft.Insert(particle)) return true;

        Debug.Log("Failed to insert particles");
        return false;
    }

    bool Intersect(Vector2 particle, float range)
    {
        float left = self.centerX - self.halfWidth;
        float right = self.centerX + self.halfWidth;
        float top = self.centerY + self.halfHeight;
        float bottom = self.centerY - self.halfHeight;

        float nearX = Mathf.Max(left, Mathf.Min(particle.x, right));
        float nearY = Mathf.Max(bottom, Mathf.Min(particle.y, top));

        float distX = nearX - particle.x;
        float distY = nearY - particle.y;
        return distX * distX + distY * distY <= range * range;
    }

    public List<Vector2> Query(Vector2 particle, float range, List<Vector2> particlesQuery)
    {
        // check if intersect 
        if (!Intersect(particle, range)) return particlesQuery;

        // if intersect, check every particle in this quad
        foreach (var p in particles)
        {
            if (Vector2.Distance(p, particle) <= range && !(p.x == particle.x && p.y == particle.y))
            {
                particlesQuery.Add(p);
            }
        }

        // check if has children 
        if (topRight == null) return particlesQuery;

        // if has children nodes, check for children nodes
        particlesQuery = topRight.Query(particle, range, particlesQuery);
        particlesQuery = bottomRight.Query(particle, range, particlesQuery);
        particlesQuery = bottomLeft.Query(particle, range, particlesQuery);
        particlesQuery = topLeft.Query(particle, range, particlesQuery);

        return particlesQuery;
    }

    public void DebugQuadTree()
    {
        Gizmos.DrawWireCube(new Vector3(self.centerX, self.centerY, 0), new Vector3(self.halfWidth * 2, self.halfHeight * 2, 0));

        if (topRight != null)
        {
            topRight.DebugQuadTree();
            bottomRight.DebugQuadTree();
            bottomLeft.DebugQuadTree();
            topLeft.DebugQuadTree();
        }
    }
}

