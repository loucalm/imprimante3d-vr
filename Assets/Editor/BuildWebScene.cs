using System;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static BuildTP32;

// Menu TP > Build Web Scene : copie BasicScene en BasicScene_Web, retire le rig XR et remplace les interactions VR
// par des interactions souris / clavier.  Menu TP > Build WebGL : génère le build dans ../WebGL_Build.
public static class BuildWebScene
{
	const string SrcScene = "Assets/Scenes/BasicScene.unity";
	const string WebScene = "Assets/Scenes/BasicScene_Web.unity";

	[MenuItem("TP/Build Web Scene")]
	public static void Run()
	{
		AssetDatabase.DeleteAsset(WebScene);
		AssetDatabase.CopyAsset(SrcScene, WebScene);
		var scene = EditorSceneManager.OpenScene(WebScene, OpenSceneMode.Single);

		var printer = GameObject.Find("Imprimante3D");
		var root = printer.transform;
		var steps = UnityEngine.Object.FindFirstObjectByType<StepManager>();
		var imp = printer.GetComponent<ImprimanteBehaviour>();

		// 1. charnières lues avant suppression (pivot + axe en repère monde)
		string[] doorNames = { "PorteFront", "PorteD", "PorteR" };
		var pivots = new Vector3[3]; var axes = new Vector3[3];
		for (int i = 0; i < 3; i++)
		{
			var d = root.Find(doorNames[i]);
			var hj = d.GetComponent<HingeJoint>();
			pivots[i] = d.TransformPoint(hj.anchor);
			axes[i] = d.TransformDirection(hj.axis).normalized;
		}

		// 2. on retire tout le XR
		foreach (var n in new[] { "XR Origin (XR Rig)", "XR Origin Hands (XR Rig)", "XR Interaction Manager", "EventSystem", "Checklist" })
		{
			var go = GameObject.Find(n);
			if (go) UnityEngine.Object.DestroyImmediate(go);
		}
		for (int pass = 0; pass < 3; pass++)
			foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				if (!mb) continue;
				var ns = mb.GetType().Namespace ?? "";
				if (ns.StartsWith("UnityEngine.XR.Interaction.Toolkit"))
					try { UnityEngine.Object.DestroyImmediate(mb); } catch (Exception) { }
			}
		foreach (var j in UnityEngine.Object.FindObjectsByType<Joint>(FindObjectsInactive.Include, FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(j);
		foreach (var r in UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(r);
		var portCol = root.Find("USBPort").GetComponent<Collider>();
		if (portCol) UnityEngine.Object.DestroyImmediate(portCol);

		// 3. caméra + contrôles
		var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
		camGo.transform.position = new Vector3(0f, 1.55f, -0.2f);
		camGo.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
		var cam = camGo.AddComponent<Camera>();
		cam.fieldOfView = 60f; cam.nearClipPlane = 0.05f;
		camGo.AddComponent<AudioListener>();
		var cc = camGo.AddComponent<CharacterController>();
		cc.height = 1.6f; cc.radius = 0.2f; cc.center = new Vector3(0f, -0.7f, 0f);
		camGo.AddComponent<WebFirstPersonCamera>();
		camGo.AddComponent<WebInteractor>();

		var es = new GameObject("EventSystem");
		es.AddComponent<EventSystem>();
		var module = es.AddComponent<InputSystemUIInputModule>();
		module.AssignDefaultActions();

		// 4. portes
		for (int i = 0; i < 3; i++)
		{
			var d = root.Find(doorNames[i]).gameObject;
			var door = d.AddComponent<WebDoor>();
			door.pivot = pivots[i]; door.axis = axes[i]; door.openAngle = 100f;
			var click = d.AddComponent<WebClickable>();
			UnityEventTools.AddVoidPersistentListener(click.onClick, door.Toggle);
			UnityEventTools.AddIntPersistentListener(door.onFirstOpen, steps.AdvanceToStep, 3);
		}

		// 5. interrupteur
		var sw = root.Find("Interrupteur").gameObject;
		var swClick = sw.AddComponent<WebClickable>();
		UnityEventTools.AddVoidPersistentListener(swClick.onClick, sw.GetComponent<SwitchBehaviour>().ToggleSwitch);
		UnityEventTools.AddIntPersistentListener(swClick.onClick, steps.AdvanceToStep, 2);

		// 6. clé USB
		var key = GameObject.Find("CleUSB");
		var keyComp = key.AddComponent<WebKey>();
		keyComp.keyAttach = key.transform.Find("AttachTransform");
		keyComp.portAttach = root.Find("USBPort/AttachTransform");
		UnityEventTools.AddVoidPersistentListener(keyComp.onInserted, imp.ShowPrintButton);
		UnityEventTools.AddIntPersistentListener(keyComp.onInserted, steps.AdvanceToStep, 4);
		UnityEventTools.AddVoidPersistentListener(keyComp.onRemoved, imp.HidePrintButton);
		var keyClick = key.AddComponent<WebClickable>();
		UnityEventTools.AddVoidPersistentListener(keyClick.onClick, keyComp.Toggle);

		// 7. objet du plateau : un clic le retire
		var item = root.Find("ObjetPlateau");
		if (item)
		{
			var itemClick = item.gameObject.AddComponent<WebClickable>();
			UnityEventTools.AddBoolPersistentListener(itemClick.onClick, item.gameObject.SetActive, false);
		}

		// 8. écran de l'imprimante : raycaster standard
		var screen = root.Find("EcranUI");
		var tracked = screen.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
		if (tracked) UnityEngine.Object.DestroyImmediate(tracked);
		screen.gameObject.AddComponent<GraphicRaycaster>();
		screen.GetComponent<Canvas>().worldCamera = cam;

		// 9. interface écran : checklist + aide
		var ui = new GameObject("WebUI", typeof(RectTransform));
		var canvas = ui.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		var scaler = ui.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280f, 720f); scaler.matchWidthOrHeight = 0.5f;
		ui.AddComponent<GraphicRaycaster>();

		var panel = MakeImage("Checklist", ui.transform, new Color(0.05f, 0.07f, 0.12f, 0.9f), new Vector2(340f, 420f), new Vector2(16f, -16f), false);
		Anchor((RectTransform)panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f));
		MakeText("Titre", panel.transform, "Mise en route", 28f, new Vector2(320f, 40f), new Vector2(0f, 180f), TextAlignmentOptions.Center, bold: true);
		var list = MakeText("Etapes", panel.transform, "", 20f, new Vector2(320f, 290f), new Vector2(0f, 10f), TextAlignmentOptions.TopLeft);
		var reset = MakeButton("BoutonRecommencer", panel.transform, "Recommencer", new Vector2(200f, 44f), new Vector2(0f, -175f), 22f, new Color(0.7f, 0.25f, 0.2f), out _);
		steps.StepText = list;
		UnityEventTools.AddVoidPersistentListener(reset.onClick, steps.ResetTutorial);

		var help = MakeText("Aide", ui.transform, "Glisser : regarder   •   ZQSD / flèches : se déplacer   •   Molette : zoom   •   Clic : interagir", 18f, new Vector2(900f, 30f), new Vector2(0f, 24f), TextAlignmentOptions.Center);
		Anchor((RectTransform)help.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
		SetLayer(ui, 5);

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene, WebScene);
		Debug.Log("BUILD_WEBSCENE_OK");
	}

	static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot)
	{
		var pos = rt.anchoredPosition;
		rt.anchorMin = rt.anchorMax = anchor;
		rt.pivot = pivot;
		rt.anchoredPosition = pos;
	}

	[MenuItem("TP/Build WebGL")]
	public static void BuildWebGL()
	{
		PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // pas d'en-têtes serveur spéciaux
		PlayerSettings.WebGL.decompressionFallback = false;
		PlayerSettings.WebGL.template = "PROJECT:Imprimante";
		string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../WebGL_Build"));
		var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
		{
			scenes = new[] { WebScene },
			locationPathName = outDir,
			target = BuildTarget.WebGL,
			options = BuildOptions.None
		});
		Debug.Log("WEBGL_BUILD_" + report.summary.result + " size=" + report.summary.totalSize + " errors=" + report.summary.totalErrors + " out=" + outDir);
	}
}
