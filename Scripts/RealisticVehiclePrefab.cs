using System;

namespace Conibear {
	using UnityEngine;

	public enum WheelDriveType {
		RearWheelDrive,
		FrontWheelDrive,
		AllWheelDrive
	}

	[System.Serializable]
	public class Wheel {
		public enum AxelType {
			Front,
			Rear
		}

		[SerializeField]
		private AxelType m_AxelType = AxelType.Front;

		[SerializeField]
		private Transform m_WheelModelTransform;

		[SerializeField]
		private WheelCollider m_WheelCollider;

		public Transform WheelModelTransform => m_WheelModelTransform;

		public WheelCollider WheelCollider {
			get => m_WheelCollider;
		}

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

		public void InitializeWheelCollider(Rigidbody rigidbody, RealisticVehiclePrefab realisticVehicle) {
			var jointSpring = new JointSpring();

			var centerMassSpringDifference = realisticVehicle.Spring * (rigidbody.centerOfMass.z + 1) - realisticVehicle.Spring;

			// Adding more spring to the heavy part of the vehicle
			if (m_AxelType == AxelType.Front && rigidbody.centerOfMass.z > 0) {
				jointSpring.spring = realisticVehicle.Spring + centerMassSpringDifference;
			} else if (m_AxelType == AxelType.Rear && rigidbody.centerOfMass.z < 0) {
				jointSpring.spring = realisticVehicle.Spring - centerMassSpringDifference;
			} else {
				jointSpring.spring = realisticVehicle.Spring;
			}

			jointSpring.damper = jointSpring.spring * realisticVehicle.Damper;
			jointSpring.targetPosition = RealisticVehiclePrefab.WheelColliderSuspensionSpringTargetPosition;

			var forwardWheelFrictionCurve = new WheelFrictionCurve();
			forwardWheelFrictionCurve.extremumSlip = 0.4f; // unity default
			forwardWheelFrictionCurve.extremumValue = 1f; // unity default
			forwardWheelFrictionCurve.asymptoteSlip = 0.8f; // unity default
			forwardWheelFrictionCurve.asymptoteValue = 0.5f; // unity default
			forwardWheelFrictionCurve.stiffness = realisticVehicle.ForwardStiffness;

			var sidewaysWheelFrictionCurve = new WheelFrictionCurve();
			sidewaysWheelFrictionCurve.extremumSlip = 0.2f; // unity default
			sidewaysWheelFrictionCurve.extremumValue = 1f; // unity default
			sidewaysWheelFrictionCurve.asymptoteSlip = 0.5f; // unity default
			sidewaysWheelFrictionCurve.asymptoteValue = 0.75f; // unity default

			switch (m_AxelType) {
				case AxelType.Front:
					sidewaysWheelFrictionCurve.stiffness = realisticVehicle.FrontWheelsSidwaysStiffness;
					break;
				case AxelType.Rear:
					sidewaysWheelFrictionCurve.stiffness = realisticVehicle.RearWheelsSidewaysStiffness;
					break;
			}

			/* forceAppPointDistance parameter simulates the effective roll center of the suspension geometry.
			  For a standard family car the value of forceAppPointDistance should be tuned to place the application point approximately 0.3m below the rigid body center of mass.
			  Moving the application point downwards introduces more roll when cornering, while moving it upwards results in less roll when cornering.
			  The force application point is typically below the rigid body center of mass. */
			this.WheelCollider.forceAppPointDistance = rigidbody.centerOfMass.y - realisticVehicle.ForceAppPointDistance;

			this.WheelCollider.radius = realisticVehicle.WheelRadius;
			this.WheelCollider.suspensionDistance = realisticVehicle.SuspensionDistance;
			this.WheelCollider.center = realisticVehicle.WheelCenter;
			this.WheelCollider.suspensionSpring = jointSpring;
			this.WheelCollider.forwardFriction = forwardWheelFrictionCurve;
			this.WheelCollider.sidewaysFriction = sidewaysWheelFrictionCurve;
		}

		public void ApplyMotorTorque(float motorTorque) {
			m_WheelCollider.motorTorque = motorTorque;
		}

