namespace Conibear {
	using UnityEngine;

	[CreateAssetMenu(menuName = "CarData")]
	public class CarData : ScriptableObject {
		#region SerializeField

		[SerializeField]
		private GameObject m_CarModelPrefab = null;

		#endregion


		#region Public Propertiess

		public GameObject CarModelPrefab {
			get {
				if (m_CarModelPrefab == null) {
					Print.Error("CarModelPrefab null!");
					m_CarModelPrefab = new GameObject("Error Car");
				}
				return m_CarModelPrefab;
			}
		}

		#endregion


		public void Initialize(GameObject carModelPrefab) {
			{
				m_CarModelPrefab = carModelPrefab;
			}
		}
	}
}