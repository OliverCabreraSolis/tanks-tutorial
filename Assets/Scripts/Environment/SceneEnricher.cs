using UnityEngine;

public class SceneEnricher : MonoBehaviour
{
    public static void EnrichCurrentScene(Rigidbody shellPrefab = null)
    {
        GameObject envParent = GameObject.Find("EnrichedEnvironment");
        if (envParent != null)
        {
            Destroy(envParent);
        }

        envParent = new GameObject("EnrichedEnvironment");

        // 1. Helipuerto Militar con Balizas Luminosas
        CreateHelipad(envParent.transform);

        // 2. Radares Militares Giratorios con balizas rojas
        CreateRadars(envParent.transform);

        // 3. Tanques Destruidos Humeantes como Cobertura
        CreateBattlefieldWrecks(envParent.transform);

        // 4. Cráteres de Impacto
        CreateCraters(envParent.transform);

        // 5. Barriles Explosivos Tácticos
        CreateExplosiveBarrels(envParent.transform);

        // 6. Torretas Centinela Enemigas
        CreateEnemyTurrets(envParent.transform, shellPrefab);

        // 7. Barreras de Concreto (Jersey Barriers) con colisión sólida
        CreateConcreteBarriers(envParent.transform);

        // 8. Erizos Checos Antitanque (Czech Hedgehogs) con colisión sólida
        CreateCzechHedgehogs(envParent.transform);

        // 9. Bunkers y Parapetos de Sacos de Arena con colisión
        CreateSandbagBunkers(envParent.transform);

        // 10. Cajas de Munición y Suministros Militares con colisión
        CreateSupplyCrates(envParent.transform);

        // 11. Faros Delanteros de Neón para los Tanques
        EquipTankHeadlights();
    }

    private static void CreateHelipad(Transform parent)
    {
        GameObject helipadPrefab = Resources.Load<GameObject>("Helipad");
        if (helipadPrefab == null)
        {
            // Try loading from Assets path via Resources or primitive fallback
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plane.name = "HelipadFallback";
            plane.transform.position = new Vector3(20f, 0.05f, 20f);
            plane.transform.localScale = new Vector3(8f, 0.05f, 8f);
            plane.transform.SetParent(parent);
            Collider pCol = plane.GetComponent<Collider>();
            if (pCol != null) DestroyImmediate(pCol);
            Renderer r = plane.GetComponent<Renderer>();
            if (r != null) r.material = MaterialHelper.CreateMaterial(new Color(0.25f, 0.25f, 0.28f), 0.3f);
        }
        else
        {
            GameObject h = Instantiate(helipadPrefab, new Vector3(20f, 0.05f, 20f), Quaternion.identity);
            h.transform.SetParent(parent);
        }

        // Add 4 pulsing corner beacon lights for the helipad
        Vector3[] corners = {
            new Vector3(16f, 0.2f, 16f),
            new Vector3(24f, 0.2f, 16f),
            new Vector3(16f, 0.2f, 24f),
            new Vector3(24f, 0.2f, 24f)
        };

        foreach (var c in corners)
        {
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "HelipadBeacon";
            lamp.transform.position = c;
            lamp.transform.localScale = Vector3.one * 0.4f;
            lamp.transform.SetParent(parent);
            Collider col = lamp.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer lampR = lamp.GetComponent<Renderer>();
            if (lampR != null) lampR.material = MaterialHelper.CreateMaterial(Color.cyan, 0.8f);

            Light l = lamp.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = Color.cyan;
            l.range = 5f;
            l.intensity = 2f;

            lamp.AddComponent<BlinkingLight>();
        }
    }

