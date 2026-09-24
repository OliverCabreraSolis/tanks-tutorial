using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game.
        public float m_StartDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
        public float m_EndDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases.
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.
        public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc.
        public GameObject m_TankPrefab;             // Reference to the prefab the players will control.
        public TankManager[] m_Tanks;               // A collection of managers for enabling and disabling different aspects of the tanks.

        
        private int m_RoundNumber;                  // Which round the game is currently on.
        private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts.
        private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends.
        private TankManager m_RoundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won.
        private TankManager m_GameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won.

        // --- Variables de UI ---
        private int m_SelectedMode = 0; // 0 = 1P vs 3Bots, 1 = 2P, 2 = 3P, 3 = 4P
        private string[] m_Modes = { "1 Jugador (vs 3 Bots)", "2 Jugadores", "3 Jugadores", "4 Jugadores" };
        private int m_SelectedMap = 0;
        private string[] m_MapNames = { "Desierto (Clasico)", "Nevado", "Ciudad Nocturna", "Bosque Apocaliptico" };
        private Color[] m_AvailableColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan };
        private int[] m_SelectedColors = { 0, 1, 2, 3 };
        private int[] m_SelectedTypes = { 0, 0, 0, 0 };
        private string[] m_TypeNames = { "Normal", "Rapido", "Pesado" };
        private bool m_MenuOpen = true;

        private Texture2D m_MenuBackground;

        private void Start()
        {
            m_StartWait = new WaitForSeconds (m_StartDelay);
            m_EndWait = new WaitForSeconds (m_EndDelay);
            m_MenuBackground = Resources.Load<Texture2D>("menu_bg");
        }

        private void OnGUI()
        {
            if (!m_MenuOpen) return;

            // Escalar la interfaz para que siempre se vea gigante y proporcionada, asumiendo 1920x1080 de base
            Vector2 targetRes = new Vector2(1920, 1080);
            Vector3 scale = new Vector3(Screen.width / targetRes.x, Screen.height / targetRes.y, 1f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);

            // Dibujar imagen de fondo a pantalla completa (escalando inversamente para cubrir la resolucion)
            if (m_MenuBackground != null)
            {
                GUI.DrawTexture(new Rect(0, 0, 1920, 1080), m_MenuBackground, ScaleMode.ScaleAndCrop);
            }

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 80;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 36;
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontStyle = FontStyle.Bold;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 30;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            // Fondo semi-transparente
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0, 0, 0, 0.85f));
            tex.Apply();
            boxStyle.normal.background = tex;

            int width = 1200;
            int height = 900;
            GUILayout.BeginArea(new Rect((1920 - width) / 2, (1080 - height) / 2, width, height), boxStyle);
            
            GUILayout.Space(40);
            GUILayout.Label("TANKS - CONFIGURACION", titleStyle);
            GUILayout.Space(60);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Modo de Juego:", labelStyle, GUILayout.Width(300));
            if (GUILayout.Button("<", buttonStyle, GUILayout.Width(80), GUILayout.Height(60))) m_SelectedMode = (m_SelectedMode + m_Modes.Length - 1) % m_Modes.Length;
            GUILayout.Label(m_Modes[m_SelectedMode], new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(450));
            if (GUILayout.Button(">", buttonStyle, GUILayout.Width(80), GUILayout.Height(60))) m_SelectedMode = (m_SelectedMode + 1) % m_Modes.Length;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Escenario:", labelStyle, GUILayout.Width(300));
            if (GUILayout.Button("<", buttonStyle, GUILayout.Width(80), GUILayout.Height(60))) m_SelectedMap = (m_SelectedMap + m_MapNames.Length - 1) % m_MapNames.Length;
            GUILayout.Label(m_MapNames[m_SelectedMap], new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(450));
            if (GUILayout.Button(">", buttonStyle, GUILayout.Width(80), GUILayout.Height(60))) m_SelectedMap = (m_SelectedMap + 1) % m_MapNames.Length;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(50);
            
            int totalTanks = m_SelectedMode == 0 ? 4 : (m_SelectedMode + 1);

            for (int i = 0; i < totalTanks; i++)
            {
                bool isBot = (m_SelectedMode == 0 && i > 0);
                
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(isBot ? ("Bot " + i) : ("Jugador " + (i + 1)), labelStyle, GUILayout.Width(250));
                
                GUI.backgroundColor = m_AvailableColors[m_SelectedColors[i]];
                if (GUILayout.Button("Color", buttonStyle, GUILayout.Width(250), GUILayout.Height(60)))
                {
                    m_SelectedColors[i] = (m_SelectedColors[i] + 1) % m_AvailableColors.Length;
                }
                GUI.backgroundColor = Color.white; // reset
                
                GUILayout.Space(30);
                if (GUILayout.Button("Tipo: " + m_TypeNames[m_SelectedTypes[i]], buttonStyle, GUILayout.Width(250), GUILayout.Height(60)))
                {
                    m_SelectedTypes[i] = (m_SelectedTypes[i] + 1) % m_TypeNames.Length;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(20);
            }

            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("¡INICIAR MATANZA!", new GUIStyle(buttonStyle) { fontSize = 50, fontStyle = FontStyle.Bold }, GUILayout.Height(120)))
            {
                m_MenuOpen = false;
                StartGame(totalTanks);
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndArea();
        }

        private void ApplyScenario()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            Light[] lights = FindObjectsOfType<Light>();
            Light mainLight = null;
            foreach (Light l in lights) { if (l.type == LightType.Directional) { mainLight = l; break; } }

            Color groundColor = Color.white;
            GameObject levelArt = GameObject.Find("LevelArt");

            if (m_SelectedMap == 0) // Desierto (Clasico original)
            {
                RenderSettings.ambientLight = new Color(0.8f, 0.7f, 0.6f);
                if (mainLight != null) { mainLight.color = new Color(1f, 0.95f, 0.8f); mainLight.intensity = 1.0f; }
                if (levelArt != null) levelArt.SetActive(true);
            }
            else // Nieve, Ciudad o Apocaliptico
            {
                if (levelArt != null) levelArt.SetActive(false); // Eliminar completamente las rocas y objetos del desierto

                if (m_SelectedMap == 1) // Nieve
                {
                    RenderSettings.ambientLight = new Color(0.6f, 0.8f, 1f);
                    if (mainLight != null) { mainLight.color = new Color(0.8f, 0.9f, 1f); mainLight.intensity = 1.2f; }
                    groundColor = new Color(0.8f, 0.9f, 1f); 
                    CreateWeather(1); 
                }
                else if (m_SelectedMap == 2) // Ciudad Nocturna
                {
                    RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.3f);
                    if (mainLight != null) { mainLight.color = new Color(0.3f, 0.4f, 0.6f); mainLight.intensity = 0.4f; }
                    groundColor = new Color(0.15f, 0.15f, 0.18f); // Asfalto muy oscuro
                }
                else if (m_SelectedMap == 3) // Bosque Apocaliptico
                {
                    RenderSettings.ambientLight = new Color(0.8f, 0.4f, 0.2f);
                    if (mainLight != null) { mainLight.color = new Color(0.9f, 0.3f, 0.1f); mainLight.intensity = 0.8f; }
                    groundColor = new Color(0.3f, 0.2f, 0.15f); // Tierra oscura/quemada
                    CreateWeather(3); 
                }

                // Crear un suelo plano limpio, sin rastro de texturas o rocas del desierto
                GameObject customGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
                customGround.name = "CustomGround";
                customGround.transform.localScale = new Vector3(10, 1, 10); // 100x100 metros
                Renderer rend = customGround.GetComponent<Renderer>();
                rend.material.color = groundColor;

                // Muros invisibles para delimitar el mapa
                GameObject walls = new GameObject("InvisibleWalls");
                for (int i = 0; i < 4; i++) {
                    GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    w.transform.SetParent(walls.transform);
                    Destroy(w.GetComponent<MeshRenderer>()); 
                    if (i == 0) { w.transform.position = new Vector3(0, 5, 27); w.transform.localScale = new Vector3(60, 10, 2); }
                    if (i == 1) { w.transform.position = new Vector3(0, 5, -27); w.transform.localScale = new Vector3(60, 10, 2); }
                    if (i == 2) { w.transform.position = new Vector3(27, 5, 0); w.transform.localScale = new Vector3(2, 10, 60); }
                    if (i == 3) { w.transform.position = new Vector3(-27, 5, 0); w.transform.localScale = new Vector3(2, 10, 60); }
                }
            }
        }

        private void CreateWeather(int type)
        {
            GameObject weather = new GameObject("WeatherParticles");
            ParticleSystem ps = weather.AddComponent<ParticleSystem>();
            
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null) {
                rend.material = new Material(defaultShader);
            }

            var main = ps.main;
            main.maxParticles = 3000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = ps.emission;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;

            if (type == 1) 
            {
                weather.transform.position = new Vector3(0, 20, 0);
                shape.scale = new Vector3(60, 1, 60);
                emission.rateOverTime = 300;
                main.startColor = new Color(1f, 1f, 1f, 0.9f); 
                main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
                main.startSpeed = 0f;
                vel.y = new ParticleSystem.MinMaxCurve(-3f, -6f);
                vel.x = new ParticleSystem.MinMaxCurve(-1f, 1f);
                vel.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
            }
            else if (type == 3) 
            {
                weather.transform.position = new Vector3(0, 0, 0);
                shape.scale = new Vector3(60, 1, 60);
                emission.rateOverTime = 100;
                main.startColor = new Color(1f, 0.4f, 0.1f, 0.7f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
                main.startSpeed = 0f;
                vel.y = new ParticleSystem.MinMaxCurve(1f, 4f);
                vel.x = new ParticleSystem.MinMaxCurve(-1f, 1f);
            }
        }

        private void SpawnDecorations()
        {
            if (m_SelectedMap == 0) return; 

            GameObject decorGroup = new GameObject("Decorations");
            string[] paths;
            
            float scaleMin = 1.5f;
            float scaleMax = 2.5f;

            if (m_SelectedMap == 2) // Ciudad en cuadras (Grid)
            {
                paths = new string[] { "Casitas y huevadas/Large Building/large_buildingA", "Casitas y huevadas/Skyscraper/skyscraperA" };
                scaleMin = 2.0f;
                scaleMax = 3.5f;
                
                float spacing = 12f;
                for (float x = -24; x <= 24; x += spacing) {
                    for (float z = -24; z <= 24; z += spacing) {
                        // Dejar el centro libre para pelear (zona segura 15x15)
                        if (Mathf.Abs(x) < 15f && Mathf.Abs(z) < 15f) continue;
                        
                        string path = paths[Random.Range(0, paths.Length)];
                        GameObject prefab = Resources.Load<GameObject>(path);
                        if (prefab != null) {
                            GameObject obj = Instantiate(prefab);
                            obj.transform.position = new Vector3(x, 0, z);
                            obj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0); // Cuadras perfectas
                            float s = Random.Range(scaleMin, scaleMax);
                            obj.transform.localScale = new Vector3(s, s, s);
                            obj.transform.SetParent(decorGroup.transform);
                        }
                    }
                }
                return; // Fin de spawn de ciudad
            }

            // Nieve y Apocaliptico usan spawn circular en los bordes
            int numDecorations = 10;
            float minRadius = 20f; 
            float maxRadius = 24f;

            if (m_SelectedMap == 1) // Nieve
            {
                paths = new string[] { "Casitas y huevadas/Small Building/small_buildingA", "Casitas y huevadas/Sign Hospital/sign_hospital" };
                numDecorations = 12;
                scaleMin = 2f;
                scaleMax = 3f;
            }
            else // Apocaliptico
            {
                paths = new string[] { "Casitas y huevadas/Low Building/low_buildingA", "Casitas y huevadas/Low Wide/low_wideA" };
                numDecorations = 15;
                scaleMin = 2f;
                scaleMax = 3.5f; 
            }

            float angleStep = (Mathf.PI * 2f) / numDecorations;
            for (int i = 0; i < numDecorations; i++)
            {
                string path = paths[Random.Range(0, paths.Length)];
                GameObject prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    GameObject obj = Instantiate(prefab);
                    float angle = (i * angleStep) + Random.Range(-angleStep * 0.2f, angleStep * 0.2f);
                    float radius = Random.Range(minRadius, maxRadius); 
                    obj.transform.position = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                    
                    float currentScale = Random.Range(scaleMin, scaleMax);
                    obj.transform.localScale = new Vector3(currentScale, currentScale, currentScale);

                    if (m_SelectedMap == 3) {
                        obj.transform.rotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
                        obj.transform.position -= new Vector3(0, currentScale * 0.2f, 0); 
                    } 
                    else {
                        obj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                    }
                    
                    obj.transform.SetParent(decorGroup.transform);
                }
            }
        }

        private void StartGame(int totalTanks)
        {
            ApplyScenario();
            SpawnDecorations();

            TankManager[] newTanks = new TankManager[totalTanks];
            for (int i = 0; i < totalTanks; i++) {
                newTanks[i] = new TankManager();
                newTanks[i].m_PlayerColor = m_AvailableColors[m_SelectedColors[i]];
                newTanks[i].m_PlayerNumber = i + 1;
                
                if (i < m_Tanks.Length && m_Tanks[i] != null && m_Tanks[i].m_SpawnPoint != null) {
                    newTanks[i].m_SpawnPoint = m_Tanks[i].m_SpawnPoint;
                } else {
                    GameObject sp = new GameObject("SpawnPoint" + (i + 1));
                    sp.transform.position = new Vector3(Mathf.Cos(i * Mathf.PI/2) * 12, 0, Mathf.Sin(i * Mathf.PI/2) * 12);
                    sp.transform.rotation = Quaternion.LookRotation(Vector3.zero - sp.transform.position);
                    newTanks[i].m_SpawnPoint = sp.transform;
                }
            }
            m_Tanks = newTanks;

            SpawnAllTanks();
            ApplyTankTypes(totalTanks);
            SetCameraTargets();

            StartCoroutine(GameLoop());
        }



        private void ApplyTankTypes(int totalTanks)
        {
            for (int i = 0; i < totalTanks; i++) {
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

                // Attach AI if it's 1-player mode and this is not player 1
                if (m_SelectedMode == 0 && i > 0)
                {
                    m_Tanks[i].m_Instance.AddComponent<TankAI>();
                }
            }
        }

        private void SpawnAllTanks()
        {
            // For all the tanks...
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                // ... create them, set their player number and references needed for control.
                m_Tanks[i].m_Instance =
                    Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
                m_Tanks[i].m_PlayerNumber = i + 1;
                m_Tanks[i].Setup();
            }
        }


        private void SetCameraTargets()
        {
            // Create a collection of transforms the same size as the number of tanks.
            Transform[] targets = new Transform[m_Tanks.Length];

            // For each of these transforms...
            for (int i = 0; i < targets.Length; i++)
            {
                // ... set it to the appropriate tank transform.
                targets[i] = m_Tanks[i].m_Instance.transform;
            }

            // These are the targets the camera should follow.
            m_CameraControl.m_Targets = targets;
        }


        // This is called from start and will run each phase of the game one after another.
        private IEnumerator GameLoop ()
        {
            // Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
            yield return StartCoroutine (RoundStarting ());

            // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
            yield return StartCoroutine (RoundPlaying());

            // Once execution has returned here, run the 'RoundEnding' coroutine, again don't return until it's finished.
            yield return StartCoroutine (RoundEnding());

            // This code is not run until 'RoundEnding' has finished.  At which point, check if a game winner has been found.
            if (m_GameWinner != null || (m_SelectedMode == 0 && m_Tanks.Length > 0 && m_Tanks[0].m_Instance != null && !m_Tanks[0].m_Instance.activeSelf))
            {
                // If there is a game winner, restart the level.
                SceneManager.LoadScene (0);
            }
            else
            {
                // If there isn't a winner yet, restart this coroutine so the loop continues.
                // Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end.
                StartCoroutine (GameLoop ());
            }
        }


        private IEnumerator RoundStarting ()
        {
            // As soon as the round starts reset the tanks and make sure they can't move.
            ResetAllTanks ();
            DisableTankControl ();

            // Snap the camera's zoom and position to something appropriate for the reset tanks.
            m_CameraControl.SetStartPositionAndSize ();

            // Increment the round number and display text showing the players what round it is.
            m_RoundNumber++;
            m_MessageText.text = "ROUND " + m_RoundNumber;

            // Wait for the specified length of time until yielding control back to the game loop.
            yield return m_StartWait;
        }


        private IEnumerator RoundPlaying ()
        {
            // As soon as the round begins playing let the players control the tanks.
            EnableTankControl ();

            // Clear the text from the screen.
            m_MessageText.text = string.Empty;

            // While there is not one tank left...
            while (!OneTankLeft())
            {
                if (m_SelectedMode == 0 && m_Tanks.Length > 0 && m_Tanks[0].m_Instance != null && !m_Tanks[0].m_Instance.activeSelf) 
                {
                    break; // Player is dead in 1P mode, end round immediately
                }
                // ... return on the next frame.
                yield return null;
            }
        }


        private IEnumerator RoundEnding ()
        {
            // Stop tanks from moving.
            DisableTankControl ();

            // Clear the winner from the previous round.
            m_RoundWinner = null;

            // See if there is a winner now the round is over.
            m_RoundWinner = GetRoundWinner ();

            // If there is a winner, increment their score.
            if (m_RoundWinner != null)
                m_RoundWinner.m_Wins++;

            // Now the winner's score has been incremented, see if someone has one the game.
            m_GameWinner = GetGameWinner ();

            // Get a message based on the scores and whether or not there is a game winner and display it.
            string message = EndMessage ();
            m_MessageText.text = message;

            // Wait for the specified length of time until yielding control back to the game loop.
            yield return m_EndWait;
        }


        // This is used to check if there is one or fewer tanks remaining and thus the round should end.
        private bool OneTankLeft()
        {
            // Start the count of tanks left at zero.
            int numTanksLeft = 0;

            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                // ... and if they are active, increment the counter.
                if (m_Tanks[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }

            // If there are one or fewer tanks remaining return true, otherwise return false.
            return numTanksLeft <= 1;
        }
        
        
        // This function is to find out if there is a winner of the round.
        // This function is called with the assumption that 1 or fewer tanks are currently active.
        private TankManager GetRoundWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                // ... and if one of them is active, it is the winner so return it.
                if (m_Tanks[i].m_Instance.activeSelf)
                    return m_Tanks[i];
            }

            // If none of the tanks are active it is a draw so return null.
            return null;
        }


        // This function is to find out if there is a winner of the game.
        private TankManager GetGameWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                // ... and if one of them has enough rounds to win the game, return it.
                if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                    return m_Tanks[i];
            }

            // If no tanks have enough rounds to win, return null.
            return null;
        }


        // Returns a string message to display at the end of each round.
        private string EndMessage()
        {
            if (m_SelectedMode == 0 && m_Tanks.Length > 0 && m_Tanks[0].m_Instance != null && !m_Tanks[0].m_Instance.activeSelf) 
            {
                return "<color=#FF0000>GAME OVER</color>\n¡Fuiste aniquilado!";
            }

            // By default when a round ends there are no winners so the default end message is a draw.
            string message = "DRAW!";

            // If there is a winner then change the message to reflect that.
            if (m_RoundWinner != null)
                message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

            // Add some line breaks after the initial message.
            message += "\n\n\n\n";

            // Go through all the tanks and add each of their scores to the message.
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Wins + " WINS\n";
            }

            // If there is a game winner, change the entire message to reflect that.
            if (m_GameWinner != null)
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

            return message;
        }


        // This function is used to turn all the tanks back on and reset their positions and properties.
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
}