using UnityEngine;

namespace Complete
{
    public class ShellExplosion : MonoBehaviour
    {
        public LayerMask m_TankMask;                        // Used to filter what the explosion affects, this should be set to "Players".
        public ParticleSystem m_ExplosionParticles;         // Reference to the particles that will play on explosion.
        public AudioSource m_ExplosionAudio;                // Reference to the audio that will play on explosion.
        public float m_MaxDamage = 100f;                    // The amount of damage done if the explosion is centred on a tank.
        public float m_ExplosionForce = 1000f;              // The amount of force added to a tank at the centre of the explosion.
        public float m_MaxLifeTime = 2f;                    // The time in seconds before the shell is removed.
        public float m_ExplosionRadius = 5f;                // The maximum distance away from the explosion tanks can be and are still affected.

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

        private void Start ()
        {
            // If it isn't destroyed by then, destroy the shell after it's lifetime.
            Destroy (gameObject, m_MaxLifeTime);
        }


        private void OnTriggerEnter (Collider other)
        {
            // If the shell collided directly with the shooter's own body right at the barrel exit, ignore it
            if (m_Shooter != null && (other.gameObject == m_Shooter || other.transform.IsChildOf(m_Shooter.transform)))
            {
                return;
            }

            // Track tanks already damaged by this explosion so compound colliders don't double-hit
            System.Collections.Generic.HashSet<GameObject> affectedTanks = new System.Collections.Generic.HashSet<GameObject>();

            // Collect all colliders in the blast radius (covers direct hits, walls, ground, and obstacles)
            Collider[] colliders = Physics.OverlapSphere (transform.position, m_ExplosionRadius);

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null) continue;

                // Check for tank health components on the collider or any parent
                TankHealth targetHealth = colliders[i].GetComponentInParent<TankHealth> ();
                global::TankHealth baseHealth = colliders[i].GetComponentInParent<global::TankHealth> ();

                GameObject tankObj = null;
                if (targetHealth != null) tankObj = targetHealth.gameObject;
                else if (baseHealth != null) tankObj = baseHealth.gameObject;

                if (tankObj != null)
                {
                    if (affectedTanks.Contains(tankObj))
                        continue;
                    affectedTanks.Add(tankObj);

                    Rigidbody targetRigidbody = tankObj.GetComponent<Rigidbody> ();
                    if (targetRigidbody != null)
                    {
                        // Cancel any knockback/recoil so the tank stays firmly in place
                        targetRigidbody.velocity = Vector3.zero;
                        targetRigidbody.angularVelocity = Vector3.zero;
                    }

                    // Calculate damage based on the closest point of the tank to the explosion impact point
                    Vector3 closestPoint = colliders[i].ClosestPoint(transform.position);
                    float damage = CalculateDamage (closestPoint);

                    if (damage > 0f)
                    {
                        if (targetHealth != null) targetHealth.TakeDamage (damage);
                        else if (baseHealth != null) baseHealth.TakeDamage (damage);
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
                // Unparent the particles from the shell.
                m_ExplosionParticles.transform.parent = null;

                // Fix particle material so it is never purple
                MaterialHelper.FixParticleSystem(m_ExplosionParticles, new Color(1f, 0.6f, 0.1f, 0.95f));

                // Play the particle system.
                m_ExplosionParticles.Play();

                // Once the particles have finished, destroy the gameobject they are on.
                ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;
                Destroy (m_ExplosionParticles.gameObject, mainModule.duration);
            }

            // Play the explosion sound effect.
            if (m_ExplosionAudio != null)
            {
                m_ExplosionAudio.Play();
            }

            // Destroy the shell.
            Destroy (gameObject);
        }


        private float CalculateDamage (Vector3 targetPosition)
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
}