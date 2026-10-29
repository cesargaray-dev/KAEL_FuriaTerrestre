using UnityEngine;

public class LevelAudioManager : MonoBehaviour
{
    public static LevelAudioManager Instance { get; private set; }

    [Header("Clips de Audio (Arrastra tus MP3/WAV aquí cuando los tengas)")]
    [Tooltip("Si los dejas vacíos, el sistema genera audio sintetizado Cyberpunk automáticamente")]
    public AudioClip backgroundMusicClip;
    public AudioClip kaelPunchClip;
    public AudioClip kaelHeavyPunchClip;
    public AudioClip kaelUltimateLaserClip;
    public AudioClip enemyAttackClip;
    public AudioClip enemyDieClip;
    public AudioClip bossAlertClip;

    [Header("Control de Volumen")]
    [Range(0f, 1f)] public float musicVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 0.65f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;

        // Si aún no se han asignado archivos externos en el Inspector, generamos los clips base en memoria
        if (backgroundMusicClip == null) backgroundMusicClip = GenerateCyberSynthLoop();
        if (kaelPunchClip == null) kaelPunchClip = GenerateImpactSfx(160f, 45f, 0.11f);
        if (kaelHeavyPunchClip == null) kaelHeavyPunchClip = GenerateImpactSfx(110f, 28f, 0.22f);
        if (kaelUltimateLaserClip == null) kaelUltimateLaserClip = GenerateLaserSfx(0.85f);
        if (enemyAttackClip == null) enemyAttackClip = GenerateImpactSfx(240f, 80f, 0.14f);
        if (enemyDieClip == null) enemyDieClip = GenerateCrumbleSfx(0.32f);
        if (bossAlertClip == null) bossAlertClip = GenerateAlarmSfx(0.65f);
    }

    void Start()
    {
        // La música sintetizada de fondo SOLO debe sonar en el Nivel 2 (Lab_Zahir)
        bool isLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Lab_Zahir";
        if (isLevel2 && backgroundMusicClip != null)
        {
            musicSource.clip = backgroundMusicClip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
        else
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }
        }
    }

    void Update()
    {
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    public void PlayPunch(bool isHeavyStep3)
    {
        if (sfxSource == null) return;
        AudioClip clip = isHeavyStep3 ? kaelHeavyPunchClip : kaelPunchClip;
        if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayUltimateLaser()
    {
        if (sfxSource != null && kaelUltimateLaserClip != null)
            sfxSource.PlayOneShot(kaelUltimateLaserClip, sfxVolume);
    }

    public void PlayEnemyAttack()
    {
        if (sfxSource != null && enemyAttackClip != null)
            sfxSource.PlayOneShot(enemyAttackClip, sfxVolume * 0.75f);
    }

    public void PlayEnemyDie()
    {
        if (sfxSource != null && enemyDieClip != null)
            sfxSource.PlayOneShot(enemyDieClip, sfxVolume * 0.85f);
    }

    public void PlayBossAlert()
    {
        if (sfxSource != null && bossAlertClip != null)
            sfxSource.PlayOneShot(bossAlertClip, sfxVolume);
    }

    // --- SÍNTESIS PROCEDURAL LIGERA (RESPALDO HASTA QUE PONGAS TUS PROPIOS AUDIOS) ---
    private AudioClip GenerateCyberSynthLoop()
    {
        int sampleRate = 22050;
        float duration = 4.0f; // Compás de 4 segundos en bucle perfecto
        int totalSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] data = new float[totalSamples];

        // Progresión Cyberpunk oscura en Re menor (D2, F2, G2, A1)
        float[] bassNotes = { 73.42f, 73.42f, 87.31f, 73.42f, 98.00f, 87.31f, 65.41f, 55.00f };
        float stepDuration = duration / bassNotes.Length;

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            int step = Mathf.FloorToInt(t / stepDuration) % bassNotes.Length;
            float localT = (t % stepDuration) / stepDuration;

            float freq = bassNotes[step];
            float env = Mathf.Exp(-localT * 4.2f);
            float saw = 2f * ((t * freq) - Mathf.Floor(0.5f + t * freq));
            float sub = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t);

            // Pulso de bombo electrónico cada 0.5s
            float kickEnv = Mathf.Exp(-localT * 18f);
            float kick = Mathf.Sin(2f * Mathf.PI * (110f * kickEnv + 35f) * localT) * kickEnv;

            data[i] = Mathf.Clamp((saw * 0.22f + sub * 0.35f) * env + kick * 0.40f, -0.8f, 0.8f);
        }

        AudioClip clip = AudioClip.Create("BaseVorst_SynthLoop", totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateImpactSfx(float startFreq, float endFreq, float duration)
    {
        int sampleRate = 22050;
        int samples = Mathf.FloorToInt(sampleRate * duration);
        float[] data = new float[samples];
        System.Random rng = new System.Random(42);

        for (int i = 0; i < samples; i++)
        {
            float progress = (float)i / samples;
            float env = 1f - progress;
            env *= env;
            float freq = Mathf.Lerp(startFreq, endFreq, progress);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * ((float)i / sampleRate));
            float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.35f;
            data[i] = (tone * 0.7f + noise) * env;
        }

        AudioClip clip = AudioClip.Create("Impact_SFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateLaserSfx(float duration)
    {
        int sampleRate = 22050;
        int samples = Mathf.FloorToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / samples;
            float env = Mathf.Sin(progress * Mathf.PI);
            float freq = 520f + Mathf.Sin(t * 45f) * 140f;
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.6f;
        }

        AudioClip clip = AudioClip.Create("Laser_SFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateCrumbleSfx(float duration)
    {
        int sampleRate = 22050;
        int samples = Mathf.FloorToInt(sampleRate * duration);
        float[] data = new float[samples];
        System.Random rng = new System.Random(101);

        for (int i = 0; i < samples; i++)
        {
            float progress = (float)i / samples;
            float env = 1f - progress;
            float lowTone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(140f, 38f, progress) * ((float)i / sampleRate));
            float crackle = ((float)rng.NextDouble() * 2f - 1f) * 0.45f;
            data[i] = (lowTone * 0.55f + crackle) * env;
        }

        AudioClip clip = AudioClip.Create("Crumble_SFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateAlarmSfx(float duration)
    {
        int sampleRate = 22050;
        int samples = Mathf.FloorToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = (t % 0.32f < 0.16f) ? 440f : 330f;
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f;
        }

        AudioClip clip = AudioClip.Create("BossAlarm_SFX", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
