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
    // FIX: If no audio data was passed, exit early instead of crashing!
    if (data == null)
    {
        Debug.LogWarning("SoundManager: Received null WeaponAudioData! Make sure it is assigned on the Weapon component.");
        return;
    }

    AudioClip clipToPlay = data.GetRandomFireClip();
    if (clipToPlay == null) return;

    // ... rest of your audio instantiation code ...
    GameObject temporaryAudioObj = Instantiate(oneShotAudioSourcePrefab, position, Quaternion.identity);
    AudioSource audioSource = temporaryAudioObj.GetComponent<AudioSource>();

    audioSource.clip = clipToPlay;
    audioSource.pitch = 1f + Random.Range(-data.pitchRandomness, data.pitchRandomness);
    
    audioSource.Play();
    Destroy(temporaryAudioObj, clipToPlay.length);
}
}