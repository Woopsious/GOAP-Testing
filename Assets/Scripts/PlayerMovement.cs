using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	Rigidbody rb;

	private readonly float moveSpeed = 12f;
	private readonly float rotateSpeed = 30f;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}
	private void FixedUpdate()
	{
		MovePlayer();
		Rotateplayer();
	}

	void MovePlayer()
	{
		Vector3 moveInput = new(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		rb.linearVelocity = moveInput.x * moveSpeed * transform.right + moveInput.z * moveSpeed * transform.forward;
	}

	void Rotateplayer()
	{
		if (Input.GetKey(KeyCode.Q))
			transform.Rotate(-Vector3.up * rotateSpeed * Time.deltaTime);
		else if (Input.GetKey(KeyCode.E))
			transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
	}
}
