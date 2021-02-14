namespace Conibear {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	[CreateAssetMenu(menuName = "VehicleData")]
	public class VehicleData : ScriptableObject {
		#region SerializeField

		[SerializeField]
		private float m_MaxSteerAngle = 30;

		[SerializeField]
		private float m_MotorForce = 500;

		[SerializeField]
		[Tooltip("percentage multiplier of motorForce")]
		[Range(1.25f, 5)]
		private float m_BreakForcePercent = 2f;

		#endregion


		#region Public Properties

		public float MaxSteerAngle => m_MaxSteerAngle;
		public float MotorForce => m_MotorForce;
		public float BreakForce => m_MotorForce * m_BreakForcePercent;

		#endregion
	}
}