    private static void CreateRadars(Transform parent)
    {
        Vector3[] radarPositions = {
            new Vector3(-22f, 0f, 22f),
            new Vector3(22f, 0f, -22f)
        };

        foreach (var pos in radarPositions)
        {
            GameObject radarObj = new GameObject("MilitaryRadar");
            radarObj.transform.position = pos;
            radarObj.transform.SetParent(parent);

            // Radar Tower Mast
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "RadarMast";
            mast.transform.SetParent(radarObj.transform);
            mast.transform.localPosition = new Vector3(0, 2f, 0);
            mast.transform.localScale = new Vector3(0.6f, 2f, 0.6f);
            mast.GetComponent<Renderer>().material = MaterialHelper.CreateMaterial(new Color(0.25f, 0.25f, 0.28f), 0.4f);

            // Rotating Dish Head
            GameObject dishHead = new GameObject("RotatingDishHead");
            dishHead.transform.SetParent(radarObj.transform);
            dishHead.transform.localPosition = new Vector3(0, 4.2f, 0);

            GameObject dish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dish.name = "DishMesh";
            dish.transform.SetParent(dishHead.transform);
            dish.transform.localPosition = Vector3.zero;
            dish.transform.localRotation = Quaternion.Euler(60f, 0, 0);
            dish.transform.localScale = new Vector3(2.5f, 0.15f, 1.4f);
            dish.GetComponent<Renderer>().material = MaterialHelper.CreateMaterial(new Color(0.4f, 0.42f, 0.45f), 0.5f);

            // Red beacon light on top
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "RadarBeacon";
            beacon.transform.SetParent(dishHead.transform);
            beacon.transform.localPosition = new Vector3(0, 0.8f, 0);
            beacon.transform.localScale = Vector3.one * 0.35f;
            Collider bCol = beacon.GetComponent<Collider>();
            if (bCol != null) Destroy(bCol);

            beacon.GetComponent<Renderer>().material = MaterialHelper.CreateMaterial(Color.red, 0.8f);

            Light l = beacon.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = Color.red;
            l.range = 6f;
            l.intensity = 3f;
            beacon.AddComponent<BlinkingLight>();

            // Continuous 360 rotation
            dishHead.AddComponent<ContinuousRotation>().m_Speed = 50f;
        }
    }

    private static void CreateBattlefieldWrecks(Transform parent)
    {
        Vector3[] wreckPositions = {
            new Vector3(-12f, 0.1f, 8f),
            new Vector3(12f, 0.1f, -8f)
        };

        foreach (var pos in wreckPositions)
        {
            GameObject wreck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wreck.name = "BattleWreck_Cover";
            wreck.transform.position = pos + Vector3.up * 0.6f;
            wreck.transform.rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0, 360f), Random.Range(-5f, 5f));
            wreck.transform.localScale = new Vector3(2.4f, 1.2f, 3.2f);
            wreck.transform.SetParent(parent);

