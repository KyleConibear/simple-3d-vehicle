using System;
using System.Collections;
using System.Collections.Generic;
using Conibear;
using UnityEngine;

public class ArcadeVehicle : MonoBehaviour {
	[SerializeField]
	private float m_ForwardForce, m_ReverseForce = 20f;

	[SerializeField]
	private float m_TurnSpeed = 15f;

	[SerializeField]
	private Rigidbody m_SphereRigidbody;

	[SerializeField]
	private Transform m_CarModel;

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
		m_SphereRigidbody.transform.parent = null;
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
			m_SphereRigidbody.drag = 4;
		} else {
			m_SphereRigidbody.drag = 0.1f;
		}
	}
	

	void FixedUpdate() {

		transform.position = Vector3.MoveTowards(transform.position, m_SphereRigidbody.position, 1);
		
		if (m_IsCarGrounded)
			m_SphereRigidbody.AddForce(transform.forward * m_MoveInput, ForceMode.Acceleration);
		else {
			m_SphereRigidbody.AddForce(transform.up * -9.8f * m_SphereRigidbody.mass);
		}
		
		Debug.Log(m_SphereRigidbody.velocity.magnitude * 3.6);
	}
}