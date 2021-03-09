using System;
using System.Collections;
using System.Collections.Generic;
using Conibear;
using UnityEngine;

[System.Serializable]
public struct Gear {
	#region SerializeFields

	[SerializeField]
	private float m_MinSpeed;

	[SerializeField]
	private float m_MaxSpeed;

	[SerializeField]
	private AnimationCurve m_PowerCurve;

	public float MinSpeed => m_MinSpeed;
	public float MaxSpeed => m_MaxSpeed;

	#endregion
}

public class ArcadeVehicle : Vehicle {
	[SerializeField]
	private Gear[] m_Gears;

	[SerializeField]
	private AudioSource m_EngineSound, m_SkidSound;

	[SerializeField]
	private float m_PlaySkidAudioThreshold = 0.5f;

	[SerializeField]
	private float m_SkidFadeSpeed = 2f;

	[SerializeField]
	protected Transform m_FLWheel, m_FRWheel, m_RLWheel, m_RRWheel;

	[SerializeField]
	private uint m_ForwardForce = 200;

	[SerializeField]
	private uint m_ReverseForce = 150;

	[SerializeField]
	private float m_TurnSpeed = 15f;

	[SerializeField]
	private SphereColliderGroundChecker m_SphereGroundCheck;

	private float m_MoveInput;
	private float m_TurnInput;

	private bool m_IsCarGrounded;

	private Gear Gear => m_Gears[base.CurrentGear];
	
	private void Awake() {
		//m_SphereRigidbody = GetComponent<Rigidbody>();
	}

	// Start is called before the first frame update
	void Start() {
		base.Rigidbody.transform.parent = null;
	}

	// Update is called once per frame
	void Update() {
		m_MoveInput = Input.GetAxisRaw("Vertical");
		m_TurnInput = Input.GetAxisRaw("Horizontal");
		m_MoveInput *= m_MoveInput > 0 ? m_ForwardForce : m_ReverseForce;

		var newRotation = m_TurnInput * m_TurnSpeed * Time.deltaTime * Input.GetAxisRaw("Vertical");
		transform.Rotate(0, newRotation, 0, Space.World);

		m_IsCarGrounded = m_SphereGroundCheck.IsGrounded(out var hit);
		var targetQuaternion = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
		transform.rotation = Quaternion.Slerp(transform.rotation, targetQuaternion, 0.1f);

		if (m_IsCarGrounded) {
			base.Rigidbody.drag = 4;
		} else {
			base.Rigidbody.drag = 0.1f;
		}
		
		this.PlayEngineAudio();
		this.PlaySkidAudio(m_TurnInput);
		this.SetFrontWheelsAngle(m_TurnInput);
	}


	protected override void FixedUpdate() {
		transform.position = Vector3.MoveTowards(transform.position, base.Rigidbody.position, 1);

		if (m_IsCarGrounded)
			base.Rigidbody.AddForce(transform.forward * m_MoveInput, ForceMode.Acceleration);
		else {
			base.Rigidbody.AddForce(transform.up * -9.8f * base.Rigidbody.mass);
		}

		base.FixedUpdate();
	}


	private void PlayEngineAudio() {
		if (m_EngineSound != null) {
			m_EngineSound.pitch = 1f + ((this.Rigidbody.velocity.magnitude / this.Gear.MaxSpeed) * 2f);
		}
	}

	private void PlaySkidAudio(float wheelAngle) {
		if (m_SkidSound != null) {
			if (Mathf.Abs(wheelAngle) > m_PlaySkidAudioThreshold) {
				m_SkidSound.volume = Mathf.MoveTowards(m_SkidSound.volume, 1f, m_SkidFadeSpeed * Time.deltaTime);
			} else {
				m_SkidSound.volume = Mathf.MoveTowards(m_SkidSound.volume, 0f, m_SkidFadeSpeed * Time.deltaTime);
			}
		}
	}

	/// <summary>
	/// turning the wheels
	/// </summary>
	/// <param name="input">user input (Input.GetAxis("Horizontal")) </param>
	protected void SetFrontWheelsAngle(float input) {
		m_FLWheel.localRotation = Quaternion.Euler(m_FLWheel.localRotation.eulerAngles.x, (input * base.MaxTurnAngle) - 180, m_FLWheel.localRotation.eulerAngles.z);
		m_FRWheel.localRotation = Quaternion.Euler(m_FRWheel.localRotation.eulerAngles.x, (input * MaxTurnAngle), m_FRWheel.localRotation.eulerAngles.z);
	}
}