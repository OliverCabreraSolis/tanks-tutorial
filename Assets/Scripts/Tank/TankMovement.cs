using UnityEngine;
using UnityEngine.SceneManagement;


public class TankMovement : MonoBehaviour
{
    public int m_PlayerNumber = 1; // (Degisken)       
    public float m_Speed = 9.5f; // (Degisken)           
    public float m_TurnSpeed = 145f; // (Degisken)      
    public AudioSource m_MovementAudio; //AudioSource bileseni  
    public AudioClip m_EngineIdling; //A.Clip bileseni      
    public AudioClip m_EngineDriving; //A.Clip bileseni     
    public float m_PitchRange = 0.2f; // (Degisken)

    private float m_SpeedMultiplier = 1f;
    private float m_SpeedBoostTimer = 0f;

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        m_SpeedMultiplier = multiplier;
        m_SpeedBoostTimer = duration;
    }

    
    private string m_MovementAxisName; // (Degisken)    
    private string m_TurnAxisName; // (Degisken)       
    private Rigidbody m_Rigidbody; // Rigidbody bileseni        
    private float m_MovementInputValue; // (Degisken)   
    private float m_TurnInputValue; // (Degisken)       
    private float m_OriginalPitch; // (Degisken)        


    private void Awake() //Start fonksiyonundan önce calisan bir fonksiyondur
    {
        m_Rigidbody = GetComponent<Rigidbody>(); //Scriptin yuklendigi objenin Rigidbody bilesenini m_Rigidbody degiskenine tanimlar
    }


    private void OnEnable () //Obje etkin oldugunda calisicak fonksiyon
    {
        m_Rigidbody.isKinematic = false; //Objenin fizik motoru tarafindan algilanmasini saglar
        m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        m_MovementInputValue = 0f; //Degiskene 0 float degeri tanimlanmis
        m_TurnInputValue = 0f; //Degiskene 0 float degeri tanimlanmis

        ParticleSystem[] psList = GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < psList.Length; i++)
        {
            MaterialHelper.FixParticleSystem(psList[i], new Color(0.85f, 0.8f, 0.72f, 0.45f));
            psList[i].Play();
        }
    }


    private void OnDisable () //Obje kapali oldugunda calisicak fonksiyon
    {
        m_Rigidbody.isKinematic = true; //Objenin fizik motoru tarafindan algilanmamasini saglar
    }


    private void Start()
    {
        m_MovementAxisName = "Vertical" + m_PlayerNumber;
        m_TurnAxisName = "Horizontal" + m_PlayerNumber;

        m_OriginalPitch = m_MovementAudio.pitch; //Ses ayari
    }


    private void Update()
    {
        // Fuerza a que las físicas NUNCA se apaguen
        m_Rigidbody.isKinematic = false;

        if (m_SpeedBoostTimer > 0f)
        {
            m_SpeedBoostTimer -= Time.deltaTime;
            if (m_SpeedBoostTimer <= 0f)
            {
                m_SpeedMultiplier = 1f;
            }
        }

        if (m_PlayerNumber <= 2)
        {
            m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
            m_TurnInputValue = Input.GetAxis(m_TurnAxisName);
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

        EngineAudio();
    }

    private void EngineAudio()
    {
        // Play the correct audio clip based on whether or not the tank is moving and what audio is currently playing.
        if(Mathf.Abs (m_MovementInputValue) < 0.1f && Mathf.Abs (m_TurnInputValue) < 0.1f)
        {
            if(m_MovementAudio.clip == m_EngineDriving)
            {
                m_MovementAudio.clip = m_EngineIdling;
                m_MovementAudio.pitch = Random.Range (m_OriginalPitch - m_PitchRange , m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
        else
        {
            if(m_MovementAudio.clip == m_EngineIdling)
            {
                m_MovementAudio.clip = m_EngineDriving;
                m_MovementAudio.pitch = Random.Range (m_OriginalPitch - m_PitchRange , m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
    }


    private void FixedUpdate()
    {
        // Zero angular velocity to prevent uncontrolled spin
        m_Rigidbody.angularVelocity = Vector3.zero;

        // Move and turn the tank.
        Move();
        Turn();
    }


    private void Move()
    {
        Vector3 velocity = transform.forward * m_MovementInputValue * (m_Speed * m_SpeedMultiplier);
        velocity.y = m_Rigidbody.velocity.y;
        m_Rigidbody.velocity = velocity;
    }




    private void Turn()
    {
        float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime; //turn degiskeni olusuturulup icerisine "m_TurnInputValue * m_TurnSpeed * Time.deltaTime" degerlerinin carpimi atanmis

        //Hedef obje kendi etrafinda donme hareketi sagladigi icin rotation (Y ekseni) ile donme islemi saglar bunu da Vectorler ile degil Quaternion.Euler ile saglariz
        Quaternion turnRotation = Quaternion.Euler (0f, turn , 0f); 
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
    }
}