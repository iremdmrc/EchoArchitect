using UnityEngine;

public class EchoArchitectRuntimeTown : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BuildFallbackTown()
    {
        if (GameObject.Find("GeneratedTown") != null || GameObject.Find("GeneratedTownRuntime") != null)
            return;

        GameObject player = EnsurePlayer();
        GameObject monster = EnsureMonster();
        MicSpectrum mic = EnsureAudioManager();

        GameObject root = new GameObject("GeneratedTownRuntime");
        CreateGround(root.transform);
        CreateGrass(root.transform);
        CreateRoad(root.transform);
        CreateHouseBlocks(root.transform);
        CreateForest(root.transform);
        CreateStreetLights(root.transform);
        CreatePerimeter(root.transform);
        CreateEscapeGate(root.transform);

        PlayerNoiseEmitter noiseEmitter = player.GetComponent<PlayerNoiseEmitter>();
        if (noiseEmitter == null)
            noiseEmitter = player.AddComponent<PlayerNoiseEmitter>();

        noiseEmitter.micSpectrum = mic;

        PlayerMove move = player.GetComponent<PlayerMove>();
        if (move != null)
            move.noiseEmitter = noiseEmitter;

        MonsterAI ai = monster.GetComponent<MonsterAI>();
        if (ai == null)
            ai = monster.AddComponent<MonsterAI>();

        ai.player = player.transform;
        ai.noiseEmitter = noiseEmitter;

        ConfigureLighting();
    }

    static GameObject EnsurePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.tag = "Player";
            player.name = "Player";
        }

        player.transform.position = new Vector3(0f, 1.2f, -34f);
        player.transform.rotation = Quaternion.identity;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
            controller = player.AddComponent<CharacterController>();

        controller.height = 1.9f;
        controller.radius = 0.34f;
        controller.center = new Vector3(0f, 0.92f, 0f);

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        if (capsule != null)
            capsule.enabled = false;

        PlayerMove move = player.GetComponent<PlayerMove>();
        if (move == null)
            move = player.AddComponent<PlayerMove>();

        move.walkSpeed = 4.5f;
        move.sprintSpeed = 6.9f;
        move.crouchSpeed = 2.7f;
        move.gravity = -26f;
        move.jumpHeight = 1.4f;
        move.groundMask = LayerMask.GetMask("Default");

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

        Renderer renderer = player.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;

        return player;
    }

    static GameObject EnsureMonster()
    {
        GameObject monster = GameObject.Find("Monster");
        if (monster == null)
        {
            monster = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            monster.name = "Monster";
        }

        monster.transform.position = new Vector3(0f, 1.1f, 32f);
        monster.transform.localScale = new Vector3(1.4f, 2.2f, 1.4f);

        Renderer renderer = monster.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = CreateRuntimeMaterial(
                new Color(0.08f, 0.08f, 0.09f),
                new Color(0.35f, 0.02f, 0.02f));
        }

        return monster;
    }

    static MicSpectrum EnsureAudioManager()
    {
        GameObject audioManager = GameObject.Find("AudioManager");
        if (audioManager == null)
            audioManager = new GameObject("AudioManager");

        MicSpectrum mic = audioManager.GetComponent<MicSpectrum>();
        if (mic == null)
            mic = audioManager.AddComponent<MicSpectrum>();

        mic.gain = 55f;
        mic.smooth = 12f;
        return mic;
    }

    static void ConfigureLighting()
    {
        Light directional = Object.FindObjectOfType<Light>();
        if (directional == null || directional.type != LightType.Directional)
        {
            GameObject lightObject = new GameObject("Directional Light");
            directional = lightObject.AddComponent<Light>();
            directional.type = LightType.Directional;
        }

        directional.transform.rotation = Quaternion.Euler(21f, -24f, 0f);
        directional.intensity = 0.38f;
        directional.color = new Color(0.4f, 0.44f, 0.5f);
        directional.shadows = LightShadows.Soft;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0125f;
        RenderSettings.fogColor = new Color(0.055f, 0.06f, 0.075f);
        RenderSettings.ambientLight = new Color(0.085f, 0.09f, 0.1f);
    }

    static void CreateGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(8.5f, 1f, 8.5f);
        ground.GetComponent<Renderer>().material = CreateRuntimeMaterial(new Color(0.11f, 0.11f, 0.12f), Color.black);
    }

    static void CreateGrass(Transform parent)
    {
        Material grassMaterial = CreateRuntimeMaterial(new Color(0.16f, 0.23f, 0.14f), Color.black);

        for (int x = -15; x <= 15; x++)
        {
            for (int z = -15; z <= 15; z++)
            {
                if (Mathf.Abs(x) < 3)
                    continue;

                GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Quad);
                grass.name = "GrassPatch";
                grass.transform.SetParent(parent);
                grass.transform.position = new Vector3(x * 1.8f, 0.15f, z * 1.8f);
                grass.transform.rotation = Quaternion.Euler(90f, (x * 17 + z * 11) % 360, 0f);
                grass.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
                grass.GetComponent<Renderer>().material = grassMaterial;
                Object.Destroy(grass.GetComponent<Collider>());
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
        road.GetComponent<Renderer>().material = CreateRuntimeMaterial(new Color(0.06f, 0.06f, 0.07f), Color.black);
    }

    static void CreateHouseBlocks(Transform parent)
    {
        Vector3[] houses =
        {
            new Vector3(-14f, 2.3f, -26f),
            new Vector3(14f, 2.3f, -26f),
            new Vector3(-14f, 2.3f, -13f),
            new Vector3(14f, 2.3f, -13f),
            new Vector3(-14f, 2.3f, 0f),
            new Vector3(14f, 2.3f, 0f),
            new Vector3(-14f, 2.3f, 13f),
            new Vector3(14f, 2.3f, 13f),
            new Vector3(-14f, 2.3f, 26f),
            new Vector3(14f, 2.3f, 26f)
        };

        for (int i = 0; i < houses.Length; i++)
        {
            GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.name = "RuntimeHouse_" + i;
            house.transform.SetParent(parent);
            house.transform.position = houses[i];
            house.transform.localScale = new Vector3(7f, 4.6f, 7f);
            house.GetComponent<Renderer>().material = CreateRuntimeMaterial(new Color(0.16f, 0.14f, 0.13f), Color.black);

            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof_" + i;
            roof.transform.SetParent(house.transform);
            roof.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            roof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            roof.transform.localScale = new Vector3(5.4f, 1.2f, 5.4f);
            roof.GetComponent<Renderer>().material = CreateRuntimeMaterial(new Color(0.12f, 0.06f, 0.05f), Color.black);
        }
    }

    static void CreateForest(Transform parent)
    {
        for (int i = -5; i <= 5; i++)
        {
            CreateTree(parent, new Vector3(-27f, 0f, i * 7f));
            CreateTree(parent, new Vector3(27f, 0f, i * 7f));
        }

        for (int i = -3; i <= 3; i++)
        {
            CreateTree(parent, new Vector3(i * 8f, 0f, -42f));
            CreateTree(parent, new Vector3(i * 8f, 0f, 42f));
        }
    }

    static void CreateTree(Transform parent, Vector3 position)
    {
        GameObject root = new GameObject("Tree");
        root.transform.SetParent(parent);
        root.transform.position = position;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(root.transform);
        trunk.transform.localPosition = new Vector3(0f, 2f, 0f);
        trunk.transform.localScale = new Vector3(0.45f, 2f, 0.45f);
        trunk.GetComponent<Renderer>().material = CreateRuntimeMaterial(new Color(0.16f, 0.1f, 0.08f), Color.black);

        for (int i = 0; i < 3; i++)
        {
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.transform.SetParent(root.transform);
            leaves.transform.localPosition = new Vector3(0f, 3.4f + i, 0f);
            leaves.transform.localScale = Vector3.one * (2.8f - (i * 0.45f));
            leaves.GetComponent<Renderer>().material = CreateRuntimeMaterial(new Color(0.07f, 0.12f, 0.08f), Color.black);
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

    static void CreatePerimeter(Transform parent)
    {
        CreatePerimeterWall(parent, new Vector3(0f, 2.5f, -47f), new Vector3(70f, 5f, 2f));
        CreatePerimeterWall(parent, new Vector3(0f, 2.5f, 47f), new Vector3(70f, 5f, 2f));
        CreatePerimeterWall(parent, new Vector3(-35f, 2.5f, 0f), new Vector3(2f, 5f, 96f));
        CreatePerimeterWall(parent, new Vector3(35f, 2.5f, 0f), new Vector3(2f, 5f, 96f));
    }

    static void CreatePerimeterWall(Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "PerimeterWall";
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = CreateRuntimeMaterial(new Color(0.09f, 0.09f, 0.1f), Color.black);
    }

    static void CreateStreetLight(Transform parent, Vector3 position)
    {
        GameObject lightRoot = new GameObject("RuntimeStreetLight");
        lightRoot.transform.SetParent(parent);
        lightRoot.transform.position = position;

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.SetParent(lightRoot.transform);
        pole.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        pole.transform.localScale = new Vector3(0.12f, 2.2f, 0.12f);
        pole.GetComponent<Renderer>().material = CreateRuntimeMaterial(new Color(0.07f, 0.07f, 0.08f), Color.black);

        Light pointLight = lightRoot.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 9f;
        pointLight.intensity = 3.2f;
        pointLight.color = new Color(0.96f, 0.48f, 0.16f);
        pointLight.shadows = LightShadows.Soft;
    }

    static void CreateEscapeGate(Transform parent)
    {
        GameObject gate = new GameObject("EscapeGate");
        gate.transform.SetParent(parent);
        gate.transform.position = new Vector3(0f, 0f, 38f);

        CreateGatePiece(gate.transform, new Vector3(-2.2f, 2.3f, 0f), new Vector3(0.8f, 4.6f, 0.8f));
        CreateGatePiece(gate.transform, new Vector3(2.2f, 2.3f, 0f), new Vector3(0.8f, 4.6f, 0.8f));
        CreateGatePiece(gate.transform, new Vector3(0f, 4.8f, 0f), new Vector3(5.2f, 0.7f, 0.8f));

        GameObject trigger = new GameObject("EscapeTrigger");
        trigger.transform.SetParent(gate.transform);
        trigger.transform.localPosition = new Vector3(0f, 1.5f, -1f);
        BoxCollider collider = trigger.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(6f, 3f, 4f);
        trigger.AddComponent<EscapeZone>();
    }

    static void CreateGatePiece(Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.transform.SetParent(parent);
        piece.transform.localPosition = localPosition;
        piece.transform.localScale = localScale;
        piece.GetComponent<Renderer>().material = CreateRuntimeMaterial(new Color(0.14f, 0.12f, 0.12f), Color.black);
    }

    static Material CreateRuntimeMaterial(Color baseColor, Color emissionColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = baseColor;

        if (emissionColor.maxColorComponent > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor);
        }

        return material;
    }
}
