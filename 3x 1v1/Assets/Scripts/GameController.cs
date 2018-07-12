using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour 
{
	public static GameController Instance { get; private set; }

	public GameObject DefaultCam;
	public bool roundIsActive;

	public Animator RoundCountDown;

	[Space (10)]
	public Slider[] HealthSliders;
	public RawImage[] MinimapBorders;
	public SimpleFollow[] MinimapSimpleFollowScripts;
	public Transform[] MinimapFollowers;

	void Awake ()
	{
		Instance = this;
	}

	void Start ()
	{
		DefaultCam.SetActive (true);
	}

	public void StartRoundCountdown ()
	{
		RoundCountDown.enabled = true;
		RoundCountDown.Play (0);
	}
}