		public void ApplyBreak(float breakForce) {
			this.WheelCollider.brakeTorque = breakForce;
		}

		public void ReleaseBreak() {
			this.WheelCollider.brakeTorque = 0;
		}

		public void ApplySteerAngle(float steerAngle) {
			m_WheelCollider.steerAngle = steerAngle;
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

	public class RealisticVehiclePrefab : MonoBehaviour {
		#region Public Fields

		public const float RigidbodyDrag = 0f;
		public const float RigidbodyAngularDrag = 0f;
		public const float WheelColliderSuspensionSpringTargetPosition = 0.5f;

		#endregion


		#region ShowOnly Fields

		[Header("ReadOnly Stats")]
		[ShowOnly] [SerializeField]
		[Tooltip("The distance between the front and rear axles")]
		private float m_WheelBase = 0f;

		[ShowOnly] [SerializeField]
		[Tooltip("The width between the front wheels")]
		private float m_FrontTrack = 0f;

		[ShowOnly] [SerializeField]
		[Tooltip("The width between the rear wheels")]
		private float m_RearTrack = 0f;

		#endregion


		#region SerializeField

		[Header("Wheels")]
		[SerializeField]
		private Wheel m_FrontLeftWheel;

		[SerializeField]
		private Wheel m_FrontRightWheel, m_RearLeftWheel, m_RearRightWheel;

		[Header("General Car Specs")]
		[SerializeField]
		private WheelDriveType m_WheelDriveType = WheelDriveType.RearWheelDrive;

		[SerializeField]
		[Tooltip("Is relative to Mass")]
		//[Range(1, 10)]
		private float m_MotorForce = 500;

		[SerializeField]
		[Range(0, 100)]
		private int m_DownForce = 50;

		[SerializeField]
		[Range(1.25f, 5)]
		[Tooltip("The percentage multiplier of motorForce.")]
		private float m_BreakForcePercent = 2f;

		[SerializeField]
		[Range(30, 45)]
		private float m_MaxSteerAngle = 30;

		[SerializeField]
		[Range(6, 24)]
		[Tooltip("The tightest circle it can make with the steering wheel turned full to one side.")]
		private float m_TurnRadius = 9f;

		[Header("Rigidbody Settings")]
		[SerializeField]
		[Tooltip("Average sedan = 2000")]
		[Range(1000, 10000)]
		private int m_Mass = 2000;

		[SerializeField]
		[Range(-1, 1)]
		private float m_CenterOfMassYPosition = 0f;

		[SerializeField]
		[Range(-1, 1)]
		private float m_CenterOfMassZPosition = 0f;

		[Header("Wheel Collider Settings")]
		[Header("Wheels")]
		[SerializeField]
		[Range(0.1f, 1)]
		private float m_WheelRadius = 0.5f;

		[SerializeField]
		private Vector3 m_WheelCenter = Vector3.zero;

		[SerializeField]
		[Range(0.1f, 1)]
		[Tooltip("The size (height) of the spring.")]
		private float m_SuspensionDistance = 0.3f;

		[SerializeField]
		[Range(0.1f, 0.5f)]
		[Tooltip("Application point of the suspension and tire forces measured from the base of the resting wheel.\n\n" +
		         "This is specified as a distance along the local up vector of the vehicle's rigidbody from the base of the wheel\n" +
		         "at its rest coordinate (the rest coordinate of the wheel is determined by the value WheelCollider.spring.targetPosition).\n" +
		         "This parameter simulates the effective roll center of the suspension geometry.\n" +
		         "For a standard family car the value of forceAppPointDistance should be tuned to place the application point approximately 0.3m below the rigidbody center of mass.\n" +
		         "Moving the application point downwards introduces more roll when cornering, while moving it upwards results in less roll when cornering.\n" +
		         "The force application point is typically below the rigid body center of mass.")]
		private float m_ForceAppPointDistance = 0.3f;

		[Header("Suspension Spring")]
		[SerializeField]
		[Range(15, 30)]
		[Tooltip("Spring force attempts to reach the Target Position.\nA larger value makes the suspension reach the Target Position faster.")]
		private float m_SpringMultipleOfMass = 23.5f;

		[SerializeField]
		[Range(3, 24)]
		[Tooltip("Dampens the suspension velocity.\nA larger value makes the Suspension Spring move slower.")]
		private int m_DamperAsPercentageOfSpring = 12;

		[Header(("Forward Friction"))]
		[SerializeField]
		[Range(1, 3)]
		private float m_ForwardStiffness = 1.82f;

		[Header("Sideways Friction")]
		[SerializeField]
		[Range(0.5f, 1.5f)]
		[Tooltip("Having less front wheel stiffness than the rear can prevent spinning out")]
		private float m_FrontWheelsSidewaysStiffness = 1.18f;

		[SerializeField]
		[Range(0.5f, 1.5f)]
		[Tooltip("Having more rear wheel stiffness than the front can prevent spinning out")]
		private float m_RearWheelsSidewaysStiffness = 1.22f;

		#endregion


		#region Public Properties

		public Wheel FrontLeftWheel => m_FrontLeftWheel;
		public Wheel FrontRightWheel => m_FrontRightWheel;
		public Wheel RearLeftWheel => m_RearLeftWheel;
		public Wheel RearRightWheel => m_RearRightWheel;
		public WheelDriveType WheelDriveType => m_WheelDriveType;
		public float MotorForce => m_MotorForce * this.Mass;
		public int DownForce => m_DownForce;
		public float BreakForce => m_MotorForce * m_BreakForcePercent;
		public float MaxSteerAngle => m_MaxSteerAngle;

		public float WheelBase {
			get {
				if (m_WheelBase == 0f) {
					m_WheelBase = Mathf.Abs(this.FrontLeftWheel.WheelCollider.transform.position.z - this.RearLeftWheel.WheelCollider.transform.position.z);
				}

				return m_WheelBase;
			}
		}

		public float FrontTrack {
			get {
				if (m_FrontTrack == 0f) {
					m_FrontTrack = Mathf.Abs(this.FrontLeftWheel.WheelCollider.transform.position.x - this.FrontRightWheel.WheelCollider.transform.position.x);
				}

				return m_FrontTrack;
			}
		}

		public float RearTrack {
			get {
				if (m_RearTrack == 0f) {
					m_RearTrack = Mathf.Abs(this.RearLeftWheel.WheelCollider.transform.position.x - this.RearRightWheel.WheelCollider.transform.position.x);
				}

				return m_RearTrack;
			}
		}

		public float TurnRadius => m_TurnRadius;
		public int Mass => m_Mass;
		public Vector3 CenterOfMassOffSet => new Vector3(0, m_CenterOfMassYPosition, m_CenterOfMassZPosition);
		public float WheelRadius => m_WheelRadius;
		public Vector3 WheelCenter => m_WheelCenter;

		public float SuspensionDistance => m_SuspensionDistance;

		public float ForceAppPointDistance => m_ForceAppPointDistance;

		public float Spring => this.Mass * m_SpringMultipleOfMass;
		public float Damper => m_DamperAsPercentageOfSpring * 0.01f;
		public float ForwardStiffness => m_ForwardStiffness;
		public float FrontWheelsSidwaysStiffness => m_FrontWheelsSidewaysStiffness;
		public float RearWheelsSidewaysStiffness => m_RearWheelsSidewaysStiffness;

		#endregion


		#region MonoBehaviour Methods

		private void OnEnable() {
			this.InitializeFields();
		}

		#endregion


		#region Internal Methods

		private void InitializeFields() {
			var wheelBase = this.WheelBase;
			var frontTrack = this.FrontTrack;
			var rearTrack = this.RearTrack;
		}

		public void InitializeWheels(Rigidbody rigidbody) {
			this.FrontLeftWheel.InitializeWheelCollider(rigidbody, this);
			this.FrontRightWheel.InitializeWheelCollider(rigidbody, this);
			this.RearLeftWheel.InitializeWheelCollider(rigidbody, this);
			this.RearRightWheel.InitializeWheelCollider(rigidbody, this);
		}

		#endregion
	}
}