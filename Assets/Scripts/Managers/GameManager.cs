using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
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
    private Color[] m_AvailableColors = {
        new Color(1f, 0.22f, 0.22f),    // Rojo Carmesí
        new Color(0.18f, 0.55f, 1f),    // Azul Neón
        new Color(0.18f, 0.95f, 0.35f), // Verde Esmeralda
        new Color(1f, 0.85f, 0.15f),    // Amarillo Oro
        new Color(0.95f, 0.2f, 0.85f),  // Magenta Eléctrico
        new Color(0.1f, 0.92f, 0.98f),  // Cian Criogénico
        new Color(1f, 0.52f, 0.1f),     // Naranja Fuego
        new Color(0.72f, 0.4f, 1f)      // Púrpura Real
    };

    private string[] m_ColorNames = {
        "Rojo Carmesí",
        "Azul Neón",
        "Verde Esmeralda",
        "Amarillo Oro",
        "Magenta Eléctrico",
        "Cian Criogénico",
        "Naranja Fuego",
        "Púrpura Real"
    };

    private int[] m_SelectedColors = { 0, 1, 2, 3 };
    private int[] m_SelectedTypes = { 0, 0, 0, 0 };
    private string[] m_TypeNames = { "🎯 Balanceado", "⚡ Rápido", "🛡️ Pesado" };
    private string[] m_TypeStats = {
        "100 HP | Vel: 9.5 | Cañón Estándar",
        "75 HP | Vel: 13.5 | Súper Ágil",
        "150 HP | Vel: 6.5 | Blindaje Alto"
    };
    private bool m_MenuOpen = true;
    private bool m_ShowControlsModal = false;

    private Texture2D m_TexCardBg;
    private Texture2D m_TexInnerBox;

    private Texture2D MakeSolidTexture(int w, int h, Color col)
    {
        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(w, h);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void Start()
    {
        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);
        m_TexCardBg = MakeSolidTexture(2, 2, new Color(0.08f, 0.09f, 0.14f, 0.94f));
        m_TexInnerBox = MakeSolidTexture(2, 2, new Color(0.05f, 0.06f, 0.09f, 0.88f));
    }

    private void OnGUI()
    {
        if (!m_MenuOpen)
        {
            DrawInGameControlsHUD();
            return;
        }

        Vector2 targetRes = new Vector2(1920, 1080);
        Vector3 scale = new Vector3(Screen.width / targetRes.x, Screen.height / targetRes.y, 1f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);

        if (m_TexCardBg == null) m_TexCardBg = MakeSolidTexture(2, 2, new Color(0.08f, 0.09f, 0.14f, 0.94f));
        if (m_TexInnerBox == null) m_TexInnerBox = MakeSolidTexture(2, 2, new Color(0.05f, 0.06f, 0.09f, 0.88f));

        int mainWidth = 1400;
        int mainHeight = 980;
        Rect mainRect = new Rect((1920 - mainWidth) / 2, (1080 - mainHeight) / 2, mainWidth, mainHeight);

        GUIStyle panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = m_TexCardBg;

        GUILayout.BeginArea(mainRect, panelStyle);
        GUILayout.Space(25);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 58,
            fontStyle = FontStyle.Bold,
            richText = true
        };
        GUILayout.Label("<color=#FFD700>★ TANKS: ULTIMATE BATTLE ARENA ★</color>", titleStyle);

        GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24,
            richText = true
        };
        GUILayout.Label("<color=#00E5FF><i>Configura tu flota, selecciona el mapa y conquista el campo de batalla</i></color>", subtitleStyle);
        GUILayout.Space(30);

        // Player count selector
        GUIStyle cardBox = new GUIStyle(GUI.skin.box);
        cardBox.normal.background = m_TexInnerBox;

        GUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.BeginVertical(cardBox, GUILayout.Width(1340), GUILayout.Height(100));
        GUILayout.BeginHorizontal();
        GUILayout.Label("<color=#FFA500><b>CANTIDAD DE TANQUES EN BATALLA:</b></color>", new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, richText = true });
        GUILayout.FlexibleSpace();

        GUIStyle arrowBtn = new GUIStyle(GUI.skin.button) { fontSize = 28, fontStyle = FontStyle.Bold };
        if (GUILayout.Button("◄", arrowBtn, GUILayout.Width(70), GUILayout.Height(55))) m_NumPlayers = Mathf.Max(2, m_NumPlayers - 1);
        
        GUIStyle countStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 32, fontStyle = FontStyle.Bold, richText = true };
        GUILayout.Label("<color=#00FF7F>" + m_NumPlayers + " TANQUES</color>", countStyle, GUILayout.Width(220), GUILayout.Height(55));

        if (GUILayout.Button("►", arrowBtn, GUILayout.Width(70), GUILayout.Height(55))) m_NumPlayers = Mathf.Min(4, m_NumPlayers + 1);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.Space(30);
        GUILayout.EndHorizontal();

        GUILayout.Space(25);

        // Player cards
        GUILayout.BeginHorizontal();
        GUILayout.Space(30);
        int cardWidth = (1340 / m_NumPlayers) - 15;
        for (int i = 0; i < m_NumPlayers; i++)
        {
            Color tankColor = m_AvailableColors[m_SelectedColors[i]];
            string colorHex = ColorUtility.ToHtmlStringRGB(tankColor);

            GUILayout.BeginVertical(cardBox, GUILayout.Width(cardWidth), GUILayout.Height(330));
            GUIStyle tankTitle = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            GUILayout.Label($"<color=#{colorHex}>[JUGADOR {i + 1}]</color>", tankTitle);
            GUILayout.Space(10);

            GUILayout.Label("Color del Blindaje:", new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter });
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = tankColor;
            GUIStyle colorBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold, richText = true };
            if (GUILayout.Button("● " + m_ColorNames[m_SelectedColors[i]], colorBtnStyle, GUILayout.Height(55)))
            {
                m_SelectedColors[i] = (m_SelectedColors[i] + 1) % m_AvailableColors.Length;
            }
            GUI.backgroundColor = prevBg;

            GUILayout.Space(15);
            GUILayout.Label("Clase de Tanque:", new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter });
            GUIStyle typeBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 19, fontStyle = FontStyle.Bold, richText = true };
            if (GUILayout.Button(m_TypeNames[m_SelectedTypes[i]], typeBtnStyle, GUILayout.Height(50)))
            {
                m_SelectedTypes[i] = (m_SelectedTypes[i] + 1) % m_TypeNames.Length;
            }

            GUILayout.Space(5);
            GUIStyle statStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, richText = true };
            GUILayout.Label("<color=#A0E0A0>" + m_TypeStats[m_SelectedTypes[i]] + "</color>", statStyle);

            GUILayout.EndVertical();
            if (i < m_NumPlayers - 1) GUILayout.Space(15);
        }
        GUILayout.Space(30);
        GUILayout.EndHorizontal();

        GUILayout.Space(25);

        // Power-Ups info
        GUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUILayout.BeginVertical(cardBox, GUILayout.Width(1340), GUILayout.Height(65));
        GUIStyle powerupBadgeStyle = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            richText = true,
            fontStyle = FontStyle.Bold
        };
        GUILayout.Label("⚡ INNOVACIONES EN ARENA:  <color=#FF9900>⚡ Turbo Nitro</color>  |  <color=#00E5FF>🛡️ Escudo Plasma</color>  |  <color=#FF1493>💥 Triple Cañón</color>  |  <color=#00FF7F>💚 Nano Cura</color>  |  <color=#FF3333>🎯 Torretas Láser</color>  |  <color=#FFAA00>💥 Barriles Explosivos</color>", powerupBadgeStyle);
        GUILayout.EndVertical();
        GUILayout.Space(30);
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        // Action buttons
        GUILayout.BeginHorizontal();
        GUILayout.Space(30);
        GUI.backgroundColor = new Color(0.15f, 0.45f, 0.9f);
        GUIStyle auxBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold };
        if (GUILayout.Button("🎮 GUÍA DE CONTROLES", auxBtnStyle, GUILayout.Width(350), GUILayout.Height(85)))
        {
            m_ShowControlsModal = !m_ShowControlsModal;
        }

        GUILayout.Space(30);
        GUI.backgroundColor = new Color(0.1f, 0.9f, 0.35f);
        GUIStyle startBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 42, fontStyle = FontStyle.Bold };
        if (GUILayout.Button("🔥 ¡INICIAR COMBATE! 🔥", startBtnStyle, GUILayout.Height(85)))
        {
            m_MenuOpen = false;
            StartGame();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.Space(30);
        GUILayout.EndHorizontal();
        GUILayout.Space(25);

        GUILayout.EndArea();

        if (m_ShowControlsModal)
        {
            DrawControlsModal();
        }
    }

    private void DrawControlsModal()
    {
        int modalWidth = 900;
        int modalHeight = 650;
        Rect modalRect = new Rect((1920 - modalWidth) / 2, (1080 - modalHeight) / 2, modalWidth, modalHeight);

        GUIStyle modalBox = new GUIStyle(GUI.skin.box);
        modalBox.normal.background = MakeSolidTexture(2, 2, new Color(0.04f, 0.05f, 0.08f, 0.98f));

        GUILayout.BeginArea(modalRect, modalBox);
        GUILayout.Space(25);

        GUIStyle modalTitle = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            richText = true
        };
        GUILayout.Label("<color=#FFD700>🎮 GUÍA COMPLETA DE CONTROLES</color>", modalTitle);
        GUILayout.Space(20);

        GUIStyle controlRowStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true };
        GUILayout.BeginVertical(new GUIStyle(GUI.skin.box));
        GUILayout.Label("<color=#FF4444><b>• JUGADOR 1 (Rojo):</b></color> Disparar con <b>[BARRA ESPACIADORA]</b> | Mover con <b>[W, A, S, D]</b>", controlRowStyle);
        GUILayout.Space(10);
        GUILayout.Label("<color=#4488FF><b>• JUGADOR 2 (Azul):</b></color> Disparar con <b>[ENTER / RETURN]</b> | Mover con <b>[FLECHAS DIRECCIONALES]</b>", controlRowStyle);
        GUILayout.Space(10);
        GUILayout.Label("<color=#44FF44><b>• JUGADOR 3 (Verde):</b></color> Disparar con <b>[SHIFT DERECHO]</b> | Mover con <b>[I, J, K, L]</b>", controlRowStyle);
        GUILayout.Space(10);
        GUILayout.Label("<color=#FFFF44><b>• JUGADOR 4 (Amarillo):</b></color> Disparar con <b>[ENTER DEL TECLADO NUMÉRICO]</b> | Mover con <b>[NUMPAD 8, 4, 5, 6]</b>", controlRowStyle);
        GUILayout.EndVertical();

        GUILayout.Space(25);
        GUIStyle tipStyle = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            richText = true
        };
        GUILayout.Label("<color=#00FFFF>💡 <b>Mecánica de Tiro:</b> Mantén presionado el botón para cargar la barra de potencia.\nAl soltarlo se dispara el proyectil con el alcance deseado.\nSi la barra se llena al máximo, disparará automáticamente.</color>", tipStyle);

        GUILayout.FlexibleSpace();
        GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
        if (GUILayout.Button("ENTENDIDO - CERRAR", new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold }, GUILayout.Height(60)))
        {
            m_ShowControlsModal = false;
        }
        GUI.backgroundColor = Color.white;
        GUILayout.Space(20);

        GUILayout.EndArea();
    }

    private void DrawInGameControlsHUD()
    {
        GUI.matrix = Matrix4x4.identity;
        GUIStyle hudBox = new GUIStyle(GUI.skin.box);
        hudBox.normal.background = MakeSolidTexture(1, 1, new Color(0, 0, 0, 0.75f));

        GUIStyle textStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 13,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold
        };

        int count = (m_Tanks != null) ? m_Tanks.Length : 2;
        int hudWidth = 530;
        int hudHeight = 30 + count * 22;
        GUILayout.BeginArea(new Rect(10, Screen.height - hudHeight - 10, hudWidth, hudHeight), hudBox);
        GUILayout.Label("🎮 CONTROLES (Mantén el botón para cargar potencia, suelta para disparar):", textStyle);

        if (m_Tanks != null)
        {
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                string info = "";
                string colorHex = ColorUtility.ToHtmlStringRGB(m_Tanks[i].m_PlayerColor);
                if (i == 0) info = $"<color=#{colorHex}>Jugador 1</color>: Disparo [BARRA ESPACIADORA] | Mover [W,A,S,D]";
                else if (i == 1) info = $"<color=#{colorHex}>Jugador 2</color>: Disparo [ENTER] | Mover [FLECHAS]";
                else if (i == 2) info = $"<color=#{colorHex}>Jugador 3</color>: Disparo [SHIFT DERECHO] | Mover [I,J,K,L]";
                else if (i == 3) info = $"<color=#{colorHex}>Jugador 4</color>: Disparo [ENTER NUMPAD] | Mover [NUMPAD 8,4,5,6]";

                GUIStyle pStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 };
                GUILayout.Label(info, pStyle);
            }
        }
        GUILayout.EndArea();
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
                sp.transform.position = new Vector3(Mathf.Cos(i * Mathf.PI/2) * 10, 0.35f, Mathf.Sin(i * Mathf.PI/2) * 10);
                sp.transform.rotation = Quaternion.LookRotation(Vector3.zero - sp.transform.position);
                newTanks[i].m_SpawnPoint = sp.transform;
            }
        }
        m_Tanks = newTanks;

        // Enrich scene with radars, helipad, explosive barrels, wrecks and hostile turrets
        Rigidbody shellRef = m_TankPrefab != null ? m_TankPrefab.GetComponent<TankShooting>()?.m_Shell : null;
        SceneEnricher.EnrichCurrentScene(shellRef);

        if (GetComponent<PowerUpSpawner>() == null)
        {
            gameObject.AddComponent<PowerUpSpawner>();
        }

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
            if (type == 0) { // Normal
                movement.m_Speed = 9.5f;
                health.m_StartingHealth = 100f;
            } else if (type == 1) { // Rapido
                movement.m_Speed = 13.5f; 
                health.m_StartingHealth = 75f; 
            } else if (type == 2) { // Pesado
                movement.m_Speed = 6.5f;
                health.m_StartingHealth = 150f;
                shooting.m_MaxLaunchForce = 38f; 
            }
        }
    }


    private void SpawnAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            Vector3 spawnPos = m_Tanks[i].m_SpawnPoint.position + Vector3.up * 0.35f;
            m_Tanks[i].m_Instance =
                Instantiate(m_TankPrefab, spawnPos, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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