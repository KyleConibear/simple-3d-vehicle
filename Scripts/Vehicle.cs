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


		public float SuspensionTravel {
			get {
				var wheel = this.WheelCollider;
				float travel = 1.0f;
				WheelHit hit;
				if (this.IsGrounded(out hit)) {
					travel = (-wheel.transform.InverseTransformPoint(hit.point).y - wheel.radius) / wheel.suspensionDistance;
				}

				return travel;
			}
		}

		public void InitializeWheelCollider(VehicleData vehicleData) {
			var jointSpring = new JointSpring();
			jointSpring.spring = vehicleData.Spring;
			jointSpring.damper = vehicleData.Damper;
			jointSpring.targetPosition = vehicleData.TargetPosition;

			var forwardWheelFrictionCurve = new WheelFrictionCurve();
			forwardWheelFrictionCurve.extremumSlip = 0.4f; // unity default
			forwardWheelFrictionCurve.extremumValue = 1f; // unity default
			forwardWheelFrictionCurve.asymptoteSlip = 0.8f; // unity default
			forwardWheelFrictionCurve.asymptoteValue = 0.5f; // unity default
			forwardWheelFrictionCurve.stiffness = vehicleData.ForwardStiffness;

			var sidewaysWheelFrictionCurve = new WheelFrictionCurve();
			sidewaysWheelFrictionCurve.extremumSlip = 0.2f; // unity default
			sidewaysWheelFrictionCurve.extremumValue = 1f; // unity default
			sidewaysWheelFrictionCurve.asymptoteSlip = 0.5f; // unity default
			sidewaysWheelFrictionCurve.asymptoteValue = 0.75f; // unity default
			sidewaysWheelFrictionCurve.stiffness = vehicleData.SidwaysStiffness;

			this.WheelCollider.suspensionSpring = jointSpring;
			this.WheelCollider.forwardFriction = forwardWheelFrictionCurve;
			this.WheelCollider.sidewaysFriction = sidewaysWheelFrictionCurve;
		}

		public void ApplyBreak(float breakForce) {
			this.WheelCollider.brakeTorque = breakForce;
		}

		public void ReleaseBreak() {
			this.WheelCollider.brakeTorque = 0;
		}

		public bool IsGrounded(out WheelHit hit) {
			return this.WheelCollider.GetGroundHit(out hit);
		}

		public void UpdateWheelPosition() {
			if (this.WheelModelTransform == null)
				return;

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

		private void Start() {
			this.InitializeRigidbody();
			this.InitializeWheels();
		}

		private void FixedUpdate() {
			this.Steer(Input.GetAxis("Horizontal"));
			this.Accelerate(Input.GetAxis("Vertical"));
			this.UpdateStabilizerBars();

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

		private void InitializeRigidbody() {
			this.Rigidbody.mass = m_VehicleData.Mass;
			this.Rigidbody.centerOfMass += new Vector3(0, 0, m_VehicleData.CenterOfMassOffSet);
			this.Rigidbody.drag = m_VehicleData.Drag;
			this.Rigidbody.angularDrag = m_VehicleData.AngularDrag;
		}

		private void InitializeWheels() {
			var spring = m_VehicleData.Spring;
			var forwardStiffness = m_VehicleData.ForwardStiffness;
			var sidewaysStiffness = m_VehicleData.SidwaysStiffness;

			m_FrontLeftWheel.InitializeWheelCollider(m_VehicleData);
			m_FrontRightWheel.InitializeWheelCollider(m_VehicleData);
			m_RearLeftWheel.InitializeWheelCollider(m_VehicleData);
			m_RearRightWheel.InitializeWheelCollider(m_VehicleData);
		}

		private void Steer(float input) {
			var m_steeringAngle = m_VehicleData.MaxSteerAngle * input;

			m_FrontLeftWheel.WheelCollider.steerAngle = m_steeringAngle;
			m_FrontRightWheel.WheelCollider.steerAngle = m_steeringAngle;
		}

		private void Accelerate(float input) {
			if (input == 0) {
				return;
			}

			Print.Message($"this.Rigidbody.velocity.z: <{this.Rigidbody.velocity.z}>");
			var localVelocity = this.Rigidbody.transform.InverseTransformDirection(this.Rigidbody.velocity);
			if (input < 0f && localVelocity.z > 0f || input > 0f && localVelocity.z < 0f) { // moving in opposite direction of input
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

		private void UpdateStabilizerBars() {
			this.UpdateFrontStabilizerBar();
			this.UpdateRearStabilizerBar();
		}

		private void UpdateFrontStabilizerBar() {
			var antiRollForce = (m_FrontLeftWheel.SuspensionTravel - m_FrontRightWheel.SuspensionTravel) * this.m_VehicleData.Spring;

			if (m_FrontLeftWheel.IsGrounded(out var leftWheelHit)) {
				var leftWheelForce = m_FrontLeftWheel.WheelCollider.transform.up * -antiRollForce;
				m_Rigidbody.AddForceAtPosition(leftWheelForce, m_FrontLeftWheel.WheelCollider.transform.position);
			}


			if (m_FrontRightWheel.IsGrounded(out var rightWheelHit)) {
				var rightWheelForce = m_FrontRightWheel.WheelCollider.transform.up * antiRollForce;
				m_Rigidbody.AddForceAtPosition(rightWheelForce, m_FrontRightWheel.WheelCollider.transform.position);
			}
		}

		private void UpdateRearStabilizerBar() {
			var antiRollForce = (m_RearLeftWheel.SuspensionTravel - m_RearRightWheel.SuspensionTravel) * this.m_VehicleData.Spring;

			if (m_RearLeftWheel.IsGrounded(out var leftWheelHit)) {
				var leftWheelForce = m_RearLeftWheel.WheelCollider.transform.up * -antiRollForce;
				m_Rigidbody.AddForceAtPosition(leftWheelForce, m_RearLeftWheel.WheelCollider.transform.position);
			}


			if (m_RearRightWheel.IsGrounded(out var rightWheelHit)) {
				var rightWheelForce = m_RearRightWheel.WheelCollider.transform.up * antiRollForce;
				m_Rigidbody.AddForceAtPosition(rightWheelForce, m_RearRightWheel.WheelCollider.transform.position);
			}
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