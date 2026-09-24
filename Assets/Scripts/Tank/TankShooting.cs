using UnityEngine;
using UnityEngine.UI;

public class TankShooting : MonoBehaviour
{
    public int m_PlayerNumber = 1;     
    public Rigidbody m_Shell;           
    public Transform m_FireTransform;   
    public Slider m_AimSlider;          
    public AudioSource m_ShootingAudio; 
    public AudioClip m_ChargingClip;   
    public AudioClip m_FireClip;        
    public float m_MinLaunchForce = 15f;
    public float m_MaxLaunchForce = 30f;
    public float m_MaxChargeTime = 0.75f;

    private string m_FireButton;       
    private float m_CurrentLaunchForce; 
    private float m_ChargeSpeed;        
    private bool m_Fired;              

    private LineRenderer m_LaserSight;

    private void OnEnable() 
    {
        m_CurrentLaunchForce = m_MinLaunchForce;
        m_AimSlider.value = m_MinLaunchForce;
        if (m_LaserSight != null) m_LaserSight.enabled = true;
    }

    private void OnDisable()
    {
        if (m_LaserSight != null) m_LaserSight.enabled = false;
    }

    private void InitLaserSight()
    {
        if (m_LaserSight != null || m_FireTransform == null) return;

        GameObject laserObj = new GameObject("LaserSight");
        laserObj.transform.SetParent(m_FireTransform, false);
        laserObj.transform.localPosition = Vector3.zero;
        laserObj.transform.localRotation = Quaternion.identity;

        m_LaserSight = laserObj.AddComponent<LineRenderer>();
        m_LaserSight.startWidth = 0.05f;
        m_LaserSight.endWidth = 0.02f;
        m_LaserSight.positionCount = 2;
        m_LaserSight.useWorldSpace = true;
        m_LaserSight.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_LaserSight.receiveShadows = false;

        Color laserCol = new Color(1f, 0.18f, 0.18f, 0.85f);
        m_LaserSight.material = MaterialHelper.CreateMaterial(laserCol, 0f, true);
        m_LaserSight.startColor = laserCol;
        m_LaserSight.endColor = new Color(1f, 0.2f, 0.2f, 0.2f);
    }

