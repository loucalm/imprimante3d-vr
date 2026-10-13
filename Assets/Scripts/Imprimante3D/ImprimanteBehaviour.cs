// Assets/Scripts/Imprimante3D/ImprimanteBehaviour.cs
using TMPro;
using UnityEngine;

public class ImprimanteBehaviour : MonoBehaviour
{
	public TMP_Text screenText;      // texte du bouton "Imprimer"
	public GameObject printButton;   // bouton "Imprimer" de l'écran
	public GameObject idleLabel;     // message affiché tant que le bouton est caché
	public StepManager stepManager;
	public Transform[] spools;       // bobines, tournent pendant l'impression
	public bool isPrinting;

	bool keyInserted;

	void Start()
	{
		printButton.SetActive(false);
	}

	public void StartPrinting()
	{
		if (!isPrinting)
		{
			screenText.text = "Impression...";
			isPrinting = true;
		}
	}

	// Événement "Select Entered" du Socket Interactor du port USB.
	public void ShowPrintButton() => keyInserted = true;

	// Événement "Select Exited" du Socket Interactor : la clé est retirée.
	public void HidePrintButton() => keyInserted = false;

	void Update()
	{
		// Clé branchée avant d'avoir fait les étapes 1 et 2 : on rattrape l'étape 4 dès que possible.
		if (keyInserted && stepManager.Step == 3) stepManager.AdvanceToStep(4);

		// Le bouton n'apparaît que si la clé est là ET que les étapes précédentes sont faites
		// (sinon on pourrait imprimer sans avoir allumé l'imprimante).
		bool canPrint = keyInserted && stepManager.Step >= 4;
		if (printButton.activeSelf != canPrint) printButton.SetActive(canPrint);
		if (idleLabel) idleLabel.SetActive(!canPrint);

		if (isPrinting)
			foreach (var s in spools) if (s) s.Rotate(Vector3.up, 90f * Time.deltaTime, Space.Self);
	}
}
