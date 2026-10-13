// Assets/Scripts/Checklist/StepManager.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement; // important pour pouvoir utiliser SceneManager.LoadScene()

public class StepManager : MonoBehaviour
{
	public int Step = 0;
	public TMP_Text StepText; // zone d'affichage de la checklist
	public List<string> checklist;
	public GameObject[] arrows; // arrows[0] : interrupteur, [1] : portes, [2] : port USB, [3] : bouton Imprimer

	void Start()
	{
		// Liste de chaque étape en format texte. Peut être remplie dans l'éditeur,
		// dans ce cas, les valeurs ci-dessous seront ignorées.
		if (checklist == null || checklist.Count == 0)
		{
			checklist = new List<string>(4)
			{
				"Mettez l'imprimante sur ON",
				"Inspecter le compartiment d'impression",
				"Brancher la clé USB dans le port",
				"Appuyez sur le bouton \"imprimer\""
			};
		}
		StepText.text = "";
		AdvanceToStep(1);
	}

	public void ResetTutorial()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	public void AdvanceToStep(int stepToAdvance)
	{
		// On ne passe à l'étape suivante que si on est à l'étape d'avant (interdire de sauter une étape)
		if (stepToAdvance != Step + 1) return;

		if (Step < checklist.Count)
			StepText.text += $"{Step + 1}. {checklist[Step]}\n\n";
		else
			StepText.text = "Et voilà ! Vous pouvez recommencer le guide en cliquant sur le bouton.";

		if (Step - 1 >= 0 && Step - 1 < arrows.Length && arrows[Step - 1])
			arrows[Step - 1].SetActive(false); // Cacher la flèche d'avant (si elle existe)
		if (Step < arrows.Length && arrows[Step])
			arrows[Step].SetActive(true); // Afficher la flèche d'après (si elle existe)

		Step++;
	}
}
