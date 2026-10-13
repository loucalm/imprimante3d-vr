using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using static BuildImprimanteScene;

// Menu TP > Build TP3.2 - Checklist : écran UI, StepManager, checklist, flèches (à lancer après "Build Imprimante Scene").
public static class BuildTP32
{
	const string ScenePath = "Assets/Scenes/BasicScene.unity";
	const int UILayer = 5;

	[MenuItem("TP/Build TP3.2 - Checklist")]
	public static void Run()
	{
		var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
		var printer = GameObject.Find("Imprimante3D");
		root = printer.transform;

		// nettoyage des éléments provisoires du TP3.1 s'ils existent
		foreach (var n in new[] { "TexteEcran", "BoutonImprimer", "LabelBouton", "BedZone", "EcranUI", "FlechesPortes", "FlecheUSB", "FlecheBouton" })
		{
			var t = root.Find(n);
			if (t) Object.DestroyImmediate(t.gameObject);
		}
		foreach (var go in new[] { GameObject.Find("StepManager"), GameObject.Find("Checklist") })
			if (go) Object.DestroyImmediate(go);
		// PrintWorkflow / PlateItem (TP3.1) n'existent plus : on retire les composants orphelins
		foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
		var oldImp = printer.GetComponent<ImprimanteBehaviour>();
		if (oldImp) Object.DestroyImmediate(oldImp);

		// Event System VR
		if (!Object.FindFirstObjectByType<EventSystem>())
		{
			var es = new GameObject("EventSystem");
			es.AddComponent<EventSystem>();
			es.AddComponent<XRUIInputModule>();
		}

		var cam = Camera.main ? Camera.main : Object.FindFirstObjectByType<Camera>();

		// ----- Écran de l'imprimante : Canvas World Space -----
		var screen = new GameObject("EcranUI", typeof(RectTransform));
		screen.transform.SetParent(root, false);
		screen.transform.position = B(0.2465f, 0.182f, 0.1855f);
		screen.transform.rotation = Quaternion.LookRotation(-BD(1, 0, 0), Vector3.up);
		MakeCanvas(screen, new Vector2(154f, 87f), cam);
		var screenPanel = MakeImage("Panel", screen.transform, new Color(0.04f, 0.06f, 0.1f), Vector2.zero, Vector2.zero, true);
		var idle = MakeText("TexteAttente", screen.transform, "Insérez une clé USB", 12f, new Vector2(140f, 30f), Vector2.zero, TextAlignmentOptions.Center);
		var printButton = MakeButton("BoutonImprimer", screen.transform, "Imprimer", new Vector2(110f, 34f), Vector2.zero, 16f, new Color(0.1f, 0.6f, 0.25f), out var printLabel);

		// ----- StepManager + Checklist -----
		var stepGo = new GameObject("StepManager");
		var steps = stepGo.AddComponent<StepManager>();

		var checklist = new GameObject("Checklist", typeof(RectTransform));
		var cpos = new Vector3(-0.65f, 1.3f, 0.55f);
		checklist.transform.position = cpos;
		checklist.transform.rotation = Quaternion.LookRotation(new Vector3(cpos.x, 0f, cpos.z), Vector3.up); // regarde le joueur
		MakeCanvas(checklist, new Vector2(400f, 500f), cam);
		MakeImage("Panel", checklist.transform, new Color(0.05f, 0.07f, 0.12f, 0.92f), Vector2.zero, Vector2.zero, true);
		MakeText("Titre", checklist.transform, "Mise en route", 30f, new Vector2(380f, 44f), new Vector2(0f, 220f), TextAlignmentOptions.Center, bold: true);
		var list = MakeText("Etapes", checklist.transform, "", 22f, new Vector2(370f, 320f), new Vector2(0f, 20f), TextAlignmentOptions.TopLeft);
		var reset = MakeButton("BoutonRecommencer", checklist.transform, "Recommencer", new Vector2(220f, 48f), new Vector2(0f, -210f), 24f, new Color(0.7f, 0.25f, 0.2f), out _);
		steps.StepText = list;
		UnityEventTools.AddVoidPersistentListener(reset.onClick, steps.ResetTutorial);

		// ----- Flèches -----
		var arrowSwitch = root.Find("FlecheInterrupteur").gameObject;
		var arrowDoors = new GameObject("FlechesPortes");
		arrowDoors.transform.SetParent(root, true);
		MakeArrow("FlecheFacade", B(0.42f, 0.2625f, -0.06f), BD(-1, 0, 0)).transform.SetParent(arrowDoors.transform, true);
		MakeArrow("FlecheFilD", B(0.165f, 0.45f, -0.087f), BD(0, -1, 0)).transform.SetParent(arrowDoors.transform, true);
		MakeArrow("FlecheFilR", B(0.169f, -0.45f, -0.087f), BD(0, 1, 0)).transform.SetParent(arrowDoors.transform, true);
		var arrowUsb = MakeArrow("FlecheUSB", B(0.2f, 0.238f, 0.42f), Vector3.down);
		var arrowPrint = MakeArrow("FlecheBouton", B(0.36f, 0.182f, 0.1855f), BD(-1, 0, 0));
		steps.arrows = new[] { arrowSwitch, arrowDoors, arrowUsb, arrowPrint };
		foreach (var a in steps.arrows) a.SetActive(false);

		// ----- ImprimanteBehaviour -----
		var imp = printer.AddComponent<ImprimanteBehaviour>();
		imp.screenText = printLabel;
		imp.printButton = printButton.gameObject;
		imp.idleLabel = idle.gameObject;
		imp.stepManager = steps;
		imp.spools = new[] { root.Find("BobineD"), root.Find("BobineR") };
		UnityEventTools.AddVoidPersistentListener(printButton.onClick, imp.StartPrinting);
		UnityEventTools.AddIntPersistentListener(printButton.onClick, steps.AdvanceToStep, 5);

		// ----- Câblage des étapes -----
		// 1 -> 2 : interrupteur
		var swSimple = root.Find("Interrupteur").GetComponent<XRSimpleInteractable>();
		UnityEventTools.AddIntPersistentListener(swSimple.firstSelectEntered, steps.AdvanceToStep, 2);
		// 2 -> 3 : on touche une porte
		foreach (var n in new[] { "PorteFront", "PorteD", "PorteR" })
			UnityEventTools.AddIntPersistentListener(root.Find(n).GetComponent<XRGrabInteractable>().firstSelectEntered, steps.AdvanceToStep, 3);
		// 3 -> 4 : clé dans le port + affichage du bouton
		var socket = root.Find("USBPort").GetComponent<XRSocketInteractor>();
		UnityEventTools.AddIntPersistentListener(socket.selectEntered, steps.AdvanceToStep, 4);
		UnityEventTools.AddVoidPersistentListener(socket.selectEntered, imp.ShowPrintButton);
		UnityEventTools.AddVoidPersistentListener(socket.selectExited, imp.HidePrintButton);

		// Layer UI sur tous les canvas
		SetLayer(screen, UILayer);
		SetLayer(checklist, UILayer);

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene, ScenePath);
		Debug.Log("BUILD_TP32_OK");
	}

	// ---------- helpers UI ----------
	internal static void MakeCanvas(GameObject go, Vector2 size, Camera cam)
	{
		var c = go.AddComponent<Canvas>();
		c.renderMode = RenderMode.WorldSpace;
		c.worldCamera = cam;
		go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
		go.AddComponent<TrackedDeviceGraphicRaycaster>();
		var rt = (RectTransform)go.transform;
		rt.sizeDelta = size;
		rt.localScale = Vector3.one * 0.001f;
	}

	internal static Image MakeImage(string name, Transform parent, Color color, Vector2 size, Vector2 pos, bool stretch)
	{
		var go = new GameObject(name, typeof(RectTransform), typeof(Image));
		go.transform.SetParent(parent, false);
		var rt = (RectTransform)go.transform;
		if (stretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
		else { rt.sizeDelta = size; rt.anchoredPosition = pos; }
		var img = go.GetComponent<Image>(); img.color = color;
		return img;
	}

	internal static TMP_Text MakeText(string name, Transform parent, string text, float fontSize, Vector2 size, Vector2 pos, TextAlignmentOptions align, bool bold = false)
	{
		var go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		var rt = (RectTransform)go.transform; rt.sizeDelta = size; rt.anchoredPosition = pos;
		var t = go.AddComponent<TextMeshProUGUI>();
		t.text = text; t.fontSize = fontSize; t.alignment = align; t.color = Color.white; t.raycastTarget = false;
		if (bold) t.fontStyle = FontStyles.Bold;
		return t;
	}

	internal static Button MakeButton(string name, Transform parent, string label, Vector2 size, Vector2 pos, float fontSize, Color color, out TMP_Text labelText)
	{
		var img = MakeImage(name, parent, color, size, pos, false);
		var btn = img.gameObject.AddComponent<Button>();
		btn.targetGraphic = img;
		var colors = btn.colors; colors.highlightedColor = new Color(1f, 1f, 1f, 1f); colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f); btn.colors = colors;
		labelText = MakeText("Texte", img.transform, label, fontSize, size, Vector2.zero, TextAlignmentOptions.Center);
		((RectTransform)labelText.transform).anchorMin = Vector2.zero; ((RectTransform)labelText.transform).anchorMax = Vector2.one;
		((RectTransform)labelText.transform).offsetMin = ((RectTransform)labelText.transform).offsetMax = Vector2.zero;
		return btn;
	}

	internal static void SetLayer(GameObject go, int layer)
	{
		foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
	}
}
