using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class CompanionDialogueManager : MonoBehaviour
{
    public static CompanionDialogueManager Instance { get; private set; }

    [System.Serializable]
    public struct DialogueLine
    {
        public string speaker;
        public string text;
        public string portraitKey;

        public DialogueLine(string spk, string txt, string key)
        {
            speaker = spk;
            text = txt;
            portraitKey = key;
        }
    }

    private Texture2D portKaelNormal;
    private Texture2D portKaelShout;
    private Texture2D portNexaNormal;
    private Texture2D portNexaAlert;
    private Texture2D panelCyanTex;
    private Texture2D panelCrimsonTex;
    private Texture2D panelGoldTex;

    private Queue<DialogueLine> lineQueue = new Queue<DialogueLine>();
    private DialogueLine currentLine;
    private bool isShowingDialogue = false;
    private float charTimer = 0f;
    private int visibleChars = 0;
    private float autoAdvanceTimer = 0f;

    private HashSet<int> triggeredWaves = new HashSet<int>();

    // --- AUDIO SINTÉTICO Y EFECTOS CINEMÁTICOS PARA SUBTÍTULOS Y VIÑETAS ---
    private AudioSource voiceSource;
    private AudioClip clipVoiceKael;
    private AudioClip clipVoiceNexa;
    private AudioClip clipVoiceAlert;
    private AudioClip clipCinematicBoom;
    private int lastSpokenCharIndex = -1;

    public static CompanionDialogueManager EnsureExists()
    {
        if (Instance != null) return Instance;
        CompanionDialogueManager existing = FindAnyObjectByType<CompanionDialogueManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("CompanionDialogueManager");
        Instance = go.AddComponent<CompanionDialogueManager>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Instance.gameObject != gameObject &&
                Instance.GetComponent<PlayerStats>() == null &&
                Instance.GetComponent<WaveSpawner>() == null)
            {
                Destroy(Instance.gameObject);
            }
            else if (GetComponent<PlayerStats>() == null && GetComponent<WaveSpawner>() == null)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                Destroy(this);
                return;
            }
        }
        Instance = this;

        portKaelNormal  = LoadTexAsTexture("StoryUI/Portrait_Kael_Normal");
        portKaelShout   = LoadTexAsTexture("StoryUI/Portrait_Kael_Shout");
        portNexaNormal  = LoadTexAsTexture("StoryUI/Portrait_Nexa_Normal");
        portNexaAlert   = LoadTexAsTexture("StoryUI/Portrait_Nexa_Alert");
        panelCyanTex    = LoadTexAsTexture("StoryUI/Panel_Cyber_Cyan");
        panelCrimsonTex = LoadTexAsTexture("StoryUI/Panel_Cyber_Crimson");
        panelGoldTex    = LoadTexAsTexture("StoryUI/Panel_Cyber_Gold");

        InitSyntheticVoiceAudio();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void InitSyntheticVoiceAudio()
    {
        voiceSource = gameObject.GetComponent<AudioSource>();
        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
        }
        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f;

        clipVoiceKael = CreateSyntheticVoiceClip("Voice_Kael", 165f, 210f, 0.036f, false);
        clipVoiceNexa = CreateSyntheticVoiceClip("Voice_Nexa", 640f, 880f, 0.032f, true);
        clipVoiceAlert = CreateSyntheticVoiceClip("Voice_Alert", 420f, 290f, 0.042f, true);
        clipCinematicBoom = CreateCinematicPulseClip();
    }

    private AudioClip CreateSyntheticVoiceClip(string name, float f0, float f1, float duration, bool harmonicRich)
    {
        int sampleRate = 22050;
        int samples = Mathf.Max(64, Mathf.FloorToInt(sampleRate * duration));
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float p = (float)i / samples;
            float env = Mathf.Sin(p * Mathf.PI) * Mathf.Exp(-p * 2.2f);
            float freq = Mathf.Lerp(f0, f1, p);
            float s = Mathf.Sin(2f * Mathf.PI * freq * t);
            if (harmonicRich)
            {
                s = s * 0.65f + Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.35f;
            }
            data[i] = Mathf.Clamp(s * env * 0.32f, -0.8f, 0.8f);
        }
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateCinematicPulseClip()
    {
        int sampleRate = 22050;
        float duration = 0.42f;
        int samples = Mathf.FloorToInt(sampleRate * duration);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float p = (float)i / samples;
            float env = Mathf.Clamp01(p / 0.08f) * Mathf.Exp(-p * 4.2f);
            float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(135f, 48f, p) * t) * 0.65f;
            float shimmer = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(520f, 780f, p) * t) * 0.25f;
            data[i] = Mathf.Clamp((sub + shimmer) * env * 0.6f, -0.9f, 0.9f);
        }
        AudioClip clip = AudioClip.Create("SFX_Cinematic_Pulse", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void PlaySyntheticVoiceTick(string portraitKey, int charIndex)
    {
        if (charIndex <= 0 || charIndex == lastSpokenCharIndex || (charIndex % 3 != 0)) return;
        lastSpokenCharIndex = charIndex;

        if (AudioManager.Instance != null && AudioManager.Instance.isMuted) return;
        if (voiceSource == null) InitSyntheticVoiceAudio();
        if (voiceSource == null) return;

        float vol = (AudioManager.Instance != null) ? AudioManager.Instance.masterVolume * 0.42f : 0.35f;
        AudioClip clip = clipVoiceNexa;
        if (!string.IsNullOrEmpty(portraitKey))
        {
            if (portraitKey.StartsWith("kael")) clip = clipVoiceKael;
            else if (portraitKey.Contains("alert") || portraitKey.Contains("dark")) clip = clipVoiceAlert;
        }
        if (clip != null)
        {
            voiceSource.pitch = UnityEngine.Random.Range(0.94f, 1.07f);
            voiceSource.PlayOneShot(clip, vol);
        }
    }

    public void PlayCinematicTransitionEffect()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.isMuted) return;
        if (voiceSource == null) InitSyntheticVoiceAudio();
        if (voiceSource != null && clipCinematicBoom != null)
        {
            float vol = (AudioManager.Instance != null) ? AudioManager.Instance.masterVolume * 0.68f : 0.55f;
            voiceSource.pitch = 1f;
            voiceSource.PlayOneShot(clipCinematicBoom, vol);
        }
    }

    Texture2D LoadTexAsTexture(string path)
    {
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex != null) return tex;
        Sprite sp = Resources.Load<Sprite>(path);
        return (sp != null) ? sp.texture : null;
    }

    void Start()
    {
        TriggerWaveDialogue(1);
    }

    void Update()
    {
        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null && !triggeredWaves.Contains(spawner.currentWave))
        {
            TriggerWaveDialogue(spawner.currentWave);
        }

        if (!isShowingDialogue) return;

        if (visibleChars < currentLine.text.Length)
        {
            charTimer += Time.unscaledDeltaTime * 48f;
            visibleChars = Mathf.Clamp(Mathf.FloorToInt(charTimer), 0, currentLine.text.Length);
            PlaySyntheticVoiceTick(currentLine.portraitKey, visibleChars);
        }
        else
        {
            autoAdvanceTimer += Time.unscaledDeltaTime;
            if (autoAdvanceTimer >= 4.5f)
            {
                NextLine();
            }
        }
    }

    public void ShowCustomMessage(string speaker, string text, Color accentColor, float duration = 4.5f)
    {
        lineQueue.Clear();
        isShowingDialogue = false;
        EnqueueLine(speaker, text, "nexa_normal");
    }

    public void TriggerWaveDialogue(int wave)
    {
        if (triggeredWaves.Contains(wave)) return;
        triggeredWaves.Add(wave);

        string sceneName = SceneManager.GetActiveScene().name;
        bool isLevel4 = (sceneName == "04_Final_NexusThroneroom") ||
                        (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 4);
        bool isLevel3 = !isLevel4 && ((sceneName == "03_Nivel3_Colmena") ||
                        (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 3));
        bool isLevel2 = !isLevel4 && !isLevel3 && ((sceneName == "Lab_Zahir") ||
                        (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 2));

        // --- NIVEL 4 DEFINITIVO: EL TRONO DE LA DISONANCIA (CÚPULA NAVE NÚCLEO) ---
        if (isLevel4)
        {
            if (wave == 1)
            {
                EnqueueLine("N.E.X.A. [IA TÁCTICA]", "Hemos alcanzado la cúpula superior de la Nave Núcleo: el Trono de la Disonancia. Frente al ventanal orbital de la Tierra yace la cápsula de bio-conversión de Lía.", "nexa_normal");
                EnqueueLine("KAEL", "¡Lía! Está suspendida entre esos rayos violetas... ¡Guardia Pretoriana Vorst al frente! Ningún escudo ni trinchera evitará que la libere.", "kael_shout");
            }
            else if (wave == 3)
            {
                EnqueueLine("N.E.X.A. [IA TÁCTICA]", "Cuando estés junto a la cápsula de Lía puedes pulsar [E] para sincronizar un pulso fraternal de Nexo (+15% HP y +25% Carga Ultimate).", "nexa_normal");
            }
            else if (wave == 5)
            {
                EnqueueLine("N.E.X.A. [ALERTA]", "¡Amenaza Pretoriana! El Custodio Pretoriano Vorst salta hacia nuestra posición con su lanza de energía. ¡Sal del círculo rojo de advertencia o bloquea con Parry [S]!", "nexa_alert");
                EnqueueLine("KAEL", "¡Leeré su caída y destrozaré su lanza en el aire!", "kael_shout");
            }
            else if (wave == 8)
            {
                EnqueueLine("N.E.X.A. [CONFESIÓN CRÍTICA]", "Kael... antes de enfrentar al Trono debes saber la verdad que oculté en mi matriz: el Guantelete Nexo fue forjado usando el corazón del primer Soberano Vorst.", "nexa_alert");
                EnqueueLine("N.E.X.A. [CONFESIÓN CRÍTICA]", "Si desatas toda tu furia sin control contra el núcleo, corres el riesgo de absorber su corona y sustituir al tirano en lugar de destruirlo.", "nexa_alert");
                EnqueueLine("KAEL", "No seré su marioneta, N.E.X.A. Lucho por Lía y por la Tierra, no para heredar un trono manchado.", "kael_shout");
            }
            else if (wave == 10)
            {
                EnqueueLine("N.E.X.A. [EMERGENCIA FINAL]", "¡EL ARCONTE SUPREMO — SOBERANO VORST desciende al combate! Prepárate: Duelo Marcial en Fase 1, pilares orbitales en Fase 2 y Fusión con el Núcleo en Fase 3.", "nexa_alert");
                EnqueueLine("N.E.X.A. [TÁCTICA NEXO]", "Cuando active su Campo de Fuerza en Fase 3, encadena combos hasta Sobremarcha (x30) y dispara la Ultimate 'Sobrecarga Nexo' [K] para quebrarlo.", "nexa_normal");
                EnqueueLine("KAEL", "¡Todo termina aquí, Soberano! ¡Por la humanidad y por mi hermana Lía!", "kael_shout");
            }
            return;
        }

        // --- BLOQUE 5 (Issue #23): DIÁLOGOS EXCLUSIVOS DEL NIVEL 3 (COLMENA DE BIO-CABLES) ---
        if (isLevel3)
        {
            if (wave == 1)
            {
                EnqueueLine("N.E.X.A. [IA TÁCTICA]", "Hemos ingresado al corazón de la Nave Núcleo Vorst: el Laberinto de Bio-Cables. Estos conductos orgánicos alimentan miles de cápsulas de estasis con civiles secuestrados del Sector 7.", "nexa_normal");
                EnqueueLine("KAEL", "Los monstruos aquí dentro son quimeras biomecánicas puras... ¡Mantente alerta, N.E.X.A., liberaremos a todos!", "kael_shout");
            }
            else if (wave == 3)
            {
                EnqueueLine("KAEL", "¡Espera! Escuché el eco de la voz de Lía a través del enlace neural del Guantelete... ¡Está muy cerca!", "kael_shout");
                EnqueueLine("N.E.X.A. [IA TÁCTICA]", "Confirmado, Kael. Su bio-señal resuena con tu matriz Nexo, pero su cápsula real ha sido asegurada por el Carcelero en la cámara principal de este sector.", "nexa_normal");
            }
            else if (wave == 5)
            {
                EnqueueLine("N.E.X.A. [ALERTA]", "¡Anomalía psiónica detectada! El Inquisidor Psíquico Vorst desciende rodeado de tres anillos mentales. ¡Bloquea con [S] en el instante exacto para hacer Parry a sus ondas!", "nexa_alert");
                EnqueueLine("KAEL", "¡Ninguna ilusión mental detendrá mi Guantelete!", "kael_shout");
            }
            else if (wave == 8)
            {
                EnqueueLine("N.E.X.A. [REVELACIÓN]", "Kael... los registros genéticos de estos bio-cables coinciden con tu armadura. Tu Guantelete Nexo fue forjado originalmente con tecnología extraída de este mismo núcleo biomecánico.", "nexa_alert");
                EnqueueLine("KAEL", "Entonces usaré su propia creación para destruir su colmena desde adentro.", "kael_normal");
            }
            else if (wave == 10)
            {
                EnqueueLine("N.E.X.A. [EMERGENCIA]", "¡ALERTA EXTREMA! El techo de la cámara colapsa: ¡El Carcelero Biotecnológico VORTEX-OMEGA ha descendido! Custodia el reactor maestro de las cápsulas de estasis.", "nexa_alert");
                EnqueueLine("KAEL", "¡VORTEX-OMEGA! ¡Devuélveme a Lía y libera a mi gente ahora mismo!", "kael_shout");
            }
            return;
        }

        if (isLevel2)
        {
            if (wave == 1)
            {
                EnqueueLine("N.E.X.A. [IA TÁCTICA]", "Hemos penetrado el perímetro de la Base Vorst. La esfera de convergencia púrpura al fondo alimenta los escudos de la Nave Núcleo donde está Lía.", "nexa_normal");
                EnqueueLine("KAEL", "Los Vorst de esta fortaleza son más agresivos. ¡No importa cuántos envíen, derribaremos esa torre!", "kael_shout");
            }
            else if (wave == 3)
            {
                EnqueueLine("N.E.X.A. [IA TÁCTICA]", "Las lecturas de blindaje enemigo se han duplicado en este sector. Mantén tus mejoras de Fuerza y Vigor al día con el oro recolectado.", "nexa_normal");
            }
            else if (wave == 5)
            {
                EnqueueLine("N.E.X.A. [ALERTA]", "¡Impacto sísmico al frente! Un Demoledor Bruto Vorst con reactor pectoral carmesí se aproxima. ¡Sus puñetazos propulsados son letales!", "nexa_alert");
                EnqueueLine("KAEL", "¡Apuntaré directo al núcleo rojo de su pecho!", "kael_shout");
            }
            else if (wave == 8)
            {
                EnqueueLine("N.E.X.A. [ALERTA]", "Estamos cerca de la cámara del reactor. Detecto una entidad psiónica de clase Soberano levitando sobre la plataforma principal.", "nexa_alert");
            }
            else if (wave == 10)
            {
                EnqueueLine("N.E.X.A. [EMERGENCIA]", "¡ALERTA DE JEFE DE ZONA! El Arconte Vorst ha descendido con 4 orbes de energía psiónica. ¡Ataca a distancia con rayos de plasma magenta!", "nexa_alert");
                EnqueueLine("KAEL", "¡Tus orbes no te salvarán, Arconte! ¡Sobrecarga Nexo al límite!", "kael_shout");
            }
            return;
        }

        if (wave == 1)
        {
            EnqueueLine("N.E.X.A. [IA TÁCTICA]", "Sistemas del Guantelete Nexo al 100%. Kael, he triangulado la cápsula donde retienen a tu hermana Lía: está en la Nave Núcleo sobre el Sector 7.", "nexa_normal");
            EnqueueLine("KAEL", "¡Resiste, Lía! N.E.X.A., desvía toda la energía a los propulsores de impacto. Ninguna horda Vorst me va a detener.", "kael_shout");
            EnqueueLine("N.E.X.A. [IA TÁCTICA]", "Enjambre de Caminantes Vorst al frente. Recolecta el oro de sus núcleos para mejorar tus atributos en tiempo real.", "nexa_normal");
        }
        else if (wave == 3)
        {
            EnqueueLine("N.E.X.A. [IA TÁCTICA]", "Lecturas de resonancia subiendo. Cuando el Guantelete llegue al 100% de carga, activa la Sobrecarga Nexo para barrer toda la avenida.", "nexa_normal");
        }
        else if (wave == 5)
        {
            EnqueueLine("N.E.X.A. [ALERTA]", "¡Cuidado, Kael! Detecto firmas sísmicas pesadas. Un Bruto Vorst acorazado lidera esta oleada. ¡Atento cuando brille en naranja!", "nexa_alert");
            EnqueueLine("KAEL", "Entre más grande sea su armadura, más fuerte caerá contra el asfalto.", "kael_normal");
        }
        else if (wave == 8)
        {
            EnqueueLine("N.E.X.A. [ALERTA]", "Kael, tu pulso cardíaco muestra Disonancia Nexo. El Guantelete te da poder, pero si absorbes demasiada energía Vorst pondrás en riesgo tu humanidad.", "nexa_alert");
            EnqueueLine("KAEL", "No me importa el precio mientras logre sacar a Lía con vida de esa nave.", "kael_normal");
        }
        else if (wave == 10)
        {
            EnqueueLine("N.E.X.A. [EMERGENCIA]", "¡ALERTA MÁXIMA! El Comandante Vorst del Sector 7 ha descendido para bloquear el puente. ¡Guarda tu Sobrecarga Nexo para romper su guardia!", "nexa_alert");
            EnqueueLine("KAEL", "¡Es hora de acabar con el líder de este sector! ¡A máxima potencia, N.E.X.A.!", "kael_shout");
        }
    }

    public void EnqueueLine(string speaker, string text, string portraitKey)
    {
        lineQueue.Enqueue(new DialogueLine(speaker, text, portraitKey));
        if (!isShowingDialogue)
        {
            NextLine();
        }
    }

    public void NextLine()
    {
        if (lineQueue.Count > 0)
        {
            currentLine = lineQueue.Dequeue();
            isShowingDialogue = true;
            charTimer = 0f;
            visibleChars = 0;
            autoAdvanceTimer = 0f;
            lastSpokenCharIndex = -1;
        }
        else
        {
            isShowingDialogue = false;
        }
    }

    // --- FUNCIONES AUXILIARES DE ESCALADO NATIVO (1920x1080 -> Píxeles Reales) ---
    Rect ScaleRect(float x, float y, float w, float h)
    {
        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;
        return new Rect(
            Mathf.Round(x * sx),
            Mathf.Round(y * sy),
            Mathf.Round(w * sx),
            Mathf.Round(h * sy)
        );
    }

    int ScaleFont(int baseSize)
    {
        float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        return Mathf.Max(10, Mathf.RoundToInt(baseSize * scale));
    }

    void DrawStyledText(Rect r, string txt, int baseSize, TextAnchor align, Color col, bool wordWrap = false)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.richText = true;
        st.fontSize = ScaleFont(baseSize);
        st.fontStyle = FontStyle.Bold;
        st.alignment = align;
        st.wordWrap = wordWrap;

        string shadowTxt = System.Text.RegularExpressions.Regex.Replace(txt, "<color=[^>]+>|</color>", "");
        st.normal.textColor = new Color(0f, 0f, 0f, 0.92f);
        GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), shadowTxt, st);

        st.normal.textColor = col;
        GUI.Label(r, txt, st);
    }

    void OnGUI()
    {
        if (!isShowingDialogue) return;
        if (BridgeLevel1To2Controller.Instance != null &&
            BridgeLevel1To2Controller.Instance.isBridgeActive &&
            !BridgeLevel1To2Controller.Instance.isInPlayablePhase)
            return;
        if (EndingCinematicController.Instance != null &&
            EndingCinematicController.Instance.isEndingActive)
            return;
        if (EndingsManager.Instance != null &&
            EndingsManager.Instance.isCinematicActive)
            return;
        if (PlayerStats.Instance != null && PlayerStats.Instance.IsAnyBlockingModalOpen)
            return;

        GUI.matrix = Matrix4x4.identity;

        Rect box = ScaleRect(490f, 94f, 940f, 174f);
        bool isAlert = currentLine.portraitKey == "nexa_alert";
        bool isKael = currentLine.portraitKey.StartsWith("kael");
        bool isCorruptMoral = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);

        Color borderCol = isAlert ? new Color(1f, 0.25f, 0.4f, 0.96f) : (isKael ? new Color(1f, 0.82f, 0.22f, 0.96f) : new Color(0.22f, 0.92f, 1f, 0.96f));
        if (!isKael && isCorruptMoral)
        {
            borderCol = new Color(1f, 0.18f, 0.42f, 0.96f);
        }
        Color bgCol = new Color(0.03f, 0.06f, 0.12f, 0.96f);

        Texture2D panelSkin = (isAlert || (!isKael && isCorruptMoral)) ? panelCrimsonTex : (isKael ? panelGoldTex : panelCyanTex);
        if (panelSkin != null)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.98f);
            GUI.DrawTexture(box, panelSkin, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = borderCol;
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = bgCol;
            GUI.DrawTexture(new Rect(box.x + 2f, box.y + 2f, box.width - 4f, box.height - 4f), Texture2D.whiteTexture);
            GUI.color = borderCol;
            GUI.DrawTexture(new Rect(box.x, box.y, box.width, 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Panel del Retrato con marco luminoso
        Rect portFrame = ScaleRect(512f, 110f, 142f, 142f);
        GUI.color = borderCol;
        GUI.DrawTexture(portFrame, Texture2D.whiteTexture);
        Rect portRect = new Rect(portFrame.x + 2f, portFrame.y + 2f, portFrame.width - 4f, portFrame.height - 4f);
        GUI.color = new Color(0.04f, 0.07f, 0.14f, 1f);
        GUI.DrawTexture(portRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        Texture2D activePort = GetPortraitTexture(currentLine.portraitKey);
        if (activePort != null)
        {
            GUI.DrawTexture(portRect, activePort, ScaleMode.ScaleToFit);
        }

        // Efecto de escaneo holográfico + BLOQUE 4: Líneas de interferencia roja (Glitch) en Corrupción Vorst
        if (!isKael)
        {
            float scanY = portRect.y + Mathf.PingPong(Time.unscaledTime * (portRect.height * 0.7f), Mathf.Max(1f, portRect.height - 3f));
            GUI.color = new Color(borderCol.r, borderCol.g, borderCol.b, 0.42f);
            GUI.DrawTexture(new Rect(portRect.x, scanY, portRect.width, 2f), Texture2D.whiteTexture);

            if (isCorruptMoral)
            {
                // Glitch rojo de interferencia biológica sobre N.E.X.A.
                for (int g = 0; g < 3; g++)
                {
                    float gy = portRect.y + Mathf.Repeat((Time.unscaledTime * (65f + g * 37f)) + g * 41f, portRect.height - 4f);
                    GUI.color = new Color(1f, 0.10f, 0.28f, 0.55f);
                    GUI.DrawTexture(new Rect(portRect.x, gy, portRect.width, 3f), Texture2D.whiteTexture);
                }
            }
            GUI.color = Color.white;
        }

        // Nombre del hablante a resolución nativa
        string speakerHeader = "📡 " + currentLine.speaker;
        if (!isKael && isCorruptMoral)
        {
            speakerHeader += "  [⚠ GLITCH: SOBRECARGA VORST]";
        }
        DrawStyledText(ScaleRect(672f, 108f, 736f, 32f), speakerHeader, 19, TextAnchor.MiddleLeft, borderCol, false);

        // Texto con fuente nítida nativa
        string shownText = currentLine.text.Substring(0, Mathf.Clamp(visibleChars, 0, currentLine.text.Length));
        DrawStyledText(ScaleRect(672f, 144f, 736f, 88f), shownText, 19, TextAnchor.UpperLeft, Color.white, true);

        // Indicador de avance
        string hintLabel = (visibleChars < currentLine.text.Length) ? "[Toca para completar]" : "[Toca para continuar ▶]";
        DrawStyledText(ScaleRect(672f, 234f, 736f, 24f), hintLabel, 14, TextAnchor.MiddleRight, new Color(0.72f, 0.88f, 1f), false);

        if (GUI.Button(box, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

            if (visibleChars < currentLine.text.Length)
                visibleChars = currentLine.text.Length;
            else
                NextLine();
        }
    }

    Texture2D GetPortraitTexture(string key)
    {
        switch (key)
        {
            case "kael_normal": return portKaelNormal;
            case "kael_shout":  return portKaelShout;
            case "nexa_alert":  return portNexaAlert;
            default:            return portNexaNormal;
        }
    }
}
