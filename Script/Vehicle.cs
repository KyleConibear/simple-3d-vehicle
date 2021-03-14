namespace Conibear {
	using System.Collections;
	using UnityEngine;


	public class Vehicle : MonoBehaviour {
		#region Internal Consts

		private const float RigidbodyDrag = 5f;
		private const float MaxTurnAngle = 30f;
		private const float ShiftDelay = 0.5f;
		private const float SkidAudioThresholdFactor = 0.9f;
		private const float TerminalVelocityThresholdFactor = 0.9f;
		private const float MeterPerSecondConversionToMilesPerHour = 2.23693629f;
		private const float GravityMultiplier = 3;

		#endregion


		#region ShowOnly SerializeFields

		[Header("Debug Stats")]
		[ShowOnly] [SerializeField]
		private int m_Power = 0;

		[ShowOnly] [SerializeField]
		private int m_Force;

		[ShowOnly] [SerializeField]
		private int m_CurrentGear = 1;

		[ShowOnly] [SerializeField]
		private float m_CurrentSpeed = 0;

		#endregion


		#region SerializeFields

		[Header("Component References")]
		[SerializeField]
		private Rigidbody m_Rigidbody;

		[SerializeField]
		private AnimationCurve[] m_GearCurves;

		[SerializeField]
		private float m_Duration;

		[SerializeField]
		private AudioSource m_EngineSound, m_SkidSound;

		[SerializeField]
		private float m_PlaySkidAudioThreshold = MaxTurnAngle * SkidAudioThresholdFactor;

		[SerializeField]
		private float m_SkidFadeSpeed = 2f;

		[SerializeField]
		protected Transform m_FLWheel, m_FRWheel;

		[SerializeField]
		private uint m_ForwardForce = 200;

		[SerializeField]
		private SphereColliderGroundChecker m_SphereGroundCheck;

		#endregion


		#region Internal Fields

		private float m_MoveInput;
		private float m_PreviousMoveInput;
		private float m_TurnInput;

		[SerializeField]
		private float m_ForceLerp = 0;

		private IEnumerator m_ShiftCourtine;

		#endregion


		#region Internal Properties

		private bool IsGrounded => m_SphereGroundCheck.IsRigidbodyGrounded(m_Rigidbody);

		private float Force {
			get {
				if (m_MoveInput != 0) {
					m_ForceLerp += Time.deltaTime / m_Duration;
					if (m_ForceLerp > 1)
						m_ForceLerp = 1;
				} else {
					m_ForceLerp -= Time.deltaTime / (m_Duration * 5); // replace magic number with const

					if (m_ForceLerp < 0)
						m_ForceLerp = 0;
				}

				m_Force = (int) (Power * this.GearPowerCurve.Evaluate(m_ForceLerp));
				return m_Force;
			}
		}

		private float CurrentSpeed => m_CurrentSpeed = m_Rigidbody.velocity.magnitude * MeterPerSecondConversionToMilesPerHour;
		private float TerminalVelocity => Math.TerminalVelocity(m_Rigidbody, transform.forward * this.Power);
		private bool HasReachedTerminalVelocityThreshold => this.m_Rigidbody.velocity.magnitude >= (this.TerminalVelocity * TerminalVelocityThresholdFactor);
		private int PowerStep => (int) (m_ForwardForce / this.NumberOfGears);

		private int Power {
			get {
				if (m_MoveInput < 0) {
					m_Power = -PowerStep; // Driving in reverse
				} else {
					m_Power = PowerStep * m_CurrentGear;
				}

				return m_Power;
			}
		}

		private int NumberOfGears => m_GearCurves.Length;
		private int CurrentGearIndex => m_CurrentGear - 1;
		private AnimationCurve GearPowerCurve => m_GearCurves[this.CurrentGearIndex];
		private bool CanShiftUp => false;//m_CurrentGear < this.NumberOfGears && this.HasReachedTerminalVelocityThreshold;
		private bool CanShiftDown => m_CurrentGear > 1 && CurrentSpeed <= PowerStep * (this.CurrentGearIndex);

		#endregion


		#region MonoBehaviour Methods

		// Start is called before the first frame update
		void Awake() {
			this.InitializeRigidbody();
		}
		
		// Update is called once per frame
		void Update() {
			m_MoveInput = Input.GetAxisRaw("Vertical");
			m_TurnInput = Input.GetAxisRaw("Horizontal");

			this.PlayEngineAudio();
			this.PlaySkidAudio();
			this.SetFrontWheelsAngle();
		}


		protected void FixedUpdate() {
			if (m_ShiftCourtine == null) {
				// go to next gear
				if (this.CanShiftUp) {
					m_ShiftCourtine = ShiftCourtine(true);
					StartCoroutine(m_ShiftCourtine);
				} else if (this.CanShiftDown) {
					//m_ShiftCourtine = ShiftCourtine(false);
					//StartCoroutine(m_ShiftCourtine);
				}
			}


			this.Accelerate();
			this.Steer();
			this.AddDownwardForce();
		}


		private void OnDrawGizmos() {
			Gizmos.DrawLine(transform.position, transform.position + (transform.forward * 5));
			Gizmos.color = Color.green;

			Gizmos.DrawLine(m_Rigidbody.position, m_Rigidbody.position + m_Rigidbody.velocity);
			Gizmos.color = Color.red;
		}

		#endregion


		#region Internal Methods

		private void InitializeRigidbody() {
			m_Rigidbody.transform.parent = null;
			m_Rigidbody.drag = RigidbodyDrag;
		}


		private IEnumerator ShiftCourtine(bool shiftUp) {
			if (shiftUp) {
				m_CurrentGear++;
			} else {
				m_CurrentGear--;
			}

			yield return new WaitForSeconds(ShiftDelay);
			m_ShiftCourtine = null;
		}


		private void Accelerate() {
			if (this.IsGrounded) {
				m_Rigidbody.AddForce(transform.forward * this.Force, ForceMode.Acceleration);
			}

			transform.position = m_Rigidbody.position;
		}
		private void Steer() {
			var newRotation = 0f;
			if (Force == 0) {
				return;
			} else if (Force > 0) {
				newRotation = m_TurnInput * this.PowerStep * Time.deltaTime;
			} else if (Force < 0) {
				newRotation = m_TurnInput * -this.PowerStep * Time.deltaTime;
			}

			transform.Rotate(0, newRotation, 0, Space.World);
		}
		private void AddDownwardForce() {
			if (this.IsGrounded) {
				return;
			}

			var downwardForce = transform.up * (Physics.gravity.y * GravityMultiplier * RigidbodyDrag);
			m_Rigidbody.AddForce(downwardForce, ForceMode.Acceleration);
		}

		private void PlayEngineAudio() {
			if (m_EngineSound != null) {
				//m_EngineSound.pitch = (m_Power / Power) * 2f;
			}
		}

		private void PlaySkidAudio() {
			if (m_SkidSound != null) {
				if (Mathf.Abs(m_TurnInput * MaxTurnAngle) > m_PlaySkidAudioThreshold) {
					m_SkidSound.volume = Mathf.MoveTowards(m_SkidSound.volume, 1f, m_SkidFadeSpeed * Time.deltaTime);
				} else {
					m_SkidSound.volume = Mathf.MoveTowards(m_SkidSound.volume, 0f, m_SkidFadeSpeed * Time.deltaTime);
				}
			}
		}

		private void SetFrontWheelsAngle() {
			m_FLWheel.localRotation = Quaternion.Euler(m_FLWheel.localRotation.eulerAngles.x, (m_TurnInput * MaxTurnAngle) - 180, m_FLWheel.localRotation.eulerAngles.z);
			m_FRWheel.localRotation = Quaternion.Euler(m_FRWheel.localRotation.eulerAngles.x, (m_TurnInput * MaxTurnAngle), m_FRWheel.localRotation.eulerAngles.z);
		}

		#endregion
	}
}