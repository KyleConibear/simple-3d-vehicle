using UnityEngine.Experimental.XR;

namespace Conibear {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	[RequireComponent(typeof(Rigidbody))]
	public class Car : MonoBehaviour {
		#region SerializeField

		[SerializeField]
		private CarData m_CarData = null;

		#endregion


		#region Internal Fields

		private Rigidbody m_Rigidbody = null;

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
		void Start() {
			this.Initialization();
		}

		// Update is called once per frame
		void Update() {
			this.Drive(Vector2.up);
		}

		#endregion


		#region Internal Methods

		private void Initialization() {
			this.SpawnCar();
		}

		private void SpawnCar() {
			Instantiate(CarData.CarModelPrefab, this.transform);
		}

		private void Drive(Vector2 direction) {
			Rigidbody.velocity = new Vector3(direction.x, 0, direction.y);
		}

		#endregion
	}
}