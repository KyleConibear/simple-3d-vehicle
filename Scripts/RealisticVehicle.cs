using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class Wheel {
	[SerializeField]
	private Transform m_WheelModelTransform;

	[SerializeField]
	private WheelCollider m_WheelCollider;

	public Transform WheelModelTransform => m_WheelModelTransform;
	public WheelCollider WheelCollider => m_WheelCollider;


	public void UpdateWheelPosition() {
		var position = WheelModelTransform.position;
		var rotation = WheelModelTransform.rotation;

		WheelCollider.GetWorldPose(out position, out rotation);

		WheelModelTransform.position = position;
		WheelModelTransform.rotation = rotation;
	}
}

public class RealisticVehicle : MonoBehaviour {
	#region SerializeFields

	[Header("Controls")]
	[SerializeField]
	private float maxSteerAngle = 30;

	[SerializeField]
	private float motorForce = 50;

	[Header("Wheels")]
	[SerializeField]
	private Wheel m_FrontLeftWheel;

	[SerializeField]
	private Wheel m_FrontRightWheel, m_RearLeftWheel, m_RearRightWheel;

	#endregion


	#region MonoBehaviour Methods

	private void FixedUpdate() {
		Steer(Input.GetAxis("Horizontal"));
		Accelerate(Input.GetAxis("Vertical"));
	}

	private void LateUpdate() {
		UpdateWheelPositions();
	}

	#endregion


	#region Internal Methods

	private void Steer(float input) {
		var m_steeringAngle = maxSteerAngle * input;

		m_FrontLeftWheel.WheelCollider.steerAngle = m_steeringAngle;
		m_FrontRightWheel.WheelCollider.steerAngle = m_steeringAngle;
	}

	private void Accelerate(float input) {
		m_RearLeftWheel.WheelCollider.motorTorque = input * motorForce;
		m_RearRightWheel.WheelCollider.motorTorque = input * motorForce;
	}

	private void Break() {
	}

	private void UpdateWheelPositions() {
		m_FrontLeftWheel.UpdateWheelPosition();
		m_FrontRightWheel.UpdateWheelPosition();
		m_RearLeftWheel.UpdateWheelPosition();
		m_RearRightWheel.UpdateWheelPosition();
	}

	#endregion
}