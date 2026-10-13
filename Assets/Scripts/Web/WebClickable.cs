using UnityEngine;
using UnityEngine.Events;

// Objet cliquable à la souris dans la version WebGL (remplace les Interactables XR).
public class WebClickable : MonoBehaviour
{
	public UnityEvent onClick = new UnityEvent();
}
