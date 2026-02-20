using UnityEngine;

public class Wheel : MonoBehaviour {
    public GameObject wheelMesh;
    public float mass = 20, radius = 0.5f,
        maxSpeed = 1000f,
        motorTorque, brakeTorque;

    public AnimationCurve powerCurve;
    public LayerMask collisionLayerMask;

    [Header("Friction")]
    public float tireGripFactor = 0.03f;

    [Header("Suspension")]
    public float suspensionRestDist = 0.5f;
    public float springStrength = 400, springDamper = 20;

    [Header("Wheel Mesh Orientation")]
    public Vector3 meshRotationOffset = Vector3.zero;
    public bool invertSpinDirection = false;
    public enum SpinAxis { X, Y, Z }
    public SpinAxis meshSpinAxis = SpinAxis.X;

    [Header("Gizmos")]
    public int wheelGizmoResolution = 32;
    public Color color = Color.white;
    public bool grounded;
    public bool drawSidewaysForce;
    public Color sidewaysForceColor = Color.red;
    public bool drawDirectionArrows = true;
    public float arrowLength = 1.0f;

    //  variables
    [HideInInspector] public float hitDistance;
    Rigidbody carRb;
    float currentTorque;
    Vector3 sidewaysForce;
    float meshRotationAngle;

    void Awake() {
        carRb = GetComponentInParent<Rigidbody>();
    }

    void OnValidate() {
        if (wheelMesh != null && !Application.isPlaying) {
            wheelMesh.transform.localRotation = Quaternion.Euler(meshRotationOffset);
        }
    }
    void Update() {
        SetWheelGrounded();

        if (wheelMesh != null) {
            Vector3 pointVelocity = carRb.GetPointVelocity(transform.position);
            float forwardSpeed = Vector3.Dot(pointVelocity, transform.forward);
            float spinSign = invertSpinDirection ? -1f : 1f;
            float rotationStep = (forwardSpeed * Time.deltaTime / radius) * Mathf.Rad2Deg * spinSign;
            meshRotationAngle += rotationStep;

            Vector3 spinEuler = Vector3.zero;
            switch (meshSpinAxis) {
                case SpinAxis.X: spinEuler = new Vector3(meshRotationAngle, 0, 0); break;
                case SpinAxis.Y: spinEuler = new Vector3(0, meshRotationAngle, 0); break;
                case SpinAxis.Z: spinEuler = new Vector3(0, 0, meshRotationAngle); break;
            }
            wheelMesh.transform.localRotation = Quaternion.Euler(meshRotationOffset) * Quaternion.Euler(spinEuler);
        }
    }

    void FixedUpdate() {
        if (grounded) {
            ApplySuspensionForce();
            ApplySidewaysForce();
            ApplyAccelerationForce();
            ApplyBrakeForce();
        }
    }

    void SetWheelGrounded() {
        RaycastHit hit;
        grounded = Physics.Raycast(transform.position, -transform.parent.up, out hit, radius, collisionLayerMask);
        hitDistance = hit.distance;
    }
    public void ApplySuspensionForce() {
        Vector3 springDir = transform.up;
        Vector3 tireWorldVel = carRb.GetPointVelocity(transform.position);
        float offset = suspensionRestDist - hitDistance;
        float vel = Vector3.Dot(springDir, tireWorldVel);
        float force = (offset * springStrength) - (vel * springDamper);
        carRb.AddForceAtPosition(springDir * force, transform.position);
    }

    void ApplySidewaysForce() {
        Vector3 wheelRight = transform.right;
        Vector3 tireWorldVel = carRb.GetPointVelocity(transform.position);
        float steeringVel = Vector3.Dot(wheelRight, tireWorldVel);
        float desiredVelChange = -steeringVel * tireGripFactor;
        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
        sidewaysForce = wheelRight * mass * desiredAccel;
        carRb.AddForceAtPosition(sidewaysForce, transform.position);
    }

    void ApplyAccelerationForce() {
        Vector3 accelDir = transform.forward;
        if (motorTorque != 0) {
            float carSpeed = Vector3.Dot(carRb.transform.forward, carRb.linearVelocity);
            float normalizedSpeed = (Mathf.Clamp01(Mathf.Abs(carSpeed) / maxSpeed));
            currentTorque = powerCurve.Evaluate(normalizedSpeed) * motorTorque;
            carRb.AddForceAtPosition(accelDir * currentTorque, transform.position);
        }
    }

    public void ApplyBrakeForce() {
        if (brakeTorque > 0) {
            Vector3 tireWorldVel = carRb.GetPointVelocity(transform.position);
            Vector3 forwardDir = transform.forward;
            Vector3 brakeDir = -Vector3.Project(tireWorldVel, forwardDir.normalized).normalized;
            carRb.AddForceAtPosition(brakeDir * brakeTorque, transform.position);
        }
    }

    void OnDrawGizmos() {
        // Draw Wheel
        Gizmos.color = color;
        Vector3 startPoint = transform.position + transform.right * radius;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, Vector3.one);

        for (int i = 1; i <= wheelGizmoResolution; i++) {
            float angle = i * 2.0f * Mathf.PI / wheelGizmoResolution;
            Vector3 localEndPoint = new Vector3(0.0f, Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
            Vector3 endPoint = rotationMatrix.MultiplyPoint3x4(localEndPoint) + transform.position;
            Gizmos.DrawLine(startPoint, endPoint);
            startPoint = endPoint;
        }

        // Draw Sideways Force
        if (drawSidewaysForce) {
            Gizmos.color = sidewaysForceColor;
            Gizmos.DrawLine(transform.position, transform.position + sidewaysForce); // Scale for visibility
        }

        // Draw Direction Arrows
        if (drawDirectionArrows) {
            if (wheelMesh != null) {
                float meshArrowLength = arrowLength * 0.7f;
                Vector3 meshPos = wheelMesh.transform.position;
                Quaternion previewRotation = transform.rotation * Quaternion.Euler(meshRotationOffset);
                GizmosExtra.DrawArrow(meshPos, previewRotation * Vector3.forward * meshArrowLength, new Color(0.4f, 0.4f, 1f));
                GizmosExtra.DrawArrow(meshPos, previewRotation * Vector3.right * meshArrowLength, new Color(1f, 0.4f, 0.4f));
                GizmosExtra.DrawArrow(meshPos, previewRotation * Vector3.up * meshArrowLength, new Color(0.4f, 1f, 0.4f));
            }
        }

        Quaternion wheelRotation = transform.rotation * Quaternion.Euler(meshRotationOffset);
        Vector3 spinAxisWorld = Vector3.right;
        Vector3 arcStartWorld = Vector3.up;
        switch (meshSpinAxis) {
            case SpinAxis.X:
                spinAxisWorld = wheelRotation * Vector3.right;
                arcStartWorld = wheelRotation * Vector3.up;
                break;
            case SpinAxis.Y:
                spinAxisWorld = wheelRotation * Vector3.up;
                arcStartWorld = wheelRotation * Vector3.forward;
                break;
            case SpinAxis.Z:
                spinAxisWorld = wheelRotation * Vector3.forward;
                arcStartWorld = wheelRotation * Vector3.up;
                break;
        }
        float arcSign = invertSpinDirection ? -1f : 1f;
        GizmosExtra.DrawSpinArc(transform.position, spinAxisWorld, arcStartWorld, radius * 1.3f, 300f * arcSign, 32, Color.yellow);
    }
}