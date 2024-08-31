using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using asim.unity.helpers;
using asim.unity.geometry;

public class BowyerWatsonAnimate : MonoBehaviour
{
    [SerializeField] Text Text;
    [SerializeField] GameObject Points;
    [SerializeField] MeshFilter Meshfilter;

    Triangle superTriangle;
    List<Triangle> triangles = new List<Triangle>();
    List<Vector3> vertices = new();
    List<Edge> edges = new();

    //Debug
    List<Circle> circles = new List<Circle>();
    Vector2? currentV;
    IEnumerator Start()
    {
        List<Vector3> vertices = new();
        List<Vector2> points = new();

        //Step 0. Collect Vertices
        for (int i = 0; i < Points.transform.childCount; i++)
        {
            var vertex = Points.transform.GetChild(i).transform.position;
            vertices.Add(vertex);

            points.Add(vertex);
        }

        //1. Create Super Triangle using welzl algorithm to get minimum circle
        var minCircle = SmallestCircle.Welzl(new(points));
        float step = Mathf.PI * 2 / 3;
        Vector3 center = (Vector3)minCircle.Center;
        float radius = minCircle.Radius * 3f;
        Vector3 tp0 = center + new Vector3(Mathf.Cos(step * 0), Mathf.Sin(step * 0), 0) * radius;
        Vector3 tp1 = center + new Vector3(Mathf.Cos(step * 1), Mathf.Sin(step * 1), 0) * radius;
        Vector3 tp2 = center + new Vector3(Mathf.Cos(step * 2), Mathf.Sin(step * 2), 0) * radius;
        superTriangle = new Triangle(tp0, tp1, tp2);

        triangles = new() { superTriangle };

        yield return StartCoroutine(add(points[0], triangles));
        triangles = coroutineValue;

        yield return StartCoroutine(add(points[1], triangles));
        triangles = coroutineValue;

        yield return StartCoroutine(add(points[2], triangles));
        triangles = coroutineValue;

        yield return StartCoroutine(add(points[3], triangles));
        triangles = coroutineValue;

        // Remove triangles that share edges with super triangle
        triangles = triangles.FindAll(triangle =>
            !(triangle.v0 == superTriangle.v0 || triangle.v0 == superTriangle.v1 || triangle.v0 == superTriangle.v2 ||
            triangle.v1 == superTriangle.v0 || triangle.v1 == superTriangle.v1 || triangle.v1 == superTriangle.v2 ||
            triangle.v2 == superTriangle.v0 || triangle.v2 == superTriangle.v1 || triangle.v2 == superTriangle.v2)
        );

        List<int> tri = new List<int>(triangles.Count * 3);
        for (int i = 0; i < triangles.Count; i++)
        {
            var windingOrder = GeometryUtils.Orientation(triangles[i].v0, triangles[i].v1, triangles[i].v2);
            if (windingOrder < 0)
            {
                tri.Add(vertices.FindIndex(v => v == (Vector3)triangles[i].v0));
                tri.Add(vertices.FindIndex(v => v == (Vector3)triangles[i].v1));
                tri.Add(vertices.FindIndex(v => v == (Vector3)triangles[i].v2));
            }
            else //if (windingOrder > 0)
            {
                tri.Add(vertices.FindIndex(v => v == (Vector3)triangles[i].v2));
                tri.Add(vertices.FindIndex(v => v == (Vector3)triangles[i].v1));
                tri.Add(vertices.FindIndex(v => v == (Vector3)triangles[i].v0));
            }
        }

        //Step 4.create the mesh
        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tri, 0);
        Meshfilter.mesh = mesh;

        Text.text = "Delaunay BowyaerWatson Triangulation Triangle Count: " + triangles.Count;

