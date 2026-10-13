using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Caméra à la première personne : glisser = regarder, ZQSD / WASD / flèches = se déplacer, molette = zoom.
[RequireComponent(typeof(CharacterController))]
public class WebFirstPersonCamera : MonoBehaviour
{
	public float moveSpeed = 1.5f;
	public float lookSpeed = 0.15f;
	public float minFov = 25f, maxFov = 60f;

	Camera cam;
	CharacterController controller;
	float yaw, pitch;
	bool dragging;

	void Start()
	{
		cam = GetComponent<Camera>();
		controller = GetComponent<CharacterController>();
		var e = transform.eulerAngles;
		yaw = e.y;
		pitch = e.x > 180f ? e.x - 360f : e.x;
	}

	void Update()
	{
		var mouse = Mouse.current;
		var kb = Keyboard.current;
		if (mouse == null || kb == null) return;

		if (mouse.leftButton.wasPressedThisFrame)
			dragging = !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
		if (mouse.leftButton.wasReleasedThisFrame) dragging = false;

		if (dragging)
		{
			Vector2 d = mouse.delta.ReadValue();
			yaw += d.x * lookSpeed;
			pitch = Mathf.Clamp(pitch - d.y * lookSpeed, -80f, 80f);
			transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
		}

		float fwd = 0f, right = 0f;
		if (kb.wKey.isPressed || kb.zKey.isPressed || kb.upArrowKey.isPressed) fwd += 1f;
		if (kb.sKey.isPressed || kb.downArrowKey.isPressed) fwd -= 1f;
		if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) right += 1f;
		if (kb.aKey.isPressed || kb.qKey.isPressed || kb.leftArrowKey.isPressed) right -= 1f;

		var flatFwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
		var flatRight = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
		controller.Move((flatFwd * fwd + flatRight * right) * (moveSpeed * Time.deltaTime));

		float scroll = mouse.scroll.ReadValue().y;
		if (Mathf.Abs(scroll) > 0.01f)
			cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - Mathf.Sign(scroll) * 4f, minFov, maxFov);
	}
}
