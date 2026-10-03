using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Rebuilds the prototype scene (level, primitive models, player prefab, HUD) from scratch.
/// WARNING: overwrites Assets/Scenes/PhotoPrototype.unity and Assets/Prefabs/Player.prefab.
/// </summary>
public static class PhotoPrototypeSceneBuilder
{
    const string ScenePath = "Assets/Scenes/PhotoPrototype.unity";
    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

    [MenuItem("Tools/For The Record/Rebuild Prototype Scene")]
    static void RebuildFromMenu()
    {
        if (EditorUtility.DisplayDialog("Rebuild prototype scene",
                "This overwrites " + ScenePath + " and " + PlayerPrefabPath + ". Continue?", "Rebuild", "Cancel"))
            Build();
    }

    public static void Build()
    {
        EditorSceneManager.SaveOpenScenes();
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets", "Prefabs");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        BuildEnvironment();
        var playerPrefab = BuildPlayerPrefab();
        BuildPlayerManager(playerPrefab);
        var capture = BuildBigCamera(new Vector3(0f, 1.4f, -8f));
        BuildFlash(new Vector3(-5f, 0.3f, -9f));
        BuildRemote(new Vector3(5f, 0.15f, -9f), capture);

        var statue = BuildStatue(new Vector3(-12f, 0f, 8f));
        var orb = BuildOrb(new Vector3(12f, 0f, 8.5f));
        var stroller = BuildStroller(new Vector3(-4f, 0f, 4.5f));

        BuildHud(capture.GetComponent<HeavyCamera>());
        var manager = new GameObject("Game Manager").AddComponent<PhotoGameManager>();
        SetArray(manager, "tasks", statue, orb, stroller);

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    // ---------- Level ----------

    static void BuildEnvironment()
    {
        Camera.main.transform.SetPositionAndRotation(new Vector3(0f, 26f, -20f), Quaternion.Euler(55f, 0f, 0f));
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            light.intensity = 0.4f;

        var level = new GameObject("Level").transform;
        var ground = Mat("Ground", new Color(0.35f, 0.4f, 0.35f));
        var wall = Mat("Wall", new Color(0.55f, 0.5f, 0.45f));
        var crate = Mat("Crate", new Color(0.6f, 0.42f, 0.2f));

        Prim(PrimitiveType.Plane, "Ground", level, Vector3.zero, new Vector3(3.6f, 1f, 2.6f), ground);

        // Outer walls
        Prim(PrimitiveType.Cube, "Wall North", level, new Vector3(0f, 0.75f, 13f), new Vector3(36.5f, 1.5f, 0.5f), wall);
        Prim(PrimitiveType.Cube, "Wall South", level, new Vector3(0f, 0.75f, -13f), new Vector3(36.5f, 1.5f, 0.5f), wall);
        Prim(PrimitiveType.Cube, "Wall East", level, new Vector3(18f, 0.75f, 0f), new Vector3(0.5f, 1.5f, 26f), wall);
        Prim(PrimitiveType.Cube, "Wall West", level, new Vector3(-18f, 0.75f, 0f), new Vector3(0.5f, 1.5f, 26f), wall);

        // Statue room (north-west) and orb room (north-east)
        Prim(PrimitiveType.Cube, "Statue Room Wall", level, new Vector3(-12f, 1f, 2f), new Vector3(11f, 2f, 0.5f), wall);
        Prim(PrimitiveType.Cube, "Orb Room Wall", level, new Vector3(6f, 1f, 8.5f), new Vector3(0.5f, 2f, 8.5f), wall);

        // Obstacles
        Prim(PrimitiveType.Cube, "Crate A", level, new Vector3(0f, 0.5f, -3f), new Vector3(2f, 1f, 2f), crate);
        Prim(PrimitiveType.Cube, "Crate B", level, new Vector3(-3.5f, 0.5f, -2f), new Vector3(1.5f, 1f, 1.5f), crate);
        Prim(PrimitiveType.Cube, "Crate C", level, new Vector3(9f, 0.75f, -4f), new Vector3(1.5f, 1.5f, 1.5f), crate);
        Prim(PrimitiveType.Cube, "Crate D", level, new Vector3(-10f, 0.75f, -5f), new Vector3(1.5f, 1.5f, 3f), crate);
        Prim(PrimitiveType.Cylinder, "Pillar A", level, new Vector3(-5f, 1.5f, 7f), new Vector3(1f, 1.5f, 1f), wall);
        Prim(PrimitiveType.Cylinder, "Pillar B", level, new Vector3(2f, 1.5f, 9.5f), new Vector3(1f, 1.5f, 1f), wall);
    }

    // ---------- Player ----------

    static GameObject BuildPlayerPrefab()
    {
        var coat = Mat("PlayerCoat", Color.white);
        var skin = Mat("Skin", new Color(0.95f, 0.76f, 0.6f));
        var black = Mat("CameraBlack", new Color(0.08f, 0.08f, 0.08f));

        var player = new GameObject("Player");
        var t = player.transform;
        var capsule = player.AddComponent<CapsuleCollider>();
        capsule.height = 2f;
        capsule.radius = 0.5f;

        var body = Visual(PrimitiveType.Capsule, "Coat", t, new Vector3(0f, -0.3f, 0f), new Vector3(0.9f, 0.65f, 0.9f), coat);
        var armL = Visual(PrimitiveType.Capsule, "Arm Left", t, new Vector3(-0.45f, 0f, 0.3f), new Vector3(0.2f, 0.35f, 0.2f), coat, new Vector3(90f, 0f, 0f));
        var armR = Visual(PrimitiveType.Capsule, "Arm Right", t, new Vector3(0.45f, 0f, 0.3f), new Vector3(0.2f, 0.35f, 0.2f), coat, new Vector3(90f, 0f, 0f));
        Visual(PrimitiveType.Sphere, "Head", t, new Vector3(0f, 0.6f, 0f), Vector3.one * 0.55f, skin);
        Visual(PrimitiveType.Cube, "Nose", t, new Vector3(0f, 0.58f, 0.28f), Vector3.one * 0.1f, skin);
        Visual(PrimitiveType.Cylinder, "Hat Brim", t, new Vector3(0f, 0.85f, 0f), new Vector3(0.75f, 0.03f, 0.75f), black);
        Visual(PrimitiveType.Cylinder, "Hat Top", t, new Vector3(0f, 1f, 0f), new Vector3(0.45f, 0.15f, 0.45f), black);
        Visual(PrimitiveType.Cube, "Foot Left", t, new Vector3(-0.2f, -0.93f, 0.1f), new Vector3(0.25f, 0.15f, 0.4f), black);
        Visual(PrimitiveType.Cube, "Foot Right", t, new Vector3(0.2f, -0.93f, 0.1f), new Vector3(0.25f, 0.15f, 0.4f), black);
        var hold = Empty("HoldPoint", t, new Vector3(0f, 0f, 1f));

        player.AddComponent<Rigidbody>();
        var input = player.AddComponent<PlayerInput>();
        input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        input.defaultActionMap = "Player";
        var controller = player.AddComponent<PlayerController>();
        Set(controller, "holdPoint", hold);
        SetArray(controller, "tintRenderers", body.GetComponent<Renderer>(), armL.GetComponent<Renderer>(), armR.GetComponent<Renderer>());

        var prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        Object.DestroyImmediate(player);
        return prefab;
    }

    static void BuildPlayerManager(GameObject playerPrefab)
    {
        var players = new GameObject("Players");
        var manager = players.AddComponent<PlayerInputManager>();
        manager.playerPrefab = playerPrefab;
        var so = new SerializedObject(manager);
        so.FindProperty("m_MaxPlayerCount").intValue = 2;
        so.ApplyModifiedPropertiesWithoutUndo();

        var spawn1 = Empty("Spawn 1", players.transform, new Vector3(-3f, 1f, -10.5f));
        var spawn2 = Empty("Spawn 2", players.transform, new Vector3(3f, 1f, -10.5f));
        SetArray(players.AddComponent<PlayerSpawner>(), "spawnPoints", spawn1, spawn2);
    }

    // ---------- Big camera, flash, remote ----------

    static PhotoCapture BuildBigCamera(Vector3 position)
    {
        var wood = Mat("CameraWood", new Color(0.45f, 0.27f, 0.12f));
        var black = Mat("CameraBlack", new Color(0.08f, 0.08f, 0.08f));
        var brass = Mat("Brass", new Color(0.8f, 0.62f, 0.2f));
        var cloth = Mat("Cloth", new Color(0.25f, 0.05f, 0.08f));

        // Root sits at the top of the tripod; the box body is above it, the legs below.
        var cam = new GameObject("Big Camera");
        var t = cam.transform;
        t.position = position;

        Visual(PrimitiveType.Cube, "Body", t, new Vector3(0f, 0.6f, 0f), new Vector3(1.4f, 1.2f, 1.8f), wood);
        Visual(PrimitiveType.Cube, "Bellows", t, new Vector3(0f, 0.6f, 1.15f), new Vector3(1f, 0.9f, 0.5f), black);
        Visual(PrimitiveType.Cube, "Front Plate", t, new Vector3(0f, 0.6f, 1.45f), new Vector3(1.2f, 1.1f, 0.1f), wood);
        Visual(PrimitiveType.Cylinder, "Lens Barrel", t, new Vector3(0f, 0.6f, 1.65f), new Vector3(0.5f, 0.15f, 0.5f), brass, new Vector3(90f, 0f, 0f));
        Visual(PrimitiveType.Cube, "Cloth", t, new Vector3(0f, 0.75f, -1.05f), new Vector3(1.6f, 1.1f, 0.4f), cloth);
        Visual(PrimitiveType.Cylinder, "Tripod Head", t, new Vector3(0f, -0.05f, 0f), new Vector3(0.6f, 0.05f, 0.6f), brass);
        Visual(PrimitiveType.Cylinder, "Leg Front", t, new Vector3(0f, -0.7f, 0.35f), new Vector3(0.12f, 0.75f, 0.12f), wood, new Vector3(25f, 0f, 0f));
        Visual(PrimitiveType.Cylinder, "Leg Back Left", t, new Vector3(-0.3f, -0.7f, -0.2f), new Vector3(0.12f, 0.75f, 0.12f), wood, new Vector3(-15f, 0f, -22f));
        Visual(PrimitiveType.Cylinder, "Leg Back Right", t, new Vector3(0.3f, -0.7f, -0.2f), new Vector3(0.12f, 0.75f, 0.12f), wood, new Vector3(-15f, 0f, 22f));
        Visual(PrimitiveType.Cylinder, "Pole Left", t, new Vector3(-1.2f, 0.3f, 0f), new Vector3(0.12f, 0.5f, 0.12f), brass, new Vector3(0f, 0f, 90f));
        Visual(PrimitiveType.Cylinder, "Pole Right", t, new Vector3(1.2f, 0.3f, 0f), new Vector3(0.12f, 0.5f, 0.12f), brass, new Vector3(0f, 0f, 90f));
        var handleLeft = Empty("Handle Left", t, new Vector3(-1.7f, 0.3f, 0f));
        var handleRight = Empty("Handle Right", t, new Vector3(1.7f, 0.3f, 0f));

        var bodyCollider = cam.AddComponent<BoxCollider>();
        bodyCollider.center = new Vector3(0f, 0.6f, 0f);
        bodyCollider.size = new Vector3(1.4f, 1.2f, 1.8f);
        var tripodCollider = cam.AddComponent<BoxCollider>();
        tripodCollider.center = new Vector3(0f, -0.7f, 0f);
        tripodCollider.size = new Vector3(1.2f, 1.4f, 1.2f);

        var lens = Empty("Lens", t, new Vector3(0f, 0.6f, 1.8f)).gameObject.AddComponent<Camera>();
        lens.fieldOfView = 45f;
        lens.enabled = false;

        cam.AddComponent<Rigidbody>().mass = 40f;
        var heavy = cam.AddComponent<HeavyCamera>();
        Set(heavy, "leftHandle", handleLeft);
        Set(heavy, "rightHandle", handleRight);
        var capture = cam.AddComponent<PhotoCapture>();
        Set(capture, "lens", lens);
        return capture;
    }

    static void BuildFlash(Vector3 position)
    {
        var flash = new GameObject("Flash");
        flash.transform.position = position;
        Prim(PrimitiveType.Cube, "Body", flash.transform, Vector3.zero, new Vector3(0.4f, 0.6f, 0.4f), Mat("FlashYellow", new Color(1f, 0.85f, 0.2f)));
        Visual(PrimitiveType.Cylinder, "Reflector", flash.transform, new Vector3(0f, 0.2f, 0.22f), new Vector3(0.5f, 0.03f, 0.5f), Mat("PlayerCoat", Color.white), new Vector3(90f, 0f, 0f));
        var light = Empty("Light", flash.transform, new Vector3(0f, 0.2f, 0.3f)).gameObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.intensity = 60f;
        light.enabled = false;
        flash.AddComponent<Rigidbody>();
        Set(flash.AddComponent<FlashUnit>(), "flashLight", light);
    }

    static void BuildRemote(Vector3 position, PhotoCapture capture)
    {
        var remote = new GameObject("Remote Trigger");
        remote.transform.position = position;
        Prim(PrimitiveType.Cube, "Body", remote.transform, Vector3.zero, new Vector3(0.3f, 0.3f, 0.3f), Mat("RemoteGreen", new Color(0.2f, 0.8f, 0.3f)));
        Visual(PrimitiveType.Cylinder, "Button", remote.transform, new Vector3(0f, 0.17f, 0f), new Vector3(0.15f, 0.03f, 0.15f), Mat("Red", Color.red));
        remote.AddComponent<Rigidbody>();
        Set(remote.AddComponent<RemoteTrigger>(), "photoCamera", capture);
    }

    // ---------- Subjects ----------

    static PhotoSubject BuildStatue(Vector3 position)
    {
        var stone = Mat("Stone", new Color(0.8f, 0.8f, 0.78f));
        var statue = new GameObject("Statue");
        var t = statue.transform;
        t.position = position;
        var box = statue.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.6f, 0f);
        box.size = new Vector3(1.6f, 3.2f, 1.6f);

        Visual(PrimitiveType.Cube, "Pedestal", t, new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 0.8f, 1.6f), stone);
        Visual(PrimitiveType.Cylinder, "Torso", t, new Vector3(0f, 1.6f, 0f), new Vector3(0.8f, 0.8f, 0.8f), stone);
        Visual(PrimitiveType.Sphere, "Head", t, new Vector3(0f, 2.75f, 0f), Vector3.one * 0.7f, stone);
        Visual(PrimitiveType.Capsule, "Arm Raised", t, new Vector3(0.6f, 2.4f, 0f), new Vector3(0.25f, 0.5f, 0.25f), stone, new Vector3(0f, 0f, -35f));
        Visual(PrimitiveType.Capsule, "Arm Lowered", t, new Vector3(-0.55f, 1.7f, 0f), new Vector3(0.25f, 0.5f, 0.25f), stone, new Vector3(0f, 0f, -15f));
        return AddSubject(statue, "the Statue");
    }

    static PhotoSubject BuildOrb(Vector3 position)
    {
        var orb = new GameObject("Orb");
        var t = orb.transform;
        t.position = position;
        var sphere = orb.AddComponent<SphereCollider>();
        sphere.center = new Vector3(0f, 1.9f, 0f);
        sphere.radius = 1f;

        Prim(PrimitiveType.Cylinder, "Pedestal", t, new Vector3(0f, 0.4f, 0f), new Vector3(1.2f, 0.4f, 1.2f), Mat("Stone", new Color(0.8f, 0.8f, 0.78f)));
        Visual(PrimitiveType.Sphere, "Sphere", t, new Vector3(0f, 1.9f, 0f), Vector3.one * 2f, Mat("SubjectBlue", new Color(0.3f, 0.4f, 0.9f)));
        Visual(PrimitiveType.Cylinder, "Ring", t, new Vector3(0f, 1.9f, 0f), new Vector3(2.7f, 0.03f, 2.7f), Mat("Brass", new Color(0.8f, 0.62f, 0.2f)), new Vector3(20f, 0f, 20f));
        return AddSubject(orb, "the Orb");
    }

    static PhotoSubject BuildStroller(Vector3 position)
    {
        var suit = Mat("StrollerSuit", new Color(0.5f, 0.2f, 0.6f));
        var skin = Mat("Skin", new Color(0.95f, 0.76f, 0.6f));
        var black = Mat("CameraBlack", new Color(0.08f, 0.08f, 0.08f));

        var stroller = new GameObject("Stroller");
        var t = stroller.transform;
        t.position = position;
        var capsule = stroller.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1f, 0f);
        capsule.height = 2f;
        capsule.radius = 0.5f;
        stroller.AddComponent<Rigidbody>().isKinematic = true;

        Visual(PrimitiveType.Capsule, "Body", t, new Vector3(0f, 0.75f, 0f), new Vector3(0.9f, 0.75f, 0.9f), suit);
        Visual(PrimitiveType.Sphere, "Head", t, new Vector3(0f, 1.75f, 0f), Vector3.one * 0.55f, skin);
        Visual(PrimitiveType.Cube, "Nose", t, new Vector3(0f, 1.73f, 0.28f), Vector3.one * 0.1f, skin);
        Visual(PrimitiveType.Cylinder, "Hat Brim", t, new Vector3(0f, 2f, 0f), new Vector3(0.7f, 0.03f, 0.7f), black);
        Visual(PrimitiveType.Cylinder, "Top Hat", t, new Vector3(0f, 2.3f, 0f), new Vector3(0.42f, 0.3f, 0.42f), black);
        Visual(PrimitiveType.Cylinder, "Cane", t, new Vector3(0.5f, 0.6f, 0.3f), new Vector3(0.06f, 0.6f, 0.06f), black);

        var so = new SerializedObject(stroller.AddComponent<PatrolMover>());
        so.FindProperty("travel").vector3Value = new Vector3(8f, 0f, 0f);
        so.ApplyModifiedPropertiesWithoutUndo();
        return AddSubject(stroller, "the Stroller");
    }

    static PhotoSubject AddSubject(GameObject go, string displayName)
    {
        var subject = go.AddComponent<PhotoSubject>();
        var so = new SerializedObject(subject);
        so.FindProperty("displayName").stringValue = displayName;
        so.ApplyModifiedPropertiesWithoutUndo();
        return subject;
    }

    // ---------- HUD ----------

    static void BuildHud(HeavyCamera heavyCamera)
    {
        var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.transform;
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Throw strength circle (first child, so it draws behind the texts)
        var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        var circle = UiImage("Throw Circle", canvas, knob, new Color(0f, 0f, 0f, 0.6f), new Vector2(90f, 90f));
        var fill = UiImage("Fill", circle.transform, knob, Color.yellow, new Vector2(70f, 70f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        fill.fillAmount = 0.5f;
        circle.gameObject.SetActive(false);
        var indicator = canvasGo.AddComponent<ThrowIndicator>();
        Set(indicator, "target", heavyCamera);
        Set(indicator, "circle", circle.gameObject);
        Set(indicator, "fill", fill);

        var task = UiText("Task", canvas, 44, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -100f), new Vector2(-30f, -20f));
        var message = UiText("Message", canvas, 110, TextAnchor.MiddleCenter, new Vector2(0f, 0.6f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero);
        var help = UiText("Controls", canvas, 24, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0.6f, 0f), new Vector2(30f, 20f), new Vector2(0f, 120f));
        help.text = "Move: WASD / left stick\nGrab (hold): Space / gamepad north\nUse item, or hold together to charge a throw: Enter / gamepad west";

        var panel = UiImage("Photo Panel", canvas, null, Color.white, new Vector2(680f, 580f));
        var panelRect = panel.rectTransform;
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-30f, 30f);
        var photoGo = new GameObject("Photo", typeof(RectTransform), typeof(RawImage));
        photoGo.transform.SetParent(panel.transform, false);
        var photoRect = photoGo.GetComponent<RectTransform>();
        photoRect.anchorMin = photoRect.anchorMax = photoRect.pivot = new Vector2(0.5f, 1f);
        photoRect.anchoredPosition = new Vector2(0f, -20f);
        photoRect.sizeDelta = new Vector2(640f, 480f);
        var verdict = UiText("Verdict", panel.transform, 30, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 10f), new Vector2(-10f, 70f));
        verdict.color = Color.black;
        Object.DestroyImmediate(verdict.GetComponent<Outline>());

        var hud = canvasGo.AddComponent<PhotoHUD>();
        Set(hud, "taskText", task);
        Set(hud, "messageText", message);
        Set(hud, "photoPanel", panel.gameObject);
        Set(hud, "photoImage", photoGo.GetComponent<RawImage>());
        Set(hud, "verdictText", verdict);
    }

    static Image UiImage(string name, Transform parent, Sprite sprite, Color color, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = size;
        return image;
    }

    static Text UiText(string name, Transform parent, int size, TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        go.AddComponent<Outline>().effectColor = Color.black;
        var rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return text;
    }

    // ---------- Helpers ----------

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    static Material Mat(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(GraphicsSettings.currentRenderPipeline.defaultShader);
        mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    /// <summary>Primitive that keeps its collider.</summary>
    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    /// <summary>Primitive without collider, only for looks.</summary>
    static GameObject Visual(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Vector3 euler = default)
    {
        var go = Prim(type, name, parent, localPosition, scale, material);
        go.transform.localRotation = Quaternion.Euler(euler);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static Transform Empty(string name, Transform parent, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    static void Set(Object target, string property, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetArray(Object target, string property, params Object[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(property);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
