using System;
using System.Collections;
using System.Collections.Generic;
using Conibear;
using UnityEngine;

public class ArcadeVehicle : Vehicle {
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
}