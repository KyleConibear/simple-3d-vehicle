using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "VehicleData")]
public class VehicleData : ScriptableObject {
	#region SerializeFields

	[Header("Stats")]
	
	[SerializeField]
	private AnimationCurve[] m_GearCurves;
	
	[SerializeField]
	private int m_MaxForce = 300;

	[SerializeField]
	private int m_TurnSpeed = 50;

	[Header("Audio")]
	[SerializeField]
	private AudioClip engineAudioClip;

	[SerializeField]
	private AudioClip skidAudioClip;

	[SerializeField]
	private float m_SkidFadeSpeed = 2f;

	#endregion


	#region Public Properties

	public AnimationCurve[] GearCurves => m_GearCurves;

	public int NumberOfGears => this.GearCurves.Length;
	public int MaxForce => m_MaxForce;

	public int TurnSpeed => m_TurnSpeed;

	public AudioClip EngineAudioClip => engineAudioClip;

	public AudioClip SkidAudioClip => skidAudioClip;

	public float SkidFadeSpeed => m_SkidFadeSpeed;

	#endregion
}