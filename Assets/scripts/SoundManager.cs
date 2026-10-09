using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private GameObject oneShotAudioSourcePrefab; // A simple prefab with an AudioSource component

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayWeaponShot(WeaponAudioData data, Vector3 position)
{
    if (data == null)
    {
        Debug.LogWarning("SoundManager: Received null WeaponAudioData! Make sure it is assigned on the Weapon component.");
        return;
    }
    
    AudioClip clipToPlay = data.GetRandomFireClip();
    if (clipToPlay == null) return;

    GameObject temporaryAudioObj = Instantiate(oneShotAudioSourcePrefab, position, Quaternion.identity);
    AudioSource audioSource = temporaryAudioObj.GetComponent<AudioSource>();

    audioSource.clip = clipToPlay;
    audioSource.volume = Mathf.Clamp01(data.fireVolume);
    audioSource.pitch = 1f + Random.Range(-data.pitchRandomness, data.pitchRandomness);
    
    audioSource.Play();
    Destroy(temporaryAudioObj, clipToPlay.length);
}
public void PlayClip(AudioClip clip, Vector3 position, float volume = 1f, float pitchRandomness = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("SoundManager: Received null AudioClip! Make sure it is assigned.");
            return;
        }
        GameObject obj = Instantiate(oneShotAudioSourcePrefab, position, Quaternion.identity);
        AudioSource audioSource = obj.GetComponent<AudioSource>();

        audioSource.clip = clip;
        audioSource.volume = Mathf.Clamp01(volume);
        audioSource.pitch = 1f; 
        audioSource.Play();

        Destroy(obj, clip.length);
    }
}