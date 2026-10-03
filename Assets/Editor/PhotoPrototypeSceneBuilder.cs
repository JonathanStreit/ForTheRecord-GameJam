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
    const string ControlsPrefabPath = "Assets/Prefabs/Controls.prefab";
    static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.1f, 0.8f);

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
        BuildFlash(new Vector3(-5f, 1f, -9f));
        BuildRemote(new Vector3(5f, 0f, -9f), capture);

        var stone = Mat("Stone", new Color(0.8f, 0.8f, 0.78f));
        var gold = Mat("Gold", new Color(1f, 0.78f, 0.15f));
        gold.SetFloat("_Metallic", 0.9f);
        gold.SetFloat("_Smoothness", 0.8f);
        var statue = BuildStatue("Statue", "the Statue", new Vector3(-14f, 0f, -1f), stone, 100);
        var orb = BuildOrb(new Vector3(14f, 0f, -1f), 100);
        // The only subject behind the fence: the camera has to be thrown over for this one.
        var goldenStatue = BuildStatue("Golden Statue", "the Golden Statue", new Vector3(0f, 0f, 10f), gold, 500);
        var visitor = BuildVisitor(new Vector3(-6f, 0f, -0.5f), new Vector3(12f, 0f, 0f), 300);

        // Game camera follows the players and keeps the big camera in view.
        var coopCamera = Camera.main.gameObject.AddComponent<CoopCamera>();
        var cameraSo = new SerializedObject(coopCamera);
        cameraSo.FindProperty("defaultFocus").vector3Value = new Vector3(0f, 0f, -8f);
        cameraSo.ApplyModifiedPropertiesWithoutUndo();
        SetArray(coopCamera, "extraTargets", capture.transform);

        BuildHud();
        var manager = new GameObject("Game Manager").AddComponent<PhotoGameManager>();
        SetArray(manager, "tasks", statue, orb, visitor, goldenStatue);

        AssetDatabase.SaveAssets();
        AssignSoundsAndPrompts();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
    }

    // ---------- Pieces that can also be applied to an existing scene / prefab ----------

    /// <summary>Adds the walk animation and the footstep sound to a player object (expects the builder's child names).</summary>
    public static void SetupPlayerAnimation(GameObject player)
    {
        var t = player.transform;
        var animator = player.GetComponent<PlayerAnimator>();
        if (animator == null) animator = player.AddComponent<PlayerAnimator>();
        Set(animator, "footLeft", t.Find("Foot Left"));
        Set(animator, "footRight", t.Find("Foot Right"));
        Set(animator, "armLeft", t.Find("Arm Left"));
        Set(animator, "armRight", t.Find("Arm Right"));
        Set(animator, "body", t.Find("Coat"));

        var existing = t.Find("Footsteps");
        var source = existing != null ? existing.GetComponent<AudioSource>() : Empty("Footsteps", t, Vector3.zero).gameObject.AddComponent<AudioSource>();
        source.clip = Clip("Players_Footsteps.mp3");
        source.loop = true;
        source.playOnAwake = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        Set(animator, "footsteps", source);
    }

    /// <summary>Adds the item pick-up prompt panel to the HUD canvas and hooks it up.</summary>
    public static void BuildPickupPrompt(Transform canvas, PhotoHUD hud)
    {
        var gold = new Color(1f, 0.85f, 0.3f);
        var panel = UiImage("Item Prompt", canvas, null, PanelColor, new Vector2(640f, 150f));
        Anchor(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(-150f, 40f));
        var title = UiText("Title", panel.transform, 28, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, -10f));
        title.color = gold;
        var buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/WestButton.png");
        if (buttonSprite != null)
        {
            var icon = UiImage("Button Icon", panel.transform, buttonSprite, Color.white, new Vector2(70f, 70f));
            icon.preserveAspect = true;
            Anchor(icon.rectTransform, Vector2.zero, new Vector2(20f, 18f));
        }
        var text = UiText("Text", panel.transform, 24, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(110f, 10f), new Vector2(-20f, -50f));
        text.lineSpacing = 1.2f;

        Set(hud, "promptPanel", panel.gameObject);
        Set(hud, "promptTitle", title);
        Set(hud, "promptText", text);
    }

    /// <summary>Assigns the clips from Assets/Sounds and the pick-up prompt texts to the objects in the open scene.</summary>
    public static void AssignSoundsAndPrompts()
    {
        Set(Object.FindAnyObjectByType<PhotoCapture>(), "shutterSound", Clip("Camera_Shutter_Sound.wav"));
        Set(Object.FindAnyObjectByType<HeavyCamera>(), "impactSound", Clip("Dropping_Camera_Sound.wav"));
        var manager = Object.FindAnyObjectByType<PhotoGameManager>();
        Set(manager, "goodPhotoSound", Clip("Good_Photo_Sound.wav"));
        Set(manager, "badPhotoSound", Clip("Bad_Photo_Sound.mp3"));

        SetPrompt(Object.FindAnyObjectByType<FlashUnit>(), "FLASH",
            "Tap: 3-2-1 countdown, then it lights the subject for 2 seconds\nHold, then release: throw it");
        SetPrompt(Object.FindAnyObjectByType<RemoteTrigger>(), "REMOTE TRIGGER",
            "Tap: take the photo (stay close to the camera)\nHold, then release: throw it");
    }

    static void SetPrompt(CarryItem item, string title, string text)
    {
        var so = new SerializedObject(item);
        so.FindProperty("promptTitle").stringValue = title;
        so.FindProperty("promptText").stringValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static AudioClip Clip(string fileName) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/" + fileName);

    // The round restarts by reloading the scene, which only works for scenes in the build list.
    static void AddSceneToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == ScenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ---------- Level ----------

    static void BuildEnvironment()
    {
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

        // Invisible walls above the outer walls, plus a ceiling, so nothing can be thrown out of the level
        const float barrierHeight = 20f;
        InvisibleWall("Barrier North", level, new Vector3(0f, barrierHeight * 0.5f, 13.5f), new Vector3(38f, barrierHeight, 1f));
        InvisibleWall("Barrier South", level, new Vector3(0f, barrierHeight * 0.5f, -13.5f), new Vector3(38f, barrierHeight, 1f));
        InvisibleWall("Barrier East", level, new Vector3(18.5f, barrierHeight * 0.5f, 0f), new Vector3(1f, barrierHeight, 28f));
        InvisibleWall("Barrier West", level, new Vector3(-18.5f, barrierHeight * 0.5f, 0f), new Vector3(1f, barrierHeight, 28f));
        InvisibleWall("Barrier Ceiling", level, new Vector3(0f, barrierHeight + 0.5f, 0f), new Vector3(38f, 1f, 28f));

        // Fence across the level: the north strip can only be entered through the player-only gates,
        // so the big camera has to be thrown over the fence.
        var fence = Mat("Fence", new Color(0.55f, 0.12f, 0.12f));
        var gate = Mat("Gate", new Color(0.3f, 0.75f, 0.35f));
        const float fenceZ = 4f, fenceHeight = 1.2f, gateWidth = 1.6f;
        float[] gateXs = { -9f, 9f };
        float segmentStart = -17.75f;
        for (int i = 0; i <= gateXs.Length; i++)
        {
            float segmentEnd = i < gateXs.Length ? gateXs[i] - gateWidth * 0.5f : 17.75f;
            Prim(PrimitiveType.Cube, "Fence " + (i + 1), level, new Vector3((segmentStart + segmentEnd) * 0.5f, fenceHeight * 0.5f, fenceZ),
                new Vector3(segmentEnd - segmentStart, fenceHeight, 0.3f), fence);
            if (i == gateXs.Length) break;

            var gateRoot = Empty("Player Gate " + (i + 1), level, new Vector3(gateXs[i], 0f, fenceZ));
            Visual(PrimitiveType.Cube, "Floor Mark", gateRoot, new Vector3(0f, 0.02f, 0f), new Vector3(gateWidth, 0.04f, 2f), gate);
            Visual(PrimitiveType.Cylinder, "Post Left", gateRoot, new Vector3(-gateWidth * 0.5f, 1f, 0f), new Vector3(0.25f, 1f, 0.25f), gate);
            Visual(PrimitiveType.Cylinder, "Post Right", gateRoot, new Vector3(gateWidth * 0.5f, 1f, 0f), new Vector3(0.25f, 1f, 0.25f), gate);
            var blocker = gateRoot.gameObject.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0f, fenceHeight * 0.5f, 0f);
            blocker.size = new Vector3(gateWidth, fenceHeight, 0.3f);
            gateRoot.gameObject.AddComponent<PlayerOnlyGate>();
            segmentStart = gateXs[i] + gateWidth * 0.5f;
        }


        // Obstacles
        Prim(PrimitiveType.Cube, "Crate A", level, new Vector3(0f, 0.5f, -3f), new Vector3(2f, 1f, 2f), crate);
        Prim(PrimitiveType.Cube, "Crate B", level, new Vector3(-3.5f, 0.5f, -2f), new Vector3(1.5f, 1f, 1.5f), crate);
        Prim(PrimitiveType.Cube, "Crate C", level, new Vector3(9f, 0.75f, -4f), new Vector3(1.5f, 1.5f, 1.5f), crate);
        Prim(PrimitiveType.Cube, "Crate D", level, new Vector3(-10f, 0.75f, -5f), new Vector3(1.5f, 1.5f, 3f), crate);
        Prim(PrimitiveType.Cylinder, "Pillar A", level, new Vector3(-5f, 1.5f, 10f), new Vector3(1f, 1.5f, 1f), wall);
        Prim(PrimitiveType.Cylinder, "Pillar B", level, new Vector3(5f, 1.5f, 10f), new Vector3(1f, 1.5f, 1f), wall);
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
        Set(controller, "stepDust", BuildStepDust(t));

        // Lets the other player pick this one up (held above the head) and throw them.
        var carryable = new SerializedObject(player.AddComponent<PlayerCarryable>());
        carryable.FindProperty("grabRadius").floatValue = 1.5f;
        carryable.FindProperty("holdOffset").vector3Value = new Vector3(0f, 1.7f, -1f);
        carryable.FindProperty("maxThrowSpeed").floatValue = 9f;
        carryable.ApplyModifiedPropertiesWithoutUndo();

        SetupPlayerAnimation(player);

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
        Visual(PrimitiveType.Cylinder, "Lens Barrel", t, new Vector3(0f, 0.6f, 1.65f), new Vector3(0.6f, 0.15f, 0.6f), black, new Vector3(90f, 0f, 0f));
        Visual(PrimitiveType.Sphere, "Lens Glass", t, new Vector3(0f, 0.6f, 1.8f), new Vector3(0.5f, 0.5f, 0.2f), black);
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

        // Wedge on the ground showing what the lens can see.
        var cone = new GameObject("Vision Cone", typeof(MeshFilter), typeof(MeshRenderer));
        cone.transform.SetParent(t, false);
        var coneRenderer = cone.GetComponent<MeshRenderer>();
        coneRenderer.sharedMaterial = TransparentMat("VisionCone", new Color(1f, 1f, 1f, 0.25f));
        coneRenderer.shadowCastingMode = ShadowCastingMode.Off;
        // Keep the wedge out of the photos: it lives on the TransparentFX layer, which the lens skips.
        cone.layer = LayerMask.NameToLayer("TransparentFX");
        lens.cullingMask = ~(1 << cone.layer);
        Set(cone.AddComponent<VisionCone>(), "photoCamera", capture);

        // Ring on the ground showing how close a player has to stand to grab the camera.
        var heavySo = new SerializedObject(heavy);
        heavySo.FindProperty("grabRadius").floatValue = 3f;
        heavySo.ApplyModifiedPropertiesWithoutUndo();
        // Its halves fill with the colours of the players holding the handles and it turns red before a drop.
        var ring = new GameObject("Grab Ring");
        ring.transform.SetParent(t, false);
        ring.layer = cone.layer;
        var cameraRing = ring.AddComponent<CameraGrabRing>();
        Set(cameraRing, "target", heavy);
        Set(cameraRing, "material", TransparentMat("GrabRing", new Color(1f, 1f, 1f, 0.6f)));

        // Dust ring that plays where the dropped or thrown camera lands.
        Set(heavy, "landingShockwave", BuildShockwave());
        return capture;
    }

    static void AddGrabRing(Grabbable target)
    {
        var ring = new GameObject("Grab Range Ring", typeof(MeshFilter), typeof(MeshRenderer));
        ring.transform.SetParent(target.transform, false);
        ring.layer = LayerMask.NameToLayer("TransparentFX");
        var ringRenderer = ring.GetComponent<MeshRenderer>();
        ringRenderer.sharedMaterial = TransparentMat("GrabRing", new Color(1f, 1f, 1f, 0.6f));
        ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
        Set(ring.AddComponent<GrabRangeRing>(), "target", target);
    }

    // ---------- Particles ----------

    static ParticleSystem BuildStepDust(Transform player)
    {
        var dust = NewDustSystem("Step Dust", player, new Vector3(0f, -0.9f, -0.2f));
        var main = dust.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.gravityModifier = -0.03f;
        var emission = dust.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 2f;
        var shape = dust.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;
        return dust;
    }

    static ParticleSystem BuildShockwave()
    {
        var shockwave = NewDustSystem("Camera Shockwave", null, Vector3.zero);
        var main = shockwave.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        var emission = shockwave.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
        // Flat circle on the ground, particles fly outwards from its edge and slow down quickly.
        var shape = shockwave.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.8f;
        shape.radiusThickness = 0f;
        shape.rotation = new Vector3(90f, 0f, 0f);
        var limit = shockwave.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.drag = 4f;
        return shockwave;
    }

    /// <summary>Particle system with the shared dust look: soft beige puffs that grow and fade out.</summary>
    static ParticleSystem NewDustSystem(string name, Transform parent, Vector3 localPosition)
    {
        var system = Empty(name, parent, localPosition).gameObject.AddComponent<ParticleSystem>();
        var main = system.main;
        main.startColor = new Color(0.85f, 0.82f, 0.75f, 0.6f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.2f));
        var color = system.colorOverLifetime;
        color.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;

        var particleRenderer = system.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
        particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
        return system;
    }

    static void BuildFlash(Vector3 position)
    {
        var flash = new GameObject("Flash");
        flash.transform.position = position;
        var t = flash.transform;
        var black = Mat("CameraBlack", new Color(0.08f, 0.08f, 0.08f));
        var white = Mat("SoftboxWhite", Color.white);

        // Studio softbox: tripod stand, thin pole and a black box with a big white front panel.
        // Root is at the middle of the pole (1 m above the feet), where the player holds it.
        Visual(PrimitiveType.Cylinder, "Leg Front", t, new Vector3(0f, -0.7f, 0.25f), new Vector3(0.05f, 0.4f, 0.05f), black, new Vector3(-40f, 0f, 0f));
        Visual(PrimitiveType.Cylinder, "Leg Back Right", t, new Vector3(0.217f, -0.7f, -0.125f), new Vector3(0.05f, 0.4f, 0.05f), black, new Vector3(-40f, 120f, 0f));
        Visual(PrimitiveType.Cylinder, "Leg Back Left", t, new Vector3(-0.217f, -0.7f, -0.125f), new Vector3(0.05f, 0.4f, 0.05f), black, new Vector3(-40f, 240f, 0f));
        Visual(PrimitiveType.Cylinder, "Pole", t, new Vector3(0f, 0.2f, 0f), new Vector3(0.06f, 0.65f, 0.06f), black);
        Visual(PrimitiveType.Cylinder, "Pole Clamp", t, new Vector3(0f, 0f, 0f), new Vector3(0.1f, 0.05f, 0.1f), black);
        Visual(PrimitiveType.Cube, "Lamp Mount", t, new Vector3(0f, 1.1f, -0.15f), new Vector3(0.35f, 0.35f, 0.3f), black);
        Visual(PrimitiveType.Cube, "Softbox", t, new Vector3(0f, 1.1f, 0.15f), new Vector3(0.95f, 1.3f, 0.4f), black);
        Visual(PrimitiveType.Cube, "Diffuser", t, new Vector3(0f, 1.1f, 0.36f), new Vector3(0.88f, 1.23f, 0.04f), white);
        var softboxCollider = flash.AddComponent<BoxCollider>();
        softboxCollider.center = new Vector3(0f, 1.1f, 0.1f);
        softboxCollider.size = new Vector3(0.95f, 1.3f, 0.6f);
        var standCollider = flash.AddComponent<BoxCollider>();
        standCollider.center = new Vector3(0f, -0.4f, 0f);
        standCollider.size = new Vector3(1f, 1.2f, 1f);

        var light = Empty("Light", t, new Vector3(0f, 1.1f, 0.45f)).gameObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.intensity = 60f;
        light.enabled = false;
        flash.AddComponent<Rigidbody>();
        var flashUnit = flash.AddComponent<FlashUnit>();
        Set(flashUnit, "flashLight", light);
        AddGrabRing(flashUnit);
    }

    static void BuildRemote(Vector3 position, PhotoCapture capture)
    {
        var remote = new GameObject("Remote Trigger");
        remote.transform.position = position;
        var t = remote.transform;
        var black = Mat("CameraBlack", new Color(0.08f, 0.08f, 0.08f));
        var red = Mat("Red", Color.red);

        // Chunky hand-held box with one big red button on top and an antenna. Root is at the bottom.
        Visual(PrimitiveType.Cube, "Body", t, new Vector3(0f, 0.12f, 0f), new Vector3(0.55f, 0.24f, 0.8f), Mat("RemoteGreen", new Color(0.2f, 0.8f, 0.3f)));
        Visual(PrimitiveType.Cylinder, "Button Base", t, new Vector3(0f, 0.26f, -0.1f), new Vector3(0.5f, 0.02f, 0.5f), black);
        Visual(PrimitiveType.Cylinder, "Button", t, new Vector3(0f, 0.32f, -0.1f), new Vector3(0.42f, 0.05f, 0.42f), red);
        Visual(PrimitiveType.Sphere, "Button Dome", t, new Vector3(0f, 0.36f, -0.1f), new Vector3(0.42f, 0.2f, 0.42f), red);
        Visual(PrimitiveType.Cylinder, "Antenna", t, new Vector3(0.18f, 0.5f, 0.32f), new Vector3(0.04f, 0.3f, 0.04f), black);
        Visual(PrimitiveType.Sphere, "Antenna Tip", t, new Vector3(0.18f, 0.82f, 0.32f), Vector3.one * 0.1f, red);
        var box = remote.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.2f, 0f);
        box.size = new Vector3(0.55f, 0.4f, 0.8f);
        remote.AddComponent<Rigidbody>();
        var trigger = remote.AddComponent<RemoteTrigger>();
        Set(trigger, "photoCamera", capture);
        AddGrabRing(trigger);
    }

    // ---------- Subjects ----------

    static PhotoSubject BuildStatue(string name, string displayName, Vector3 position, Material stone, int points)
    {
        var statue = new GameObject(name);
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
        return AddSubject(statue, displayName, points);
    }

    static PhotoSubject BuildOrb(Vector3 position, int points)
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
        return AddSubject(orb, "the Orb", points);
    }

    static PhotoSubject BuildVisitor(Vector3 position, Vector3 travel, int points)
    {
        var suit = Mat("VisitorSuit", new Color(0.5f, 0.2f, 0.6f));
        var skin = Mat("Skin", new Color(0.95f, 0.76f, 0.6f));
        var black = Mat("CameraBlack", new Color(0.08f, 0.08f, 0.08f));

        var stroller = new GameObject("Visitor");
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
        so.FindProperty("travel").vector3Value = travel;
        so.ApplyModifiedPropertiesWithoutUndo();
        return AddSubject(stroller, "the Visitor", points);
    }

    static PhotoSubject AddSubject(GameObject go, string displayName, int points)
    {
        var subject = go.AddComponent<PhotoSubject>();
        var so = new SerializedObject(subject);
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("points").intValue = points;
        so.ApplyModifiedPropertiesWithoutUndo();
        return subject;
    }

    // ---------- HUD ----------

    static void BuildHud()
    {
        var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.transform;
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        var dark = PanelColor;

        // One throw power circle per player (first children, so they draw behind everything else)
        for (int side = 0; side < 2; side++)
        {
            var circle = UiImage("Throw Circle Player " + (side + 1), canvas, knob, dark, new Vector2(70f, 70f));
            var fill = RadialFill("Fill", circle.transform, knob, 54f);
            circle.gameObject.SetActive(false);
            var indicator = canvasGo.AddComponent<ThrowIndicator>();
            Set(indicator, "circle", circle.gameObject);
            Set(indicator, "fill", fill);
            var indicatorSo = new SerializedObject(indicator);
            indicatorSo.FindProperty("playerIndex").intValue = side;
            indicatorSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // Assignment list, top left
        var taskPanel = UiImage("Assignments", canvas, null, dark, new Vector2(560f, 280f));
        Anchor(taskPanel.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -30f));
        var taskTitle = UiText("Title", taskPanel.transform, 26, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, -12f));
        taskTitle.text = "ASSIGNMENTS";
        taskTitle.color = new Color(1f, 0.85f, 0.3f);
        var task = UiText("List", taskPanel.transform, 34, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, -55f));
        task.supportRichText = true;
        task.lineSpacing = 1.2f;

        // Round timer, top centre: a ring that empties and changes colour
        var timer = UiImage("Timer", canvas, knob, dark, new Vector2(170f, 170f));
        Anchor(timer.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f));
        timer.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        var timerFill = RadialFill("Ring", timer.transform, knob, 150f);
        UiImage("Center", timer.transform, knob, new Color(0.08f, 0.08f, 0.1f, 1f), new Vector2(112f, 112f));
        var timerText = UiText("Time", timer.transform, 44, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Score, top right
        var scorePanel = UiImage("Score", canvas, null, dark, new Vector2(300f, 150f));
        Anchor(scorePanel.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -30f));
        var scoreTitle = UiText("Title", scorePanel.transform, 26, TextAnchor.UpperCenter, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -12f));
        scoreTitle.text = "POINTS";
        scoreTitle.color = new Color(1f, 0.85f, 0.3f);
        var score = UiText("Value", scorePanel.transform, 80, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(0f, -30f));

        var message = UiText("Message", canvas, 100, TextAnchor.MiddleCenter, new Vector2(0f, 0.45f), new Vector2(1f, 0.8f), Vector2.zero, Vector2.zero);
        // The controls help is a hand-made prefab; it is only placed here, never generated or changed.
        var controlsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ControlsPrefabPath);
        if (controlsPrefab != null) PrefabUtility.InstantiatePrefab(controlsPrefab, canvas);

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
        Set(hud, "scoreText", score);
        Set(hud, "timerRoot", timer.rectTransform);
        Set(hud, "timerFill", timerFill);
        Set(hud, "timerText", timerText);
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.9f, 0.15f, 0.1f), 0f), new GradientColorKey(new Color(1f, 0.8f, 0.1f), 0.4f), new GradientColorKey(new Color(0.3f, 0.85f, 0.3f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        var hudSo = new SerializedObject(hud);
        hudSo.FindProperty("timerColors").gradientValue = gradient;
        hudSo.ApplyModifiedPropertiesWithoutUndo();
        Set(hud, "messageText", message);
        Set(hud, "photoPanel", panel.gameObject);
        Set(hud, "photoImage", photoGo.GetComponent<RawImage>());
        Set(hud, "verdictText", verdict);
        BuildPickupPrompt(canvas, hud);
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

    static Image RadialFill(string name, Transform parent, Sprite sprite, float size)
    {
        var fill = UiImage(name, parent, sprite, Color.yellow, new Vector2(size, size));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        return fill;
    }

    /// <summary>Pins a rect to a screen corner/edge, using the same point as anchor and pivot.</summary>
    static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
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

    static Material TransparentMat(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
        {
            mat.SetColor("_BaseColor", color);
            return mat;
        }
        mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_Cull", (float)CullMode.Off);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
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

    static void InvisibleWall(string name, Transform parent, Vector3 position, Vector3 size)
    {
        var wall = Empty(name, parent, position);
        wall.gameObject.AddComponent<BoxCollider>().size = size;
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
