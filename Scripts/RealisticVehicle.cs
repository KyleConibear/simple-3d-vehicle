using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RealisticVehicle : MonoBehaviour {
	#region SerializeFields

	[Header("Transforms")]
	[SerializeField]
	private Transform
		m_FrontLeftWheelModelTransform,
		m_FrontRightWheelModelTransform,
		m_RearLeftWheelModelTransform,
		m_RearRightWheelModelTransform;

	[Header("WheelColliders")]
	[SerializeField]
	private WheelCollider
		m_FrontLeftWheelCollider,
		m_FrontRightWheelCollider,
		m_RearLeftWheelCollider,
		m_RearRightWheelCollider;

	[Header("Controls")]
	[SerializeField]
	private float maxSteerAngle = 30;

	[SerializeField]
	private float motorForce = 50;

	#endregion


	#region MonoBehaviour Methods

	private void FixedUpdate() {
		Steer(Input.GetAxis("Horizontal"));
		Accelerate(Input.GetAxis("Vertical"));
	}

	private void LateUpdate() {
		UpdateWheelPoses();
	}

	#endregion


	#region Internal Methods

	private void Steer(float input) {
		var m_steeringAngle = maxSteerAngle * input;
		m_FrontLeftWheelCollider.steerAngle = m_steeringAngle;
		m_FrontRightWheelCollider.steerAngle = m_steeringAngle;
	}

	private void Accelerate(float input) {
		m_RearLeftWheelCollider.motorTorque = input * motorForce;
		m_RearRightWheelCollider.motorTorque = input * motorForce;
	}

	private void UpdateWheelPoses() {
		UpdateWheelPose(m_FrontLeftWheelCollider, m_FrontLeftWheelModelTransform);
		UpdateWheelPose(m_FrontRightWheelCollider, m_FrontRightWheelModelTransform);
		UpdateWheelPose(m_RearLeftWheelCollider, m_RearLeftWheelModelTransform);
		UpdateWheelPose(m_RearRightWheelCollider, m_RearRightWheelModelTransform);
	}

	private void UpdateWheelPose(WheelCollider collider, Transform transform) {
		var position = transform.position;
		var rotation = transform.rotation;

		collider.GetWorldPose(out position, out rotation);

		transform.position = position;
		transform.rotation = rotation;
	}

	#endregion
}