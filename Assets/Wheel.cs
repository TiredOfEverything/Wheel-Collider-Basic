  // Copyright 2024 TiredOfEverything

  // Licensed under the Apache License, Version 2.0 (the "License");
  // you may not use this file except in compliance with the License.
  // You may obtain a copy of the License at

  // http://www.apache.org/licenses/LICENSE-2.0

  // Unless required by applicable law or agreed to in writing, software
  // distributed under the License is distributed on an "AS IS" BASIS,
  // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  // See the License for the specific language governing permissions and
  // limitations under the License.

  using UnityEngine;

  public class Wheel : MonoBehaviour {
      public float mass = 20, radius = 0.5f,
          maxSpeed = 1000f,
          motorTorque, brakeTorque;

      public AnimationCurve powerCurve;
      public LayerMask collisionLayerMask;

      [Header ("Friction")]
      public float tireGripFactor = 0.03f;

      [Header ("Suspension")]
      public float suspensionRestDist = 0.5f;
      public float springStrength = 400, springDamper = 20;

      [Header ("Gizmos")]
      public int wheelGizmoResolution = 32;
      public Color color = Color.white;
      public bool grounded;
      public bool drawSidewaysForce;
      public Color sidewaysForceColor = Color.red;

      // private variables
      [HideInInspector] public float hitDistance;
      Rigidbody carRb;
      float currentTorque;
      Vector3 sidewaysForce;

      void Awake () {
          carRb = GetComponentInParent<Rigidbody> ();
      }
      void Update () {
          SetWheelGrounded ();
      }

      void FixedUpdate () {
          if (grounded) {
              ApplySuspensionForce ();
              ApplySidewaysForce ();
              ApplyAccelerationForce ();
              ApplyBrakeForce ();
          }
      }

      void SetWheelGrounded () {
          RaycastHit hit;
          grounded = Physics.Raycast (transform.position, -transform.parent.up, out hit, radius, collisionLayerMask);
          hitDistance = hit.distance;
      }
      public void ApplySuspensionForce () {
          Vector3 springDir = transform.up;
          Vector3 tireWorldVel = carRb.GetPointVelocity (transform.position);
          float offset = suspensionRestDist - hitDistance;
          float vel = Vector3.Dot (springDir, tireWorldVel);
          float force = (offset * springStrength) - (vel * springDamper);
          carRb.AddForceAtPosition (springDir * force, transform.position);
      }

      void ApplySidewaysForce () {
          Vector3 wheelRight = transform.right;
          Vector3 tireWorldVel = carRb.GetPointVelocity (transform.position);
          float steeringVel = Vector3.Dot (wheelRight, tireWorldVel);
          float desiredVelChange = -steeringVel * tireGripFactor;
          float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
          sidewaysForce = wheelRight * mass * desiredAccel;
          carRb.AddForceAtPosition (sidewaysForce, transform.position);
      }

      void ApplyAccelerationForce () {
          Vector3 accelDir = transform.forward;
          if (motorTorque != 0) {
              float carSpeed = Vector3.Dot (carRb.transform.forward, carRb.velocity);
              float normalizedSpeed = (Mathf.Clamp01 (Mathf.Abs (carSpeed) / maxSpeed));
              currentTorque = powerCurve.Evaluate (normalizedSpeed) * motorTorque;
              carRb.AddForceAtPosition (accelDir * currentTorque, transform.position);
          }
      }

      public void ApplyBrakeForce () {
          if (brakeTorque > 0) {
              Vector3 tireWorldVel = carRb.GetPointVelocity (transform.position);
              Vector3 forwardDir = transform.forward;
              Vector3 brakeDir = -Vector3.Project (tireWorldVel, forwardDir.normalized).normalized;
              carRb.AddForceAtPosition (brakeDir * brakeTorque, transform.position);
          }
      }

      private void OnDrawGizmos () {
          // Draw Wheel
          Gizmos.color = color;
          Vector3 startPoint = transform.position + transform.right * radius;
          Matrix4x4 rotationMatrix = Matrix4x4.TRS (Vector3.zero, transform.rotation, Vector3.one);

          for (int i = 1; i <= wheelGizmoResolution; i++) {
              float angle = i * 2.0f * Mathf.PI / wheelGizmoResolution;
              Vector3 localEndPoint = new Vector3 (0.0f, Mathf.Sin (angle), Mathf.Cos (angle)) * radius;
              Vector3 endPoint = rotationMatrix.MultiplyPoint3x4 (localEndPoint) + transform.position;
              Gizmos.DrawLine (startPoint, endPoint);
              startPoint = endPoint;
          }

          // Draw Sideways Force
          if (drawSidewaysForce) {
              Gizmos.color = sidewaysForceColor;
              Gizmos.DrawLine (transform.position, transform.position + sidewaysForce); // Scale for visibility
          }
      }
  }