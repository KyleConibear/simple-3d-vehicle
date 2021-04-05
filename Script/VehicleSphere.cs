namespace Conibear {
	using UnityEngine;

	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(SphereColliderGroundChecker))]
	public class VehicleSphere : MonoBehaviour {
		#region Internal Consts

		private const float RigidbodyDrag = 5f;
		private const float GravityMultiplier = 3;

		#endregion


		#region Internal Fields

		private Rigidbody m_Rigidbody = null;

		private SphereColliderGroundChecker m_SphereColliderGroundChecker = null;

		#endregion


		#region Public Properties

		public Rigidbody Rigidbody {
			get {
				if (m_Rigidbody == null) {
					m_Rigidbody = GetComponent<Rigidbody>();
				}

				return m_Rigidbody;
			}
		}

		public SphereColliderGroundChecker SphereColliderGroundChecker {
			get {
				if (m_SphereColliderGroundChecker == null) {
					m_SphereColliderGroundChecker = GetComponent<SphereColliderGroundChecker>();
				}

				return m_SphereColliderGroundChecker;
			}
		}

		public bool IsGrounded => this.SphereColliderGroundChecker.IsRigidbodyGrounded(m_Rigidbody);

		#endregion


		#region MonoBehaviour Methods

		private void Awake() {
			this.InitializeRigidbody();
		}

		#endregion


		#region Internal Methods

		private void InitializeRigidbody() {
			this.Rigidbody.transform.parent = null;
			this.Rigidbody.drag = RigidbodyDrag;
		}

		#endregion
	}
}