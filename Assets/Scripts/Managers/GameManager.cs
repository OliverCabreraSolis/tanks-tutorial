using UnityEngine;
using System.Collections;
//using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int m_NumRoundsToWin = 5;        
    public float m_StartDelay = 3f;         
    public float m_EndDelay = 3f;           
    public CameraControl m_CameraControl;   
    public Text m_MessageText;              
    public GameObject m_TankPrefab;         
    public TankManager[] m_Tanks;           


    private int m_RoundNumber;              
    private WaitForSeconds m_StartWait;     
    private WaitForSeconds m_EndWait;       
    private TankManager m_RoundWinner;
    private TankManager m_GameWinner;       

    // --- Variables de UI ---
    private int m_NumPlayers = 4;
    private Color[] m_AvailableColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan };
    private int[] m_SelectedColors = { 0, 1, 2, 3 };
    private int[] m_SelectedTypes = { 0, 0, 0, 0 };
    private string[] m_TypeNames = { "Normal", "Rapido", "Pesado" };
    private bool m_MenuOpen = true;


    private void Start()
    {
        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);
        // El juego ahora espera a que el jugador configure la partida
    }

    private void OnGUI()
    {
        if (!m_MenuOpen) return;

        int width = 400;
        int height = 350;
        GUILayout.BeginArea(new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height), GUI.skin.box);
        GUILayout.Label("Configuracion de Partida", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20 });

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Jugadores: " + m_NumPlayers);
        if (GUILayout.Button("-", GUILayout.Width(30))) m_NumPlayers = Mathf.Max(2, m_NumPlayers - 1);
        if (GUILayout.Button("+", GUILayout.Width(30))) m_NumPlayers = Mathf.Min(4, m_NumPlayers + 1);
        GUILayout.EndHorizontal();

        for (int i = 0; i < m_NumPlayers; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Jugador " + (i + 1), GUILayout.Width(80));
            if (GUILayout.Button("Color: " + GetColorName(m_SelectedColors[i]), GUILayout.Width(120)))
            {
                m_SelectedColors[i] = (m_SelectedColors[i] + 1) % m_AvailableColors.Length;
            }
            if (GUILayout.Button("Tipo: " + m_TypeNames[m_SelectedTypes[i]], GUILayout.Width(120)))
            {
                m_SelectedTypes[i] = (m_SelectedTypes[i] + 1) % m_TypeNames.Length;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(20);
        if (GUILayout.Button("¡INICIAR JUEGO!", GUILayout.Height(50)))
        {
            m_MenuOpen = false;
            StartGame();
        }
        GUILayout.EndArea();
    }

    private string GetColorName(int index) {
        switch(index) {
            case 0: return "Rojo";
            case 1: return "Azul";
            case 2: return "Verde";
            case 3: return "Amarillo";
            case 4: return "Magenta";
            case 5: return "Cyan";
            default: return "Otro";
        }
    }

    private void StartGame()
    {
        TankManager[] newTanks = new TankManager[m_NumPlayers];
        for (int i = 0; i < m_NumPlayers; i++) {
            newTanks[i] = new TankManager();
            newTanks[i].m_PlayerColor = m_AvailableColors[m_SelectedColors[i]];
            newTanks[i].m_PlayerNumber = i + 1;
            
            if (i < m_Tanks.Length && m_Tanks[i] != null && m_Tanks[i].m_SpawnPoint != null) {
                newTanks[i].m_SpawnPoint = m_Tanks[i].m_SpawnPoint;
            } else {
                GameObject sp = new GameObject("SpawnPoint" + (i + 1));
                sp.transform.position = new Vector3(Mathf.Cos(i * Mathf.PI/2) * 10, 0, Mathf.Sin(i * Mathf.PI/2) * 10);
                sp.transform.rotation = Quaternion.LookRotation(Vector3.zero - sp.transform.position);
                newTanks[i].m_SpawnPoint = sp.transform;
            }
        }
        m_Tanks = newTanks;

        SpawnAllTanks();
        ApplyTankTypes();
        SetCameraTargets();

        StartCoroutine(GameLoop());
    }

    private void ApplyTankTypes()
    {
        for (int i = 0; i < m_NumPlayers; i++) {
            TankMovement movement = m_Tanks[i].m_Instance.GetComponent<TankMovement>();
            TankHealth health = m_Tanks[i].m_Instance.GetComponent<TankHealth>();
            TankShooting shooting = m_Tanks[i].m_Instance.GetComponent<TankShooting>();

            int type = m_SelectedTypes[i];
            if (type == 1) { // Rapido
                movement.m_Speed = 18f; 
                health.m_StartingHealth = 70f; 
            } else if (type == 2) { // Pesado
                movement.m_Speed = 8f;
                health.m_StartingHealth = 150f;
                shooting.m_MaxLaunchForce = 40f; 
            }
        }
    }


    private void SpawnAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].m_Instance =
                Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
            m_Tanks[i].m_PlayerNumber = i + 1;
            m_Tanks[i].Setup();
        }
    }


    private void SetCameraTargets()
    {
        Transform[] targets = new Transform[m_Tanks.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            targets[i] = m_Tanks[i].m_Instance.transform;
        }

        m_CameraControl.m_Targets = targets;
    }


    private IEnumerator GameLoop()
    {
        yield return StartCoroutine(RoundStarting());
        yield return StartCoroutine(RoundPlaying());
        yield return StartCoroutine(RoundEnding());

        if (m_GameWinner != null)
        {
            Application.LoadLevel(Application.loadedLevel);
        }
        else
        {
            StartCoroutine(GameLoop());
        }
   }


    private IEnumerator RoundStarting()
    {
        ResetAllTanks();
        DisableTankControl();

        m_CameraControl.SetStartPositionAndSize();

        m_RoundNumber++;
        m_MessageText.text = "ROUND" + m_RoundNumber;

        yield return m_StartWait;
    }


    private IEnumerator RoundPlaying()
    {
        EnableTankControl();
        m_MessageText.text = string.Empty;

        while(!OneTankLeft())
        {
            yield return null;
        }
    }


    private IEnumerator RoundEnding()
    {
        DisableTankControl();

        m_RoundWinner = null;

        m_RoundWinner = GetRoundWinner();

        if (m_RoundWinner != null)
            m_RoundWinner.m_Wins++;

        m_GameWinner = GetGameWinner();

        string message = EndMessage();
        m_MessageText.text = message;    

        yield return m_EndWait;
    }


    private bool OneTankLeft()
    {
        int numTanksLeft = 0;

        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Instance.activeSelf)
                numTanksLeft++;
        }

        return numTanksLeft <= 1;
    }


    private TankManager GetRoundWinner()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Instance.activeSelf)
                return m_Tanks[i];
        }

        return null;
    }


    private TankManager GetGameWinner()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                return m_Tanks[i];
        }

        return null;
    }


    private string EndMessage()
    {
        string message = "DRAW!";

        if (m_RoundWinner != null)
            message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

        message += "\n\n\n\n";

        for (int i = 0; i < m_Tanks.Length; i++)
        {
            message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Wins + " WINS\n";
        }

        if (m_GameWinner != null)
            message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

        return message;
    }


    private void ResetAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].Reset();
        }
    }


    private void EnableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].EnableControl();
        }
    }


    private void DisableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].DisableControl();
        }
    }
}