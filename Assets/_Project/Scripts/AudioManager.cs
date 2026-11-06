using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string PREF_VOLUME = "KAEL_MasterVolume";
    private const string PREF_MUTED = "KAEL_IsMuted";

    [Header("Pistas de Música (Resources/Audio)")]
    public AudioClip musicMenuIntro;
    public AudioClip musicSector7Combat;
    public AudioClip musicBossVorst;
    public AudioClip musicVictoryDilemma;

    [Header("Efectos de Sonido Sintetizados")]
    public AudioClip sfxUiClick;
    public AudioClip sfxHitLight;
    public AudioClip sfxHitHeavy;
    public AudioClip sfxUltimateBeam;
    public AudioClip sfxLevelUp;

    [Header("Configuración de Transición")]
    public float crossfadeDuration = 1.2f;
    [Range(0f, 1f)] public float baseMusicScale = 0.55f;
    [Range(0f, 1f)] public float baseSfxScale = 0.80f;

    private float _masterVolume = 0.85f;
    public float masterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PREF_VOLUME, _masterVolume);
            PlayerPrefs.Save();
            ApplyVolumeSettings();
        }
    }

    private bool _isMuted = false;
    public bool isMuted
    {
        get => _isMuted;
        set
        {
            _isMuted = value;
            PlayerPrefs.SetInt(PREF_MUTED, _isMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVolumeSettings();
        }
    }

    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource activeMusicSource;
    private AudioSource fadingOutMusicSource;
    private AudioSource sfxSource;

    private AudioClip currentTargetClip;
    private Coroutine crossfadeCoroutine;
    private float activeSourceBlend = 1f;
    private float fadingSourceBlend = 0f;

    private bool bossMusicActive = false;
    private int lastObservedWave = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("AudioManager_Singleton");
            Instance = go.AddComponent<AudioManager>();
            DontDestroyOnLoad(go);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _masterVolume = PlayerPrefs.GetFloat(PREF_VOLUME, 0.85f);
        _isMuted = PlayerPrefs.GetInt(PREF_MUTED, 0) == 1;

        musicSourceA = ConfigureMusicSource("MusicSource_A");
        musicSourceB = ConfigureMusicSource("MusicSource_B");
        activeMusicSource = musicSourceA;
        fadingOutMusicSource = musicSourceB;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        LoadMusicResources();
        GenerateProceduralSFX();
        ApplyVolumeSettings();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void Start()
    {
        EvaluateSceneMusic(SceneManager.GetActiveScene().name, immediate: false);
    }

    private AudioSource ConfigureMusicSource(string label)
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.priority = 32;
        src.volume = 0f;
        return src;
    }

    private void LoadMusicResources()
    {
        if (musicMenuIntro == null)
            musicMenuIntro = Resources.Load<AudioClip>("Audio/Music_Menu_Intro");
        if (musicSector7Combat == null)
            musicSector7Combat = Resources.Load<AudioClip>("Audio/Music_Sector7_Combat");
        if (musicBossVorst == null)
            musicBossVorst = Resources.Load<AudioClip>("Audio/Music_Boss_Vorst");
        if (musicVictoryDilemma == null)
            musicVictoryDilemma = Resources.Load<AudioClip>("Audio/Music_Victory_Dilemma");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bossMusicActive = false;
        lastObservedWave = -1;
        EvaluateSceneMusic(scene.name, immediate: false);
    }

    private void EvaluateSceneMusic(string sceneName, bool immediate)
    {
        if (sceneName == "00_MainMenu")
        {
            PlayMenuMusic(immediate ? 0.1f : crossfadeDuration);
        }
        else
        {
            PlayCombatMusic(immediate ? 0.1f : crossfadeDuration);
        }
    }

    void Update()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "00_MainMenu")
        {
            if (currentTargetClip != musicMenuIntro && musicMenuIntro != null)
            {
                PlayMenuMusic();
            }
            return;
        }

        // Monitorear estados de dilema moral y victoria en PlayerStats
        if (PlayerStats.Instance != null)
        {
            if (PlayerStats.Instance.isChoosingDilemma || PlayerStats.Instance.isLevelCompleted)
            {
                if (currentTargetClip != musicVictoryDilemma && musicVictoryDilemma != null)
                {
                    PlayVictoryDilemmaMusic();
                }
                return;
            }
        }

        // Monitorear oleadas de WaveSpawner para transiciones entre combate normal y jefes
        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null && spawner.currentWave != lastObservedWave && spawner.currentWave > 0)
        {
            lastObservedWave = spawner.currentWave;
            bool isBossOrMiniBoss = (spawner.currentWave == 5 || spawner.currentWave == spawner.totalWaves);

            if (isBossOrMiniBoss)
            {
                PlayBossMusic();
            }
            else if (bossMusicActive || currentTargetClip != musicSector7Combat)
            {
                PlayCombatMusic();
            }
        }
    }

    public void PlayMenuMusic(float duration = -1f)
    {
        bossMusicActive = false;
        if (musicMenuIntro != null)
            CrossfadeTo(musicMenuIntro, duration < 0f ? crossfadeDuration : duration);
    }

    public void PlayCombatMusic(float duration = -1f)
    {
        bossMusicActive = false;
        if (musicSector7Combat != null)
            CrossfadeTo(musicSector7Combat, duration < 0f ? crossfadeDuration : duration);
    }

    public void PlayBossMusic(float duration = -1f)
    {
        bossMusicActive = true;
        if (musicBossVorst != null)
            CrossfadeTo(musicBossVorst, duration < 0f ? crossfadeDuration : duration);
    }

    public void PlayVictoryDilemmaMusic(float duration = -1f)
    {
        bossMusicActive = false;
        if (musicVictoryDilemma != null)
            CrossfadeTo(musicVictoryDilemma, duration < 0f ? crossfadeDuration : duration);
    }

    public void PlayVictoryAndDilemmaMusic(float duration = -1f)
    {
        PlayVictoryDilemmaMusic(duration);
    }

    public void PlayHeavyHitSfx()
    {
        PlayHitImpact(true);
    }

    public void PlayParryClang(bool isPerfectParry)
    {
        if (_isMuted || sfxSource == null) return;
        AudioClip clip = isPerfectParry ? sfxLevelUp : sfxHitHeavy;
        if (clip != null)
            sfxSource.PlayOneShot(clip, _masterVolume * baseSfxScale * 0.92f);
    }

    public void CrossfadeTo(AudioClip nextClip, float duration = 1.2f)
    {
        if (nextClip == null) return;
        if (currentTargetClip == nextClip && activeMusicSource != null && activeMusicSource.isPlaying)
            return;

        currentTargetClip = nextClip;

        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }

        crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(nextClip, Mathf.Max(0.05f, duration)));
    }

    private IEnumerator CrossfadeRoutine(AudioClip nextClip, float duration)
    {
        // Intercambiar fuentes para que la entrante sea activeMusicSource
        AudioSource previousSource = activeMusicSource;
        activeMusicSource = (previousSource == musicSourceA) ? musicSourceB : musicSourceA;
        fadingOutMusicSource = previousSource;

        activeMusicSource.clip = nextClip;
        activeMusicSource.volume = 0f;
        activeMusicSource.loop = true;
        activeMusicSource.Play();

        float startFadeOutBlend = (fadingOutMusicSource != null && fadingOutMusicSource.isPlaying) ? activeSourceBlend : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Curva suave (SmoothStep) para el crossfade
            float smoothT = t * t * (3f - 2f * t);

            activeSourceBlend = smoothT;
            fadingSourceBlend = startFadeOutBlend * (1f - smoothT);
            ApplyVolumeSettings();

            yield return null;
        }

        activeSourceBlend = 1f;
        fadingSourceBlend = 0f;

        if (fadingOutMusicSource != null)
        {
            fadingOutMusicSource.Stop();
            fadingOutMusicSource.volume = 0f;
        }

        ApplyVolumeSettings();
        crossfadeCoroutine = null;
    }

    public void ApplyVolumeSettings()
    {
        float effectiveMaster = _isMuted ? 0f : _masterVolume;
        float maxMusicVol = effectiveMaster * baseMusicScale;

        if (activeMusicSource != null)
            activeMusicSource.volume = maxMusicVol * activeSourceBlend;

        if (fadingOutMusicSource != null)
            fadingOutMusicSource.volume = maxMusicVol * fadingSourceBlend;

        if (sfxSource != null)
            sfxSource.volume = effectiveMaster * baseSfxScale;

        if (LevelAudioManager.Instance != null)
        {
            LevelAudioManager.Instance.sfxVolume = effectiveMaster * 0.65f;
        }
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        if (!isMuted) PlayUIClick();
    }

    // --- REPRODUCCIÓN DE EFECTOS DE SONIDO SINTETIZADOS ---
    public void PlayUIClick()
    {
        if (_isMuted || sfxSource == null || sfxUiClick == null) return;
        sfxSource.PlayOneShot(sfxUiClick, _masterVolume * baseSfxScale * 0.75f);
    }

    public void PlayHitImpact(bool isHeavy = false)
    {
        if (_isMuted || sfxSource == null) return;
        AudioClip clip = isHeavy ? sfxHitHeavy : sfxHitLight;
        if (clip != null)
            sfxSource.PlayOneShot(clip, _masterVolume * baseSfxScale * (isHeavy ? 0.95f : 0.75f));
    }

    public void PlayUltimateBeam()
    {
        if (_isMuted || sfxSource == null || sfxUltimateBeam == null) return;
        sfxSource.PlayOneShot(sfxUltimateBeam, _masterVolume * baseSfxScale * 1.0f);
    }

    public void PlayLevelUp()
    {
        if (_isMuted || sfxSource == null || sfxLevelUp == null) return;
        sfxSource.PlayOneShot(sfxLevelUp, _masterVolume * baseSfxScale * 0.90f);
    }

    // --- SÍNTESIS PROCEDURAL DE EFECTOS DE SONIDO ---
    private void GenerateProceduralSFX()
    {
        sfxUiClick = CreateUIClickClip();
        sfxHitLight = CreateHitImpactClip(175f, 48f, 0.11f, false);
        sfxHitHeavy = CreateHitImpactClip(115f, 26f, 0.23f, true);
        sfxUltimateBeam = CreateUltimateBeamClip(0.90f);
        sfxLevelUp = CreateLevelUpClip(0.55f);
    }

    private AudioClip CreateUIClickClip()
    {
        int sampleRate = 22050;
        float duration = 0.065f;
        int samples = Mathf.FloorToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / samples;
            float env = Mathf.Exp(-progress * 6.5f);
            // Doble pulso digital cian Nexo (880 Hz -> 1320 Hz)
            float freq = (progress < 0.35f) ? 880f : 1320f;
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            float harmonic = Mathf.Sin(2f * Mathf.PI * (freq * 2f) * t) * 0.3f;
            data[i] = Mathf.Clamp((tone + harmonic) * env * 0.55f, -0.9f, 0.9f);
        }

        AudioClip clip = AudioClip.Create("SFX_UI_Click", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateHitImpactClip(float startFreq, float endFreq, float duration, bool heavy)
    {
        int sampleRate = 22050;
        int samples = Mathf.FloorToInt(sampleRate * duration);
        float[] data = new float[samples];
        System.Random rng = new System.Random(heavy ? 77 : 33);

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / samples;
            float env = (1f - progress) * (1f - progress);
            float freq = Mathf.Lerp(startFreq, endFreq, Mathf.Sqrt(progress));
            float punchTone = Mathf.Sin(2f * Mathf.PI * freq * t);
            float subBass = heavy ? Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.5f : 0f;
            float noise = ((float)rng.NextDouble() * 2f - 1f) * (heavy ? 0.42f : 0.28f) * Mathf.Exp(-progress * 5f);
            data[i] = Mathf.Clamp((punchTone * 0.68f + subBass + noise) * env, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create(heavy ? "SFX_Kael_HeavyHit" : "SFX_Kael_LightHit", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateUltimateBeamClip(float duration)
    {
        int sampleRate = 22050;
        int samples = Mathf.FloorToInt(sampleRate * duration);
        float[] data = new float[samples];
        System.Random rng = new System.Random(99);

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / samples;
            float attack = Mathf.Clamp01(progress / 0.12f);
            float release = Mathf.Clamp01((1f - progress) / 0.22f);
            float env = attack * release;

            float carrierFreq = 460f + Mathf.Sin(t * 52f) * 145f + (progress * 180f);
            float subFreq = 110f + Mathf.Sin(t * 18f) * 25f;
            float beamWave = Mathf.Sin(2f * Mathf.PI * carrierFreq * t) * 0.48f;
            float subWave = Mathf.Sin(2f * Mathf.PI * subFreq * t) * 0.35f;
            float plasmaNoise = ((float)rng.NextDouble() * 2f - 1f) * 0.22f;

            data[i] = Mathf.Clamp((beamWave + subWave + plasmaNoise) * env, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("SFX_Ultimate_Beam", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateLevelUpClip(float duration)
    {
        int sampleRate = 22050;
        int samples = Mathf.FloorToInt(sampleRate * duration);
        float[] data = new float[samples];

        // Arpegio ascendente heroico (Do5 -> Mi5 -> Sol5 -> Do6)
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / samples;
            int noteIdx = Mathf.Clamp(Mathf.FloorToInt(progress * notes.Length), 0, notes.Length - 1);
            float localProg = (progress * notes.Length) - noteIdx;

            float freq = notes[noteIdx];
            float env = Mathf.Exp(-localProg * 2.4f) * (1f - progress * 0.25f);
            float fundamental = Mathf.Sin(2f * Mathf.PI * freq * t);
            float overtone = Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.28f;

            data[i] = Mathf.Clamp((fundamental + overtone) * env * 0.55f, -0.9f, 0.9f);
        }

        AudioClip clip = AudioClip.Create("SFX_Level_Up", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
