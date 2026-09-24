using UnityEngine;

namespace Complete
{
    public class TankMovement : MonoBehaviour
    {
        public int m_PlayerNumber = 1;              // Used to identify which tank belongs to which player.  This is set by this tank's manager.
        public float m_Speed = 9.5f;                 // How fast the tank moves forward and back (calibrado a 9.5f para control táctico).
        public float m_TurnSpeed = 145f;            // How fast the tank turns in degrees per second.
        public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
        public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
        public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
		public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.

        private string m_MovementAxisName;          // The name of the input axis for moving forward and back.
        private string m_TurnAxisName;              // The name of the input axis for turning.
        private Rigidbody m_Rigidbody;              // Reference used to move the tank.
        private float m_MovementInputValue;         // The current value of the movement input.
        private float m_TurnInputValue;             // The current value of the turn input.
        private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
        private ParticleSystem[] m_particleSystems; // References to all the particles systems used by the Tanks

        private void Awake ()
        {
            m_Rigidbody = GetComponent<Rigidbody> ();
        }


        private void OnEnable ()
        {
            // When the tank is turned on, make sure it's not kinematic and freeze X/Z rotation.
            m_Rigidbody.isKinematic = false;
            m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // Also reset the input values.
            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;

            // We grab all the Particle systems child of that Tank to be able to Stop/Play them on Deactivate/Activate
            // It is needed because we move the Tank when spawning it, and if the Particle System is playing while we do that
            // it "think" it move from (0,0,0) to the spawn point, creating a huge trail of smoke
            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                MaterialHelper.FixParticleSystem(m_particleSystems[i], new Color(0.85f, 0.8f, 0.72f, 0.45f));
                m_particleSystems[i].Play();
            }
        }


        private void OnDisable ()
        {
            // When the tank is turned off, set it to kinematic so it stops moving.
            m_Rigidbody.isKinematic = true;

            // Stop all particle system so it "reset" it's position to the actual one instead of thinking we moved when spawning
            for(int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Stop();
            }
        }


        private void Start ()
        {
            // The axes names are based on player number.
            m_MovementAxisName = "Vertical" + m_PlayerNumber;
            m_TurnAxisName = "Horizontal" + m_PlayerNumber;

            // Store the original pitch of the audio source.
            m_OriginalPitch = m_MovementAudio.pitch;
        }


        public bool m_IsBot = false;
        private float m_SpeedMultiplier = 1f;
        private float m_SpeedBoostTimer = 0f;

        public void ApplySpeedBoost(float multiplier, float duration)
        {
            m_SpeedMultiplier = multiplier;
            m_SpeedBoostTimer = duration;
        }

        public void SetBotInput(float movement, float turn)
        {
            m_MovementInputValue = movement;
            m_TurnInputValue = turn;
        }

        private void Update ()
        {
            if (m_SpeedBoostTimer > 0f)
            {
                m_SpeedBoostTimer -= Time.deltaTime;
                if (m_SpeedBoostTimer <= 0f)
                {
                    m_SpeedMultiplier = 1f;
                }
            }

            if (!m_IsBot)
            {
                if (m_PlayerNumber == 1)
                {
                    float move = 0f;
                    float turn = 0f;
                    if (Input.GetKey(KeyCode.W)) move += 1f;
                    if (Input.GetKey(KeyCode.S)) move -= 1f;
                    if (Input.GetKey(KeyCode.D)) turn += 1f;
                    if (Input.GetKey(KeyCode.A)) turn -= 1f;

                    if (Mathf.Approximately(move, 0f)) move = Input.GetAxisRaw(m_MovementAxisName);
                    if (Mathf.Approximately(turn, 0f)) turn = Input.GetAxisRaw(m_TurnAxisName);

                    m_MovementInputValue = move;
                    m_TurnInputValue = turn;
                }
                else if (m_PlayerNumber == 2)
                {
                    float move = 0f;
                    float turn = 0f;
                    if (Input.GetKey(KeyCode.UpArrow)) move += 1f;
                    if (Input.GetKey(KeyCode.DownArrow)) move -= 1f;
                    if (Input.GetKey(KeyCode.RightArrow)) turn += 1f;
                    if (Input.GetKey(KeyCode.LeftArrow)) turn -= 1f;

                    if (Mathf.Approximately(move, 0f)) move = Input.GetAxisRaw(m_MovementAxisName);
                    if (Mathf.Approximately(turn, 0f)) turn = Input.GetAxisRaw(m_TurnAxisName);

                    m_MovementInputValue = move;
                    m_TurnInputValue = turn;
                }
                else if (m_PlayerNumber == 3)
                {
                    m_MovementInputValue = (Input.GetKey(KeyCode.I) ? 1f : 0f) - (Input.GetKey(KeyCode.K) ? 1f : 0f);
                    m_TurnInputValue = (Input.GetKey(KeyCode.L) ? 1f : 0f) - (Input.GetKey(KeyCode.J) ? 1f : 0f);
                }
                else if (m_PlayerNumber == 4)
                {
                    m_MovementInputValue = (Input.GetKey(KeyCode.Keypad8) ? 1f : 0f) - (Input.GetKey(KeyCode.Keypad5) ? 1f : 0f);
                    m_TurnInputValue = (Input.GetKey(KeyCode.Keypad6) ? 1f : 0f) - (Input.GetKey(KeyCode.Keypad4) ? 1f : 0f);
                }
            }

            EngineAudio ();
        }


        private void EngineAudio ()
        {
            // If there is no input (the tank is stationary)...
            if (Mathf.Abs (m_MovementInputValue) < 0.1f && Mathf.Abs (m_TurnInputValue) < 0.1f)
            {
                // ... and if the audio source is currently playing the driving clip...
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    // ... change the clip to idling and play it.
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range (m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play ();
                }
            }
            else
            {
                // Otherwise if the tank is moving and if the idling clip is currently playing...
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    // ... change the clip to driving and play.
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }


        private void FixedUpdate ()
        {
            // Zero any residual angular velocity from impacts to prevent crazy spinning
            m_Rigidbody.angularVelocity = Vector3.zero;

            // Adjust the rigidbodies position and orientation in FixedUpdate.
            Move ();
            Turn ();
        }


        private void Move ()
        {
            // Apply horizontal velocity aligned with tank forward direction, preserving vertical gravity for smooth slope following
            Vector3 velocity = transform.forward * m_MovementInputValue * (m_Speed * m_SpeedMultiplier);
            velocity.y = m_Rigidbody.velocity.y;
            m_Rigidbody.velocity = velocity;
        }


        private void Turn ()
        {
            // Determine the number of degrees to be turned based on the input, speed and fixedDeltaTime.
            float turn = m_TurnInputValue * m_TurnSpeed * Time.fixedDeltaTime;

            // Make this into a rotation in the y axis.
            Quaternion turnRotation = Quaternion.Euler (0f, turn, 0f);

            // Apply this rotation to the rigidbody's rotation.
            m_Rigidbody.MoveRotation (m_Rigidbody.rotation * turnRotation);
        }
    }
}