using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Vehicle : MonoBehaviour {
	[SerializeField]
	private WheelCollider m_FrontLeftWheel, m_FrontRightWheel, m_RearLeftWheel, m_ReadRightWheel;


	public float maxSteerAngle = 30;
	public float motorForce = 50;

	private float m_horizontalInput;
	private float m_steeringAngle;
	private float m_verticalInput;

	private void FixedUpdate() {
		GetInput();
		Steer();
		Accelerate();
		//UpdateWheelPoses();
	}

	public void GetInput() {
		m_horizontalInput = Input.GetAxis("Horizontal");
		m_verticalInput = Input.GetAxis("Vertical");
	}

	private void Steer() {
		m_steeringAngle = maxSteerAngle * m_horizontalInput;
		m_FrontLeftWheel.steerAngle = m_steeringAngle;
		m_FrontRightWheel.steerAngle = m_steeringAngle;
	}

	private void Accelerate() {
		m_FrontLeftWheel.motorTorque = m_verticalInput * motorForce;
		m_FrontRightWheel.motorTorque = m_verticalInput * motorForce;
	}

	private void UpdateWheelPoses() {
		UpdateWheelPose(m_FrontLeftWheel, m_FrontLeftWheel.transform);
		UpdateWheelPose(m_FrontRightWheel, m_FrontRightWheel.transform);
		UpdateWheelPose(m_RearLeftWheel, m_RearLeftWheel.transform);
		UpdateWheelPose(m_ReadRightWheel, m_ReadRightWheel.transform);
	}

	private void UpdateWheelPose(WheelCollider _collider, Transform _transform) {
		Vector3 _pos = _transform.position;
		Quaternion _quat = _transform.rotation;

		_collider.GetWorldPose(out _pos, out _quat);

		_transform.position = _pos;
		_transform.rotation = _quat;
	}
}