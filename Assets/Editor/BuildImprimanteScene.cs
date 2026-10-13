using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Menu TP > Build Imprimante Scene : importe les modèles, extrait les matériaux, construit la scène BasicScene.
public static class BuildImprimanteScene
{
	const string PrinterModel = "Assets/Models/Imprimante3D_stagiaireCor.blend";
	const string KeyModel = "Assets/Models/clef.fbx";
	const string ScenePath = "Assets/Scenes/BasicScene.unity";
	const string RigPrefab = "Assets/Samples/XR Interaction Toolkit/3.3.2/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
	const float TableTop = 0.75f;

	internal static Transform root; // racine du modèle d'imprimante (rotation = yaw seul)

	// Repère Blender (X avant, Y gauche/droite, Z haut) -> repère local du modèle importé dans Unity.
	internal static Vector3 B(float x, float y, float z) => root.TransformPoint(new Vector3(-x, z, -y));
	internal static Vector3 BD(float x, float y, float z) => root.TransformDirection(new Vector3(-x, z, -y));

	[MenuItem("TP/Build Imprimante Scene")]
	public static void Run()
	{
		SetupModelImport(PrinterModel, "Assets/Materials/Imprimante3D");
		SetupModelImport(KeyModel, "Assets/Materials/CleUSB");
		FixMaterials();
		BuildScene();
		Debug.Log("BUILD_IMPRIMANTE_OK");
	}

