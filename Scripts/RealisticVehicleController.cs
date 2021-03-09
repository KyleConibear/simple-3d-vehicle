namespace Conibear {
	using UnityEngine;

	[RequireComponent(typeof(Rigidbody))]
	public class RealisticVehicleController : MonoBehaviour {
		#region SerializeFields

		[SerializeField]
		private GameObject m_realisticVehiclePrefab;

		#endregion


		#region Internal Fields

		private RealisticVehiclePrefab m_Vehicle;

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

		private void Awake() {
			this.SpawnCarBody();
		}

		private void Start() {
			this.InitializeRigidbody();
			this.InitializeVehicle();
		}

		private void FixedUpdate() {
			this.Accelerate(Input.GetAxis("Vertical"));
			this.Steer(Input.GetAxis("Horizontal"));

			this.ApplyDownwardForce();
			this.UpdateStabilizerBars();

			if (Input.GetKey(KeyCode.Space)) {
				this.ApplyBreak();
			} else {
				this.ReleaseBreak();
			}
		}

		private void LateUpdate() {
			UpdateWheelPositions();

			//Print.Message($"KilometerPerHour: <{this.KilometerPerHour}>", this);
		}

		#endregion


		#region Internal Methods

		private void SpawnCarBody() {
			m_Vehicle = Instantiate(m_realisticVehiclePrefab, this.transform).GetComponent<RealisticVehiclePrefab>();
		}

		private void InitializeRigidbody() {
			this.Rigidbody.mass = m_Vehicle.RigidbodyMass;
			this.Rigidbody.centerOfMass += m_Vehicle.CenterOfMassOffSet;
			this.Rigidbody.drag = RealisticVehiclePrefab.RigidbodyDrag;
			this.Rigidbody.angularDrag = RealisticVehiclePrefab.RigidbodyAngularDrag;
		}

		private void InitializeVehicle() {
			m_Vehicle.InitializeWheels(m_Rigidbody);
		}

		private void Steer(float input) {
			// Ackermann steering formula
			// Inner wheel turns slightly more than outer for improved control

			var wheelBase = m_Vehicle.WheelBase;
			var turnRadius = m_Vehicle.TurnRadius;
			var rearTrack = m_Vehicle.RearTrack;

			float leftSteeringAngle = 0;
			float rightSteeringAngle = 0;

			if (input > 0) {
				leftSteeringAngle = Mathf.Rad2Deg * Mathf.Atan((wheelBase / (turnRadius + (rearTrack / 2)))) * input;
				rightSteeringAngle = Mathf.Rad2Deg * Mathf.Atan((wheelBase / (turnRadius - (rearTrack / 2)))) * input;
			} else if (input < 0) {
				leftSteeringAngle = Mathf.Rad2Deg * Mathf.Atan((wheelBase / (turnRadius - (rearTrack / 2)))) * input;
				rightSteeringAngle = Mathf.Rad2Deg * Mathf.Atan((wheelBase / (turnRadius + (rearTrack / 2)))) * input;
			}

			m_Vehicle.FrontLeftWheel.ApplySteerAngle(leftSteeringAngle);
			m_Vehicle.FrontRightWheel.ApplySteerAngle(rightSteeringAngle);
		}

		private void Accelerate(float input) {
			int numberOfWheels = m_Vehicle.WheelDriveType == WheelDriveType.AllWheelDrive ? 4 : 2;
			float motorPower = input * (m_Vehicle.MotorForce / numberOfWheels);

			switch (m_Vehicle.WheelDriveType) {
				case WheelDriveType.RearWheelDrive:
					m_Vehicle.RearLeftWheel.ApplyMotorTorque(motorPower);
					m_Vehicle.RearRightWheel.ApplyMotorTorque(motorPower);
					break;
				case WheelDriveType.FrontWheelDrive:
					m_Vehicle.FrontLeftWheel.ApplyMotorTorque(motorPower);
					m_Vehicle.FrontRightWheel.ApplyMotorTorque(motorPower);
					break;
				case WheelDriveType.AllWheelDrive:
					m_Vehicle.FrontLeftWheel.ApplyMotorTorque(motorPower);
					m_Vehicle.FrontRightWheel.ApplyMotorTorque(motorPower);
					m_Vehicle.RearLeftWheel.ApplyMotorTorque(motorPower);
					m_Vehicle.RearRightWheel.ApplyMotorTorque(motorPower);
					break;
			}
		}

		private void ApplyBreak() {
			m_IsBreaking = true;
			m_Vehicle.FrontLeftWheel.ApplyBreak(m_Vehicle.BreakForce);
			m_Vehicle.FrontRightWheel.ApplyBreak(m_Vehicle.BreakForce);
			m_Vehicle.RearLeftWheel.ApplyBreak(m_Vehicle.BreakForce);
			m_Vehicle.RearRightWheel.ApplyBreak(m_Vehicle.BreakForce);
		}

		private void ReleaseBreak() {
			m_IsBreaking = false;
			m_Vehicle.FrontLeftWheel.ReleaseBreak();
			m_Vehicle.FrontRightWheel.ReleaseBreak();
			m_Vehicle.RearLeftWheel.ReleaseBreak();
			m_Vehicle.RearRightWheel.ReleaseBreak();
		}

		private void ApplyDownwardForce() {
			Rigidbody.AddForce(-transform.up * m_Vehicle.DownForce * Rigidbody.velocity.magnitude);
		}

		private void UpdateStabilizerBars() {
			this.UpdateFrontStabilizerBar();
			this.UpdateRearStabilizerBar();
		}

		private void UpdateFrontStabilizerBar() {
			var antiRollForce = (m_Vehicle.FrontLeftWheel.SuspensionTravel - m_Vehicle.FrontRightWheel.SuspensionTravel) * m_Vehicle.Spring;

			if (m_Vehicle.FrontLeftWheel.IsGrounded(out var leftWheelHit)) {
				var leftWheelForce = m_Vehicle.FrontLeftWheel.WheelCollider.transform.up * -antiRollForce;
				Rigidbody.AddForceAtPosition(leftWheelForce, m_Vehicle.FrontLeftWheel.WheelCollider.transform.position);
			}


			if (m_Vehicle.FrontRightWheel.IsGrounded(out var rightWheelHit)) {
				var rightWheelForce = m_Vehicle.FrontRightWheel.WheelCollider.transform.up * antiRollForce;
				Rigidbody.AddForceAtPosition(rightWheelForce, m_Vehicle.FrontRightWheel.WheelCollider.transform.position);
			}
		}

		private void UpdateRearStabilizerBar() {
			var antiRollForce = (m_Vehicle.RearLeftWheel.SuspensionTravel - m_Vehicle.RearRightWheel.SuspensionTravel) * m_Vehicle.Spring;

			if (m_Vehicle.RearLeftWheel.IsGrounded(out var leftWheelHit)) {
				var leftWheelForce = m_Vehicle.RearLeftWheel.WheelCollider.transform.up * -antiRollForce;
				Rigidbody.AddForceAtPosition(leftWheelForce, m_Vehicle.RearLeftWheel.WheelCollider.transform.position);
			}


			if (m_Vehicle.RearRightWheel.IsGrounded(out var rightWheelHit)) {
				var rightWheelForce = m_Vehicle.RearRightWheel.WheelCollider.transform.up * antiRollForce;
				Rigidbody.AddForceAtPosition(rightWheelForce, m_Vehicle.RearRightWheel.WheelCollider.transform.position);
			}
		}

		private void UpdateWheelPositions() {
			m_Vehicle.FrontLeftWheel.UpdateWheelPosition();
			m_Vehicle.FrontRightWheel.UpdateWheelPosition();
			m_Vehicle.RearLeftWheel.UpdateWheelPosition();
			m_Vehicle.RearRightWheel.UpdateWheelPosition();
		}

		#endregion
	}
}