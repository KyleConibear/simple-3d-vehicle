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

		public void InitializeWheelCollider(RealisticVehicleData realisticVehicleData) {
			var jointSpring = new JointSpring();
			jointSpring.spring = realisticVehicleData.Spring;
			jointSpring.damper = realisticVehicleData.Damper;
			jointSpring.targetPosition = realisticVehicleData.TargetPosition;

			var forwardWheelFrictionCurve = new WheelFrictionCurve();
			forwardWheelFrictionCurve.extremumSlip = 0.4f; // unity default
			forwardWheelFrictionCurve.extremumValue = 1f; // unity default
			forwardWheelFrictionCurve.asymptoteSlip = 0.8f; // unity default
			forwardWheelFrictionCurve.asymptoteValue = 0.5f; // unity default
			forwardWheelFrictionCurve.stiffness = realisticVehicleData.ForwardStiffness;

			var sidewaysWheelFrictionCurve = new WheelFrictionCurve();
			sidewaysWheelFrictionCurve.extremumSlip = 0.2f; // unity default
			sidewaysWheelFrictionCurve.extremumValue = 1f; // unity default
			sidewaysWheelFrictionCurve.asymptoteSlip = 0.5f; // unity default
			sidewaysWheelFrictionCurve.asymptoteValue = 0.75f; // unity default
			sidewaysWheelFrictionCurve.stiffness = realisticVehicleData.SidwaysStiffness;

			this.WheelCollider.radius = realisticVehicleData.WheelRadius;
			this.WheelCollider.center = realisticVehicleData.WheelCenter;
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
	public class RealisticVehicle : MonoBehaviour {
		#region SerializeFields

		[Header("Data")]
		[SerializeField]
		private RealisticVehicleData m_RealisticVehicleData;

		[Header("Wheels")]
		[SerializeField]
		private Wheel m_FrontLeftWheel;

		[SerializeField]
		private Wheel m_FrontRightWheel, m_RearLeftWheel, m_RearRightWheel;

		#endregion


		#region Internal Fields

		private const float m_MetersPerSecondConversionRateToKilometerPerHour = 3.6f;

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


		#region Public Properties

		public float KilometerPerHour {
			get {
				var velocity = this.Rigidbody.velocity;
				var forwardWheelRotation = this.Rigidbody.transform.rotation * Vector3.forward;
				float vProj = Vector3.Dot(velocity, forwardWheelRotation);
				var projVelocity = vProj * forwardWheelRotation;
				float speed = projVelocity.magnitude * Mathf.Sign(vProj);
				return speed * m_MetersPerSecondConversionRateToKilometerPerHour;
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

			Print.Message($"KilometerPerHour: <{this.KilometerPerHour}>", this);
		}

		#endregion


		#region Internal Methods

		private void InitializeRigidbody() {
			this.Rigidbody.mass = m_RealisticVehicleData.Mass;
			this.Rigidbody.centerOfMass += m_RealisticVehicleData.CenterOfMassOffSet;
			this.Rigidbody.drag = m_RealisticVehicleData.Drag;
			this.Rigidbody.angularDrag = m_RealisticVehicleData.AngularDrag;
		}

		private void InitializeWheels() {
			var spring = m_RealisticVehicleData.Spring;
			var forwardStiffness = m_RealisticVehicleData.ForwardStiffness;
			var sidewaysStiffness = m_RealisticVehicleData.SidwaysStiffness;

			m_FrontLeftWheel.InitializeWheelCollider(m_RealisticVehicleData);
			m_FrontRightWheel.InitializeWheelCollider(m_RealisticVehicleData);
			m_RearLeftWheel.InitializeWheelCollider(m_RealisticVehicleData);
			m_RearRightWheel.InitializeWheelCollider(m_RealisticVehicleData);
		}

		public float radius = 6;

		private void Steer(float input) {
			// Ackermann steering formula
			// Inner wheel turns slightly more than outer for improved control

			var wheelBase = m_RealisticVehicleData.WheelBase;
			var turnRadius = m_RealisticVehicleData.TurnRadius;
			var rearTrack = m_RealisticVehicleData.RearTrack;
			
			float leftSteeringAngle = 0;
			float rightSteeringAngle = 0;

			if (input > 0) {
				leftSteeringAngle = Mathf.Rad2Deg * Mathf.Atan((wheelBase / (turnRadius + (rearTrack / 2)))) * input;
				rightSteeringAngle = Mathf.Rad2Deg * Mathf.Atan((wheelBase / (turnRadius - (rearTrack / 2)))) * input;
			} else if (input < 0) {
				leftSteeringAngle = Mathf.Rad2Deg * Mathf.Atan((wheelBase / (turnRadius - (rearTrack / 2)))) * input;
				rightSteeringAngle = Mathf.Rad2Deg * Mathf.Atan((wheelBase / (turnRadius + (rearTrack / 2)))) * input;
			}

			m_FrontLeftWheel.WheelCollider.steerAngle = leftSteeringAngle;
			m_FrontRightWheel.WheelCollider.steerAngle = rightSteeringAngle;
		}

		private void Accelerate(float input) {
			if (input == 0) {
				return;
			}

			var localVelocity = this.Rigidbody.transform.InverseTransformDirection(this.Rigidbody.velocity);
			if (input < 0f && localVelocity.z > 0f || input > 0f && localVelocity.z < 0f) { // moving in opposite direction of input
				this.ApplyBreak();
				return;
			}

			if (m_IsBreaking) {
				this.ReleaseBreak();
			}

			int numberOfWheels = m_RealisticVehicleData.WheelDriveType == WheelDriveType.AllWheelDrive ? 4 : 2;
			float moterPower = input * (m_RealisticVehicleData.MotorForce / numberOfWheels);

			switch (m_RealisticVehicleData.WheelDriveType) {
				case WheelDriveType.RearWheelDrive:
					m_RearLeftWheel.WheelCollider.motorTorque = moterPower;
					m_RearRightWheel.WheelCollider.motorTorque = moterPower;
					break;
				case WheelDriveType.FrontWheelDrive:
					m_FrontLeftWheel.WheelCollider.motorTorque = moterPower;
					m_FrontRightWheel.WheelCollider.motorTorque = moterPower;
					break;
				case WheelDriveType.AllWheelDrive:
					m_FrontLeftWheel.WheelCollider.motorTorque = moterPower;
					m_FrontRightWheel.WheelCollider.motorTorque = moterPower;
					m_RearLeftWheel.WheelCollider.motorTorque = moterPower;
					m_RearRightWheel.WheelCollider.motorTorque = moterPower;
					break;
			}
		}

		private void ApplyBreak() {
			m_IsBreaking = true;
			m_FrontLeftWheel.ApplyBreak(m_RealisticVehicleData.BreakForce);
			m_FrontRightWheel.ApplyBreak(m_RealisticVehicleData.BreakForce);
			m_RearLeftWheel.ApplyBreak(m_RealisticVehicleData.BreakForce);
			m_RearRightWheel.ApplyBreak(m_RealisticVehicleData.BreakForce);
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
			var antiRollForce = (m_FrontLeftWheel.SuspensionTravel - m_FrontRightWheel.SuspensionTravel) * this.m_RealisticVehicleData.Spring;

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
			var antiRollForce = (m_RearLeftWheel.SuspensionTravel - m_RearRightWheel.SuspensionTravel) * this.m_RealisticVehicleData.Spring;

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