        yield return null;
    }

    void Update()
    {
        
    }

    List<Triangle> coroutineValue = new();
    IEnumerator add(Vector2 vertex, List<Triangle> triangles)
    {
        edges = new List<Edge>();

        var newTriangles = new List<Triangle>();
        foreach (var triangle in triangles)
        {
            var c = GeometryUtils.GetCircumcircle(triangle.v0, triangle.v1, triangle.v2);
            circles.Add(c);
            currentV = vertex;

            yield return new WaitForSeconds(1);

            if (GeometryUtils.InsideCircumcircle(vertex, triangle.v0, triangle.v1, triangle.v2))
            {
                edges.Add(new Edge(triangle.v0, triangle.v1));
                edges.Add(new Edge(triangle.v1, triangle.v2));
                edges.Add(new Edge(triangle.v2, triangle.v0));

                yield return new WaitForSeconds(1);
            }
            else
            {
                newTriangles.Add(triangle);
            }

            circles.Remove(c);
            currentV = null;
        }
        triangles = newTriangles;

        // Get unique edges
        edges = uniqueEdges(edges);
        var e2 = new List<Edge>(edges);

        yield return new WaitForSeconds(1);

        // Create new triangles from the unique edges and new vertex
        foreach (var edge in e2)
        {
            triangles.Add(new Triangle(edge.v0, edge.v1, vertex));
            edges.Remove(edge);
        }

        coroutineValue = triangles;

        yield return new WaitForSeconds(1);
    }

    List<Edge> uniqueEdges(List<Edge> edges)
    {
        var uniqueEdges = new List<Edge>();

        for (var i = 0; i < edges.Count; ++i)
        {
            var isUnique = true;

            // See if edge is unique
            for (var j = 0; j < edges.Count; ++j)
            {
                if (i != j && edges[i].Equals(edges[j]))
                {
                    isUnique = false;
                    break;
                }
            }
             
            if(isUnique) uniqueEdges.Add(edges[i]);
        }

        return uniqueEdges;
    }



    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        //Gizmos.DrawLine(superTriangle.v0, superTriangle.v1);
        //Gizmos.DrawLine(superTriangle.v1, superTriangle.v2);
        //Gizmos.DrawLine(superTriangle.v2, superTriangle.v0);

        foreach (var circle in circles)
        {
            UnityOnGizmoHelper.DrawCircle(circle.Center, circle.Radius, 100, Quaternion.identity);
        }

        Gizmos.color = Color.cyan;
        foreach (var triangle in triangles)
        {
            UnityOnGizmoHelper.DrawTriangleLine(triangle.v0, triangle.v1, triangle.v2);
        }

        Gizmos.color = Color.green;
        foreach (var triangle in coroutineValue)
        {
            UnityOnGizmoHelper.DrawTriangleLine(triangle.v0, triangle.v1, triangle.v2);
        }

        Gizmos.color = Color.blue;
        foreach (var edge in edges)
        {
            Gizmos.DrawLine(edge.v0, edge.v1);
        }

        Gizmos.color = Color.white;
        if (currentV.HasValue)
        {
            UnityOnGizmoHelper.DrawCircle(currentV.Value, 1, 100, Quaternion.identity);
        }
    }

    struct Triangle
    {
        public Vector2 v0 { get; set; }
        public Vector2 v1 { get; set; }
        public Vector2 v2 { get; set; }

        public Triangle(Vector2 v0, Vector2 v1, Vector2 v2)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;
        }
    }
    struct Edge : IEquatable<Edge>
    {
        public Vector2 v0 { get; set; }
        public Vector2 v1 { get; set; }

        public Edge(Vector2 v0, Vector2 v1)
        {
            this.v0 = v0;
            this.v1 = v1;
        }

        public bool Equals(Edge other)
        {
            return (v0 == other.v0 && v1 == other.v1) || (v0 == other.v1 && v1 == other.v0);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Edge other = (Edge)obj;

            return (v0 == other.v0 && v1 == other.v1) || (v0 == other.v1 && v1 == other.v0);
        }
    }
}