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
        private string[] m_Modes = {
            "🤖 1 Jugador (vs 3 Bots IA)",
            "⚔️ 2 Jugadores (Duelo)",
            "💥 3 Jugadores (Trifulca)",
            "👑 4 Jugadores (Guerra Total)"
        };
        private string[] m_ModeBadges = {
            "<color=#00FF7F>[MODO CAMPAÑA / PVE]</color>",
            "<color=#1E90FF>[DUELO CLÁSICO 1v1]</color>",
            "<color=#FFA500>[TODOS CONTRA TODOS 3P]</color>",
            "<color=#FF1493>[BATALLA MASIVA 4P]</color>"
        };

        private int m_SelectedMap = 0;
        private string[] m_MapNames = {
            "🏜️ Desierto (Clásico)",
            "❄️ Glaciar Nevado",
            "🏙️ Ciudad Nocturna Neón",
            "🌋 Bosque Apocalíptico"
        };
        private string[] m_MapDescriptions = {
            "Terreno desértico con cañones rocosos, trincheras y restos de guerra.",
            "Ventisca helada de nieve con asfalto ártico y visibilidad hostil.",
            "Cuadrícula urbana con rascacielos iluminados, callejones y asfalto oscuro.",
            "Lluvia de fuego y cenizas volcánicas con estructuras industriales en ruinas."
        };

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
        private Texture2D m_MenuBackground;

        private Texture2D m_TexCardBg;
        private Texture2D m_TexHeaderBg;
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
            m_StartWait = new WaitForSeconds (m_StartDelay);
            m_EndWait = new WaitForSeconds (m_EndDelay);
            m_MenuBackground = Resources.Load<Texture2D>("menu_bg");

            m_TexCardBg = MakeSolidTexture(2, 2, new Color(0.08f, 0.09f, 0.14f, 0.92f));
            m_TexHeaderBg = MakeSolidTexture(2, 2, new Color(0.12f, 0.14f, 0.22f, 0.95f));
            m_TexInnerBox = MakeSolidTexture(2, 2, new Color(0.05f, 0.06f, 0.09f, 0.85f));

            MaterialHelper.FixAllParticleSystemsInScene();
        }

        private void OnGUI()
        {
            if (!m_MenuOpen)
            {
                DrawInGameControlsHUD();
                return;
            }

            // Escalar la interfaz para que siempre se vea nítida en 1920x1080
            Vector2 targetRes = new Vector2(1920, 1080);
            Vector3 scale = new Vector3(Screen.width / targetRes.x, Screen.height / targetRes.y, 1f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);

            if (m_MenuBackground != null)
            {
                GUI.DrawTexture(new Rect(0, 0, 1920, 1080), m_MenuBackground, ScaleMode.ScaleAndCrop);
            }

            // Overlay degradado de fondo para dar contraste cinematográfico
            if (m_TexCardBg == null) m_TexCardBg = MakeSolidTexture(2, 2, new Color(0.08f, 0.09f, 0.14f, 0.92f));
            if (m_TexHeaderBg == null) m_TexHeaderBg = MakeSolidTexture(2, 2, new Color(0.12f, 0.14f, 0.22f, 0.95f));
            if (m_TexInnerBox == null) m_TexInnerBox = MakeSolidTexture(2, 2, new Color(0.05f, 0.06f, 0.09f, 0.85f));

            int totalTanks = m_SelectedMode == 0 ? 4 : (m_SelectedMode + 1);

            int mainWidth = 1400;
            int mainHeight = 980;
            Rect mainRect = new Rect((1920 - mainWidth) / 2, (1080 - mainHeight) / 2, mainWidth, mainHeight);

            GUIStyle panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = m_TexCardBg;

            GUILayout.BeginArea(mainRect, panelStyle);

            // --- HEADER CON TÍTULO NEÓN ---
            GUILayout.Space(20);
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
            GUILayout.Space(25);

            // --- SECCIÓN 1: MODO Y ESCENARIO (CARDS EN PARALELO) ---
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);

            // TARJETA DE MODO
            GUIStyle cardBox = new GUIStyle(GUI.skin.box);
            cardBox.normal.background = m_TexInnerBox;

            GUILayout.BeginVertical(cardBox, GUILayout.Width(640), GUILayout.Height(160));
            GUIStyle sectionHeader = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, richText = true };
            GUILayout.Label("<color=#FFA500>⚙️ MODO DE JUEGO:</color> " + m_ModeBadges[m_SelectedMode], sectionHeader);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUIStyle arrowBtn = new GUIStyle(GUI.skin.button) { fontSize = 28, fontStyle = FontStyle.Bold };
            if (GUILayout.Button("◄", arrowBtn, GUILayout.Width(60), GUILayout.Height(55)))
            {
                m_SelectedMode = (m_SelectedMode + m_Modes.Length - 1) % m_Modes.Length;
            }

            GUIStyle valStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28, fontStyle = FontStyle.Bold, richText = true };
            GUILayout.Label("<color=#FFFFFF>" + m_Modes[m_SelectedMode] + "</color>", valStyle, GUILayout.Height(55));

            if (GUILayout.Button("►", arrowBtn, GUILayout.Width(60), GUILayout.Height(55)))
            {
                m_SelectedMode = (m_SelectedMode + 1) % m_Modes.Length;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(40);

            // TARJETA DE ESCENARIO
            GUILayout.BeginVertical(cardBox, GUILayout.Width(640), GUILayout.Height(160));
            GUILayout.Label("<color=#00FFCC>🌍 ESCENARIO DE COMBATE:</color>", sectionHeader);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◄", arrowBtn, GUILayout.Width(60), GUILayout.Height(55)))
            {
                m_SelectedMap = (m_SelectedMap + m_MapNames.Length - 1) % m_MapNames.Length;
            }

            GUILayout.Label("<color=#FFFF00>" + m_MapNames[m_SelectedMap] + "</color>", valStyle, GUILayout.Height(55));

            if (GUILayout.Button("►", arrowBtn, GUILayout.Width(60), GUILayout.Height(55)))
            {
                m_SelectedMap = (m_SelectedMap + 1) % m_MapNames.Length;
            }
            GUILayout.EndHorizontal();

            GUIStyle descStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, richText = true };
            GUILayout.Label("<color=#A0C0D0>" + m_MapDescriptions[m_SelectedMap] + "</color>", descStyle);
            GUILayout.EndVertical();

            GUILayout.Space(30);
            GUILayout.EndHorizontal();

            GUILayout.Space(25);

            // --- SECCIÓN 2: PERSONALIZACIÓN DE TANQUES ---
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            GUILayout.Label("<color=#FFD700>🛡️ FLOTA DE COMBATE Y TANQUES ACTIVOS:</color>", sectionHeader);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Space(30);

            int cardWidth = (1340 / totalTanks) - 15;
            for (int i = 0; i < totalTanks; i++)
            {
                bool isBot = (m_SelectedMode == 0 && i > 0);
                Color tankColor = m_AvailableColors[m_SelectedColors[i]];
                string colorHex = ColorUtility.ToHtmlStringRGB(tankColor);

                GUILayout.BeginVertical(cardBox, GUILayout.Width(cardWidth), GUILayout.Height(370));
                
                // Header del tanque con su color distintivo
                GUIStyle tankTitle = new GUIStyle(GUI.skin.label) {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };
                string roleTag = isBot ? $"<color=#FF4444>[BOT IA {i}]</color>" : $"<color=#{colorHex}>[JUGADOR {i + 1}]</color>";
                GUILayout.Label(roleTag, tankTitle);
                GUILayout.Space(10);

                // Muestra de color grande interactiva
                GUILayout.Label("Color del Blindaje:", new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter });
                
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = tankColor;
                GUIStyle colorBtnStyle = new GUIStyle(GUI.skin.button) {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };

                if (GUILayout.Button("● " + m_ColorNames[m_SelectedColors[i]], colorBtnStyle, GUILayout.Height(55)))
                {
                    m_SelectedColors[i] = (m_SelectedColors[i] + 1) % m_AvailableColors.Length;
                }
                GUI.backgroundColor = prevBg;

                GUILayout.Space(12);

                // Selector de Tipo / Clase de Tanque
                GUILayout.Label("Clase de Tanque:", new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter });
                
                GUIStyle typeBtnStyle = new GUIStyle(GUI.skin.button) {
                    fontSize = 19,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };

                if (GUILayout.Button(m_TypeNames[m_SelectedTypes[i]], typeBtnStyle, GUILayout.Height(45)))
                {
                    m_SelectedTypes[i] = (m_SelectedTypes[i] + 1) % m_TypeNames.Length;
                }

                GUILayout.Space(4);
                GUIStyle statStyle = new GUIStyle(GUI.skin.label) {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    richText = true
                };
                GUILayout.Label("<color=#A0E0A0>" + m_TypeStats[m_SelectedTypes[i]] + "</color>", statStyle);

                // Resumen de Controles directo en la tarjeta
                string controlHint = "";
                if (isBot) controlHint = "<color=#AAAAAA>🤖 Controlado por IA Táctica</color>";
                else if (i == 0) controlHint = "<color=#FFFF00>🎯 Fuego: [ESPACIO]</color>\n<color=#88CCFF>🕹️ Mover: [W, A, S, D]</color>";
                else if (i == 1) controlHint = "<color=#FFFF00>🎯 Fuego: [ENTER]</color>\n<color=#88CCFF>🕹️ Mover: [FLECHAS]</color>";
                else if (i == 2) controlHint = "<color=#FFFF00>🎯 Fuego: [SHIFT D.]</color>\n<color=#88CCFF>🕹️ Mover: [I, J, K, L]</color>";
                else if (i == 3) controlHint = "<color=#FFFF00>🎯 Fuego: [NUM ENTER]</color>\n<color=#88CCFF>🕹️ Mover: [8, 4, 5, 6]</color>";

                GUILayout.Space(6);
                GUIStyle ctrlStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13, richText = true, fontStyle = FontStyle.Bold };
                GUILayout.Label(controlHint, ctrlStyle);

                GUILayout.EndVertical();
                if (i < totalTanks - 1) GUILayout.Space(15);
            }

            GUILayout.Space(30);
            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            // --- SECCIÓN 3: INNOVACIONES Y POWER-UPS ACTIVOS ---
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

            // --- SECCIÓN 4: BOTONES DE ACCIÓN INFERIORES ---
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);

            // Botón de Controles
            GUI.backgroundColor = new Color(0.15f, 0.45f, 0.9f);
            GUIStyle auxBtnStyle = new GUIStyle(GUI.skin.button) {
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            if (GUILayout.Button("🎮 GUÍA DE CONTROLES", auxBtnStyle, GUILayout.Width(350), GUILayout.Height(85)))
            {
                m_ShowControlsModal = !m_ShowControlsModal;
            }

            GUILayout.Space(30);

            // Botón Principal Gigante
            GUI.backgroundColor = new Color(0.1f, 0.9f, 0.35f);
            GUIStyle startBtnStyle = new GUIStyle(GUI.skin.button) {
                fontSize = 42,
                fontStyle = FontStyle.Bold
            };
            if (GUILayout.Button("🔥 ¡INICIAR COMBATE! 🔥", startBtnStyle, GUILayout.Height(85)))
            {
                m_MenuOpen = false;
                StartGame(totalTanks);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(30);
            GUILayout.EndHorizontal();
            GUILayout.Space(25);

            GUILayout.EndArea();

            // --- MODAL DE CONTROLES EMERGENTE ---
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

            GUIStyle controlRowStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 20,
                richText = true
            };

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

        private void ApplyScenario()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            Light[] lights = FindObjectsOfType<Light>();
            Light mainLight = null;
            foreach (Light l in lights) { if (l.type == LightType.Directional) { mainLight = l; break; } }

            GameObject levelArt = GameObject.Find("CompleteLevelArt");
            if (levelArt == null) levelArt = GameObject.Find("LevelArt");
            // ALWAYS keep LevelArt ACTIVE so original ramps, colliders and arena ground are intact!
            if (levelArt != null)
            {
                levelArt.SetActive(true);
            }

            // Clean up any legacy or duplicate runtime objects
            GameObject prevGround = GameObject.Find("CustomGround");
            if (prevGround != null) Destroy(prevGround);
            GameObject prevWalls = GameObject.Find("InvisibleWalls");
            if (prevWalls != null) Destroy(prevWalls);
            GameObject prevPerim = GameObject.Find("ArenaPerimeterColliders");
            if (prevPerim != null) Destroy(prevPerim);
            GameObject prevWeather = GameObject.Find("WeatherParticles");
            if (prevWeather != null) Destroy(prevWeather);
            GameObject prevDecor = GameObject.Find("Decorations");
            if (prevDecor != null) Destroy(prevDecor);

            if (m_SelectedMap == 0) // Desierto (Clásico)
            {
                RenderSettings.ambientLight = new Color(0.82f, 0.72f, 0.6f);
                if (mainLight != null) { mainLight.color = new Color(1f, 0.95f, 0.85f); mainLight.intensity = 1.0f; }

                if (levelArt != null)
                {
                    Renderer[] rList = levelArt.GetComponentsInChildren<Renderer>();
                    foreach (var r in rList)
                    {
                        if (r.sharedMaterial != null)
                        {
                            Color sandCol = new Color(0.88f, 0.76f, 0.54f);
                            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", sandCol);
                            if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", sandCol);
                        }
                    }
                }
            }
            else if (m_SelectedMap == 1) // Nieve / Glaciar Ártico
            {
                // Cold winter lighting
                RenderSettings.ambientLight = new Color(0.72f, 0.84f, 1.0f);
                if (mainLight != null) { mainLight.color = new Color(0.88f, 0.94f, 1.0f); mainLight.intensity = 1.25f; }

                // Tint LevelArt meshes to crisp snow and frost
                if (levelArt != null)
                {
                    Renderer[] rList = levelArt.GetComponentsInChildren<Renderer>();
                    foreach (var r in rList)
                    {
                        if (r.sharedMaterial != null)
                        {
                            string oName = r.gameObject.name.ToLower();
                            Color snowCol = oName.Contains("rock") 
                                ? new Color(0.75f, 0.82f, 0.90f) 
                                : new Color(0.92f, 0.96f, 1.0f);

                            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", snowCol);
                            if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", snowCol);
                        }
                    }
                }

                CreateWeather(1); // Snowfall with URP particles
            }
            else if (m_SelectedMap == 2) // Ciudad Nocturna
            {
                RenderSettings.ambientLight = new Color(0.22f, 0.25f, 0.38f);
                if (mainLight != null) { mainLight.color = new Color(0.35f, 0.5f, 0.75f); mainLight.intensity = 0.55f; }

                if (levelArt != null)
                {
                    Renderer[] rList = levelArt.GetComponentsInChildren<Renderer>();
                    foreach (var r in rList)
                    {
                        if (r.sharedMaterial != null)
                        {
                            Color cityCol = new Color(0.22f, 0.24f, 0.28f);
                            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", cityCol);
                            if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", cityCol);
                        }
                    }
                }
            }
            else if (m_SelectedMap == 3) // Páramo Apocalíptico
            {
                RenderSettings.ambientLight = new Color(0.75f, 0.35f, 0.15f);
                if (mainLight != null) { mainLight.color = new Color(1.0f, 0.38f, 0.12f); mainLight.intensity = 0.95f; }

                if (levelArt != null)
                {
                    Renderer[] rList = levelArt.GetComponentsInChildren<Renderer>();
                    foreach (var r in rList)
                    {
                        if (r.sharedMaterial != null)
                        {
                            Color scorchCol = new Color(0.28f, 0.2f, 0.16f);
                            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", scorchCol);
                            if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", scorchCol);
                        }
                    }
                }

                CreateWeather(3); // Ash / Embers
            }
        }

        private void CreateWeather(int type)
        {
            GameObject weather = new GameObject("WeatherParticles");
            ParticleSystem ps = weather.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.material = MaterialHelper.CreateParticleMaterial(type == 1 ? new Color(0.95f, 0.98f, 1f, 0.9f) : new Color(1f, 0.45f, 0.1f, 0.8f), type == 3);
            MaterialHelper.FixParticleSystem(ps, type == 1 ? new Color(0.95f, 0.98f, 1f, 0.9f) : new Color(1f, 0.45f, 0.1f, 0.8f));

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
            ps.Play();
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
                
                float spacing = 14f;
                for (float x = -26; x <= 26; x += spacing) {
                    for (float z = -26; z <= 26; z += spacing) {
                        // Dejar el centro libre para pelear (zona segura 20x20)
                        if (Mathf.Abs(x) < 18f && Mathf.Abs(z) < 18f) continue;
                        
                        string path = paths[Random.Range(0, paths.Length)];
                        GameObject prefab = Resources.Load<GameObject>(path);
                        if (prefab != null) {
                            GameObject obj = Instantiate(prefab);
                            obj.transform.position = new Vector3(x, 0, z);
                            obj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);
                            float s = Random.Range(scaleMin, scaleMax);
                            obj.transform.localScale = new Vector3(s, s, s);
                            obj.transform.SetParent(decorGroup.transform);

                            // Ensure solid collider on city buildings so tanks collide with them
                            Collider[] bCols = obj.GetComponentsInChildren<Collider>();
                            if (bCols.Length == 0)
                            {
                                Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
                                if (rends.Length > 0)
                                {
                                    Bounds b = rends[0].bounds;
                                    for (int r = 1; r < rends.Length; r++) b.Encapsulate(rends[r].bounds);
                                    BoxCollider boxCol = obj.AddComponent<BoxCollider>();
                                    boxCol.center = obj.transform.InverseTransformPoint(b.center);
                                    Vector3 localSize = obj.transform.InverseTransformVector(b.size);
                                    boxCol.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
                                }
                            }
                        }
                    }
                }
                return;
            }

            // Nieve y Apocaliptico usan spawn circular en los bordes
            int numDecorations = 10;
            float minRadius = 24f; 
            float maxRadius = 28f;

            if (m_SelectedMap == 1) // Nieve
            {
                paths = new string[] { "Casitas y huevadas/Small Building/small_buildingA", "Casitas y huevadas/Sign Hospital/sign_hospital" };
                numDecorations = 10;
                scaleMin = 2f;
                scaleMax = 3f;
            }
            else // Apocaliptico
            {
                paths = new string[] { "Casitas y huevadas/Low Building/low_buildingA", "Casitas y huevadas/Low Wide/low_wideA" };
                numDecorations = 12;
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

                    // Ensure solid collider on perimeter buildings so tanks cannot pass through
                    Collider[] bCols = obj.GetComponentsInChildren<Collider>();
                    if (bCols.Length == 0)
                    {
                        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
                        if (rends.Length > 0)
                        {
                            Bounds b = rends[0].bounds;
                            for (int r = 1; r < rends.Length; r++) b.Encapsulate(rends[r].bounds);
                            BoxCollider boxCol = obj.AddComponent<BoxCollider>();
                            boxCol.center = obj.transform.InverseTransformPoint(b.center);
                            Vector3 localSize = obj.transform.InverseTransformVector(b.size);
                            boxCol.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
                        }
                    }
                }
            }
        }

        private void StartGame(int totalTanks)
        {
            try
            {
                ApplyScenario();
                SpawnDecorations();

                // Enrich scene with radars, helipad, explosive barrels, wrecks and hostile turrets
                Rigidbody shellRef = m_TankPrefab != null ? m_TankPrefab.GetComponent<TankShooting>()?.m_Shell : null;
                SceneEnricher.EnrichCurrentScene(shellRef);

                // Add dynamic Power-Up spawner
                if (GetComponent<PowerUpSpawner>() == null)
                {
                    gameObject.AddComponent<PowerUpSpawner>();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[GameManager] Environment initialization note: " + ex.Message);
            }

            TankManager[] newTanks = new TankManager[totalTanks];
            for (int i = 0; i < totalTanks; i++) {
                newTanks[i] = new TankManager();
                newTanks[i].m_PlayerColor = m_AvailableColors[m_SelectedColors[i]];
                newTanks[i].m_PlayerNumber = i + 1;
                
                if (i < m_Tanks.Length && m_Tanks[i] != null && m_Tanks[i].m_SpawnPoint != null) {
                    newTanks[i].m_SpawnPoint = m_Tanks[i].m_SpawnPoint;
                } else {
                    GameObject sp = new GameObject("SpawnPoint" + (i + 1));
                    sp.transform.position = new Vector3(Mathf.Cos(i * Mathf.PI/2) * 12, 0.35f, Mathf.Sin(i * Mathf.PI/2) * 12);
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

        private void DrawInGameControlsHUD()
        {
            GUI.matrix = Matrix4x4.identity;
            GUIStyle hudBox = new GUIStyle(GUI.skin.box);
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0, 0, 0, 0.75f));
            tex.Apply();
            hudBox.normal.background = tex;

            GUIStyle textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 13;
            textStyle.normal.textColor = Color.white;
            textStyle.fontStyle = FontStyle.Bold;

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
                    if (m_SelectedMode == 0 && i > 0)
                    {
                        info = $"<color=#{colorHex}>Bot {i}</color>: IA Enemiga Hostil";
                    }
                    else if (i == 0)
                    {
                        info = $"<color=#{colorHex}>Jugador 1</color>: Disparo [BARRA ESPACIADORA] | Mover [W,A,S,D]";
                    }
                    else if (i == 1)
                    {
                        info = $"<color=#{colorHex}>Jugador 2</color>: Disparo [ENTER] | Mover [FLECHAS]";
                    }
                    else if (i == 2)
                    {
                        info = $"<color=#{colorHex}>Jugador 3</color>: Disparo [SHIFT DERECHO] | Mover [I,J,K,L]";
                    }
                    else if (i == 3)
                    {
                        info = $"<color=#{colorHex}>Jugador 4</color>: Disparo [ENTER NUMPAD] | Mover [NUMPAD 8,4,5,6]";
                    }

                    GUIStyle pStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 };
                    GUILayout.Label(info, pStyle);
                }
            }
            GUILayout.EndArea();
        }

        private void ApplyTankTypes(int totalTanks)
        {
            for (int i = 0; i < totalTanks; i++) {
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
                Vector3 spawnPos = m_Tanks[i].m_SpawnPoint.position + Vector3.up * 0.35f;
                m_Tanks[i].m_Instance =
                    Instantiate(m_TankPrefab, spawnPos, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
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
            m_MessageText.text = "¡RONDA " + m_RoundNumber + "!\nPREPÁRATE...";

            yield return new WaitForSeconds(0.9f);
            m_MessageText.text = "¡A LUCHAR!";
            yield return new WaitForSeconds(0.4f);
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