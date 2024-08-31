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

    List<Triangle> triangles = new();
    List<Vector3> vertices = new();
    List<Edge> invalidEdges = new();

    //Debug
    List<Circle> circles = new List<Circle>();
    Vector2? currentVertex;

    public float AnimateStepInterval = 1;

    IEnumerator Start()
    {
        List<Vector3> vertices = new();
        List<Vector2> points = new();

        for (int i = 0; i < Points.transform.childCount; i++)
        {
            var vertex = Points.transform.GetChild(i).transform.position;
            vertices.Add(vertex);
            points.Add(vertex);
        }

        yield return StartCoroutine(UpdateText(Text, "1. Create SuperTriangle", AnimateStepInterval));

        var minCircle = SmallestCircle.Welzl(new(points));
        float step = Mathf.PI * 2 / 3;
        Vector3 center = (Vector3)minCircle.Center;
        float radius = minCircle.Radius * 3f;
        Vector3 tp0 = center + new Vector3(Mathf.Cos(step * 0), Mathf.Sin(step * 0), 0) * radius;
        Vector3 tp1 = center + new Vector3(Mathf.Cos(step * 1), Mathf.Sin(step * 1), 0) * radius;
        Vector3 tp2 = center + new Vector3(Mathf.Cos(step * 2), Mathf.Sin(step * 2), 0) * radius;
        var superTriangle = new Triangle(tp0, tp1, tp2);

        triangles = new() { superTriangle };

        yield return StartCoroutine(UpdateText(Text, "2. Add points one by one", AnimateStepInterval));

        var animatespeed = AnimateStepInterval;
        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];

            currentVertex = point;
            yield return StartCoroutine(addVertex(point, triangles, animatespeed));
            currentVertex = null;

            animatespeed *= 0.6f;
            animatespeed = Mathf.Clamp(animatespeed,0.1f,3);
        }

        yield return StartCoroutine(UpdateText(Text, "3. Removing triangles that share with SuperTriangle", AnimateStepInterval));

        triangles = triangles.FindAll(triangle =>
            !(triangle.v0 == superTriangle.v0 || triangle.v0 == superTriangle.v1 || triangle.v0 == superTriangle.v2 ||
            triangle.v1 == superTriangle.v0 || triangle.v1 == superTriangle.v1 || triangle.v1 == superTriangle.v2 ||
            triangle.v2 == superTriangle.v0 || triangle.v2 == superTriangle.v1 || triangle.v2 == superTriangle.v2)
        );

        yield return StartCoroutine(UpdateText(Text, "4. Create Mesh", AnimateStepInterval));

        List<int> reorderedTriangles = new List<int>(triangles.Count * 3);
        for (int i = 0; i < triangles.Count; i++)
        {
            var windingOrder = GeometryUtils.Orientation(triangles[i].v0, triangles[i].v1, triangles[i].v2);
            if (windingOrder < 0)
            {
                reorderedTriangles.Add(vertices.FindIndex(v => v == (Vector3)triangles[i].v0));
                reorderedTriangles.Add(vertices.FindIndex(v => v == (Vector3)triangles[i].v1));
                reorderedTriangles.Add(vertices.FindIndex(v => v == (Vector3)triangles[i].v2));
            }
            else //if (windingOrder > 0)
            {
                reorderedTriangles.Add(vertices.FindIndex(v => v == (Vector3)triangles[i].v2));
                reorderedTriangles.Add(vertices.FindIndex(v => v == (Vector3)triangles[i].v1));
                reorderedTriangles.Add(vertices.FindIndex(v => v == (Vector3)triangles[i].v0));
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(reorderedTriangles, 0);
        Meshfilter.mesh = mesh;

        Text.text = "Delaunay BowyaerWatson Triangulation Triangle Count: " + triangles.Count;

        yield return null;
    }

    IEnumerator addVertex(Vector2 vertex, List<Triangle> intriangles, float AnimateStepInterval)
    {
        invalidEdges = new List<Edge>();

        var newTriangles = new List<Triangle>();
        foreach (var triangle in intriangles)
        {
            yield return StartCoroutine(UpdateText(Text, "2a. For each existing triangle create Circumcircle", AnimateStepInterval));

            var c = GeometryUtils.GetCircumcircle(triangle.v0, triangle.v1, triangle.v2);
            circles.Add(c);

            yield return StartCoroutine(UpdateText(Text, "2b. Is point in Circumcircle of triangle?", AnimateStepInterval));

            if (GeometryUtils.InsideCircumcircle(vertex, triangle.v0, triangle.v1, triangle.v2))
            {
                yield return StartCoroutine(UpdateText(Text, "2c. Yes! Invalidate Triangle (Convert to Edges)", AnimateStepInterval));

                invalidEdges.Add(new Edge(triangle.v0, triangle.v1));
                invalidEdges.Add(new Edge(triangle.v1, triangle.v2));
                invalidEdges.Add(new Edge(triangle.v2, triangle.v0));
            }
            else
            {
                yield return StartCoroutine(UpdateText(Text, "2d. No! Check other Triangles", AnimateStepInterval));

                newTriangles.Add(triangle);
            }

            circles.Remove(c);
        }

        triangles = newTriangles;

        yield return StartCoroutine(UpdateText(Text, "2e. Filter for Unique Edges", AnimateStepInterval));

        invalidEdges = uniqueEdges(invalidEdges);
        var e2 = new List<Edge>(invalidEdges);

        yield return StartCoroutine(UpdateText(Text, "2e. Create Triangle from Unique Edges", AnimateStepInterval));

        foreach (var edge in e2)
        {
            newTriangles.Add(new Triangle(edge.v0, edge.v1, vertex));
            invalidEdges.Remove(edge);
        }
        triangles = newTriangles;

        yield return StartCoroutine(UpdateText(Text, "2f. Finished, Add Next Point", AnimateStepInterval));
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

            if (isUnique) uniqueEdges.Add(edges[i]);
        }

        return uniqueEdges;
    }

    IEnumerator UpdateText(Text text, string value, float delay)
    {
        yield return new WaitForSeconds(delay);
        text.text = value;
        yield return new WaitForSeconds(delay);
    }

    void OnDrawGizmos()
    {

        

        // Resulting Triangles
        Gizmos.color = Color.green;
        foreach (var triangle in triangles)
        {
            UnityOnGizmoHelper.DrawTriangleLine(triangle.v0, triangle.v1, triangle.v2);
        }

        // Invalid Edges/Triangles
        Gizmos.color = Color.blue;
        foreach (var edge in invalidEdges)
        {
            Gizmos.DrawLine(edge.v0, edge.v1);
        }

        // Draw Current Vertex being added
        Gizmos.color = Color.red;
        if (currentVertex.HasValue)
        {
            UnityOnGizmoHelper.DrawCircle(currentVertex.Value, 0.5f, 100, Quaternion.identity);
        }

        // Circumcircles
        Gizmos.color = Color.red;
        foreach (var circle in circles)
        {
            UnityOnGizmoHelper.DrawCircle(circle.Center, circle.Radius, 100, Quaternion.identity);
        }
    }
}