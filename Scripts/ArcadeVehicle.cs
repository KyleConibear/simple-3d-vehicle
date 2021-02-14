namespace Conibear {
	using UnityEngine;

	[RequireComponent(typeof(Rigidbody))]
	public class ArcadeVehicle : MonoBehaviour {
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


		#region Internal Fields

		private SphereColliderGroundChecker m_SphereColliderGroundChecker = null;

		#endregion


		#region SerializeField

		[SerializeField]
		private ArcadeVehicleData m_ArcadeVehicleData = null;


		[SerializeField]
		private Rigidbody m_Rigidbody = null;

		#endregion


		#region Interal Properties

		private ArcadeVehicleData VehicleData {
			get {
				if (m_ArcadeVehicleData == null) {
					Print.Error("VehicleData missing!", name);
					m_ArcadeVehicleData = ScriptableObject.CreateInstance<ArcadeVehicleData>();
				}

				return m_ArcadeVehicleData;
			}
		}

		private Rigidbody Rigidbody {
			get {
				if (m_Rigidbody == null) {
					m_Rigidbody = this.GetComponent<Rigidbody>();
				}

				return m_Rigidbody;
			}
		}

		#endregion


		#region MonoBehaviour Methods

		// Start is called before the first frame update

		private void Awake() {
			//m_SphereColliderGroundChecker = this.m_Rigidbody.GetComponent<SphereColliderGroundChecker>();
		}

		private void Start() {
			this.Initialization();
			m_Rigidbody.transform.parent = null;
		}

		public float moveInput;
		public float turnInput;


		private void FixedUpdate() {
			var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
			this.Drive(input);
		}

/*
		private void LateUpdate() {
			RaycastHit hit;
			if (!this.SphereColliderGroundChecker.IsGrounded(out hit)) {
				//this.ApplyPseudoGravity();
			}
		}
*/

		#endregion


		#region Internal Methods

		private void Initialization() {
			this.InitializePhysics();
			this.SpawnCar();
		}

		private void InitializePhysics() {
			this.Rigidbody.mass = this.VehicleData.Mass;
		}

		private void SpawnCar() {
			Instantiate(VehicleData.CarModelPrefab, this.transform);
		}

		private void Accelerate(float input) {
			this.Rigidbody.AddForce(transform.forward * this.VehicleData.m_ForwardForce * input, ForceMode.Acceleration);
		}

		private void Steer(float input) {
			this.Rigidbody.AddTorque(transform.up * this.VehicleData.m_Torque * input, ForceMode.Acceleration);
		}

		private void ApplyPseudoGravity() {
			this.Rigidbody.velocity += this.VehicleData.PseudoGravity;
		}

		#endregion


		#region Public Methods

		/// <summary>
		/// Call in MonoBehaviour.FixedUpdate method to move the car in the given direction
		/// </summary>
		/// <param name="direction">-x:left | +x:right | +y:forward | -y:backwards</param>
		public void Drive(Vector2 direction) {
			this.Steer(direction.x);
			this.Accelerate(direction.y);
		}

		#endregion
	}
}