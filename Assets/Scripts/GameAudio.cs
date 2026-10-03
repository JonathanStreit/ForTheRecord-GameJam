using UnityEngine;

/// <summary>
/// Plays one-shot sound effects in 2D (same volume wherever the game camera is).
/// </summary>
public static class GameAudio
{
    static AudioSource _source;

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (_source == null)
        {
            _source = new GameObject("Game Audio").AddComponent<AudioSource>();
            _source.spatialBlend = 0f;
        }
        _source.PlayOneShot(clip, volume);
    }
}
