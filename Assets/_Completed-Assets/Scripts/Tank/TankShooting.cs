using UnityEngine;
using UnityEngine.UI;

namespace Complete
{
    public class TankShooting : MonoBehaviour
    {
        public int m_PlayerNumber = 1;              // Used to identify the different players.
        public Rigidbody m_Shell;                   // Prefab of the shell.
        public Transform m_FireTransform;           // A child of the tank where the shells are spawned.
        public Slider m_AimSlider;                  // A child of the tank that displays the current launch force.
        public AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        public AudioClip m_ChargingClip;            // Audio that plays when each shot is charging up.
        public AudioClip m_FireClip;                // Audio that plays when each shot is fired.
        public float m_MinLaunchForce = 15f;        // The force given to the shell if the fire button is not held.
        public float m_MaxLaunchForce = 30f;        // The force given to the shell if the fire button is held for the max charge time.
        public float m_MaxChargeTime = 0.75f;       // How long the shell can charge for before it is fired at max force.


        private string m_FireButton;                // The input axis that is used for launching shells.
        private float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
        private float m_ChargeSpeed;                // How fast the launch force increases, based on the max charge time.
        private bool m_Fired;                       // Whether or not the shell has been launched with this button press.


        private LineRenderer m_LaserSight;

        private void OnEnable()
        {
            // When the tank is turned on, reset the launch force and the UI
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

        private void Start ()
        {
            // The fire axis is based on the player number.
            m_FireButton = "Fire" + m_PlayerNumber;

            // The rate that the launch force charges up is the range of possible forces by the max charge time.
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;

            InitLaserSight();
        }

        public bool m_IsBot = false;
        public bool m_BotFire = false;
        private bool m_PrevBotFire = false;

        private bool GetFireDown()
        {
            if (m_IsBot) return m_BotFire && !m_PrevBotFire;
            if (m_PlayerNumber <= 2) return Input.GetButtonDown(m_FireButton);
            if (m_PlayerNumber == 3) return Input.GetKeyDown(KeyCode.RightShift);
            if (m_PlayerNumber == 4) return Input.GetKeyDown(KeyCode.KeypadEnter);
            return false;
        }
        
        private bool GetFire()
        {
            if (m_IsBot) return m_BotFire;
            if (m_PlayerNumber <= 2) return Input.GetButton(m_FireButton);
            if (m_PlayerNumber == 3) return Input.GetKey(KeyCode.RightShift);
            if (m_PlayerNumber == 4) return Input.GetKey(KeyCode.KeypadEnter);
            return false;
        }

        private bool GetFireUp()
        {
            if (m_IsBot) return !m_BotFire && m_PrevBotFire;
            if (m_PlayerNumber <= 2) return Input.GetButtonUp(m_FireButton);
            if (m_PlayerNumber == 3) return Input.GetKeyUp(KeyCode.RightShift);
            if (m_PlayerNumber == 4) return Input.GetKeyUp(KeyCode.KeypadEnter);
            return false;
        }

        private void Update ()
        {
            UpdateLaserSight();

            // Evaluate inputs based on current and previous bot fire state
            bool fireDown = GetFireDown();
            bool fire = GetFire();
            bool fireUp = GetFireUp();
            
            // Save current bot fire state for next frame
            if (m_IsBot) m_PrevBotFire = m_BotFire;
            // The slider should have a default value of the minimum launch force.
            m_AimSlider.value = m_MinLaunchForce;

            // If the max force has been exceeded and the shell hasn't yet been launched...
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                // ... use the max force and launch the shell.
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire ();
            }
            // Otherwise, if the fire button has just started being pressed...
            else if (fireDown)
            {
                // ... reset the fired flag and reset the launch force.
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;

                // Change the clip to the charging clip and start it playing.
                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play ();
            }
            // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
            else if (fire && !m_Fired)
            {
                // Increment the launch force and update the slider.
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;

                m_AimSlider.value = m_CurrentLaunchForce;
            }
            // Otherwise, if the fire button is released and the shell hasn't been launched yet...
            else if (fireUp && !m_Fired)
            {
                // ... launch the shell.
                Fire ();
            }
        }


        public int m_TripleShotCount = 0;

        public void EnableTripleShot(int count = 3)
        {
            m_TripleShotCount = count;
            FloatingCombatText.Spawn(transform.position, "¡TRIPLE CAÑÓN!", Color.yellow, 1.2f);
        }

        private void Fire ()
        {
            // Set the fired flag so only Fire is only called once.
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
                Rigidbody shellInstance = Instantiate (m_Shell, spawnPos, m_FireTransform.rotation) as Rigidbody;
                shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;
                SetupShell(shellInstance, tankColliders);
            }

            // Change the clip to the firing clip and play it.
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play ();

            // Reset the launch force.  This is a precaution in case of missing button events.
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
}