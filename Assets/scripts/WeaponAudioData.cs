using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponAudio", menuName = "Audio/Weapon Audio Data")]
public class WeaponAudioData : ScriptableObject
{
    [Header("Firing Sounds")]
    public AudioClip[] fireClips;
    [Range(0f, 2f)] public float fireVolume = 1f; 
    
    [Header("Pitch Variation")]
    [Range(0f, 0.3f)] public float pitchRandomness = 0.05f;

    [Header("Other Weapon Sounds")]
    public AudioClip reloadClip;
    [Range(0f, 2f)] public float reloadVolume = 1f;
    public AudioClip emptyClickClip;
    [Range(0f, 2f)] public float emptyClickVolume = 1f;

    // Helper function to fetch a random shot sound
    public AudioClip GetRandomFireClip()
    {
        if (fireClips == null || fireClips.Length == 0) return null;
        int randomIndex = Random.Range(0, fireClips.Length);
        return fireClips[randomIndex];
    }
}