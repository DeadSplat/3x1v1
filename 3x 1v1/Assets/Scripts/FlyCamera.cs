using UnityEngine;

public class FlyCamera : MonoBehaviour
{
	public static FlyCamera Instance { get; private set; }

	public float movementSpeed = 1.0f;
	public float mouseSens = 2;

	void Start ()
	{
		Instance = this;
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	void Update () 
	{
		//movementSpeed = Mathf.Max (movementSpeed += Input.GetAxis ("Mouse ScrollWheel"), 0.0f);
		transform.position += (transform.right * Input.GetAxis ("Horizontal") + transform.forward * Input.GetAxis ("Vertical")) * movementSpeed * Time.deltaTime;
		transform.eulerAngles += new Vector3 (-Input.GetAxis("Mouse Y") * mouseSens * Time.deltaTime, Input.GetAxis ("Mouse X") * mouseSens * Time.deltaTime, 0);
	}
}
