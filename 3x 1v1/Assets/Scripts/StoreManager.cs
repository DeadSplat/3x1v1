using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoreManager : MonoBehaviour
{
	public static StoreManager Instance { get; private set; }
	public GameObject StoreUI;
	public bool StoreMenuActive;

	public GameObject[] Items;
	public bool[] ItemBoughtState;

	void Awake ()
	{
		Instance = this;
	}

	void Update ()
	{
		if (Input.GetKeyDown (KeyCode.B) && !GameController.Instance.roundIsActive)
		{
			StoreMenuActive = !StoreMenuActive;

			if (StoreMenuActive == true) 
			{
				StoreUI.SetActive (true);
				SetMouseState (true, 0);
				FlyCamera.Instance.enabled = false;
			}

			if (StoreMenuActive == false) 
			{
				StoreUI.SetActive (false);
				SetMouseState (false, 2);
				FlyCamera.Instance.enabled = true;
			}
		}
	}

	void SetMouseState (bool Visible, int LockState)
	{
		Cursor.visible = Visible;
		Cursor.lockState = (CursorLockMode)LockState;
	}

	// Add this function to an OnCLick event for the item button.
	public void PayItem (int cost)
	{
		// Player has enough money to buy the item in the store.
		if (PlayerController.Instance.CurrentMoney >= cost) 
		{
			PlayerController.Instance.CurrentMoney -= cost; // Pay for the item.

			// Set button bought state.
			int buttonBoughtState = GetComponent<ItemButton> ().itemId;
			ItemBoughtState [buttonBoughtState] = true;
			ActivateItem (buttonBoughtState);
		} 

		else 
		
		{
			Debug.Log ("Player does not have enough money to buy this item.");
		}
	}

	public void ActivateItem (int itemId)
	{
		if (ItemBoughtState[itemId] == true)
		{
			Items [itemId].SetActive (true);
		}
	}
}