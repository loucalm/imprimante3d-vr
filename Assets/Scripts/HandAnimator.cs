// Assets/Scripts/HandAnimator.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class HandAnimator : MonoBehaviour
{
	public InputActionProperty triggerAction; // XRI (Left|Right) Interaction/Activate Value
	public InputActionProperty gripAction;    // XRI (Left|Right) Interaction/Select Value

	float _triggerValue;
	float _gripValue;
	Animator animator;

	void Start()
	{
		animator = GetComponent<Animator>();
	}

	void Update()
	{
		_triggerValue = triggerAction.action.ReadValue<float>();
		_gripValue = gripAction.action.ReadValue<float>();

		animator.SetFloat("Trigger", _triggerValue);
		animator.SetFloat("Grip", _gripValue);
	}
}
