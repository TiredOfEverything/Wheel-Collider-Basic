using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour {

    [Foldout ("Wheel Settings")]
    public Wheel FL, FR, BL, BR;

    [Foldout ("Wheel Settings")]
    public float maxSteerAngle = 30f, steerSmoothing = 0.5f,
        steerInputMult = 0.1f;

    public DriveType driveType = DriveType.FWD;
    public float currentSteerAngle, currentMotorForce, currentBrakeForce;
    public enum DriveType { FWD, RWD, AWD }
    public float motorForce = 1000f, brakeForce = 2000f,
        boostForce = 10f, downForce = 0.1f;

    List<Wheel> wheels;
    float steerInput, accelerationInput;
    bool brakeInput, boostInput;
    Rigidbody rb;
    float flSteerAngle, frSteerAngle;

    void Start () {
        wheels = new List<Wheel> ();
        wheels.Add (FL);
        wheels.Add (FR);
        wheels.Add (BL);
        wheels.Add (BR);
        rb = GetComponent<Rigidbody> ();
    }
    void Update () {
        GetInput ();
        ApplySteerForce (currentSteerAngle);
    }

    void FixedUpdate () {
        ApplyMotorForce ();
        ApplyBrakeForce ();
        if (boostInput) {
            rb.AddForce (transform.forward * boostForce, ForceMode.Impulse);
        }
        rb.AddForce (Vector3.down * rb.velocity.magnitude * downForce);
    }

    public void GetInput () {
        currentSteerAngle = Mathf.Clamp(steerInput * steerInputMult, -maxSteerAngle, maxSteerAngle);
        currentMotorForce = accelerationInput * motorForce;
        currentBrakeForce = brakeInput ? brakeForce : 0f;
    }

    public void ApplySteerForce (float steerAngle) {
        float turnRadius = Mathf.Abs (1 / Mathf.Tan (steerAngle * Mathf.Deg2Rad));
        float wheelbase = FL.transform.localPosition.z + FR.transform.localPosition.z;
        float leftWheelOffset = wheelbase / 2f;
        float rightWheelOffset = -wheelbase / 2f;
        float leftSteerAngle = Mathf.Atan (wheelbase / (turnRadius + leftWheelOffset)) * Mathf.Rad2Deg;
        float rightSteerAngle = Mathf.Atan (wheelbase / (turnRadius + rightWheelOffset)) * Mathf.Rad2Deg;

        float targetLeftSteerAngle = Mathf.Sign (currentSteerAngle) * leftSteerAngle;
        float targetRightSteerAngle = Mathf.Sign (currentSteerAngle) * rightSteerAngle;

        flSteerAngle = Mathf.Lerp (flSteerAngle, targetLeftSteerAngle, Time.deltaTime * steerSmoothing);
        frSteerAngle = Mathf.Lerp (frSteerAngle, targetRightSteerAngle, Time.deltaTime * steerSmoothing);

        FL.transform.localRotation = Quaternion.Euler (new Vector3 (0f, flSteerAngle, 0f));
        FR.transform.localRotation = Quaternion.Euler (new Vector3 (0f, frSteerAngle, 0f));
    }

    public void ApplyMotorForce () {
        float motorTorque = currentMotorForce / (driveType == DriveType.AWD ? 4f : 2f);

        switch (driveType) {
            case DriveType.FWD:
                FL.motorTorque = FR.motorTorque = motorTorque;
                break;
            case DriveType.RWD:
                BL.motorTorque = BR.motorTorque = motorTorque;
                break;
            case DriveType.AWD:
                FL.motorTorque = FR.motorTorque = BL.motorTorque = BR.motorTorque = motorTorque;
                break;
        }
    }

    public void ApplyBrakeForce () {
        FL.brakeTorque = FR.brakeTorque = BL.brakeTorque = BR.brakeTorque = currentBrakeForce / (driveType == DriveType.AWD ? 4f : 2f);
    }

    void OnSteer (InputValue value) {
        steerInput = value.Get<float> ();
    }

    void OnAcceleration (InputValue value) {
        accelerationInput = value.Get<float> ();
    }

    void OnBrake (InputValue value) {
        brakeInput = value.isPressed;
    }

    void OnBoost (InputValue value) {
        boostInput = value.isPressed;
    }
}