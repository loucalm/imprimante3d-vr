using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Clic souris -> raycast -> WebClickable. Ignoré si on glisse (regard) ou si on clique sur l'interface.
[RequireComponent(typeof(Camera))]
public class WebInteractor : MonoBehaviour
{
	public float maxDistance = 6f;
	public float dragTolerance = 8f; // pixels

	Camera cam;
	Vector2 downPos;
	bool overUI;

	void Awake() => cam = GetComponent<Camera>();

	void Update()
	{
		var mouse = Mouse.current;
		if (mouse == null) return;

		if (mouse.leftButton.wasPressedThisFrame)
		{
			downPos = mouse.position.ReadValue();
			overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
		}

		if (mouse.leftButton.wasReleasedThisFrame && !overUI &&
			(mouse.position.ReadValue() - downPos).sqrMagnitude < dragTolerance * dragTolerance)
		{
			var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
			if (Physics.Raycast(ray, out var hit, maxDistance))
			{
				var clickable = hit.collider.GetComponentInParent<WebClickable>();
				if (clickable) clickable.onClick.Invoke();
			}
		}
	}
}
