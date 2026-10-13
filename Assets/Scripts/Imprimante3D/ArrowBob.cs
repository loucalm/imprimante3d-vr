using UnityEngine;

// Fait osciller la flèche d'indication le long de son axe local Z (vers l'interrupteur).
public class ArrowBob : MonoBehaviour
{
	public float amplitude = 0.02f;
	public float speed = 3f;
	Vector3 startLocalPos;

	void Awake() => startLocalPos = transform.localPosition;

	void Update()
	{
		transform.localPosition = startLocalPos + Vector3.forward * (Mathf.Sin(Time.time * speed) * amplitude);
	}
}
