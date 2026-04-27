using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class EchoArchitectSceneBuilder
{
    const string MainScenePath = "Assets/Main.unity";
    const string MonsterModelPath = "Assets/Stylized3DMonster/Monster01/Prefab/Monster01_03d.prefab";
    const string MonsterAvatarSourcePath = "Assets/Stylized3DMonster/Monster01/Monster01_AllAnim.fbx";
    const string MonsterControllerPath = "Assets/Stylized3DMonster/Monster01/Anim/InPlace_Anim/Monster01_AC_InPlace.controller";

    [MenuItem("Tools/Echo Architect/Rebuild Main Scene")]
    public static void RebuildMainScene()
    {
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
        if (sceneAsset == null)
        {
            Debug.LogError("Main scene could not be found at " + MainScenePath);
            return;
        }

        EditorSceneManager.OpenScene(MainScenePath);

        GameObject player = EnsurePlayer();
        GameObject monster = EnsureMonster();
        GameObject audioManager = EnsureAudioManager();
        Light light = EnsureDirectionalLight();

        ClearGeneratedContent();
        ConfigureLighting(light);
        BuildTown(player, monster, audioManager);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainScenePath, true)
        };

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Debug.Log("Echo Architect main scene rebuilt successfully.");
    }

    static void ClearGeneratedContent()
    {
        string[] keepNames =
        {
            "Player",
            "Monster",
            "AudioManager",
            "Directional Light"
        };

        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            bool keep = false;
            for (int i = 0; i < keepNames.Length; i++)
            {
                if (root.name == keepNames[i])
                {
                    keep = true;
                    break;
                }
            }

            if (!keep)
                Object.DestroyImmediate(root);
        }
    }

    static GameObject EnsurePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);

        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1.2f, -34f);
        player.transform.rotation = Quaternion.identity;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
            controller = player.AddComponent<CharacterController>();

        controller.height = 1.9f;
        controller.radius = 0.34f;
        controller.center = new Vector3(0f, 0.92f, 0f);

        PlayerMove move = player.GetComponent<PlayerMove>();
        if (move == null)
            move = player.AddComponent<PlayerMove>();

        move.walkSpeed = 4.5f;
        move.sprintSpeed = 6.9f;
        move.crouchSpeed = 2.7f;
        move.gravity = -26f;
        move.jumpHeight = 1.4f;
        move.groundMask = LayerMask.GetMask("Default");

        VoiceVisibility visibility = player.GetComponent<VoiceVisibility>();
        if (visibility == null)
            visibility = player.AddComponent<VoiceVisibility>();

        visibility.playerRenderer = player.GetComponent<Renderer>();

        Transform groundCheck = player.transform.Find("GroundCheck");
        if (groundCheck == null)
        {
            GameObject groundCheckObject = new GameObject("GroundCheck");
            groundCheck = groundCheckObject.transform;
            groundCheck.SetParent(player.transform);
        }

        groundCheck.localPosition = new Vector3(0f, -0.88f, 0f);
        move.groundCheck = groundCheck;

        Camera camera = player.GetComponentInChildren<Camera>();
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform);
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        camera.transform.localRotation = Quaternion.identity;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
        camera.farClipPlane = 120f;

        MouseLook look = camera.GetComponent<MouseLook>();
        if (look == null)
            look = camera.gameObject.AddComponent<MouseLook>();

        look.playerBody = player.transform;
        look.mouseSensitivity = 115f;

        Renderer playerRenderer = player.GetComponent<Renderer>();
        if (playerRenderer != null)
            playerRenderer.enabled = false;

        return player;
    }

    static GameObject EnsureMonster()
    {
        GameObject existingMonster = GameObject.Find("Monster");
        if (existingMonster != null)
            Object.DestroyImmediate(existingMonster);

        GameObject monster = SpawnPrefab(
            MonsterModelPath,
            "Monster",
            new Vector3(0f, 0f, 34f),
            Quaternion.Euler(0f, 180f, 0f),
            null);

        if (monster == null)
            monster = GameObject.CreatePrimitive(PrimitiveType.Capsule);

        monster.name = "Monster";
        monster.transform.position = new Vector3(0f, 0.12f, 34f);
        monster.transform.localScale = new Vector3(1.85f, 1.85f, 1.85f);
        ConfigureMonsterAnimator(monster);
        ConvertMaterialsToHdrp(monster, false);

        MonsterAI ai = monster.GetComponent<MonsterAI>();
        if (ai == null)
            ai = monster.AddComponent<MonsterAI>();

        ai.patrolSpeed = 2.25f;
        ai.chaseSpeed = 4.9f;
        ai.hearingSlack = 6f;
        ai.memoryDuration = 5.5f;
        ai.catchDistance = 1.5f;
        ai.directAwarenessDistance = 7.5f;
        ai.idleState = "Monster01_Idle";
        ai.chaseState = "Monster01_Run_InPlace";
        ai.attackState = "Monster01_Attack01_InPlace";

        CapsuleCollider collider = monster.GetComponent<CapsuleCollider>();
        if (collider == null)
            collider = monster.AddComponent<CapsuleCollider>();

        collider.center = new Vector3(0f, 0.95f, 0f);
        collider.height = 2.2f;
        collider.radius = 0.45f;

        return monster;
    }

    static void ConfigureMonsterAnimator(GameObject monster)
    {
        Animator animator = monster.GetComponent<Animator>();
        if (animator == null)
            animator = monster.AddComponent<Animator>();

        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MonsterControllerPath);
        if (controller != null)
            animator.runtimeAnimatorController = controller;

        Avatar avatar = FindAvatarAsset(MonsterAvatarSourcePath);
        if (avatar != null)
            animator.avatar = avatar;
        else
            Debug.LogWarning("Monster avatar could not be found at " + MonsterAvatarSourcePath);

        animator.applyRootMotion = false;
        animator.Rebind();
        animator.Update(0f);
    }

    static GameObject EnsureAudioManager()
    {
        GameObject audioManager = GameObject.Find("AudioManager");
        if (audioManager == null)
            audioManager = new GameObject("AudioManager");

        MicSpectrum micSpectrum = audioManager.GetComponent<MicSpectrum>();
        if (micSpectrum == null)
            micSpectrum = audioManager.AddComponent<MicSpectrum>();

        micSpectrum.gain = 55f;
        micSpectrum.smooth = 12f;

        return audioManager;
    }

    static Light EnsureDirectionalLight()
    {
        Light directional = Object.FindObjectOfType<Light>();
        if (directional == null || directional.type != LightType.Directional)
        {
            GameObject lightObject = new GameObject("Directional Light");
            directional = lightObject.AddComponent<Light>();
            directional.type = LightType.Directional;
        }

        directional.transform.rotation = Quaternion.Euler(21f, -24f, 0f);
        return directional;
    }

    static void ConfigureLighting(Light directional)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0125f;
        RenderSettings.fogColor = new Color(0.055f, 0.06f, 0.075f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.085f, 0.09f, 0.1f);
        RenderSettings.skybox = null;

        directional.intensity = 0.38f;
        directional.color = new Color(0.4f, 0.44f, 0.5f);
        directional.shadows = LightShadows.Soft;
    }

    static void BuildTown(GameObject player, GameObject monster, GameObject audioManager)
    {
        GameObject root = new GameObject("GeneratedTown");

        CreateGround(root.transform);
        CreateGrass(root.transform);
        CreateRoad(root.transform);
        CreateStreetLights(root.transform);
        CreatePerimeter(root.transform);
        CreateEscapeGate(root.transform);
        CreateTownBlocks(root.transform);
        CreateForest(root.transform);
        CreateMonsterCover(root.transform);

        PlayerNoiseEmitter noiseEmitter = player.GetComponent<PlayerNoiseEmitter>();
        if (noiseEmitter == null)
            noiseEmitter = player.AddComponent<PlayerNoiseEmitter>();

        noiseEmitter.micSpectrum = audioManager.GetComponent<MicSpectrum>();
        noiseEmitter.minimumHearingRadius = 8f;
        noiseEmitter.maximumHearingRadius = 65f;
        noiseEmitter.noiseMemoryDuration = 1.35f;
        player.GetComponent<PlayerMove>().noiseEmitter = noiseEmitter;

        MonsterAI monsterAI = monster.GetComponent<MonsterAI>();
        monsterAI.player = player.transform;
        monsterAI.noiseEmitter = noiseEmitter;
    }

    static void CreateGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(8.5f, 1f, 8.5f);
        ground.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
            "Assets/Materials/HorrorGround.mat",
            new Color(0.11f, 0.11f, 0.12f),
            Color.black);
    }

    static void CreateGrass(Transform parent)
    {
        string[] grassPaths =
        {
            "Assets/StylizedGrass/Prefabs/sTri-Plane.prefab",
            "Assets/StylizedGrass/Prefabs/sQuad-Plane.prefab"
        };

        int index = 0;
        for (int x = -10; x <= 10; x++)
        {
            for (int z = -13; z <= 13; z++)
            {
                if (Mathf.Abs(x) < 3)
                    continue;

                GameObject grass = SpawnPrefab(
                    grassPaths[index % grassPaths.Length],
                    "GrassPatch",
                    new Vector3(x * 2.6f, 0.02f, z * 2.9f),
                    Quaternion.Euler(0f, (x * 17 + z * 11) % 360, 0f),
                    parent);

                if (grass != null)
                    grass.transform.localScale = Vector3.one * 2.2f;

                index++;
            }
        }
    }

    static void CreateRoad(Transform parent)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.SetParent(parent);
        road.transform.position = new Vector3(0f, 0.05f, 0f);
        road.transform.localScale = new Vector3(12f, 0.1f, 76f);
        road.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
            "Assets/Materials/HorrorRoad.mat",
            new Color(0.06f, 0.06f, 0.07f),
            Color.black);

        for (int i = -5; i <= 5; i++)
        {
            if (i == 0)
                continue;

            GameObject laneMark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            laneMark.name = "RoadPatch_" + i;
            laneMark.transform.SetParent(parent);
            laneMark.transform.position = new Vector3(0f, 0.12f, i * 7f);
            laneMark.transform.localScale = new Vector3(0.4f, 0.03f, 2.2f);
            laneMark.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
                "Assets/Materials/HorrorLane.mat",
                new Color(0.26f, 0.24f, 0.17f),
                Color.black);
        }
    }

    static void CreateStreetLights(Transform parent)
    {
        for (int i = -4; i <= 4; i += 2)
        {
            CreateStreetLight(parent, new Vector3(-7.5f, 0f, i * 7.5f));
            CreateStreetLight(parent, new Vector3(7.5f, 0f, (i * 7.5f) + 3f));
        }
    }

    static void CreateStreetLight(Transform parent, Vector3 position)
    {
        GameObject lightRoot = new GameObject("StreetLight");
        lightRoot.transform.SetParent(parent);
        lightRoot.transform.position = position;

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.SetParent(lightRoot.transform);
        pole.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        pole.transform.localScale = new Vector3(0.12f, 2.2f, 0.12f);
        pole.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
            "Assets/Materials/HorrorPole.mat",
            new Color(0.07f, 0.07f, 0.08f),
            Color.black);

        GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lamp.transform.SetParent(lightRoot.transform);
        lamp.transform.localPosition = new Vector3(0f, 4.45f, 0f);
        lamp.transform.localScale = Vector3.one * 0.32f;
        lamp.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
            "Assets/Materials/HorrorLamp.mat",
            new Color(0.82f, 0.45f, 0.15f),
            new Color(0.95f, 0.42f, 0.08f) * 0.7f);

        Light pointLight = lightRoot.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 9f;
        pointLight.intensity = 3.2f;
        pointLight.color = new Color(0.96f, 0.48f, 0.16f);
        pointLight.shadows = LightShadows.Soft;
    }

    static void CreateEscapeGate(Transform parent)
    {
        GameObject gateVisual = SpawnPrefab(
            "Assets/FantasyEnvironments/Environments/Town/Prefabs/TownGate.prefab",
            "EscapeGate",
            new Vector3(0f, 0f, 39f),
            Quaternion.Euler(0f, 180f, 0f),
            parent);

        if (gateVisual == null)
            return;

        AddBoundsCollider(gateVisual, false);

        GameObject trigger = new GameObject("EscapeTrigger");
        trigger.transform.SetParent(gateVisual.transform);
        trigger.transform.localPosition = new Vector3(0f, 1.4f, -2f);
        BoxCollider box = trigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(6f, 3f, 5f);
        trigger.AddComponent<EscapeZone>();
    }

    static void CreatePerimeter(Transform parent)
    {
        const float wallHeight = 5f;
        const float wallY = 2.5f;
        const float northZ = 47f;
        const float southZ = -47f;

        CreatePerimeterWall(parent, new Vector3(0f, wallY, southZ), new Vector3(72f, wallHeight, 2f));
        CreatePerimeterWall(parent, new Vector3(0f, wallY, northZ), new Vector3(72f, wallHeight, 2f));
        CreatePerimeterWall(parent, new Vector3(-35f, wallY, 0f), new Vector3(2f, wallHeight, 98f));
        CreatePerimeterWall(parent, new Vector3(35f, wallY, 0f), new Vector3(2f, wallHeight, 98f));
    }

    static void CreatePerimeterWall(Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "PerimeterWall";
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
            "Assets/Materials/HorrorPerimeter.mat",
            new Color(0.09f, 0.09f, 0.1f),
            Color.black);
    }

    static void CreateTownBlocks(Transform parent)
    {
        string[] housePaths =
        {
            "Assets/FantasyEnvironments/Environments/Town/Prefabs/House1.prefab",
            "Assets/FantasyEnvironments/Environments/Town/Prefabs/House2.prefab",
            "Assets/FantasyEnvironments/Environments/Town/Prefabs/House3.prefab",
            "Assets/FantasyEnvironments/Environments/Town/Prefabs/WoodenHouse_1.prefab",
            "Assets/FantasyEnvironments/Environments/Town/Prefabs/WoodenHouse_2.prefab",
            "Assets/FantasyEnvironments/Environments/Town/Prefabs/WoodenHouse_3.prefab"
        };

        float[] zRows = { -26f, -13f, 0f, 13f, 26f };
        float[] xColumns = { -14f, 14f };
        int houseIndex = 0;

        foreach (float z in zRows)
        {
            foreach (float x in xColumns)
            {
                string path = housePaths[houseIndex % housePaths.Length];
                float rotationY = x < 0f ? 90f : -90f;
                GameObject house = SpawnPrefab(
                    path,
                    "HouseBlock_" + houseIndex,
                    new Vector3(x, 0f, z),
                    Quaternion.Euler(0f, rotationY, 0f),
                    parent);

                if (house != null)
                {
                    AddBoundsCollider(house, false);
                    house.transform.localScale = Vector3.one * 1.1f;
                }

                houseIndex++;
            }
        }

        List<Vector3> clutterPositions = new List<Vector3>
        {
            new Vector3(-5f, 0f, -18f),
            new Vector3(6f, 0f, -8f),
            new Vector3(-6f, 0f, 11f),
            new Vector3(5f, 0f, 22f),
            new Vector3(-4f, 0f, 30f)
        };

        string[] clutterPaths =
        {
            "Assets/FantasyEnvironments/Environments/Town/Prefabs/storage_barrel.prefab",
            "Assets/FantasyEnvironments/Environments/Town/Prefabs/storage_basket.prefab",
            "Assets/FantasyEnvironments/Environments/Town/Prefabs/cart1.prefab",
            "Assets/FantasyEnvironments/Environments/Town/Prefabs/cart3.prefab"
        };

        for (int i = 0; i < clutterPositions.Count; i++)
        {
            GameObject clutter = SpawnPrefab(
                clutterPaths[i % clutterPaths.Length],
                "Clutter_" + i,
                clutterPositions[i],
                Quaternion.Euler(0f, (i * 67f) % 360f, 0f),
                parent);

            if (clutter != null)
                AddBoundsCollider(clutter, false);
        }
    }

    static void CreateForest(Transform parent)
    {
        string[] treePaths =
        {
            "Assets/FantasyEnvironments/Environments/Ambient-Occlusion-Trees/Prefabs/Pine_tree1.prefab",
            "Assets/FantasyEnvironments/Environments/Ambient-Occlusion-Trees/Prefabs/Pine_tree2.prefab",
            "Assets/FantasyEnvironments/Environments/Ambient-Occlusion-Trees/Prefabs/Pine_tree3.prefab"
        };

        List<Vector3> positions = new List<Vector3>();
        for (int i = -5; i <= 5; i++)
        {
            positions.Add(new Vector3(-28f, 0f, i * 8f));
            positions.Add(new Vector3(28f, 0f, i * 8f));
        }

        for (int i = -3; i <= 3; i++)
        {
            positions.Add(new Vector3(i * 8f, 0f, -40f));
            positions.Add(new Vector3(i * 8f, 0f, 40f));
        }

        for (int i = 0; i < positions.Count; i++)
        {
            GameObject tree = SpawnPrefab(
                treePaths[i % treePaths.Length],
                "Pine_" + i,
                positions[i],
                Quaternion.Euler(0f, (i * 33f) % 360f, 0f),
                parent);

            if (tree != null)
            {
                tree.transform.localScale = Vector3.one * Random.Range(1.1f, 1.5f);
                ConvertMaterialsToHdrp(tree, true);
                ApplyPineTreeMaterials(tree);
                AddBoundsCollider(tree, false);
            }
        }
    }

    static void ApplyPineTreeMaterials(GameObject tree)
    {
        Material barkMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HDRP_Pine_bark.mat");
        Material leavesMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HDRP_Pine_leaves.mat");
        if (barkMaterial == null || leavesMaterial == null)
            return;

        Renderer[] renderers = tree.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            Material[] materials = renderer.sharedMaterials;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material current = materials[materialIndex];
                string materialName = current != null ? current.name.ToLowerInvariant() : string.Empty;
                string rendererName = renderer.name.ToLowerInvariant();
                bool looksLikeLeaves =
                    materialName.Contains("leaf") ||
                    materialName.Contains("leaves") ||
                    materialName.Contains("needle") ||
                    rendererName.Contains("leaf") ||
                    rendererName.Contains("leaves") ||
                    rendererName.Contains("needle");

                materials[materialIndex] = looksLikeLeaves ? leavesMaterial : barkMaterial;
            }

            renderer.sharedMaterials = materials;
        }
    }

    static void CreateMonsterCover(Transform parent)
    {
        Vector3[] blockers =
        {
            new Vector3(-9f, 1.1f, -4f),
            new Vector3(9f, 1.1f, 8f),
            new Vector3(-8.5f, 1.1f, 18f),
            new Vector3(8f, 1.1f, -22f)
        };

        for (int i = 0; i < blockers.Length; i++)
        {
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "CoverBlock_" + i;
            blocker.transform.SetParent(parent);
            blocker.transform.position = blockers[i];
            blocker.transform.localScale = new Vector3(2.4f, 2.2f, 2.4f);
            blocker.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
                "Assets/Materials/HorrorStone.mat",
                new Color(0.1f, 0.1f, 0.11f),
                Color.black);
        }
    }

    static GameObject SpawnPrefab(string path, string name, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("Prefab missing: " + path);
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return null;

        instance.name = name;
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        return instance;
    }

    static void AddBoundsCollider(GameObject target, bool isTrigger)
    {
        if (target.GetComponent<Collider>() != null)
            return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        BoxCollider collider = target.AddComponent<BoxCollider>();
        collider.isTrigger = isTrigger;
        collider.center = target.transform.InverseTransformPoint(bounds.center);
        collider.size = new Vector3(
            Mathf.Max(0.5f, bounds.size.x),
            Mathf.Max(0.5f, bounds.size.y),
            Mathf.Max(0.5f, bounds.size.z));
    }

    static Material GetOrCreateMaterial(string path, Color baseColor, Color emissionColor)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        material = new Material(shader);
        material.color = baseColor;

        if (emissionColor.maxColorComponent > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void ConvertMaterialsToHdrp(GameObject target, bool preserveAlphaCutout)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            Material[] materials = renderer.sharedMaterials;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material sourceMaterial = materials[materialIndex];
                if (sourceMaterial == null)
                    continue;

                materials[materialIndex] = GetOrCreateHdrpMaterial(sourceMaterial, preserveAlphaCutout);
            }

            renderer.sharedMaterials = materials;
        }
    }

    static Material GetOrCreateHdrpMaterial(Material sourceMaterial, bool preserveAlphaCutout)
    {
        string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
        string sourceName = string.IsNullOrEmpty(sourcePath)
            ? sourceMaterial.name
            : System.IO.Path.GetFileNameWithoutExtension(sourcePath);
        string targetPath = "Assets/Materials/HDRP_" + sourceName + ".mat";

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
        if (existing != null)
            return existing;

        Shader hdrpShader = Shader.Find("HDRP/Lit");
        if (hdrpShader == null)
            return sourceMaterial;

        Material hdrpMaterial = new Material(hdrpShader);
        hdrpMaterial.name = "HDRP_" + sourceName;

        Texture mainTexture = GetFirstTexture(
            sourceMaterial,
            "_BaseColorMap",
            "_BaseMap",
            "_BASE_COLOR_MAP",
            "_MainTex");
        Texture normalTexture = GetFirstTexture(
            sourceMaterial,
            "_NormalMap",
            "_NORMAL_MAP",
            "_BumpMap");
        Texture maskTexture = GetFirstTexture(
            sourceMaterial,
            "_MaskMap",
            "_MetallicGlossMap",
            "_MetallicMap",
            "_METALNESS_MAP");
        Texture emissionTexture = GetFirstTexture(
            sourceMaterial,
            "_EmissiveColorMap",
            "_EMISSION_COLOR_MAP",
            "_EmissionMap");
        Color baseColor = GetFirstColor(
            sourceMaterial,
            "_BaseColor",
            "_BASE_COLOR",
            "_Color");
        Color emissionColor = GetFirstColor(
            sourceMaterial,
            "_EmissiveColor",
            "_EMISSION_COLOR",
            "_EmissionColor");

        hdrpMaterial.SetColor("_BaseColor", baseColor);
        if (mainTexture != null)
            hdrpMaterial.SetTexture("_BaseColorMap", mainTexture);
        if (normalTexture != null)
        {
            hdrpMaterial.SetTexture("_NormalMap", normalTexture);
            hdrpMaterial.SetFloat("_NormalScale", sourceMaterial.HasProperty("_BumpScale")
                ? sourceMaterial.GetFloat("_BumpScale")
                : 1f);
        }
        if (maskTexture != null)
            hdrpMaterial.SetTexture("_MaskMap", maskTexture);
        if (emissionTexture != null)
            hdrpMaterial.SetTexture("_EmissiveColorMap", emissionTexture);
        if (emissionColor.maxColorComponent > 0f || emissionTexture != null)
        {
            hdrpMaterial.SetColor("_EmissiveColor", emissionColor);
            hdrpMaterial.EnableKeyword("_EMISSIVE_COLOR_MAP");
        }

        string lowerName = sourceName.ToLowerInvariant();
        bool isAlphaCutoutMaterial = preserveAlphaCutout &&
            (lowerName.Contains("leaf") ||
             lowerName.Contains("leaves") ||
             lowerName.Contains("needle") ||
             lowerName.Contains("pine"));

        if (isAlphaCutoutMaterial)
        {
            hdrpMaterial.SetFloat("_AlphaCutoffEnable", 1f);
            hdrpMaterial.SetFloat("_AlphaCutoff", 0.35f);
            hdrpMaterial.SetFloat("_DoubleSidedEnable", 1f);
            hdrpMaterial.SetFloat("_CullMode", 0f);
            hdrpMaterial.EnableKeyword("_ALPHATEST_ON");
        }

        HDMaterial.ValidateMaterial(hdrpMaterial);
        AssetDatabase.CreateAsset(hdrpMaterial, targetPath);
        return hdrpMaterial;
    }

    static Texture GetFirstTexture(Material sourceMaterial, params string[] propertyNames)
    {
        for (int i = 0; i < propertyNames.Length; i++)
        {
            if (!sourceMaterial.HasProperty(propertyNames[i]))
                continue;

            Texture texture = sourceMaterial.GetTexture(propertyNames[i]);
            if (texture != null)
                return texture;
        }

        return sourceMaterial.mainTexture;
    }

    static Color GetFirstColor(Material sourceMaterial, params string[] propertyNames)
    {
        for (int i = 0; i < propertyNames.Length; i++)
        {
            if (sourceMaterial.HasProperty(propertyNames[i]))
                return sourceMaterial.GetColor(propertyNames[i]);
        }

        return Color.white;
    }

    static Avatar FindAvatarAsset(string path)
    {
        Avatar directAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
        if (directAvatar != null)
            return directAvatar;

        Object[] representations = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        for (int i = 0; i < representations.Length; i++)
        {
            Avatar avatar = representations[i] as Avatar;
            if (avatar != null)
                return avatar;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            Avatar avatar = assets[i] as Avatar;
            if (avatar != null)
                return avatar;
        }

        return null;
    }
}
