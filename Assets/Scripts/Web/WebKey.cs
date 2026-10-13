using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Clé USB cliquable : un clic l'insère dans le port, un second clic la ressort.
public class WebKey : MonoBehaviour
{
	public Transform keyAttach;     // AttachTransform de la clé
	public Transform portAttach;    // AttachTransform du port USB
	public float duration = 0.6f;
	public UnityEvent onInserted = new UnityEvent();
	public UnityEvent onRemoved = new UnityEvent();

	Vector3 homePos;
	Quaternion homeRot;
	bool inserted, moving;

	void Awake() { homePos = transform.position; homeRot = transform.rotation; }

	public void Toggle()
	{
		if (moving) return;
		if (!inserted)
		{
			var rot = portAttach.rotation * Quaternion.Inverse(keyAttach.localRotation);
			var pos = portAttach.position - rot * keyAttach.localPosition;
			StartCoroutine(Move(pos, rot, () => { inserted = true; onInserted?.Invoke(); }));
		}
		else
		{
			inserted = false;
			onRemoved?.Invoke();
			StartCoroutine(Move(homePos, homeRot, null));
		}
	}

	IEnumerator Move(Vector3 pos, Quaternion rot, System.Action done)
	{
		moving = true;
		Vector3 p0 = transform.position; Quaternion r0 = transform.rotation;
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime / duration;
			float k = Mathf.SmoothStep(0f, 1f, t);
			transform.SetPositionAndRotation(Vector3.Lerp(p0, pos, k), Quaternion.Slerp(r0, rot, k));
			yield return null;
		}
		transform.SetPositionAndRotation(pos, rot);
		moving = false;
		done?.Invoke();
	}
}
