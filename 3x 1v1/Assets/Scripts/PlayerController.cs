using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class PlayerController : NetworkBehaviour
{
	public static PlayerController Instance { get; private set; }

	public bool isHost;

	public int AmountOfDeaths;

	public int PlayerId = 1;
	public int PreviousPlayerId = 1;
	public NetworkAnimator networkAnim;

	[Header ("Movement")]
	public float MouseSens = 2;

	[Header ("Jumping")]
	public float JumpCooldownTime = 2;
	public bool JumpCooldown;
	public AudioSource JumpSound;

	[Header ("Spawning")]
	private int StartingPlayer = 1;
	private bool spawned;

	[Header ("Switching")]
	public CharacterController PlayerCharacterController;
	public FirstPersonController FirstPersonControllerScript;
	public GameObject Character;
	public Camera PlayerCam;
	public LayerMask[] Layers;
	public Rigidbody RbPlayer;
	public Animator Anim;
	public GameObject[] Dummies;
	public Camera[] DummyCameras;
	public GameObject SoldierObject;

	[Header ("Health")]
	public int[] CurrentHealth;

	public int StartingHealth = 100;

	public float DamageSmoothing;

	[Header ("Currency")]
	public int CurrentMoney = 100;

	void Start ()
	{
		Instance = this;

		SetStartingHealth ();
		StartCoroutine (StartGame ());
		spawned = true;

		if (networkAnim == null) 
		{
			networkAnim = GetComponent<NetworkAnimator> ();
		}

		if (!isLocalPlayer) 
		{
			isHost = false;
			gameObject.name = "PlayerParent_Client";
			Debug.Log ("Client instance assigned.");
		}

		if (isLocalPlayer) 
		{
			isHost = true;
			gameObject.name = "PlayerParent_Host";
			Debug.Log ("Host instance assigned.");
		}
	}

	void Update ()
	{
		CheckSpawnStart ();

		//if (!isLocalPlayer) 
		//{
		//	return;
		//}

		if (Input.GetKeyDown (KeyCode.Q)) 
		{
			CurrentHealth [PlayerId - 1] -= 20;
		}

		if (Input.GetKeyDown (KeyCode.E)) 
		{
			CurrentHealth [PlayerId - 1] += 10;
		}



		if (GameController.Instance.roundIsActive == true) 
		{
			CheckPlayerRoomChange ();
			CheckJump ();
			CheckWalk ();
			CheckSquat ();
			CheckHealthDamageSliders ();
		}
	}

	void CheckJump ()
	{
		if (Input.GetKeyDown (KeyCode.Space) && 
			JumpCooldown == false && 
			FirstPersonControllerScript.m_IsSquatting == false) 
		{
			Anim.SetTrigger ("Jump");
			FirstPersonControllerScript.PlayJumpSound ();
			JumpCooldown = true;
			StartCoroutine (DoJumpCooldown ());
			FirstPersonControllerScript.m_IsSquatting = false;
			SetSquattingState (PlayerId - 1, false);
		}

		if (Input.GetKeyDown (KeyCode.Space) && 
			FirstPersonControllerScript.m_IsSquatting == true) 
		{
			JumpCooldown = true;
			StartCoroutine (DoJumpCooldown ());
			FirstPersonControllerScript.m_IsSquatting = false;
			SetSquattingState (PlayerId - 1, false);
		}
	}

	void SetSquattingState (int pId, bool squatting)
	{
		Anim.SetBool ("Squat", squatting);
	}

	void CheckWalk ()
	{
		if (FirstPersonControllerScript.m_IsIdle == true) 
		{
			Anim.SetFloat ("Speed", 0.0f);
		}

		if (FirstPersonControllerScript.m_IsWalking == true) 
		{
			Anim.SetFloat ("Speed", 0.5f);
		}

		if (FirstPersonControllerScript.m_IsRunning == true) 
		{
			Anim.SetFloat ("Speed", 1f);
		}
	}

	void CheckSquat ()
	{
		if (Input.GetKeyDown (KeyCode.LeftControl)) 
		{
			FirstPersonControllerScript.m_IsSquatting = !FirstPersonControllerScript.m_IsSquatting;

			if (FirstPersonControllerScript.m_IsSquatting == true) 
			{
				SetSquattingState (PlayerId - 1, true);
			}

			if (FirstPersonControllerScript.m_IsSquatting == false) 
			{
				SetSquattingState (PlayerId - 1, false);
			}
		}
	}

	IEnumerator DoJumpCooldown ()
	{
		yield return new WaitForSeconds (JumpCooldownTime);
		JumpCooldown = false;
	}
		
	void CheckSpawnStart ()
	{
		if (Input.GetMouseButtonDown (0) && spawned == false) 
		{
			StartCoroutine (StartGame ());
			spawned = true;
		}
	}
		
	IEnumerator StartGame ()
	{
		FirstPersonControllerScript.enabled = false;
		PlayerCharacterController.enabled = false;

		StoreManager.Instance.StoreUI.SetActive (false);
		StoreManager.Instance.StoreMenuActive = false;

		GameController.Instance.StartRoundCountdown ();

		yield return new WaitForSecondsRealtime (3.0f);

		FirstPersonControllerScript.enabled = true;
		PlayerCharacterController.enabled = true;

		StartingPlayer = Random.Range (1, 4);
		PlayerId = StartingPlayer;
		PreviousPlayerId = PlayerId;

		CheckInitPlayer ();
		CheckMinimapFollowers ();
		CheckMinimapBorders ();
		CheckActiveMinimapFollower ();
		ResetPlayerState ();

		ToggleCharacters (StartingPlayer, true);

		networkAnim.enabled = true;
		networkAnim.animator = Anim;

		yield return new WaitForSecondsRealtime (0.0f);

		GameController.Instance.roundIsActive = true;
		GameController.Instance.DefaultCam.SetActive (false);
	}
		
	void CheckPlayerRoomChange ()
	{
		if (GameController.Instance.roundIsActive == true)
		{
			// If health is 0 for that screen, we can't switch to it. Also now cannot switch to itself.

			if (Input.GetKeyDown (KeyCode.Alpha1) && CurrentHealth [0] > 0 && PlayerId != 1) 
			{
				ToggleCharacters (1, false);
			}

			if (Input.GetKeyDown (KeyCode.Alpha2) && CurrentHealth [1] > 0 && PlayerId != 2) 
			{
				ToggleCharacters (2, false);
			}

			if (Input.GetKeyDown (KeyCode.Alpha3) && CurrentHealth [2] > 0 && PlayerId != 3) 
			{
				ToggleCharacters (3, false);
			}
		}
	}
		
	void ToggleCharacters (int thisCharacter, bool firstSwitch)
	{
		if (PlayerCam.cullingMask != Layers [thisCharacter - 1]) 
		{
			PlayerCam.cullingMask = Layers [thisCharacter - 1];
		}

		if (firstSwitch == true) 
		{
			Character.transform.position = Dummies [thisCharacter - 1].transform.position;
			Character.transform.rotation = Dummies [thisCharacter - 1].transform.rotation;
		}

		if (PlayerId != thisCharacter && firstSwitch == false) 
		{
			// Update previous player ID.
			PreviousPlayerId = PlayerId;

			// Store previous dummy's rotation from the current mouselook target rotation.
			Dummies [PreviousPlayerId - 1].transform.rotation = FirstPersonControllerScript.m_MouseLook.m_CharacterTargetRot;

			DummyCameras [PreviousPlayerId - 1].transform.rotation = FirstPersonControllerScript.m_MouseLook.m_CameraTargetRot;

			// Turn on the dummy to replace the old player position.
			Dummies [PlayerId - 1].SetActive (true); 

			// Update replacing dummy's player position using the players current position.
			Dummies [PlayerId - 1].transform.position = Character.transform.position;
		}

		// Turn off the dummy that the player will replace.
		Dummies [thisCharacter - 1].SetActive (false); 

		// Update Player's position using the intended dummy's position.
		Character.transform.position = Dummies [thisCharacter - 1].transform.position;

		// Use Dummy's rotation to overwrite mouse look rotation.
		FirstPersonControllerScript.m_MouseLook.m_CharacterTargetRot = Dummies [thisCharacter - 1].transform.rotation;

		FirstPersonControllerScript.m_MouseLook.m_CameraTargetRot = DummyCameras [thisCharacter - 1].transform.rotation;

		// Finally set the new player id to the intended character.
		PlayerId = thisCharacter; 

		// Reset player states.
		ResetPlayerState ();

		CheckMinimapBorders ();
		CheckActiveMinimapFollower ();
		CheckSoldierObjectLayer ();
	}

	void ResetPlayerState ()
	{
		FirstPersonControllerScript.m_IsIdle = true;
		FirstPersonControllerScript.m_IsWalking = false;
		FirstPersonControllerScript.m_IsRunning = false;
		FirstPersonControllerScript.m_IsSquatting = false;
		FirstPersonControllerScript.m_IsJumping = false;

		Anim.SetBool ("Squat", false);
		Anim.SetBool ("Aiming", false);
	}

	void CheckInitPlayer ()
	{
		switch (StartingPlayer) 
		{
		case 1:
			Dummies [0].SetActive (false);
			break;
		case 2:
			Dummies [1].SetActive (false);
			break;
		case 3:
			Dummies [2].SetActive (false);
			break;
		}
	}

	void SetStartingHealth ()
	{
		for (int i = 0; i < CurrentHealth.Length; i++)
		{
			CurrentHealth [i] = StartingHealth;
		}
	}
		
	void CheckHealthDamageSliders ()
	{
		// Update health sliders.
		for (int i = 0; i < GameController.Instance.HealthSliders.Length; i++)
		{
			GameController.Instance.HealthSliders [i].value = Mathf.Lerp (
				GameController.Instance.HealthSliders [i].value, 
				CurrentHealth [i], 
				DamageSmoothing * Time.unscaledDeltaTime
			);

			CurrentHealth [i] = Mathf.Clamp (CurrentHealth [i], 0, StartingHealth);
		}

		if (AmountOfDeaths == 0) 
		{
			// If we are in the arena with no health, we must switch out of it.
			switch (PlayerId) 
			{
			case 1:
				if (CurrentHealth [0] <= 0) 
				{
					float RandomVal = Random.Range (0, 1);

					if (RandomVal <= 0.5f) 
					{
						ToggleCharacters (2, false);
					}

					if (RandomVal > 0.5f) 
					{
						ToggleCharacters (3, false);
					}

					AmountOfDeaths++;
				}
				break;
			case 2:
				if (CurrentHealth [1] <= 0) 
				{
					float RandomVal = Random.Range (0, 1);

					if (RandomVal <= 0.5f) 
					{
						ToggleCharacters (1, false);
					}

					if (RandomVal > 0.5f) 
					{
						ToggleCharacters (3, false);
					}

					AmountOfDeaths++;
				}
				break;
			case 3:
				if (CurrentHealth [2] <= 0) 
				{
					float RandomVal = Random.Range (0, 1);

					if (RandomVal <= 0.5f) 
					{
						ToggleCharacters (1, false);
					}

					if (RandomVal > 0.5f)
					{
						ToggleCharacters (2, false);
					}

					AmountOfDeaths++;
				}
				break;
			}
		}
			
		// Spawn in remaining area.
		if (AmountOfDeaths == 1) 
		{
			switch (PlayerId) 
			{
			case 1:
				// No health in ID 1.
				if (CurrentHealth [0] <= 0)
				{
					// No health in ID 2.
					if (CurrentHealth [1] <= 0)
					{
						ToggleCharacters (3, false); // Go to ID 3.
					} 

					else // No health in ID 3.
					
					{
						ToggleCharacters (2, false); // Go to ID 2.
					}
				}
				break;
			case 2:
				if (CurrentHealth [1] <= 0)
				{
					// No health in ID 1.
					if (CurrentHealth [0] <= 0)
					{
						ToggleCharacters (3, false); // Go to ID 3.
					} 

					else // No health in ID 3.

					{
						ToggleCharacters (1, false); // Go to ID 1.
					}
				}
				break;
			case 3:
				if (CurrentHealth [2] <= 0)
				{
					// No health in ID 1.
					if (CurrentHealth [0] <= 0)
					{
						ToggleCharacters (2, false); // Go to ID 2.
					} 

					else // No health in ID 2.

					{
						ToggleCharacters (1, false); // Go to ID 1.
					}
				}
				break;
			}
		}
	}

	void CheckMinimapBorders ()
	{
		switch (PlayerId) 
		{
		case 1:
			GameController.Instance.MinimapBorders [0].enabled = true;
			GameController.Instance.MinimapBorders [1].enabled = false;
			GameController.Instance.MinimapBorders [2].enabled = false;
			break;
		case 2:
			GameController.Instance.MinimapBorders [0].enabled = false;
			GameController.Instance.MinimapBorders [1].enabled = true;
			GameController.Instance.MinimapBorders [2].enabled = false;
			break;
		case 3:
			GameController.Instance.MinimapBorders [0].enabled = false;
			GameController.Instance.MinimapBorders [1].enabled = false;
			GameController.Instance.MinimapBorders [2].enabled = true;
			break;
		}
	}

	void CheckMinimapFollowers ()
	{
		for (int i = 0; i < GameController.Instance.MinimapFollowers.Length; i++)
		{
			GameController.Instance.MinimapFollowers [i] = Dummies [i].transform;
			GameController.Instance.MinimapSimpleFollowScripts [i].OverrideTransform = GameController.Instance.MinimapFollowers [i].transform;
			GameController.Instance.MinimapSimpleFollowScripts [i].enabled = true;
		}
	}

	void CheckActiveMinimapFollower ()
	{
		for (int i = 0; i < 3; i++) 
		{
			if (i != (PlayerId - 1))
			{
				// Replace target follow with dummy transforms.
				GameController.Instance.MinimapSimpleFollowScripts [i].OverrideTransform = 
					GameController.Instance.MinimapFollowers [i].transform;
			}

			else // i = PlayerId - 1.
			
			{
				// Replace with player character transform.
				GameController.Instance.MinimapSimpleFollowScripts [i].OverrideTransform = 
					Character.transform;
			}
		}
	}

	void CheckSoldierObjectLayer ()
	{
		switch (PlayerId)
		{
		case 1:
			SoldierObject.layer = 12;

			Transform[] SoldierCollisionsA = SoldierObject.GetComponentsInChildren<Transform> ();

			foreach (Transform soldierCol in SoldierCollisionsA)
			{
				soldierCol.gameObject.layer = 12;
			}

			break;
		case 2:
			SoldierObject.layer = 13;

			Transform[] SoldierCollisionsB = SoldierObject.GetComponentsInChildren<Transform> ();

			foreach (Transform soldierCol in SoldierCollisionsB)
			{
				soldierCol.gameObject.layer = 13;
			}
			break;
		case 3:
			SoldierObject.layer = 14;

			Transform[] SoldierCollisionsC = SoldierObject.GetComponentsInChildren<Transform> ();

			foreach (Transform soldierCol in SoldierCollisionsC)
			{
				soldierCol.gameObject.layer = 14;
			}
			break;
		}
	}
}