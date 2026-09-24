using UnityEngine;

public class ShellExplosion : MonoBehaviour
{
    public LayerMask m_TankMask;
    public ParticleSystem m_ExplosionParticles;       
    public AudioSource m_ExplosionAudio;              
    public float m_MaxDamage = 100f;                  
    public float m_ExplosionForce = 1000f;            
    public float m_MaxLifeTime = 2f;                  
    public float m_ExplosionRadius = 5f;              

    [HideInInspector] public GameObject m_Shooter;
    private float m_SpawnTime;

    private void Awake()
    {
        m_SpawnTime = Time.time;
        if (m_ExplosionParticles != null)
        {
            MaterialHelper.FixParticleSystem(m_ExplosionParticles, new Color(1f, 0.6f, 0.1f, 0.95f));
        }
    }

    private void Start()
    {
        Destroy(gameObject, m_MaxLifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // If the shell collided directly with the shooter's own body right at the barrel exit, ignore it
        if (m_Shooter != null && (other.gameObject == m_Shooter || other.transform.IsChildOf(m_Shooter.transform)))
        {
            return;
        }

        // Track tanks already damaged by this explosion so compound colliders don't double-hit
        System.Collections.Generic.HashSet<GameObject> affectedTanks = new System.Collections.Generic.HashSet<GameObject>();

        // Collect all colliders in the blast radius (covers direct hits, walls, ground, and obstacles)
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;

            TankHealth targetHealth = colliders[i].GetComponentInParent<TankHealth>();
            Complete.TankHealth compHealth = colliders[i].GetComponentInParent<Complete.TankHealth>();

            GameObject tankObj = null;
            if (targetHealth != null) tankObj = targetHealth.gameObject;
            else if (compHealth != null) tankObj = compHealth.gameObject;

            if (tankObj != null)
            {
                if (affectedTanks.Contains(tankObj))
                    continue;
                affectedTanks.Add(tankObj);

                Rigidbody targetRigitbody = tankObj.GetComponent<Rigidbody>();
                if (targetRigitbody != null)
                {
                    // Cancel any knockback/recoil so the tank stays firmly in place
                    targetRigitbody.velocity = Vector3.zero;
                    targetRigitbody.angularVelocity = Vector3.zero;
                }

                // Calculate damage based on the closest point of the tank to the explosion impact point
                Vector3 closestPoint = colliders[i].ClosestPoint(transform.position);
                float damage = CalculateDamage(closestPoint);

                if (damage > 0f)
                {
                    if (targetHealth != null) targetHealth.TakeDamage(damage);
                    else if (compHealth != null) compHealth.TakeDamage(damage);
                }
            }
        }

        // Damage nearby enemy turrets and barrels in radius
        for (int j = 0; j < colliders.Length; j++)
        {
            if (colliders[j] == null) continue;

            EnemyTurret turret = colliders[j].GetComponentInParent<EnemyTurret>();
            if (turret != null)
            {
                float dist = Vector3.Distance(transform.position, turret.transform.position);
                float dmg = Mathf.Max(0f, (1f - dist / m_ExplosionRadius) * m_MaxDamage);
                turret.TakeDamage(dmg);
            }

            ExplosiveBarrel barrel = colliders[j].GetComponentInParent<ExplosiveBarrel>();
            if (barrel != null)
            {
                barrel.Explode();
            }
        }

        if (m_ExplosionParticles != null)
        {
            m_ExplosionParticles.transform.parent = null;
            MaterialHelper.FixParticleSystem(m_ExplosionParticles, new Color(1f, 0.6f, 0.1f, 0.95f));
            m_ExplosionParticles.Play();
            ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;
            Destroy(m_ExplosionParticles.gameObject, mainModule.duration);
        }

        if (m_ExplosionAudio != null)
        {
            m_ExplosionAudio.Play();
        }

        Destroy(gameObject);
    }


    private float CalculateDamage(Vector3 targetPosition)
    {
        // Calculate distance from explosion to target's surface point
        float explosionDistance = Vector3.Distance(transform.position, targetPosition);

        // If outside radius, no damage
        if (explosionDistance > m_ExplosionRadius)
            return 0f;

        // Calculate the proportion of the maximum distance the target is away.
        float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;

        // Calculate damage scaled with distance, ensuring a solid minimum splash of 15 HP
        float damage = Mathf.Max(15f, relativeDistance * m_MaxDamage);
        return damage;
    }
}