	// ---------- 1/2 : import des modèles ----------
	static void SetupModelImport(string modelPath, string materialsDir)
	{
		var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
		importer.isReadable = true; // "Read/Write" (correction du bug futur)
		importer.SaveAndReimport();

		Directory.CreateDirectory(materialsDir);
		foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(modelPath))
		{
			if (obj is Material m)
			{
				var err = AssetDatabase.ExtractAsset(m, $"{materialsDir}/{m.name}.mat"); // "Extract Materials..."
				if (!string.IsNullOrEmpty(err)) Debug.LogWarning("ExtractAsset: " + err);
			}
		}
		AssetDatabase.WriteImportSettingsIfDirty(modelPath);
		AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
		AssetDatabase.Refresh();
	}

	static void FixMaterials()
	{
		// La vitre est opaque ! -> Surface Type "Transparent" + alpha réduit
		var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Imprimante3D/Plexiglass.mat");
		if (glass)
		{
			glass.SetFloat("_Surface", 1f);
			glass.SetFloat("_Blend", 0f);
			glass.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
			glass.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			glass.SetFloat("_ZWrite", 0f);
			glass.SetOverrideTag("RenderType", "Transparent");
			glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			glass.renderQueue = (int)RenderQueue.Transparent;
			var c = glass.GetColor("_BaseColor"); c.a = 0.25f;
			glass.SetColor("_BaseColor", c);
			EditorUtility.SetDirty(glass);
		}
		var screen = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Imprimante3D/Ecran.mat");
		if (screen) { screen.SetColor("_BaseColor", new Color(0.02f, 0.03f, 0.05f)); EditorUtility.SetDirty(screen); }
		AssetDatabase.SaveAssets();
	}

	// ---------- 3 : scène ----------
	static void BuildScene()
	{
		var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
		var cam = GameObject.Find("Main Camera");
		if (cam) Object.DestroyImmediate(cam); // le XR Origin a sa propre caméra

		new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

		var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefab));
		rig.transform.position = Vector3.zero;
		foreach (var nf in rig.GetComponentsInChildren<NearFarInteractor>(true))
		{
			nf.enableFarCasting = false; // pas de rayon laser bleu
			PrefabUtility.RecordPrefabInstancePropertyModifications(nf);
		}

		// Sol + table
		var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name = "Sol";
		floor.transform.localScale = new Vector3(2f, 1f, 2f);
		var table = GameObject.CreatePrimitive(PrimitiveType.Cube); table.name = "Table";
		table.transform.position = new Vector3(0f, TableTop / 2f, 1.0f);
		table.transform.localScale = new Vector3(1.6f, TableTop, 0.9f);
		table.isStatic = true;

		// Imprimante
		var printer = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrinterModel));
		printer.name = "Imprimante3D";
		root = printer.transform;
		root.position = new Vector3(0f, TableTop + 0.257f, 1.0f);
		root.rotation = Quaternion.Euler(0f, 270f, 0f); // face avant tournée vers le joueur (-Z)

		var body = Child("Imprimante");
		var mc = body.AddComponent<MeshCollider>(); mc.convex = false;
		body.isStatic = true;

		var frontHinge = SetupDoor(Child("PorteFront"),
			anchor: B(0.2795f, -0.297f, -0.06f), freeEdge: B(0.2795f, 0.285f, -0.06f), openDir: BD(1, 0, 0),
			handleCenter: B(0.295f, 0.2625f, -0.06f), handleHeight: 0.13f, handleRadius: 0.03f);
		var sideD = SetupDoor(Child("PorteD"),
			anchor: B(-0.025f, 0.3145f, -0.087f), freeEdge: B(0.187f, 0.3145f, -0.087f), openDir: BD(0, 1, 0),
			handleCenter: B(0.165f, 0.328f, -0.087f), handleHeight: 0.1f, handleRadius: 0.02f);
		var sideR = SetupDoor(Child("PorteR"),
			anchor: B(-0.021f, -0.316f, -0.087f), freeEdge: B(0.19f, -0.316f, -0.087f), openDir: BD(0, -1, 0),
			handleCenter: B(0.169f, -0.328f, -0.087f), handleHeight: 0.1f, handleRadius: 0.02f);

		// Interrupteur à l'arrière
		var sw = Child("Interrupteur");
		var swBox = sw.AddComponent<BoxCollider>(); swBox.size *= 1.5f;
		var swRb = sw.AddComponent<Rigidbody>(); swRb.useGravity = false; swRb.isKinematic = true;
		var swSimple = sw.AddComponent<XRSimpleInteractable>();
		var swBehaviour = sw.AddComponent<SwitchBehaviour>();
		UnityEventTools.AddVoidPersistentListener(swSimple.firstSelectEntered, swBehaviour.ToggleSwitch);

		// Flèche indiquant l'interrupteur
		MakeArrow("FlecheInterrupteur", B(-0.40f, 0.237f, -0.192f), BD(1, 0, 0));

		// Port USB (au-dessus) + Socket Interactor
		var port = new GameObject("USBPort");
		port.transform.SetParent(root, false);
		port.transform.position = B(0.2f, 0.238f, 0.262f);
		var portBox = port.AddComponent<BoxCollider>(); portBox.isTrigger = true; portBox.size = new Vector3(0.06f, 0.06f, 0.07f);
		var socket = port.AddComponent<XRSocketInteractor>();
		var portAttach = new GameObject("AttachTransform");
		portAttach.transform.SetParent(port.transform, false);
		portAttach.transform.position = B(0.2f, 0.238f, 0.259f);
		portAttach.transform.rotation = Quaternion.LookRotation(Vector3.down, BD(1, 0, 0)); // la clé entre pointe vers le bas
		socket.attachTransform = portAttach.transform;
		socket.interactionLayers = InteractionLayerMask.GetMask("USB Keys");

		// Clé USB posée à côté de l'imprimante
		var key = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(KeyModel));
		key.name = "CleUSB";
		key.transform.position = new Vector3(-0.55f, TableTop + 0.01f, 0.9f);
		key.GetComponentInChildren<MeshRenderer>().gameObject.AddComponent<BoxCollider>();
		var keyRb = key.AddComponent<Rigidbody>(); keyRb.mass = 0.05f;
		var keyGrab = key.AddComponent<XRGrabInteractable>();
		var keyAttach = new GameObject("AttachTransform");
		keyAttach.transform.SetParent(key.transform, false);
		// la racine du modèle .fbx est tournée de 270° en X : on place l'attach en repère monde
		keyAttach.transform.position = key.transform.position + new Vector3(0f, 0f, -0.005f);
		keyAttach.transform.rotation = Quaternion.identity; // Z monde = pointe de la clé
		keyGrab.attachTransform = keyAttach.transform;
		keyGrab.interactionLayers = InteractionLayerMask.GetMask("Default", "USB Keys");

		// Objet sur le plateau à retirer
		var item = GameObject.CreatePrimitive(PrimitiveType.Cube); item.name = "ObjetPlateau";
		item.transform.SetParent(root, true);
		item.transform.position = B(0.05f, 0f, -0.168f + 0.031f);
		item.transform.rotation = root.rotation;
		item.transform.localScale = Vector3.one * 0.06f;
		item.GetComponent<MeshRenderer>().sharedMaterial = MakeMaterial("Assets/Materials/ObjetPlateau.mat", new Color(1f, 0.5f, 0.1f));
		item.AddComponent<Rigidbody>().mass = 0.2f;
		item.AddComponent<XRGrabInteractable>();

		// Lumière temps réel à l'intérieur
		var lightGo = new GameObject("LumiereInterieure");
		lightGo.transform.SetParent(root, false);
		lightGo.transform.position = B(0.06f, 0f, 0.07f);
		var l = lightGo.AddComponent<Light>(); l.type = LightType.Point; l.range = 0.6f; l.intensity = 1.5f; l.color = new Color(1f, 0.95f, 0.85f);

		EditorSceneManager.SaveScene(scene, ScenePath);
		EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
	}

	// ---------- helpers ----------
	static GameObject Child(string name)
	{
		var t = root.Find(name);
		if (!t) throw new System.Exception("Enfant introuvable : " + name);
		return t.gameObject;
	}

	static HingeJoint SetupDoor(GameObject door, Vector3 anchor, Vector3 freeEdge, Vector3 openDir,
		Vector3 handleCenter, float handleHeight, float handleRadius)
	{
		var rb = door.AddComponent<Rigidbody>();
		rb.useGravity = false; rb.mass = 1f; rb.angularDamping = 2f;
		rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
		door.AddComponent<BoxCollider>();

		var handle = new GameObject("Poignee");
		handle.transform.SetParent(door.transform, true);
		handle.transform.position = handleCenter;
		handle.transform.rotation = Quaternion.identity;
		var cap = handle.AddComponent<CapsuleCollider>();
		cap.direction = 1; cap.height = handleHeight; cap.radius = handleRadius;

		var grab = door.AddComponent<XRGrabInteractable>();
		grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
		grab.useDynamicAttach = true;
		grab.throwOnDetach = false;
		grab.colliders.Clear();
		grab.colliders.Add(cap); // on ne prend la porte que par la poignée

		// axe vertical, orienté pour qu'un angle positif ouvre la porte
		var axis = Vector3.up;
		var r = freeEdge - anchor;
		if (Vector3.Dot(Quaternion.AngleAxis(30f, axis) * r - r, openDir) < 0f) axis = -axis;

		var hj = door.AddComponent<HingeJoint>();
		hj.anchor = door.transform.InverseTransformPoint(anchor);
		hj.axis = door.transform.InverseTransformDirection(axis);
		hj.useLimits = true;
		hj.limits = new JointLimits { min = 0f, max = 105f };
		return hj;
	}

	internal static Material MakeMaterial(string path, Color color)
	{
		var m = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (!m)
		{
			m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
			AssetDatabase.CreateAsset(m, path);
		}
		m.SetColor("_BaseColor", color);
		EditorUtility.SetDirty(m);
		return m;
	}

	internal static GameObject MakeArrow(string name, Vector3 pos, Vector3 towardDir)
	{
		var arrowMat = MakeMaterial("Assets/Materials/Fleche.mat", new Color(1f, 0.45f, 0f));
		var go = new GameObject(name);
		go.transform.SetParent(root, true);
		go.transform.position = pos;
		go.transform.rotation = Quaternion.LookRotation(towardDir);

		var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		Object.DestroyImmediate(shaft.GetComponent<Collider>());
		shaft.transform.SetParent(go.transform, false);
		shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
		shaft.transform.localScale = new Vector3(0.015f, 0.035f, 0.015f); // long de 7 cm le long de Z
		shaft.transform.localPosition = new Vector3(0f, 0f, -0.035f);
		shaft.GetComponent<MeshRenderer>().sharedMaterial = arrowMat;

		var head = new GameObject("Pointe");
		head.transform.SetParent(go.transform, false);
		head.AddComponent<MeshFilter>().sharedMesh = ConeMesh(0.03f, 0.05f);
		head.AddComponent<MeshRenderer>().sharedMaterial = arrowMat;

		go.AddComponent<ArrowBob>();
		return go;
	}

	// cône dont la pointe est en +Z, base en z=0
	static Mesh ConeMesh(float radius, float length)
	{
		const int n = 16;
		var v = new Vector3[n + 2]; var tri = new int[n * 6];
		v[0] = new Vector3(0, 0, length); v[1] = Vector3.zero;
		for (int i = 0; i < n; i++)
		{
			float a = i * Mathf.PI * 2f / n;
			v[i + 2] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
		}
		for (int i = 0; i < n; i++)
		{
			int a = i + 2, b = (i + 1) % n + 2;
			tri[i * 6 + 0] = 0; tri[i * 6 + 1] = b; tri[i * 6 + 2] = a;   // flanc
			tri[i * 6 + 3] = 1; tri[i * 6 + 4] = a; tri[i * 6 + 5] = b;   // base
		}
		var m = new Mesh { name = "FlecheCone", vertices = v, triangles = tri };
		m.RecalculateNormals(); m.RecalculateBounds();
		return m;
	}
}
