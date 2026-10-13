using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Porte qui s'ouvre / se ferme au clic en pivotant autour d'une charnière (pivot + axe en repère monde).
public class WebDoor : MonoBehaviour
{
	public Vector3 pivot;
	public Vector3 axis = Vector3.up;
	public float openAngle = 100f;
	public float duration = 0.6f;
	public UnityEvent onFirstOpen = new UnityEvent();

	bool isOpen, moving, openedOnce;
	float angle;

	public void Toggle()
	{
		if (moving) return;
		StartCoroutine(Rotate(isOpen ? 0f : openAngle));
		isOpen = !isOpen;
		if (isOpen && !openedOnce) { openedOnce = true; onFirstOpen?.Invoke(); }
	}

	IEnumerator Rotate(float target)
	{
		moving = true;
		float start = angle, t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime / duration;
			float next = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
			transform.RotateAround(pivot, axis, next - angle);
			angle = next;
			yield return null;
		}
		transform.RotateAround(pivot, axis, target - angle);
		angle = target;
		moving = false;
	}
}
