using UnityEngine;

// Put this on a prefab with an AudioSource (clip assigned, Play On Awake off, Spatial Blend = 1).
// The bin instantiates the prefab at its own position, the clip plays once, then the object removes itself.
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
