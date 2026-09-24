using UnityEngine;

public class ExplosiveBarrel : MonoBehaviour
{
    public float m_ExplosionRadius = 6.5f;
    public float m_ExplosionDamage = 70f;
    private bool m_Exploded = false;

    public static GameObject Create(Vector3 position)
    {
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "ExplosiveBarrel";
        barrel.transform.position = position + Vector3.up * 0.75f;
        barrel.transform.localScale = new Vector3(1.1f, 0.75f, 1.1f);

        ExplosiveBarrel comp = barrel.AddComponent<ExplosiveBarrel>();

        // Set dangerous red/black warning color
        Renderer rend = barrel.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = MaterialHelper.CreateMaterial(new Color(0.85f, 0.12f, 0.05f), 0.35f);
        }

        // Add a yellow warning light on top
        GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lamp.name = "WarningLight";
        lamp.transform.SetParent(barrel.transform);
        lamp.transform.localPosition = new Vector3(0, 1.1f, 0);
        lamp.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        Collider lampCol = lamp.GetComponent<Collider>();
        if (lampCol != null) Destroy(lampCol);

        Renderer lampRend = lamp.GetComponent<Renderer>();
        if (lampRend != null)
        {
            lampRend.material = MaterialHelper.CreateMaterial(Color.yellow, 0.9f);
        }

        return barrel;
    }

    public void Explode()
    {
        if (m_Exploded) return;
        m_Exploded = true;

        FloatingCombatText.Spawn(transform.position, "¡BOOM! EXPLOSIÓN", new Color(1f, 0.3f, 0f), 1.5f);

        // Visual flash & particle simulation
        GameObject boomObj = new GameObject("BarrelBlast");
        boomObj.transform.position = transform.position;
        ParticleSystem ps = boomObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        MaterialHelper.FixParticleSystem(ps, new Color(1f, 0.4f, 0.1f));
        ParticleSystemRenderer psR = boomObj.GetComponent<ParticleSystemRenderer>();
        if (psR != null) psR.material = MaterialHelper.CreateParticleMaterial(new Color(1f, 0.4f, 0.1f));

        var main = ps.main;
        main.loop = false;
        main.startLifetime = 0.6f;
        main.startSpeed = 8f;
        main.startSize = 1.8f;
        main.startColor = new Color(1f, 0.4f, 0.1f);
        var emit = ps.emission;
        emit.rateOverTime = 0;
        emit.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });
        ps.Play();
        Destroy(boomObj, 1.5f);

        // Damage tanks & push them
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius);
        foreach (Collider col in colliders)
        {
            if (col.gameObject == gameObject) continue;

            // Damage Complete tank
            Complete.TankHealth compHealth = col.GetComponentInParent<Complete.TankHealth>();
            if (compHealth != null)
            {
                float dist = Vector3.Distance(transform.position, compHealth.transform.position);
                float damage = Mathf.Max(10f, (1f - dist / m_ExplosionRadius) * m_ExplosionDamage);
                compHealth.TakeDamage(damage);
            }

            // Damage Base tank
            TankHealth baseHealth = col.GetComponentInParent<TankHealth>();
            if (baseHealth != null)
            {
                float dist = Vector3.Distance(transform.position, baseHealth.transform.position);
                float damage = Mathf.Max(10f, (1f - dist / m_ExplosionRadius) * m_ExplosionDamage);
                baseHealth.TakeDamage(damage);
            }

            // Chain react other barrels
            ExplosiveBarrel otherBarrel = col.GetComponentInParent<ExplosiveBarrel>();
            if (otherBarrel != null && otherBarrel != this)
            {
                otherBarrel.Invoke("Explode", Random.Range(0.1f, 0.25f));
            }
        }

        Destroy(gameObject);
    }
}
