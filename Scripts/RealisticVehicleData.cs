namespace Conibear {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	[CreateAssetMenu(menuName = "RealisticVehicleData")]
	public class RealisticVehicleData : ScriptableObject {
		#region SerializeField

		[SerializeField]
		private float m_MaxSteerAngle = 30;

		[SerializeField]
		private float m_MotorForce = 500;

		[SerializeField]
		[Tooltip("percentage multiplier of motorForce")]
		[Range(1.25f, 5)]
		private float m_BreakForcePercent = 2f;


		[Header("Rigidbody Settings")]
		[SerializeField]
		[Tooltip("Average sedan = 2000")]
		[Range(1000, 10000)]
		private int m_Mass = 2000;

		[SerializeField]
		[Tooltip("General over the drivers seat.\nOffset from calculation using sum of colliders")]
		private Vector3 m_CenterOfMassOffSet = Vector3.zero;

		[SerializeField]
		[Range(0, 0.2f)]
		private float m_Drag = 0.1f;

		[SerializeField]
		[Range(0, 0.1f)]
		private float m_AngularDrag = 0.05f;

		[Header("Wheel Collider Settings")]
		[Header("Wheels")]
		[SerializeField]
		[Range(0.1f, 1)]
		private float m_WheelRadius = 0.5f;

		[SerializeField]
		private Vector3 m_WheelCenter = Vector3.zero;

		[Header("Suspension Spring")]
		[Tooltip("Spring force attempts to reach the Target Position.\nA larger value makes the suspension reach the Target Position faster.")]
		[SerializeField]
		[Range(15, 20)]
		private float m_SpringMultipleOfMass = 17.5f;

		[Tooltip("Dampens the suspension velocity.\nA larger value makes the Suspension Spring move slower.")]
		[SerializeField]
		[Range(0.1f, 0.2f)]
		private float m_DamperAsPercentageOfSpring = 0.12f;

		[Tooltip("The suspension’s rest distance along Suspension Distance.\n1 maps to fully extended suspension, and 0 maps to fully compressed suspension.\nDefault value is 0.5, which matches the behavior of a regular car’s suspension.")]
		[SerializeField]
		[Range(0, 1)]
		private float m_TargetPosition = 0.5f;

		[Header(("Forward Friction"))]
		[SerializeField]
		[Range(0, 2)]
		private float m_ForwardStiffness = 0.82f;

		[Header("Sideways Friction")]
		[SerializeField]
		[Range(0, 1)]
		private float m_SidewaysStiffness = 0.22f;

		#endregion


		#region Public Properties

		public float MaxSteerAngle => m_MaxSteerAngle;
		public float MotorForce => m_MotorForce;
		public float BreakForce => m_MotorForce * m_BreakForcePercent;

		public int Mass => m_Mass;
		public Vector3 CenterOfMassOffSet => m_CenterOfMassOffSet;
		public float Drag => m_Drag;
		public float AngularDrag => m_AngularDrag;
		public float WheelRadius => m_WheelRadius;
		public Vector3 WheelCenter => m_WheelCenter;
		public float Spring => this.Mass * m_SpringMultipleOfMass;
		public float Damper => this.Spring * m_DamperAsPercentageOfSpring;
		public float TargetPosition => m_TargetPosition;
		public float ForwardStiffness => m_ForwardStiffness + 1;
		public float SidwaysStiffness => m_SidewaysStiffness + 1;

		#endregion
	}
}