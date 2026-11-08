
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class SceneMusicManager : MonoBehaviour
{
    public static SceneMusicManager Instance;

    [Header("Music Settings")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)]
    private float volume = 0.7f;
    [SerializeField] private bool loop = true;

    private AudioSource audioSource;
    private string originalScenePath;
    private int originalSceneBuildIndex;

    private void Awake()
    {
        // Prevent duplicate music players when restarting.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Scene currentScene = gameObject.scene;
        originalScenePath = currentScene.path;
        originalSceneBuildIndex = currentScene.buildIndex;

        // Keep music alive when reloading the scene.
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.volume = volume;
        audioSource.clip = musicClip;

        if (musicClip != null)
        {
            audioSource.Play();
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Continue playing if the same scene is reloaded.
        bool isSameScene =
            scene.path == originalScenePath &&
            scene.buildIndex == originalSceneBuildIndex;

        if (isSameScene)
        {
            return;
        }

        // Stop music when entering a different scene.
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    public void StopMusic()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}