            Renderer r = wreck.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = MaterialHelper.CreateMaterial(new Color(0.2f, 0.18f, 0.16f), 0.2f);
            }

            // Smoke particle on wrecked hull
            GameObject smoke = new GameObject("WreckSmoke");
            smoke.transform.SetParent(wreck.transform);
            smoke.transform.localPosition = new Vector3(0, 0.7f, 0);
            ParticleSystem ps = smoke.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            MaterialHelper.FixParticleSystem(ps, new Color(0.25f, 0.25f, 0.25f, 0.5f));
            ParticleSystemRenderer psR = smoke.GetComponent<ParticleSystemRenderer>();
            if (psR != null) psR.material = MaterialHelper.CreateParticleMaterial(new Color(0.25f, 0.25f, 0.25f, 0.5f));

            var main = ps.main;
            main.loop = true;
            main.startLifetime = 1.8f;
            main.startSpeed = 1.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
            var emit = ps.emission;
            emit.rateOverTime = 8;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            ps.Play();
        }
    }

    private static void CreateCraters(Transform parent)
    {
        Vector3[] craterPositions = {
            new Vector3(-6f, 0.02f, -10f),
            new Vector3(7f, 0.02f, 11f),
            new Vector3(0f, 0.02f, 0f)
        };

        foreach (var pos in craterPositions)
        {
            GameObject crater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crater.name = "CraterDecal";
            crater.transform.position = pos;
            crater.transform.localScale = new Vector3(Random.Range(3f, 4.5f), 0.02f, Random.Range(3f, 4.5f));
            crater.transform.SetParent(parent);

            Collider col = crater.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col); // purely cosmetic decal on the floor

            Renderer r = crater.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = MaterialHelper.CreateMaterial(new Color(0.14f, 0.11f, 0.08f), 0.05f);
            }
        }
    }

    private static void CreateExplosiveBarrels(Transform parent)
    {
        Vector3[] barrelSpots = {
            new Vector3(-10f, 0f, 10f),
            new Vector3(-11.5f, 0f, 10f),
            new Vector3(10f, 0f, -10f),
            new Vector3(11.5f, 0f, -10f),
            new Vector3(0f, 0f, 16f),
            new Vector3(0f, 0f, -16f)
        };

        foreach (var spot in barrelSpots)
        {
            GameObject barrel = ExplosiveBarrel.Create(spot);
            barrel.transform.SetParent(parent);
        }
    }

    private static void CreateEnemyTurrets(Transform parent, Rigidbody shellPrefab)
    {
        Vector3[] turretSpots = {
            new Vector3(-18f, 0f, -18f),
            new Vector3(18f, 0f, 18f)
        };

        foreach (var spot in turretSpots)
        {
            GameObject turret = EnemyTurret.Create(spot, shellPrefab);
            turret.transform.SetParent(parent);
        }
    }

    private static void CreateConcreteBarriers(Transform parent)
    {
        (Vector3 pos, float angle)[] barrierSpots = {
            (new Vector3(-8f, 0.65f, 4f), 40f),
            (new Vector3(8f, 0.65f, -12f), 40f),
            (new Vector3(-8f, 0.65f, -9f), -35f),
            (new Vector3(8f, 0.65f, 9f), -35f),
            (new Vector3(0f, 0.65f, 6f), 90f),
            (new Vector3(0f, 0.65f, -6f), 90f)
        };

        Material barrierMat = MaterialHelper.CreateMaterial(new Color(0.62f, 0.62f, 0.60f), 0.25f);
        Material stripeMat = MaterialHelper.CreateMaterial(new Color(0.95f, 0.75f, 0.1f), 0.5f);

        for (int i = 0; i < barrierSpots.Length; i++)
        {
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = "ConcreteBarrier_" + i;
            barrier.transform.position = barrierSpots[i].pos;
            barrier.transform.rotation = Quaternion.Euler(0f, barrierSpots[i].angle, 0f);
            barrier.transform.localScale = new Vector3(3.2f, 1.3f, 0.8f);
            barrier.transform.SetParent(parent);

            Renderer r = barrier.GetComponent<Renderer>();
            if (r != null) r.material = barrierMat;

            // Reflective warning top cap
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "WarningStripe";
            stripe.transform.SetParent(barrier.transform);
            stripe.transform.localPosition = new Vector3(0f, 0.51f, 0f);
            stripe.transform.localScale = new Vector3(1f, 0.05f, 1.02f);
            Collider sCol = stripe.GetComponent<Collider>();
            if (sCol != null) Destroy(sCol);

            Renderer sr = stripe.GetComponent<Renderer>();
            if (sr != null) sr.material = stripeMat;
        }
    }

    private static void CreateCzechHedgehogs(Transform parent)
    {
        Vector3[] hedgehogPositions = {
            new Vector3(-16f, 0.75f, 0f),
            new Vector3(18f, 0.75f, 0f),
            new Vector3(-12f, 0.75f, 16f),
            new Vector3(12f, 0.75f, -16f),
            new Vector3(-15f, 0.75f, 8f),
            new Vector3(15f, 0.75f, -8f)
        };

        Material steelMat = MaterialHelper.CreateMaterial(new Color(0.24f, 0.25f, 0.27f), 0.6f);

        for (int i = 0; i < hedgehogPositions.Length; i++)
        {
            GameObject hedgehog = new GameObject("CzechHedgehog_" + i);
            hedgehog.transform.position = hedgehogPositions[i];
            hedgehog.transform.SetParent(parent);

            Vector3[] beamRotations = {
                new Vector3(45f, 0f, 45f),
                new Vector3(-45f, 0f, 45f),
                new Vector3(0f, 90f, 45f)
            };

            for (int b = 0; b < 3; b++)
            {
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = "Beam_" + b;
                beam.transform.SetParent(hedgehog.transform);
                beam.transform.localPosition = Vector3.zero;
                beam.transform.localRotation = Quaternion.Euler(beamRotations[b]);
                beam.transform.localScale = new Vector3(0.28f, 0.28f, 2.3f);

                Renderer br = beam.GetComponent<Renderer>();
                if (br != null) br.material = steelMat;
            }
        }
    }

    private static void CreateSandbagBunkers(Transform parent)
    {
        (Vector3 pos, float angle)[] bunkerSpots = {
            (new Vector3(-15f, 0f, -6f), 25f),
            (new Vector3(15f, 0f, 6f), 205f),
            (new Vector3(-8f, 0f, 16f), 90f),
            (new Vector3(8f, 0f, -16f), -90f)
        };

        Material sandbagMat = MaterialHelper.CreateMaterial(new Color(0.70f, 0.64f, 0.50f), 0.15f);

        for (int i = 0; i < bunkerSpots.Length; i++)
        {
            GameObject bunker = new GameObject("SandbagBunker_" + i);
            bunker.transform.position = bunkerSpots[i].pos;
            bunker.transform.rotation = Quaternion.Euler(0f, bunkerSpots[i].angle, 0f);
            bunker.transform.SetParent(parent);

            // Row 1 (Lower layer - 3 bags)
            for (int r = -1; r <= 1; r++)
            {
                GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bag.name = "Bag_Base_" + r;
                bag.transform.SetParent(bunker.transform);
                bag.transform.localPosition = new Vector3(r * 1.15f, 0.25f, 0f);
                bag.transform.localScale = new Vector3(1.1f, 0.5f, 0.65f);
                Renderer br = bag.GetComponent<Renderer>();
                if (br != null) br.material = sandbagMat;
            }

            // Row 2 (Upper layer - 2 bags offset)
            for (float r = -0.5f; r <= 0.5f; r += 1.0f)
            {
                GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bag.name = "Bag_Top_" + r;
                bag.transform.SetParent(bunker.transform);
                bag.transform.localPosition = new Vector3(r * 1.15f, 0.7f, 0f);
                bag.transform.localScale = new Vector3(1.1f, 0.45f, 0.6f);
                Renderer br = bag.GetComponent<Renderer>();
                if (br != null) br.material = sandbagMat;
            }
        }
    }

    private static void CreateSupplyCrates(Transform parent)
    {
        Vector3[] cratePositions = {
            new Vector3(-11f, 0f, 13f),
            new Vector3(11f, 0f, -13f),
            new Vector3(-13f, 0f, -11f),
            new Vector3(13f, 0f, 11f)
        };

        Material oliveCrate = MaterialHelper.CreateMaterial(new Color(0.30f, 0.36f, 0.25f), 0.3f);
        Material woodCrate = MaterialHelper.CreateMaterial(new Color(0.42f, 0.34f, 0.24f), 0.2f);

        for (int i = 0; i < cratePositions.Length; i++)
        {
            GameObject crateGroup = new GameObject("SupplyCrates_" + i);
            crateGroup.transform.position = cratePositions[i];
            crateGroup.transform.SetParent(parent);

            // Crate 1
            GameObject c1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c1.name = "Crate_A";
            c1.transform.SetParent(crateGroup.transform);
            c1.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            c1.transform.localScale = new Vector3(1.15f, 1.1f, 1.15f);
            c1.GetComponent<Renderer>().material = oliveCrate;

            // Crate 2
            GameObject c2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c2.name = "Crate_B";
            c2.transform.SetParent(crateGroup.transform);
            c2.transform.localPosition = new Vector3(1.15f, 0.5f, 0.1f);
            c2.transform.localScale = new Vector3(1.05f, 1.0f, 1.05f);
            c2.GetComponent<Renderer>().material = woodCrate;

            // Crate 3 (stacked)
            GameObject c3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c3.name = "Crate_Top";
            c3.transform.SetParent(crateGroup.transform);
            c3.transform.localPosition = new Vector3(0.55f, 1.55f, 0.05f);
            c3.transform.localScale = new Vector3(0.95f, 0.9f, 0.95f);
            c3.GetComponent<Renderer>().material = oliveCrate;
        }
    }

    public static void EquipTankHeadlights()
    {
        // Add dual spotlights to all tanks for cinematic illumination
        Complete.TankMovement[] compTanks = FindObjectsOfType<Complete.TankMovement>();
        foreach (var t in compTanks)
        {
            AddHeadlightsToTank(t.gameObject);
        }

        TankMovement[] baseTanks = FindObjectsOfType<TankMovement>();
        foreach (var t in baseTanks)
        {
            AddHeadlightsToTank(t.gameObject);
        }
    }

    private static void AddHeadlightsToTank(GameObject tank)
    {
        if (tank.transform.Find("HeadlightLeft") != null) return;

        // Left Headlight
        GameObject leftLight = new GameObject("HeadlightLeft");
        leftLight.transform.SetParent(tank.transform);
        leftLight.transform.localPosition = new Vector3(-0.55f, 0.7f, 1.2f);
        leftLight.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        Light l1 = leftLight.AddComponent<Light>();
        l1.type = LightType.Spot;
        l1.color = new Color(1f, 0.95f, 0.8f);
        l1.range = 14f;
        l1.spotAngle = 45f;
        l1.intensity = 2f;

        // Right Headlight
        GameObject rightLight = new GameObject("HeadlightRight");
        rightLight.transform.SetParent(tank.transform);
        rightLight.transform.localPosition = new Vector3(0.55f, 0.7f, 1.2f);
        rightLight.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        Light l2 = rightLight.AddComponent<Light>();
        l2.type = LightType.Spot;
        l2.color = new Color(1f, 0.95f, 0.8f);
        l2.range = 14f;
        l2.spotAngle = 45f;
        l2.intensity = 2f;
    }
}

public class BlinkingLight : MonoBehaviour
{
    private Light m_Light;
    private void Start() { m_Light = GetComponent<Light>(); }
    private void Update()
    {
        if (m_Light != null)
        {
            m_Light.enabled = (Mathf.FloorToInt(Time.time * 2.5f) % 2 == 0);
        }
    }
}

public class ContinuousRotation : MonoBehaviour
{
    public float m_Speed = 40f;
    private void Update()
    {
        transform.Rotate(Vector3.up, m_Speed * Time.deltaTime, Space.World);
    }
}
