using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
public class SC_NodeConnectionLine : MaskableGraphic
{
    private Vector2[] pathPoints = Array.Empty<Vector2>();
    private float thickness = 6f;

    public void Setup(Vector2[] points, float lineThickness, Color lineColor)
    {
        CanvasRenderer canvasRenderer = GetComponent<CanvasRenderer>();
        if (canvasRenderer != null)
        {
            canvasRenderer.cullTransparentMesh = false;
        }

        pathPoints = points ?? Array.Empty<Vector2>();
        thickness = Mathf.Max(1f, lineThickness);
        color = lineColor;
        raycastTarget = false;
        SetMaterialDirty();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (pathPoints == null || pathPoints.Length < 2)
        {
            return;
        }

        Vector2 previousNormal = Vector2.zero;
        for (int pointIndex = 0; pointIndex < pathPoints.Length - 1; pointIndex++)
        {
            Vector2 previousPoint = pathPoints[pointIndex];
            Vector2 currentPoint = pathPoints[pointIndex + 1];
            Vector2 direction = currentPoint - previousPoint;
            if (direction.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            Vector2 currentNormal = new Vector2(-direction.y, direction.x).normalized * (thickness * 0.5f);
            if (previousNormal == Vector2.zero)
            {
                previousNormal = currentNormal;
            }

            AddQuad(vertexHelper, previousPoint, currentPoint, previousNormal, currentNormal);
            previousNormal = currentNormal;
        }
    }

    private void AddQuad(VertexHelper vertexHelper, Vector2 start, Vector2 end, Vector2 startNormal, Vector2 endNormal)
    {
        int startIndex = vertexHelper.currentVertCount;
        AddVertex(vertexHelper, start - startNormal);
        AddVertex(vertexHelper, start + startNormal);
        AddVertex(vertexHelper, end + endNormal);
        AddVertex(vertexHelper, end - endNormal);
        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }

    private void AddVertex(VertexHelper vertexHelper, Vector2 position)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = position;
        vertexHelper.AddVert(vertex);
    }
}
