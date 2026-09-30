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
        if (m_FireTransform != null)
        {
            m_FireTransform.localRotation = Quaternion.identity;
        }
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

        m_FireTransform.localRotation = Quaternion.identity;

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
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        else dir.Normalize();

        float maxDist = 45f;
        Vector3 endPos = startPos + dir * maxDist;
        endPos.y = startPos.y;

        Ray ray = new Ray(startPos, dir);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDist, ~LayerMask.GetMask("UI")))
        {
            if (!hit.transform.IsChildOf(transform))
            {
                endPos = hit.point;
                endPos.y = startPos.y;
            }
        }

        m_LaserSight.SetPosition(0, startPos);
        m_LaserSight.SetPosition(1, endPos);
    }

    private void Start()
    {
        m_FireButton = "Fire" + m_PlayerNumber; 
        if (m_MinLaunchForce < 20f) m_MinLaunchForce = 22f;
        if (m_MaxLaunchForce < 35f) m_MaxLaunchForce = 35f;
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;

        if (m_AimSlider != null)
        {
            m_AimSlider.minValue = m_MinLaunchForce;
            m_AimSlider.maxValue = m_MaxLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;
        }

        if (m_FireTransform != null)
        {
            m_FireTransform.localRotation = Quaternion.identity;
        }

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

        Vector3 fireDir = m_FireTransform.forward;
        fireDir.y = 0f;
        if (fireDir.sqrMagnitude < 0.0001f) fireDir = transform.forward;
        else fireDir.Normalize();

        Quaternion fireRot = Quaternion.LookRotation(fireDir, Vector3.up);

        if (m_TripleShotCount > 0)
        {
            m_TripleShotCount--;

            // Center shell
            Vector3 centerPos = m_FireTransform.position + fireDir * forwardOffset;
            centerPos.y = m_FireTransform.position.y;
            Rigidbody shellCenter = Instantiate(m_Shell, centerPos, fireRot);
            shellCenter.velocity = fireDir * m_CurrentLaunchForce;
            SetupShell(shellCenter, tankColliders);

            // Left shell (-15 deg)
            Quaternion leftRot = fireRot * Quaternion.Euler(0, -15f, 0);
            Vector3 leftDir = leftRot * Vector3.forward;
            Vector3 leftPos = m_FireTransform.position + leftDir * forwardOffset;
            leftPos.y = m_FireTransform.position.y;
            Rigidbody shellLeft = Instantiate(m_Shell, leftPos, leftRot);
            shellLeft.velocity = leftDir * m_CurrentLaunchForce;
            SetupShell(shellLeft, tankColliders);

            // Right shell (+15 deg)
            Quaternion rightRot = fireRot * Quaternion.Euler(0, 15f, 0);
            Vector3 rightDir = rightRot * Vector3.forward;
            Vector3 rightPos = m_FireTransform.position + rightDir * forwardOffset;
            rightPos.y = m_FireTransform.position.y;
            Rigidbody shellRight = Instantiate(m_Shell, rightPos, rightRot);
            shellRight.velocity = rightDir * m_CurrentLaunchForce;
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
            Vector3 spawnPos = m_FireTransform.position + fireDir * forwardOffset;
            spawnPos.y = m_FireTransform.position.y;
            Rigidbody shellInstance = Instantiate(m_Shell, spawnPos, fireRot);
            shellInstance.velocity = fireDir * m_CurrentLaunchForce;
            SetupShell(shellInstance, tankColliders);
        }

        m_ShootingAudio.clip = m_FireClip;
        m_ShootingAudio.Play();

        m_CurrentLaunchForce = m_MinLaunchForce;
    }

    private void SetupShell(Rigidbody shellRb, Collider[] tankColliders)
    {
        if (shellRb == null) return;

        shellRb.useGravity = false;
        shellRb.drag = 0f;
        shellRb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

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
        else
        {
            Complete.ShellExplosion compExp = shellRb.GetComponent<Complete.ShellExplosion>();
            if (compExp != null)
            {
                compExp.m_Shooter = gameObject;
            }
        }
    }
}