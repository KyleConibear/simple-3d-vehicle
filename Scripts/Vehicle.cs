namespace Conibear {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	internal enum SpeedType {
		MPH,
		KPH
	}

	public class Vehicle : MonoBehaviour {
		#region Internal Fields

		protected const float MeterPerSecondConversionToMilesPerHour = 2.23693629f;

		private float m_GearFactor;

		#endregion


		#region SerializeFields

		[Header("Component References")]
		[SerializeField]
		private Rigidbody m_Rigidbody;

		[Header("Debug Show-Only Values")]
		[ShowOnly] [SerializeField]
		private int m_CurrentGear;

		[ShowOnly] [SerializeField]
		private float m_RevolutionsPerMinute = 0.01f;

		[ShowOnly] [SerializeField]
		private float m_CurrentSpeed = 0;

		[Header("Vehicle Stats")]
		[SerializeField]
		private SpeedType m_SpeedType;

		[SerializeField]
		private static int NoOfGears = 5;

		[SerializeField]
		private float m_Topspeed = 200;

		[SerializeField]
		private float m_RevRangeBoundary = 1f;

		#endregion


		#region Public Properties

		public Rigidbody Rigidbody => m_Rigidbody;

		public int CurrentGear => m_CurrentGear;

		protected float CurrentSpeed {
			get {
				m_CurrentSpeed = m_Rigidbody.velocity.magnitude * MeterPerSecondConversionToMilesPerHour;
				return m_CurrentSpeed;
			}
		}

		public float MaxSpeed => m_Topspeed;

		public float RPM {
			get => m_RevolutionsPerMinute;
			private set {
				if (value > 0.01f)
					m_RevolutionsPerMinute = value;
			}
		}

		#endregion


		#region MonoBehaviour Methods

		protected virtual void FixedUpdate() {
			this.CalculateRevs();
			this.GearChanging();
			this.CapSpeed();
		}

		#endregion


		#region Internal Methods

		/// <summary>
		/// calculate engine revs (for display / sound)
		// (this is done in retrospect - revs are not used in force/power calculations)
		/// </summary>
		private void CalculateRevs() {
			CalculateGearFactor();
			var gearNumFactor = m_CurrentGear / (float) NoOfGears;
			var revsRangeMin = Math.ULerp(0f, m_RevRangeBoundary, Math.CurveFactor(gearNumFactor));
			var revsRangeMax = Math.ULerp(m_RevRangeBoundary, 1f, gearNumFactor);
			RPM = Math.ULerp(revsRangeMin, revsRangeMax, m_GearFactor);
		}

		/// <summary>
		/// gear factor is a normalised representation of the current speed within the current gear's range of speeds.
		// We smooth towards the 'target' gear factor, so that revs don't instantly snap up or down when changing gear.
		/// </summary>
		private void CalculateGearFactor() {
			float f = (1 / (float) NoOfGears);
			var targetGearFactor = Mathf.InverseLerp(f * m_CurrentGear, f * (m_CurrentGear + 1), Mathf.Abs(CurrentSpeed / MaxSpeed));
			m_GearFactor = Mathf.Lerp(m_GearFactor, targetGearFactor, Time.deltaTime * 5f);
		}

		private void GearChanging() {
			float f = Mathf.Abs(CurrentSpeed / MaxSpeed);
			float upgearlimit = (1 / (float) NoOfGears) * (m_CurrentGear + 1);
			float downgearlimit = (1 / (float) NoOfGears) * m_CurrentGear;

			if (m_CurrentGear > 0 && f < downgearlimit) {
				m_CurrentGear--;
			}

			if (f > upgearlimit && (m_CurrentGear < (NoOfGears - 1))) {
				m_CurrentGear++;
			}
		}


		private void CapSpeed() {
			float speed = m_Rigidbody.velocity.magnitude;
			switch (m_SpeedType) {
				case SpeedType.MPH:

					speed *= 2.23693629f;
					if (speed > m_Topspeed)
						m_Rigidbody.velocity = (m_Topspeed / 2.23693629f) * m_Rigidbody.velocity.normalized;
					break;

				case SpeedType.KPH:
					speed *= 3.6f;
					if (speed > m_Topspeed)
						m_Rigidbody.velocity = (m_Topspeed / 3.6f) * m_Rigidbody.velocity.normalized;
					break;
			}
		}

		#endregion
	}
}