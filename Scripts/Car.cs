namespace Conibear {
	using UnityEngine;

	public class Car : MonoBehaviour {
		#region Internal Fields

		private Rigidbody m_Rigidbody = null;

		#endregion


		#region Internal Fields

		private SphereColliderGroundChecker m_SphereColliderGroundChecker = null;

		#endregion


		public Vector3 PseudoGravity {
			get {
				if (m_PseudoGravity < 0) {
					m_PseudoGravity = 0;
				}

				return new Vector3(0, -m_PseudoGravity, 0);
			}
		}


		#region Internal Properties

		private SphereColliderGroundChecker SphereColliderGroundChecker {
			get {
				if (m_SphereColliderGroundChecker == null) {
					Print.Warning("CarModelPrefab missing SphereColliderGroundChecker");
					m_SphereColliderGroundChecker = this.GetComponentInChildren<SphereColliderGroundChecker>();
				}

				return m_SphereColliderGroundChecker;
			}
		}

		#endregion


		#region SerializeField

		[SerializeField]
		private CarData m_CarData = null;

		[SerializeField]
		[Range(0.0f, 100f)]
		private float m_PseudoGravity = 10f;

		#endregion


		#region Interal Properties

		private Rigidbody Rigidbody {
			get {
				if (m_Rigidbody == null) {
					m_Rigidbody = GetComponent<Rigidbody>();
				}

				return m_Rigidbody;
			}
		}

		private CarData CarData {
			get {
				if (m_CarData == null) {
					Print.Error("CarData missing!", name);
					m_CarData = ScriptableObject.CreateInstance<CarData>();
				}

				return m_CarData;
			}
		}

		#endregion


		#region MonoBehaviour Methods

		// Start is called before the first frame update

		private void Awake() {
			m_SphereColliderGroundChecker = GetComponentInChildren<SphereColliderGroundChecker>();
		}

		private void Start() {
			this.Initialization();


			m_SphereRigidbody.transform.parent = null;
		}

		public float moveInput;
		public float turnInput;


		// Update is called once per frame
		private void Update() {
			//this.DriveWithVelocity(Vector2.up);
			moveInput = Input.GetAxisRaw("Vertical");
			turnInput = Input.GetAxisRaw("Horizontal");

			moveInput *= moveInput > 0 ? CarData.m_ForwardForce : CarData.m_ReverseForce;

			transform.position = m_SphereRigidbody.transform.position;

			float newRotation = turnInput * CarData.m_Torque * Time.deltaTime * Input.GetAxisRaw("Vertical");
			transform.Rotate(0, newRotation, 0, Space.World);
		}

		public Rigidbody m_SphereRigidbody = null;

		private void FixedUpdate() {
			m_SphereRigidbody.AddForce(transform.forward * moveInput, ForceMode.Acceleration);
		}

		private void LateUpdate() {
			RaycastHit hit;
			if (!this.SphereColliderGroundChecker.IsGrounded(out hit)) {
				//this.ApplyPseudoGravity();
			}
		}

		#endregion


		#region Internal Methods

		private void Initialization() {
			//this.InitializePhysics();
			//this.SpawnCar();
		}

		private void InitializePhysics() {
			this.Rigidbody.mass = this.CarData.Mass;
		}

		private void SpawnCar() {
			Instantiate(CarData.CarModelPrefab, this.transform);
		}

		private void ApplyPseudoGravity() {
			this.Rigidbody.velocity += this.PseudoGravity;
		}

		#endregion


		#region Public Methods

		/// <summary>
		/// Call in an update cycle to move the car in the given direction
		/// </summary>
		/// <param name="direction">-x:left | +x:right | +y:forward | -y:backwards</param>
		public void DriveWithVelocity(Vector2 direction) {
			var y = this.Rigidbody.velocity.y;
			this.Rigidbody.velocity = new Vector3(direction.x, y, direction.y);
		}

		public void DriveWithForce() {
		}

		#endregion
	}
}