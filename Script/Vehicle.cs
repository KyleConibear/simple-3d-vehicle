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
		private const float ForceMultiplier = 10;

		#endregion


		#region ShowOnly SerializeFields

		[Header("Debug Stats")]
		[ShowOnly] [SerializeField]
		private float m_Force;

		[ShowOnly] [SerializeField]
		private float m_MaxForce;

		[ShowOnly] [SerializeField]
		private float m_ForceInterpolationPoint = 0;

		[ShowOnly] [SerializeField]
		private float m_RigidbodyMagnitude = 0;

		[ShowOnly] [SerializeField]
		private float m_TerminalVelocity = 0;

		[ShowOnly] [SerializeField]
		private bool m_HasReachedTerminalVelocityThreshold;

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
		[Range(0.5f, 0.99f)]
		private float m_AutoShiftThreshold = 0.9f;

		[SerializeField]
		private AudioSource m_EngineSound, m_SkidSound;

		[SerializeField]
		private float m_PlaySkidAudioThreshold = MaxTurnAngle * SkidAudioThresholdFactor;

		[SerializeField]
		private float m_SkidFadeSpeed = 2f;

		[SerializeField]
		protected Transform m_FLWheel, m_FRWheel;


		[SerializeField]
		private SphereColliderGroundChecker m_SphereGroundCheck;

		#endregion


		#region Internal Fields

		private float m_MoveInput;
		private float m_PreviousMoveInput;
		private float m_TurnInput;


		private IEnumerator m_ShiftCourtine;

		#endregion


		#region Internal Properties

		private bool IsGrounded => m_SphereGroundCheck.IsRigidbodyGrounded(m_Rigidbody);

		private float m_ForceTimeElapsed = 0;

		private float ForceInterpolationPoint => m_ForceInterpolationPoint = m_ForceTimeElapsed / this.GearPowerCurveDuration;

		private float Force {
			get {
				var lerpValue = Mathf.Lerp(0, this.GearPowerCurveDuration, ForceInterpolationPoint);

				if (ForceInterpolationPoint < this.GearPowerCurveDuration && m_MoveInput > 0) {
					m_ForceTimeElapsed += Time.deltaTime;
				} else if (ForceInterpolationPoint > 0 && m_MoveInput < 1) {
					m_ForceTimeElapsed -= Time.deltaTime;
				}    

				return m_Force = this.GearPowerCurve.Evaluate(lerpValue) * ForceMultiplier;
			}
		}

		private float MaxForce => m_MaxForce = this.GearPowerCurve.Evaluate(this.GearPowerCurveDuration) * ForceMultiplier;

		private float CurrentSpeed => m_CurrentSpeed = m_Rigidbody.velocity.magnitude * MeterPerSecondConversionToMilesPerHour;

		private float RigidbodyMagnitude => m_RigidbodyMagnitude = this.m_Rigidbody.velocity.magnitude;
		private float TerminalVelocity => m_TerminalVelocity = Math.TerminalVelocity(m_Rigidbody, transform.forward * this.MaxForce);
		private float AutoShiftThreshold => m_AutoShiftThreshold;
		private bool HasReachedTerminalVelocityThreshold => m_HasReachedTerminalVelocityThreshold = RigidbodyMagnitude >= TerminalVelocity * AutoShiftThreshold;

		private bool HasDroppedBelowCurrentGearVelocityThreshold => RigidbodyMagnitude < PreviousGearsTerminalVelocity * 0.6f;

		private float PreviousGearsTerminalVelocity { get; set; }

		private int NumberOfGears => m_GearCurves.Length;
		private int CurrentGearIndex => m_CurrentGear - 1;
		private AnimationCurve GearPowerCurve => m_GearCurves[this.CurrentGearIndex];

		private float GearPowerCurveDuration {
			get {
				if (GearPowerCurve.length == 0) {
					return 0;
				}

				var lastFrame = GearPowerCurve[GearPowerCurve.length - 1];

				return lastFrame.time;
			}
		}

		private bool CanShiftUp => m_CurrentGear < this.NumberOfGears - 1 && ForceInterpolationPoint >= 1f;
		private bool CanShiftDown => m_CurrentGear > 1 && ForceInterpolationPoint <= Mathf.Epsilon;

		#endregion


		#region MonoBehaviour Methods

		// Start is called before the first frame update
		private void Awake() {
			this.InitializeRigidbody();
		}

		// Update is called once per frame
		private void Update() {
			m_MoveInput = Input.GetAxisRaw("Vertical");
			m_TurnInput = Input.GetAxisRaw("Horizontal");

			this.PlayEngineAudio();
			this.PlaySkidAudio();
			this.SetFrontWheelsAngle();

			if (m_ShiftCourtine == null) {
				// go to next gear
				if (m_MoveInput > 0 && this.CanShiftUp) {
					m_ShiftCourtine = ShiftCourtine(true);
					StartCoroutine(m_ShiftCourtine);
				} else if (m_MoveInput < 1 && this.CanShiftDown) {
					m_ShiftCourtine = ShiftCourtine(false);
					StartCoroutine(m_ShiftCourtine);
				}
			}
		}

		private void FixedUpdate() {
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

		private void Accelerate() {
			if (this.IsGrounded) {
				m_Rigidbody.AddForce(transform.forward * this.Force, ForceMode.Acceleration);
			}

			transform.position = m_Rigidbody.position;

			var temp = HasReachedTerminalVelocityThreshold;
		}

		private void Steer() {
			var newRotation = 0f;
			if (Force == 0) {
				return;
			}

			newRotation = m_TurnInput * this.Force * Time.deltaTime;

			transform.Rotate(0, newRotation, 0, Space.World);
		}

		private void AddDownwardForce() {
			if (this.IsGrounded) {
				return;
			}

			var downwardForce = transform.up * (Physics.gravity.y * GravityMultiplier * RigidbodyDrag);
			m_Rigidbody.AddForce(downwardForce, ForceMode.Acceleration);
		}

		private IEnumerator ShiftCourtine(bool shiftUp) {
			PreviousGearsTerminalVelocity = TerminalVelocity;
			m_ForceTimeElapsed = 0.0f;
			if (shiftUp) {
				m_CurrentGear++;
			} else {
				m_CurrentGear--;
			}

			yield return new WaitForSeconds(ShiftDelay);
			m_ShiftCourtine = null;
		}

		private void SetFrontWheelsAngle() {
			m_FLWheel.localRotation = Quaternion.Euler(m_FLWheel.localRotation.eulerAngles.x, (m_TurnInput * MaxTurnAngle) - 180, m_FLWheel.localRotation.eulerAngles.z);
			m_FRWheel.localRotation = Quaternion.Euler(m_FRWheel.localRotation.eulerAngles.x, (m_TurnInput * MaxTurnAngle), m_FRWheel.localRotation.eulerAngles.z);
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

		#endregion
	}
}