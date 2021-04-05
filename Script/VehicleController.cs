using System;

namespace Conibear {
	using System.Collections;
	using UnityEngine;


	public class VehicleController : MonoBehaviour {
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
		private int m_Gear = 1;

		[ShowOnly] [SerializeField]
		private int m_GearMinForce;

		[ShowOnly] [SerializeField]
		private int m_GearMaxForce;

		[ShowOnly] [SerializeField]
		private float m_GearCurveInterpolationPoint = 0;

		[ShowOnly] [SerializeField]
		private float m_NormalizedGearCurveInterpolationPoint;

		[ShowOnly] [SerializeField]
		private float m_GearLerpValue;

		[ShowOnly] [SerializeField]
		private int m_Force;

		#endregion


		#region SerializeFields

		[Header("ScriptableObjects")]
		[SerializeField]
		private VehicleData m_VehicleData;

		[Header("Component References")]
		[SerializeField]
		private Transform m_VehicleModel = null;

		[SerializeField]
		private Transform m_FrontLeftWheel;

		[SerializeField]
		private Transform m_FrontRightWheel;

		[SerializeField]
		private VehicleSphere m_VehicleSphere;

		[Header("Audio")]
		[SerializeField]
		private AudioSource m_EngineAudioSource;

		[SerializeField]
		private AudioSource m_SkidAudioSource;

		#endregion


		#region Internal Fields

		private float m_MoveInput;
		private float m_TurnInput;
		private float m_ForceTimeElapsed = 0;
		private int m_GearForceSteps = 0;

		#endregion


		#region Internal Properties

		#region Components

		private Transform VehicleModel {
			get {
				if (m_VehicleModel == null) {
					var model = new GameObject();
					model.transform.parent = this.transform;
				}

				return m_VehicleModel;
			}
		}

		private VehicleSphere VehicleSphere {
			get {
				if (m_VehicleSphere == null) {
					m_VehicleSphere = new GameObject().AddComponent<VehicleSphere>();
					m_VehicleSphere.name = "VehicleSphere (ERROR)";
					//Print.NotInitializedError(VehicleSphere);
				}

				return m_VehicleSphere;
			}
		}

		private Rigidbody Rigidbody => this.VehicleSphere.Rigidbody;


		private Transform FrontLeftWheel {
			get {
				if (m_FrontLeftWheel == null) {
					m_FrontLeftWheel = new GameObject().transform;
					Print.NotInitializedError(m_FrontLeftWheel);
				}

				return m_FrontLeftWheel;
			}
		}

		private Transform FrontRightWheel {
			get {
				if (m_FrontRightWheel == null) {
					m_FrontRightWheel = new GameObject().transform;
					//Print.NotInitializedError<Transform>(m_FrontRightWheel);
				}

				return m_FrontRightWheel;
			}
		}

		#endregion


		private VehicleData VehicleData => m_VehicleData;
		private bool IsGrounded => this.VehicleSphere.IsGrounded;

		private int Gear => m_Gear;

		private AnimationCurve GearPowerCurve => VehicleData.GearCurves[this.Gear - 1];

		private int GearForceSteps => m_GearForceSteps != 0 ? m_GearForceSteps : VehicleData.MaxForce / this.VehicleData.NumberOfGears;

		private int GearMinForce {
			get {
				if (this.Gear > 0) {
					return m_GearMinForce = GearForceSteps * (this.Gear - 1);
				}

				return 0;
			}
		}

		private int GearMaxForce => m_GearMaxForce = GearForceSteps * this.Gear;

		private int Force {
			get {
				m_GearLerpValue = Mathf.Lerp(this.GearMinForce, this.GearMaxForce, this.GearPowerCurve.Evaluate(GearCurveInterpolationPoint));

				if (m_MoveInput > 0 && GearCurveInterpolationPoint < this.PowerCurveTimeEnd) {
					m_ForceTimeElapsed += Time.deltaTime;
				} else if (m_MoveInput < 1 && m_ForceTimeElapsed > 0) {
					m_ForceTimeElapsed -= Time.deltaTime;
				}

				return m_Force = (int) m_GearLerpValue;
			}
		}

		private float PowerCurveTimeEnd {
			get {
				if (GearPowerCurve.length == 0) {
					return 0;
				}

				var lastFrame = GearPowerCurve[GearPowerCurve.length - 1];

				return lastFrame.time;
			}
		}

		private float ForceTimeElapsed => m_ForceTimeElapsed;
		private float GearCurveInterpolationPoint => m_GearCurveInterpolationPoint = this.ForceTimeElapsed / this.PowerCurveTimeEnd;

		private float NormalizedGearCurveInterpolationPoint => m_NormalizedGearCurveInterpolationPoint = this.GearCurveInterpolationPoint / this.PowerCurveTimeEnd;

		private float PlaySkidAudioThreshold = MaxTurnAngle * SkidAudioThresholdFactor;

		#endregion


		#region MonoBehaviour Methods

		// Start is called before the first frame update
		private void Start() {
			this.InitializeAudio();
			this.SetModelPosition();
			this.FlipFrontLeftWheel();
		}

