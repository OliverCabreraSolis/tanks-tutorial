using UnityEngine;

public class EnemyTurret : MonoBehaviour
{
    public float m_StartingHealth = 80f;
    public float m_DetectionRange = 26f;
    public float m_FireCooldown = 3.2f;
    public float m_AimChargeTime = 1.1f;

    private float m_CurrentHealth;
    private Transform m_Head;
    private Transform m_Muzzle;
    private LineRenderer m_Laser;
    private Light m_LaserLight;
    private Transform m_CurrentTarget;
    private float m_StateTimer = 0f;
    private int m_State = 0; // 0 = Scanning, 1 = Locking/Charging, 2 = Cooldown
    private float m_ScanAngle = 0f;
    private Rigidbody m_ShellPrefab;

    public static GameObject Create(Vector3 position, Rigidbody shellPrefab = null)
    {
        GameObject turret = new GameObject("HostileEnemyTurret");
        turret.transform.position = position;

        // Base Pedestal
        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = "Pedestal";
        pedestal.transform.SetParent(turret.transform);
        pedestal.transform.localPosition = new Vector3(0, 0.4f, 0);
        pedestal.transform.localScale = new Vector3(1.8f, 0.4f, 1.8f);

        // Rotating Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "TurretHead";
        head.transform.SetParent(turret.transform);
        head.transform.localPosition = new Vector3(0, 1.1f, 0);
        head.transform.localScale = new Vector3(1.2f, 1.1f, 1.2f);

        // Gun Barrel
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "GunBarrel";
        barrel.transform.SetParent(head.transform);
        barrel.transform.localPosition = new Vector3(0, 0, 0.9f);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        barrel.transform.localScale = new Vector3(0.3f, 0.9f, 0.3f);

        // Muzzle point
        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(head.transform);
        muzzle.transform.localPosition = new Vector3(0, 0, 1.8f);

        // Red Eye / Scanner Dome
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "ScannerEye";
        eye.transform.SetParent(head.transform);
        eye.transform.localPosition = new Vector3(0, 0.5f, 0.3f);
        eye.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);

        // Materials: dark metallic for base/head, glowing red for eye
        Material darkMat = MaterialHelper.CreateMaterial(new Color(0.18f, 0.18f, 0.22f), 0.4f);
        pedestal.GetComponent<Renderer>().material = darkMat;
        head.GetComponent<Renderer>().material = darkMat;
        barrel.GetComponent<Renderer>().material = darkMat;

        Material eyeMat = MaterialHelper.CreateMaterial(new Color(1f, 0.1f, 0.1f), 0.8f);
        eye.GetComponent<Renderer>().material = eyeMat;

        // Laser Sight (LineRenderer)
        LineRenderer lr = head.AddComponent<LineRenderer>();
        lr.startWidth = 0.06f;
        lr.endWidth = 0.06f;
        lr.material = MaterialHelper.CreateMaterial(Color.red, 0f, true);
        lr.startColor = new Color(1f, 0f, 0f, 0.6f);
        lr.endColor = new Color(1f, 0f, 0f, 0.15f);
        lr.positionCount = 2;

        // Laser Light
        GameObject lObj = new GameObject("LaserLight");
        lObj.transform.SetParent(muzzle.transform);
        lObj.transform.localPosition = Vector3.zero;
        Light light = lObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = Color.red;
        light.range = 4f;
        light.intensity = 2f;

        // EnemyTurret script setup
        EnemyTurret comp = turret.AddComponent<EnemyTurret>();
        comp.m_Head = head.transform;
        comp.m_Muzzle = muzzle.transform;
        comp.m_Laser = lr;
        comp.m_LaserLight = light;
        comp.m_ShellPrefab = shellPrefab;

        // Add collider on root for hit detection
        BoxCollider box = turret.AddComponent<BoxCollider>();
        box.center = new Vector3(0, 1f, 0);
        box.size = new Vector3(2f, 2.2f, 2f);