    private void UpdateLaserSight()
    {
        if (m_LaserSight == null)
        {
            InitLaserSight();
            if (m_LaserSight == null) return;
        }

        if (!gameObject.activeInHierarchy || !enabled)
        {
            m_LaserSight.enabled = false;
            return;
        }

        m_LaserSight.enabled = true;
        Vector3 startPos = m_FireTransform.position;
        Vector3 dir = m_FireTransform.forward;
        float maxDist = 32f;
        Vector3 endPos = startPos + dir * maxDist;

        Ray ray = new Ray(startPos + dir * 0.5f, dir);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDist, ~LayerMask.GetMask("UI")))
        {
            if (!hit.transform.IsChildOf(transform))
            {
                endPos = hit.point;
            }
        }

        m_LaserSight.SetPosition(0, startPos);
        m_LaserSight.SetPosition(1, endPos);
    }

    private void Start()
    {
        m_FireButton = "Fire" + m_PlayerNumber; 
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        InitLaserSight();
    }

    private bool GetFireDown()
    {
        if (m_PlayerNumber <= 2) return Input.GetButtonDown(m_FireButton);
        if (m_PlayerNumber == 3) return Input.GetKeyDown(KeyCode.RightShift);
        if (m_PlayerNumber == 4) return Input.GetKeyDown(KeyCode.KeypadEnter);
        return false;
    }
    
    private bool GetFire()
    {
        if (m_PlayerNumber <= 2) return Input.GetButton(m_FireButton);
        if (m_PlayerNumber == 3) return Input.GetKey(KeyCode.RightShift);
        if (m_PlayerNumber == 4) return Input.GetKey(KeyCode.KeypadEnter);
        return false;
    }

    private bool GetFireUp()
    {
        if (m_PlayerNumber <= 2) return Input.GetButtonUp(m_FireButton);
        if (m_PlayerNumber == 3) return Input.GetKeyUp(KeyCode.RightShift);
        if (m_PlayerNumber == 4) return Input.GetKeyUp(KeyCode.KeypadEnter);
        return false;
    }

    private void Update()
    {
        UpdateLaserSight();

        // Track the current state of the fire button and make decisions based on the current launch force.
        m_AimSlider.value = m_MinLaunchForce;

        if(m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
        {
            m_CurrentLaunchForce = m_MaxLaunchForce;
            Fire();
        }else if(GetFireDown())
        {
            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;

            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();
        }else if(GetFire() && !m_Fired)
        {
            m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
            m_AimSlider.value = m_CurrentLaunchForce;
        }else if(GetFireUp() && !m_Fired)
        {
            Fire();
        }
    }

    public int m_TripleShotCount = 0;

    public void EnableTripleShot(int count = 3)
    {
        m_TripleShotCount = count;
        FloatingCombatText.Spawn(transform.position, "¡TRIPLE CAÑÓN!", Color.yellow, 1.2f);
    }

    private void Fire()
    {
        m_Fired = true;

        Collider[] tankColliders = GetComponentsInChildren<Collider>();
        float forwardOffset = 1.0f;

        if (m_TripleShotCount > 0)
        {
            m_TripleShotCount--;

            // Center shell
            Vector3 centerPos = m_FireTransform.position + m_FireTransform.forward * forwardOffset;
            Rigidbody shellCenter = Instantiate(m_Shell, centerPos, m_FireTransform.rotation);
            shellCenter.velocity = m_CurrentLaunchForce * m_FireTransform.forward;
            SetupShell(shellCenter, tankColliders);

            // Left shell (-15 deg)
            Quaternion leftRot = m_FireTransform.rotation * Quaternion.Euler(0, -15f, 0);
            Vector3 leftPos = m_FireTransform.position + (leftRot * Vector3.forward) * forwardOffset;
            Rigidbody shellLeft = Instantiate(m_Shell, leftPos, leftRot);
            shellLeft.velocity = m_CurrentLaunchForce * (leftRot * Vector3.forward);
            SetupShell(shellLeft, tankColliders);

            // Right shell (+15 deg)
            Quaternion rightRot = m_FireTransform.rotation * Quaternion.Euler(0, 15f, 0);
            Vector3 rightPos = m_FireTransform.position + (rightRot * Vector3.forward) * forwardOffset;
            Rigidbody shellRight = Instantiate(m_Shell, rightPos, rightRot);
            shellRight.velocity = m_CurrentLaunchForce * (rightRot * Vector3.forward);
            SetupShell(shellRight, tankColliders);

            // Prevent collision between the 3 shells so they do not detonate together
            Collider cC = shellCenter.GetComponent<Collider>();
            Collider cL = shellLeft.GetComponent<Collider>();
            Collider cR = shellRight.GetComponent<Collider>();
            if (cC != null && cL != null) Physics.IgnoreCollision(cC, cL, true);
            if (cC != null && cR != null) Physics.IgnoreCollision(cC, cR, true);
            if (cL != null && cR != null) Physics.IgnoreCollision(cL, cR, true);
        }
        else
        {
            Vector3 spawnPos = m_FireTransform.position + m_FireTransform.forward * forwardOffset;
            Rigidbody shellInstance = Instantiate (m_Shell, spawnPos, m_FireTransform.rotation);
            shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;
            SetupShell(shellInstance, tankColliders);
        }

        m_ShootingAudio.clip = m_FireClip;
        m_ShootingAudio.Play();

        m_CurrentLaunchForce = m_MinLaunchForce;
    }

    private void SetupShell(Rigidbody shellRb, Collider[] tankColliders)
    {
        if (shellRb == null) return;
        Collider shellCol = shellRb.GetComponent<Collider>();
        if (shellCol != null && tankColliders != null)
        {
            for (int i = 0; i < tankColliders.Length; i++)
            {
                if (tankColliders[i] != null)
                    Physics.IgnoreCollision(shellCol, tankColliders[i], true);
            }
        }

        ShellExplosion exp = shellRb.GetComponent<ShellExplosion>();
        if (exp != null)
        {
            exp.m_Shooter = gameObject;
        }
    }
}