		// Update is called once per frame
		private void Update() {
			m_MoveInput = Input.GetAxisRaw("Vertical");
			m_TurnInput = Input.GetAxisRaw("Horizontal");
			if (Input.GetKeyDown(KeyCode.Q)) {
				this.ShiftDown();
			} else if (Input.GetKeyDown(KeyCode.E)) {
				this.ShiftUp();
			}

			if (Input.GetKeyDown(KeyCode.Space)) {
				this.Break();
			}
		}

		private void LateUpdate() {
			this.Transmission();
			this.SetFrontWheelsAngle();
			this.PlayEngineAudio();
			this.PlaySkidAudio();
		}

		private void FixedUpdate() {
			this.Accelerate();
			this.Steer();
			this.AddDownwardForce();
		}

		private void OnDrawGizmos() {
			Gizmos.DrawLine(transform.position, transform.position + (transform.forward * 5));
			Gizmos.color = Color.green;

			Gizmos.DrawLine(this.Rigidbody.position, this.Rigidbody.position + this.Rigidbody.velocity);
			Gizmos.color = Color.red;
		}

		#endregion


		#region Internal Methods

		private void InitializeAudio() {
			m_EngineAudioSource.clip = this.VehicleData.EngineAudioClip;
			m_SkidAudioSource.clip = this.VehicleData.SkidAudioClip;
		}
		private void SetModelPosition() {
			VehicleModel.localPosition = new Vector3(0, -this.VehicleSphere.SphereColliderGroundChecker.SphereRadius, 0);
		}

		private void FlipFrontLeftWheel() {
			var scale = this.FrontLeftWheel.localScale;
			m_FrontLeftWheel.transform.localScale = new Vector3(-scale.x, scale.y, scale.z);
		}

		private void Accelerate() {
			if (this.IsGrounded) {
				if (this.Force < 0 && this.m_MoveInput > 0)
					return;
				this.Rigidbody.AddForce(transform.forward * this.Force, ForceMode.Acceleration);
			}

			transform.position = this.Rigidbody.position;
		}

		private void Break() {
			m_ForceTimeElapsed -= Time.deltaTime * this.Gear;
		}

		private void ShiftDown() {
			if (m_Gear > 1) {
				m_Gear--;
				m_ForceTimeElapsed = this.PowerCurveTimeEnd * 0.85f;
			}
		}

		private void ShiftUp() {
			if (this.Gear < this.VehicleData.NumberOfGears) {
				m_Gear++;
				m_ForceTimeElapsed = this.PowerCurveTimeEnd * 0.15f;
			}
		}

		private void Transmission() {
			if (this.m_MoveInput > 0) {
				if (this.Gear <= 0 || NormalizedGearCurveInterpolationPoint >= 1) {
					this.ShiftUp();
				}
			} else if ((this.m_MoveInput < 0 && this.Gear == 0) || NormalizedGearCurveInterpolationPoint <= Mathf.Epsilon) {
				this.ShiftDown();
			} else if (this.m_MoveInput == 0 && this.Gear == 1 && NormalizedGearCurveInterpolationPoint <= Mathf.Epsilon) {
				//this.ShiftDown();
			}
		}

		private void AddDownwardForce() {
			if (this.IsGrounded) {
				return;
			}

			var downwardForce = transform.up * (Physics.gravity.y * GravityMultiplier * RigidbodyDrag);
			this.Rigidbody.AddForce(downwardForce, ForceMode.Acceleration);
		}

		private void Steer() {
			var newRotation = 0f;
			if (Force == 0) {
				return;
			}

			newRotation = (m_TurnInput - (m_TurnInput / (this.VehicleData.NumberOfGears + 2 - this.Gear))) * this.VehicleData.TurnSpeed * Time.deltaTime;

			transform.Rotate(0, newRotation, 0, Space.World);
		}

		private void SetFrontWheelsAngle() {
			this.FrontLeftWheel.localRotation = Quaternion.Euler(this.FrontLeftWheel.localRotation.eulerAngles.x, (m_TurnInput * MaxTurnAngle) - 180, m_FrontLeftWheel.localRotation.eulerAngles.z);
			this.FrontRightWheel.localRotation = Quaternion.Euler(this.FrontRightWheel.localRotation.eulerAngles.x, (m_TurnInput * MaxTurnAngle), m_FrontRightWheel.localRotation.eulerAngles.z);
		}

		private void PlayEngineAudio() {
			if (this.VehicleData.EngineAudioClip != null) {
				if (this.NormalizedGearCurveInterpolationPoint > 0) {
					m_EngineAudioSource.volume = this.NormalizedGearCurveInterpolationPoint + 0.5f;
					m_EngineAudioSource.pitch = this.NormalizedGearCurveInterpolationPoint + 1;
				}
			}
		}

		private void PlaySkidAudio() {
			if (m_SkidAudioSource != null) {
				if (Mathf.Abs(m_TurnInput * MaxTurnAngle) > PlaySkidAudioThreshold && this.NormalizedGearCurveInterpolationPoint > 0.7f) {
					m_SkidAudioSource.volume = Mathf.MoveTowards(m_SkidAudioSource.volume, 1f, this.VehicleData.SkidFadeSpeed * Time.deltaTime);
				} else {
					m_SkidAudioSource.volume = Mathf.MoveTowards(m_SkidAudioSource.volume, 0f, this.VehicleData.SkidFadeSpeed * Time.deltaTime);
				}
			}
		}

		#endregion
	}
}