        return turret;
    }

    private void Start()
    {
        m_CurrentHealth = m_StartingHealth;
        if (m_ShellPrefab == null)
        {
            // Find shell from existing TankShooting in scene or resources
            Complete.TankShooting ts = FindObjectOfType<Complete.TankShooting>();
            if (ts != null) m_ShellPrefab = ts.m_Shell;
        }
    }

    private void Update()
    {
        UpdateTargeting();
        UpdateState();
    }

    private void UpdateTargeting()
    {
        // Find nearest active tank (player or bot)
        m_CurrentTarget = null;
        float closestDist = m_DetectionRange;

        // Check Complete tanks
        Complete.TankMovement[] compTanks = FindObjectsOfType<Complete.TankMovement>();
        foreach (var t in compTanks)
        {
            if (t.gameObject.activeSelf)
            {
                float d = Vector3.Distance(transform.position, t.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    m_CurrentTarget = t.transform;
                }
            }
        }

        // Check Base tanks
        if (m_CurrentTarget == null)
        {
            TankMovement[] baseTanks = FindObjectsOfType<TankMovement>();
            foreach (var t in baseTanks)
            {
                if (t.gameObject.activeSelf)
                {
                    float d = Vector3.Distance(transform.position, t.transform.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        m_CurrentTarget = t.transform;
                    }
                }
            }
        }
    }

    private void UpdateState()
    {
        if (m_CurrentTarget != null)
        {
            // Aim head towards target smoothly
            Vector3 targetDir = (m_CurrentTarget.position + Vector3.up * 0.5f) - m_Head.position;
            Quaternion lookRot = Quaternion.LookRotation(targetDir);
            m_Head.rotation = Quaternion.Slerp(m_Head.rotation, lookRot, 5f * Time.deltaTime);

            // Draw laser to target
            if (m_Laser != null && m_Muzzle != null)
            {
                m_Laser.enabled = true;
                m_Laser.SetPosition(0, m_Muzzle.position);
                m_Laser.SetPosition(1, m_CurrentTarget.position + Vector3.up * 0.5f);
            }

            if (m_State == 0) // Target found, start locking on
            {
                m_State = 1;
                m_StateTimer = m_AimChargeTime;
                if (m_Laser != null)
                {
                    m_Laser.startColor = new Color(1f, 0.2f, 0f, 0.9f);
                    m_Laser.startWidth = 0.12f;
                }
            }
            else if (m_State == 1) // Charging shot
            {
                m_StateTimer -= Time.deltaTime;
                // Pulse laser light
                if (m_LaserLight != null)
                {
                    m_LaserLight.intensity = 2f + Mathf.PingPong(Time.time * 15f, 4f);
                }

                if (m_StateTimer <= 0f)
                {
                    Fire();
                    m_State = 2;
                    m_StateTimer = m_FireCooldown;
                    if (m_Laser != null)
                    {
                        m_Laser.startColor = new Color(1f, 0f, 0f, 0.3f);
                        m_Laser.startWidth = 0.05f;
                    }
                }
            }
            else if (m_State == 2) // Cooldown
            {
                m_StateTimer -= Time.deltaTime;
                if (m_StateTimer <= 0f)
                {
                    m_State = 0;
                }
            }
        }
        else
        {
            // Idle scanning rotation
            m_ScanAngle += 45f * Time.deltaTime;
            m_Head.rotation = Quaternion.Euler(0, m_ScanAngle, 0);

            if (m_Laser != null && m_Muzzle != null)
            {
                m_Laser.enabled = true;
                m_Laser.startColor = new Color(1f, 0.3f, 0.3f, 0.3f);
                m_Laser.startWidth = 0.04f;
                m_Laser.SetPosition(0, m_Muzzle.position);
                m_Laser.SetPosition(1, m_Muzzle.position + m_Head.forward * 12f);
            }
            m_State = 0;
        }
    }

    private void Fire()
    {
        if (m_Muzzle == null) return;

        FloatingCombatText.Spawn(transform.position, "¡FUEGO!", Color.red, 1.1f);

        if (m_ShellPrefab != null)
        {
            Rigidbody shell = Instantiate(m_ShellPrefab, m_Muzzle.position, m_Muzzle.rotation);
            shell.velocity = m_Muzzle.forward * 22f;
        }
        else
        {
            // Direct energy projectile fallback
            GameObject energyBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            energyBall.transform.position = m_Muzzle.position;
            energyBall.transform.localScale = Vector3.one * 0.6f;
            Rigidbody rb = energyBall.AddComponent<Rigidbody>();
            rb.velocity = m_Muzzle.forward * 25f;
            energyBall.GetComponent<Renderer>().material.color = Color.red;
            Destroy(energyBall, 2f);
        }
    }

    public void TakeDamage(float amount)
    {
        m_CurrentHealth -= amount;
        FloatingCombatText.Spawn(transform.position, "-" + Mathf.RoundToInt(amount), Color.yellow, 1.2f);

        if (m_CurrentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        FloatingCombatText.Spawn(transform.position, "¡TORRETA DESTRUIDA!", Color.green, 1.5f);

        // Guaranteed Power-Up drop at turret position!
        PowerUpType randomDrop = (PowerUpType)Random.Range(0, 4);
        PowerUpItem.Create(transform.position, randomDrop);

        // Huge explosion particles
        GameObject blast = new GameObject("TurretExplosion");
        blast.transform.position = transform.position + Vector3.up * 1f;
        ParticleSystem ps = blast.AddComponent<ParticleSystem>();
        MaterialHelper.FixParticleSystem(ps, new Color(1f, 0.4f, 0.1f));
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = 0.8f;
        main.startSpeed = 9f;
        main.startSize = 2f;
        main.startColor = new Color(1f, 0.4f, 0.1f);
        var emit = ps.emission;
        emit.rateOverTime = 0;
        emit.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });
        ps.Play();
        Destroy(blast, 2f);

        Destroy(gameObject);
    }
}
