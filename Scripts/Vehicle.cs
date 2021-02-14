namespace Conibear {
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

		public void ApplyBreak(float breakForce) {
			this.WheelCollider.brakeTorque = breakForce;
		}

		public void ReleaseBreak() {
			this.WheelCollider.brakeTorque = 0;
		}

		public void UpdateWheelPosition() {
			var position = this.WheelModelTransform.position;
			var rotation = this.WheelModelTransform.rotation;

			this.WheelCollider.GetWorldPose(out position, out rotation);

			this.WheelModelTransform.position = position;
			this.WheelModelTransform.rotation = rotation;
		}
	}

	[RequireComponent(typeof(Rigidbody))]
	public class Vehicle : MonoBehaviour {
		#region SerializeFields

		[Header("Data")]
		[SerializeField]
		private VehicleData m_VehicleData;

		[Header("Wheels")]
		[SerializeField]
		private Wheel m_FrontLeftWheel;

		[SerializeField]
		private Wheel m_FrontRightWheel, m_RearLeftWheel, m_RearRightWheel;

		#endregion


		#region Internal Fields

		private Rigidbody m_Rigidbody;

		private bool m_IsBreaking = false;

		#endregion


		#region Internal Properties

		private Rigidbody Rigidbody {
			get {
				if (m_Rigidbody == null) {
					m_Rigidbody = GetComponent<Rigidbody>();
				}

				return m_Rigidbody;
			}
		}

		#endregion


		#region MonoBehaviour Methods

		private void FixedUpdate() {
			Steer(Input.GetAxis("Horizontal"));
			Accelerate(Input.GetAxis("Vertical"));

			if (Input.GetKey(KeyCode.Space)) {
				this.ApplyBreak();
			} else {
				this.ReleaseBreak();
			}
		}

		private void LateUpdate() {
			UpdateWheelPositions();
		}

		#endregion


		#region Internal Methods

		private void Steer(float input) {
			var m_steeringAngle = m_VehicleData.MaxSteerAngle * input;

			m_FrontLeftWheel.WheelCollider.steerAngle = m_steeringAngle;
			m_FrontRightWheel.WheelCollider.steerAngle = m_steeringAngle;
		}

		private void Accelerate(float input) {
			if (input < 0 && this.Rigidbody.velocity.z > 0 || input > 0 && this.Rigidbody.velocity.z < 0) { // moving in opposite direction of input
				this.ApplyBreak();
				return;
			}

			if (m_IsBreaking) {
				this.ReleaseBreak();
			}

			m_RearLeftWheel.WheelCollider.motorTorque = input * m_VehicleData.MotorForce;
			m_RearRightWheel.WheelCollider.motorTorque = input * m_VehicleData.MotorForce;
		}

		private void ApplyBreak() {
			m_IsBreaking = true;
			m_FrontLeftWheel.ApplyBreak(m_VehicleData.BreakForce);
			m_FrontRightWheel.ApplyBreak(m_VehicleData.BreakForce);
			m_RearLeftWheel.ApplyBreak(m_VehicleData.BreakForce);
			m_RearRightWheel.ApplyBreak(m_VehicleData.BreakForce);
		}

		private void ReleaseBreak() {
			m_IsBreaking = false;
			m_FrontLeftWheel.ReleaseBreak();
			m_FrontRightWheel.ReleaseBreak();
			m_RearLeftWheel.ReleaseBreak();
			m_RearRightWheel.ReleaseBreak();
		}

		private void UpdateWheelPositions() {
			m_FrontLeftWheel.UpdateWheelPosition();
			m_FrontRightWheel.UpdateWheelPosition();
			m_RearLeftWheel.UpdateWheelPosition();
			m_RearRightWheel.UpdateWheelPosition();
		}

		#endregion
	}
}