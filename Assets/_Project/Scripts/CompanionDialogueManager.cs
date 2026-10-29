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

    private Queue<DialogueLine> lineQueue = new Queue<DialogueLine>();
    private DialogueLine currentLine;
    private bool isShowingDialogue = false;
    private float charTimer = 0f;
    private int visibleChars = 0;
    private float autoAdvanceTimer = 0f;

    private HashSet<int> triggeredWaves = new HashSet<int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        portKaelNormal = LoadTexAsTexture("StoryUI/Portrait_Kael_Normal");
        portKaelShout  = LoadTexAsTexture("StoryUI/Portrait_Kael_Shout");
        portNexaNormal = LoadTexAsTexture("StoryUI/Portrait_Nexa_Normal");
        portNexaAlert  = LoadTexAsTexture("StoryUI/Portrait_Nexa_Alert");
    }

    Texture2D LoadTexAsTexture(string path)
    {
        Sprite sp = Resources.Load<Sprite>(path);
        return (sp != null) ? sp.texture : Resources.Load<Texture2D>(path);
    }

    void Start()
    {
        TriggerWaveDialogue(1);
    }

    void Update()
    {
        WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
        if (spawner != null && !triggeredWaves.Contains(spawner.currentWave))
        {
            TriggerWaveDialogue(spawner.currentWave);
        }

        if (!isShowingDialogue) return;

        if (visibleChars < currentLine.text.Length)
        {
            charTimer += Time.unscaledDeltaTime * 48f;
            visibleChars = Mathf.Clamp(Mathf.FloorToInt(charTimer), 0, currentLine.text.Length);
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

    public void TriggerWaveDialogue(int wave)
    {
        if (triggeredWaves.Contains(wave)) return;
        triggeredWaves.Add(wave);

        bool isLevel2 = (SceneManager.GetActiveScene().name == "Lab_Zahir") ||
                        (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 2);

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
        }
        else
        {
            isShowingDialogue = false;
        }
    }

    void OnGUI()
    {
        if (!isShowingDialogue) return;
        if (PlayerStats.Instance != null && (PlayerStats.Instance.isPaused || PlayerStats.Instance.isGameOver || PlayerStats.Instance.isLevelCompleted || PlayerStats.Instance.isChoosingDilemma || PlayerStats.Instance.isChoosingSkill))
            return;

        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1f));

        Rect box = new Rect(520, 110, 880, 160);
        bool isAlert = currentLine.portraitKey == "nexa_alert";
        bool isKael = currentLine.portraitKey.StartsWith("kael");

        Color borderCol = isAlert ? new Color(1f, 0.25f, 0.4f, 0.95f) : (isKael ? new Color(1f, 0.82f, 0.22f, 0.95f) : new Color(0.22f, 0.92f, 1f, 0.95f));
        Color bgCol = new Color(0.03f, 0.06f, 0.12f, 0.94f);

        // Borde y fondo de la caja
        GUI.color = borderCol;
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = bgCol;
        GUI.DrawTexture(new Rect(box.x + 3, box.y + 3, box.width - 6, box.height - 6), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Panel del Retrato (136x136 píxeles, llenando el espacio izquierdo)
        Rect portRect = new Rect(box.x + 12, box.y + 12, 136, 136);
        GUI.color = new Color(0.06f, 0.10f, 0.18f, 1f);
        GUI.DrawTexture(portRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        Texture2D activePort = GetPortraitTexture(currentLine.portraitKey);
        if (activePort != null)
        {
            GUI.DrawTexture(portRect, activePort, ScaleMode.ScaleAndCrop);
        }

        // Efecto de escaneo holográfico sobre N.E.X.A.
        if (!isKael)
        {
            float scanY = portRect.y + Mathf.PingPong(Time.unscaledTime * 95f, portRect.height - 4f);
            GUI.color = new Color(borderCol.r, borderCol.g, borderCol.b, 0.4f);
            GUI.DrawTexture(new Rect(portRect.x, scanY, portRect.width, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Nombre del hablante
        GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.fontSize = 20;
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.normal.textColor = borderCol;
        GUI.Label(new Rect(box.x + 162, box.y + 12, 540, 30), "📡 " + currentLine.speaker, nameStyle);

        // Texto con fuente más grande y legible
        GUIStyle txtStyle = new GUIStyle(GUI.skin.label);
        txtStyle.fontSize = 19;
        txtStyle.wordWrap = true;
        txtStyle.fontStyle = FontStyle.Bold;
        txtStyle.normal.textColor = Color.white;

        string shownText = currentLine.text.Substring(0, Mathf.Clamp(visibleChars, 0, currentLine.text.Length));
        GUI.Label(new Rect(box.x + 162, box.y + 46, 695, 82), shownText, txtStyle);

        // Indicador de avance
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 14;
        hintStyle.alignment = TextAnchor.LowerRight;
        hintStyle.normal.textColor = new Color(0.7f, 0.85f, 1f);
        GUI.Label(new Rect(box.x + 160, box.y + 128, 695, 24), (visibleChars < currentLine.text.Length) ? "[Toca para completar]" : "[Toca para continuar ▶]", hintStyle);

        if (GUI.Button(box, GUIContent.none, GUIStyle.none))
        {
            if (visibleChars < currentLine.text.Length)
                visibleChars = currentLine.text.Length;
            else
                NextLine();
        }

        GUI.matrix = oldMatrix;
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
