using UnityEngine;

// This is hosted on two prefabs that are configured to play either a correct or incorrect sound.
// The bin instantiates the audio prefab at its own position, the clip plays once, then the object removes itself.
[RequireComponent(typeof(AudioSource))]
public class OneShotAudio : MonoBehaviour
{
    void Start()
    {
        AudioSource source = GetComponent<AudioSource>();

        if (source.clip == null)
        {
            Destroy(gameObject);
            return;
        }

        source.Play();
        Destroy(gameObject, source.clip.length + 0.1f);
    }
}
