using GraphToolsFSM.Runtime.DataNodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphToolsFSM.Editor.Edges {
    public class TransitionEdge : Edge
    {
        private const float ArrowWidth = 12f;
        private readonly Color _activeColor = Color.green;

        public TransitionType TransitionType { get; }
        public bool Active { get; set; }

        public TransitionEdge(TransitionType transitionType)
        {
            TransitionType = transitionType;
            edgeControl.RegisterCallback<GeometryChangedEvent>(OnEdgeGeometryChanged);
            generateVisualContent += DrawArrow;
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (base.ContainsPoint(localPoint))
                return true;

            Vector2 start = PointsAndTangents[PointsAndTangents.Length / 2 - 1];
            Vector2 end = PointsAndTangents[PointsAndTangents.Length / 2];
            Vector2 mid = (start + end) / 2;

            return (localPoint - mid).sqrMagnitude <= (ArrowWidth * ArrowWidth);
        }

        private void OnEdgeGeometryChanged(GeometryChangedEvent evt)
        {
            if (PointsAndTangents.Length < 4 || output?.node == null)
                return;

            Vector2 from = output.node.GetPosition().center;
            Vector2 to = isGhostEdge ? edgeControl.to : input?.node?.GetPosition().center ?? from;

            Vector2 tangent = Vector2.zero;
            if (!isGhostEdge && input?.node != null)
            {
                Vector2 dir = (to - from).normalized;
                float distance = Vector2.Distance(from, to);
                Vector2 perpendicular = new Vector2(-dir.y, dir.x);
                float curveStrength = Mathf.Min(5f, distance * 0.3f);
                tangent = perpendicular * curveStrength;
            }

            PointsAndTangents[0] = from;
            PointsAndTangents[1] = from + tangent;
            PointsAndTangents[2] = to + tangent;
            PointsAndTangents[3] = to;

            MarkDirtyRepaint();
        }

        private void DrawArrow(MeshGenerationContext context)
        {
            Color arrowColor = GetColor();

            Vector2 start = PointsAndTangents[PointsAndTangents.Length / 2 - 1];
            Vector2 end = PointsAndTangents[PointsAndTangents.Length / 2];
            Vector2 mid = (start + end) * 0.5f;
            Vector2 direction = end - start;

            if (direction.sqrMagnitude < 0.01f) return;

            direction.Normalize();

            float distanceFromMid = ArrowWidth * Mathf.Sqrt(3) / 4;
            float perpendicularLength = ArrowWidth * 0.5f;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * perpendicularLength;

            if (TransitionType == TransitionType.Completed)
            {
                var mesh = context.Allocate(7, 9);
                Vertex[] vertices = new Vertex[7];

                vertices[0].position = mid + direction * distanceFromMid;
                vertices[1].position = mid - direction * distanceFromMid + perpendicular;
                vertices[2].position = mid - direction * distanceFromMid - perpendicular;

                float barOffset = ArrowWidth * 1.2f;
                Vector2 barCenter = mid - direction * barOffset;

                float barHalfWidth = ArrowWidth * 0.5f;
                float barThickness = ArrowWidth * 0.15f;
                Vector2 barHorizontal = new Vector2(-direction.y, direction.x) * barHalfWidth;
                Vector2 barVertical = direction * barThickness;

                vertices[3].position = barCenter - barHorizontal + barVertical;
                vertices[4].position = barCenter + barHorizontal + barVertical;
                vertices[5].position = barCenter + barHorizontal - barVertical;
                vertices[6].position = barCenter - barHorizontal - barVertical;

                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i].position += Vector3.forward * Vertex.nearZ;
                    vertices[i].tint = arrowColor;
                }

                mesh.SetAllVertices(vertices);
                mesh.SetAllIndices(new ushort[]
                {
                    0, 1, 2,         // Flèche
                    3, 4, 5,         // Rectangle haut
                    3, 5, 6          // Rectangle bas
                });
            }

            else if (TransitionType == TransitionType.Continued)
            {
                var mesh = context.Allocate(3, 3);
                Vertex[] vertices = new Vertex[3];
                ushort[] indices = new ushort[3];

                vertices[0].position = mid + direction * distanceFromMid;
                vertices[1].position = mid - direction * distanceFromMid + perpendicular;
                vertices[2].position = mid - direction * distanceFromMid - perpendicular;

                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i].position += Vector3.forward * Vertex.nearZ;
                    vertices[i].tint = arrowColor;
                    indices[i] = (ushort)i;
                }

                mesh.SetAllVertices(vertices);
                mesh.SetAllIndices(indices);
            }
        }

        private Color GetColor()
        {
            if (isGhostEdge)
                return ghostColor;
            if (selected)
                return selectedColor;
            if (Active)
                return _activeColor;
            if (output != null)
                return output.portColor;
            return defaultColor;
        }

        public void UpdateEdgeColor()
        {
            Color color = GetColor();
            edgeControl.inputColor = color;
            edgeControl.outputColor = color;
            MarkDirtyRepaint();
        }
    }
}
