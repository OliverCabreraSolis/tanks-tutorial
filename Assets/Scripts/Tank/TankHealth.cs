using UnityEngine;
using UnityEngine.UI;

public class TankHealth : MonoBehaviour
{
    public float m_StartingHealth = 100f;          
    public Slider m_Slider;                        
    public Image m_FillImage;                      
    public Color m_FullHealthColor = Color.green;  
    public Color m_ZeroHealthColor = Color.red;    
    public GameObject m_ExplosionPrefab;
    
    
    private AudioSource m_ExplosionAudio;          
    private ParticleSystem m_ExplosionParticles;   
    private float m_CurrentHealth;  
    private bool m_Dead;            


    private void Awake()
    {
        m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
        MaterialHelper.FixParticleSystem(m_ExplosionParticles, new Color(1f, 0.55f, 0.1f, 0.95f));
        m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();

        m_ExplosionParticles.gameObject.SetActive(false);
    }


    private void OnEnable()
    {
        m_CurrentHealth = m_StartingHealth;
        m_Dead = false;
        m_HasShield = false;
        if (m_ShieldVisual != null) m_ShieldVisual.SetActive(false);

        SetHealthUI();
    }
    
    public bool m_HasShield = false;
    private GameObject m_ShieldVisual;

    public void ActivateShield()
    {
        m_HasShield = true;
        if (m_ShieldVisual == null)
        {
            m_ShieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_ShieldVisual.name = "PlasmaShield";
            m_ShieldVisual.transform.SetParent(transform);
            m_ShieldVisual.transform.localPosition = new Vector3(0, 0.8f, 0);
            m_ShieldVisual.transform.localScale = new Vector3(2.8f, 2.4f, 2.8f);

            Collider col = m_ShieldVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer r = m_ShieldVisual.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = MaterialHelper.CreateMaterial(new Color(0.1f, 0.8f, 1f, 0.45f), 0.9f, true);
            }
        }
        m_ShieldVisual.SetActive(true);
        FloatingCombatText.Spawn(transform.position, "¡ESCUDO ACTIVADO!", Color.cyan, 1.2f);
    }

    public void Heal(float amount)
    {
        if (m_Dead) return;
        m_CurrentHealth = Mathf.Min(m_StartingHealth, m_CurrentHealth + amount);
        SetHealthUI();
        FloatingCombatText.Spawn(transform.position, "+" + Mathf.RoundToInt(amount) + " HP", Color.green, 1.2f);
    }

    public void TakeDamage(float amount)
    {
        if (m_HasShield)
        {
            m_HasShield = false;
            if (m_ShieldVisual != null) m_ShieldVisual.SetActive(false);
            FloatingCombatText.Spawn(transform.position, "¡ESCUDO BLOQUEADO!", Color.cyan, 1.3f);
            return;
        }

        m_CurrentHealth -= amount;
        FloatingCombatText.Spawn(transform.position, "-" + Mathf.RoundToInt(amount), Color.red, 1f);

        SetHealthUI();

        if(m_CurrentHealth <= 0 && !m_Dead)
        {
            OnDeath();
        }
    }


    private void SetHealthUI()
    {
        // Adjust the value and colour of the slider.
        m_Slider.value = m_CurrentHealth;

        m_FillImage.color = Color.Lerp (m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);

    }


    private void OnDeath()
    {
        // Play the effects for the death of the tank and deactivate it.
        m_Dead = true;

        m_ExplosionParticles.transform.position = transform.position;
        m_ExplosionParticles.gameObject.SetActive(true);

        m_ExplosionParticles.Play();
        m_ExplosionAudio.Play();

        gameObject.SetActive(false);
    }
}