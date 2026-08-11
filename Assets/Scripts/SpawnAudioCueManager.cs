using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnAudioCueManager : MonoBehaviour
{
    private static SpawnAudioCueManager _instance;

    public static SpawnAudioCueManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SpawnAudioCueManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SpawnAudioCueManager");
                    _instance = go.AddComponent<SpawnAudioCueManager>();
                }
            }
            return _instance;
        }
    }

    private AudioSource audioSource;
    private Dictionary<string, AudioClip> proceduralClips = new Dictionary<string, AudioClip>();

    private readonly float[] colFrequencies = new float[] { 261.63f, 293.66f, 329.63f, 392.00f, 440.00f };

    private readonly float[] rowMultipliers = new float[] { 1.50f, 1.25f, 1.00f, 0.75f };

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSource();
        EnsureAudioListener();
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D crisp sound
            audioSource.volume = 1.0f;
            audioSource.mute = false;
        }
    }

    private void EnsureAudioListener()
    {
        if (AudioListener.pause)
        {
            AudioListener.pause = false;
        }

        if (Camera.main != null && Camera.main.GetComponent<AudioListener>() == null)
        {
            Camera.main.gameObject.AddComponent<AudioListener>();
        }
    }

    public void PlaySpawnCue(int row, int col, IEnemyBehaviour enemy)
    {
        EnsureAudioSource();
        EnsureAudioListener();

        int safeCol = Mathf.Clamp(col, 0, colFrequencies.Length - 1);
        int safeRow = Mathf.Clamp(row, 0, rowMultipliers.Length - 1);

        float colFreq = colFrequencies[safeCol];
        float rowMultiplier = rowMultipliers[safeRow];
        float targetFreq = colFreq * rowMultiplier;

        StartCoroutine(PlayCueRoutine(targetFreq, enemy));
    }

    private IEnumerator PlayCueRoutine(float freq, IEnemyBehaviour enemy)
    {
        AudioClip gridClip = GetOrCreateTone(freq, 0.12f, ToneType.Sine);
        if (gridClip != null)
        {
            audioSource.PlayOneShot(gridClip, 0.65f);
        }

        yield return new WaitForSeconds(0.05f);

        if (enemy is BombEnemyBehaviour)
        {
            AudioClip bombClip = GetOrCreateTone(140.0f, 0.20f, ToneType.Square);
            if (bombClip != null) audioSource.PlayOneShot(bombClip, 0.8f);
        }
        else if (enemy is GoldenEnemyBehaviour)
        {
            AudioClip goldClip1 = GetOrCreateTone(freq * 1.5f, 0.10f, ToneType.Sine);
            AudioClip goldClip2 = GetOrCreateTone(freq * 2.0f, 0.12f, ToneType.Sine);
            if (goldClip1 != null) audioSource.PlayOneShot(goldClip1, 0.7f);
            yield return new WaitForSeconds(0.04f);
            if (goldClip2 != null) audioSource.PlayOneShot(goldClip2, 0.7f);
        }
        else if (enemy is ToughEnemyBehaviour)
        {
            AudioClip toughClip = GetOrCreateTone(110.0f, 0.18f, ToneType.Triangle);
            if (toughClip != null) audioSource.PlayOneShot(toughClip, 0.85f);
        }
        else if (enemy is NukeEnemyBehaviour)
        {
            AudioClip nukeClip = GetOrCreateTone(freq * 1.25f, 0.25f, ToneType.Triangle);
            if (nukeClip != null) audioSource.PlayOneShot(nukeClip, 0.85f);
        }
        else
        {
            AudioClip stdClip = GetOrCreateTone(freq * 1.25f, 0.10f, ToneType.Sine);
            if (stdClip != null) audioSource.PlayOneShot(stdClip, 0.5f);
        }
    }

    private enum ToneType { Sine, Triangle, Square }

    private AudioClip GetOrCreateTone(float frequency, float duration, ToneType toneType)
    {
        string key = $"{toneType}_{frequency:F1}_{duration:F2}";
        if (proceduralClips.TryGetValue(key, out AudioClip cachedClip) && cachedClip != null)
        {
            return cachedClip;
        }

        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * (4.0f / duration));

            float wave = 0f;
            float phase = 2f * Mathf.PI * frequency * t;

            switch (toneType)
            {
                case ToneType.Sine:
                    wave = Mathf.Sin(phase);
                    break;
                case ToneType.Triangle:
                    wave = Mathf.PingPong(t * frequency * 4f, 2f) - 1f;
                    break;
                case ToneType.Square:
                    wave = Mathf.Sin(phase) >= 0 ? 0.6f : -0.6f;
                    break;
            }

            samples[i] = wave * envelope;
        }

        AudioClip newClip = AudioClip.Create(key, sampleCount, 1, sampleRate, false);
        newClip.SetData(samples, 0);

        proceduralClips[key] = newClip;
        return newClip;
    }
}
