using UnityEngine;

public static class GizmosExtra {
    public static void DrawArrow(Vector3 origin, Vector3 direction, Color arrowColor) {
        float length = direction.magnitude;
        if (length < 0.001f) return;
        Vector3 tip = origin + direction;
        float headSize = length * 0.2f;
        Vector3 right = Vector3.Cross(direction.normalized, Vector3.up);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(direction.normalized, Vector3.forward);
        right.Normalize();
        Vector3 up = Vector3.Cross(direction.normalized, right).normalized;

        Gizmos.color = arrowColor;
        Gizmos.DrawLine(origin, tip);
        Gizmos.DrawLine(tip, tip - direction.normalized * headSize + right * headSize * 0.4f);
        Gizmos.DrawLine(tip, tip - direction.normalized * headSize - right * headSize * 0.4f);
        Gizmos.DrawLine(tip, tip - direction.normalized * headSize + up * headSize * 0.4f);
        Gizmos.DrawLine(tip, tip - direction.normalized * headSize - up * headSize * 0.4f);
    }

    public static void DrawSpinArc(Vector3 center, Vector3 axisDir, Vector3 arcStartDir, float arcRadius, float arcDegrees, int segments, Color arcColor) {
        Gizmos.color = arcColor;
        Vector3 normalizedAxis = axisDir.normalized;
        Vector3 normalizedStart = arcStartDir.normalized;
        float stepDegrees = arcDegrees / segments;

        Vector3 previousPoint = center + normalizedStart * arcRadius;
        Vector3 currentPoint = previousPoint;

        for (int i = 1; i <= segments; i++) {
            float angle = stepDegrees * i;
            Vector3 rotatedDir = Quaternion.AngleAxis(angle, normalizedAxis) * normalizedStart;
            currentPoint = center + rotatedDir * arcRadius;
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        Vector3 tangent = Vector3.Cross(normalizedAxis, (currentPoint - center).normalized).normalized;
        if (arcDegrees < 0) tangent = -tangent;
        float headSize = arcRadius * 0.25f;
        Vector3 headBack = -tangent;
        Vector3 headOutward = (currentPoint - center).normalized;
        Gizmos.DrawLine(currentPoint, currentPoint + (headBack + headOutward).normalized * headSize);
        Gizmos.DrawLine(currentPoint, currentPoint + (headBack - headOutward).normalized * headSize);
    }
}
