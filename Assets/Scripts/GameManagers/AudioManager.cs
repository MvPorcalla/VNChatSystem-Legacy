// // ============================================
// // AUDIOMANAGER.CS - HANDLES SOUND SYSTEM
// // ============================================

// using UnityEngine;

// public class AudioManager : MonoBehaviour
// {
//     public static AudioManager Instance;

//     public AudioSource musicSource;
//     public AudioSource sfxSource;
//     public AudioSource voiceSource;

//     [Range(0f, 1f)] public float masterVolume = 1f;
//     [Range(0f, 1f)] public float musicVolume = 0.7f;
//     [Range(0f, 1f)] public float sfxVolume = 1f;
//     [Range(0f, 1f)] public float voiceVolume = 1f;

//     public AudioClip buttonClickSound;
//     public AudioClip unlockSound;
//     public AudioClip notificationSound;

//     public bool muteOnFocusLost = true;
//     public bool enableAudio = true;

//     void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);
//             InitializeAudioSystem();
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }

//     void InitializeAudioSystem()
//     {
//         if (musicSource == null) { musicSource = gameObject.AddComponent<AudioSource>(); musicSource.loop = true; }
//         if (sfxSource == null) { sfxSource = gameObject.AddComponent<AudioSource>(); }
//         if (voiceSource == null) { voiceSource = gameObject.AddComponent<AudioSource>(); }
//     }

//     void UpdateVolumes()
//     {
//         float finalMaster = enableAudio ? masterVolume : 0f;
//         if (musicSource != null) musicSource.volume = finalMaster * musicVolume;
//         if (sfxSource != null) sfxSource.volume = finalMaster * sfxVolume;
//         if (voiceSource != null) voiceSource.volume = finalMaster * voiceVolume;
//     }

//     public void PlaySFX(AudioClip clip)
//     {
//         if (!enableAudio || sfxSource == null || clip == null) return;
//         sfxSource.PlayOneShot(clip);
//     }

//     public void PlayMusic(AudioClip clip, bool loop = true)
//     {
//         if (!enableAudio || musicSource == null || clip == null) return;
//         musicSource.clip = clip; musicSource.loop = loop; musicSource.Play();
//     }

//     public void PlayVoice(AudioClip clip)
//     {
//         if (!enableAudio || voiceSource == null || clip == null) return;
//         voiceSource.clip = clip; voiceSource.Play();
//     }

//     public void PlayButtonClick() => PlaySFX(buttonClickSound);
//     public void PlayUnlockSound() => PlaySFX(unlockSound);
//     public void PlayNotification() => PlaySFX(notificationSound);
// }
