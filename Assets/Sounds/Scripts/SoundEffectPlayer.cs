using UnityEngine;

public enum SoundEffectPlayerMode {
    MODE_STOPANDPLAY,
    MODE_ONESHOT
}


[System.Serializable]
public class SubSoundEffect {
    public string name = "Layer";
    public AudioSource source;
    public AudioClip[] clips;
    public float pitchMin = 1;
    public float pitchMax = 1;
    public float volumeMin = 1;
    public float volumeMax = 1;

    public float probability = 1;
    public float minDelay = 0;
    public float maxDelay = 0;

    public bool mute = false;

    private int lastUsedIndex = -1;

    public void SafeReset() {
        if(pitchMin == 0 && pitchMax == 0 && volumeMin == 0 && volumeMax == 0) {
            pitchMin = 1;
            pitchMax = 1;
            volumeMin = 1;
            volumeMax = 1;
            probability = 1;
            minDelay = 0;
            maxDelay = 0;
            mute = false;
        }
    }

    private int GetRandomNextIndex() {
        if(clips.Length == 1) {
            return 0;
        }

        int availableSize = clips.Length;
        if(lastUsedIndex>=0) {
            availableSize--;
        }
        int newIndex = Random.Range(0, availableSize);
        if(lastUsedIndex>=0 && newIndex >= lastUsedIndex) {
            newIndex++;
        }
        return newIndex;
    }

    private void PlayNow(SoundEffectPlayerMode playMode) {
        // Check if we didn't get destroyed in the mean time
        if(source == null) {
            return;
        }
        if(mute) {
            return;
        }

        // Vary the pitch & volume
        source.pitch = Random.Range(pitchMin, pitchMax);
        source.volume = Random.Range(volumeMin, volumeMax);

        // Play effect
        lastUsedIndex = GetRandomNextIndex();
        if(playMode == SoundEffectPlayerMode.MODE_STOPANDPLAY) {
            source.Stop();
            source.clip = clips[lastUsedIndex];
            source.Play();
        } else {
            source.PlayOneShot(clips[lastUsedIndex]);
        }
    }

    public void Play(MonoBehaviour owner, SoundEffectPlayerMode playMode) {
        // Effect has a probability
        if(Random.value <= probability) {
            // Delay
            float delay = Random.Range(minDelay, maxDelay);
            if(delay>0) {
                if(Application.isPlaying) {
                    owner.StartCoroutine(PlayDelayedCoroutine(delay, playMode));
                } else {
#if UNITY_EDITOR
                    Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(PlayDelayedCoroutine(delay, playMode), this);
#endif
                }
            } else {
                PlayNow(playMode);
            }
        }
    }
 
    System.Collections.IEnumerator PlayDelayedCoroutine(float delay, SoundEffectPlayerMode playMode)
    {
        if(Application.isPlaying) {
            yield return new WaitForSeconds(delay);
        } else {
#if UNITY_EDITOR
            yield return new Unity.EditorCoroutines.Editor.EditorWaitForSeconds(delay);
#endif
        }
        PlayNow(playMode);
    }

}

public class SoundEffectPlayer: ISoundEffect {

    public SubSoundEffect[] soundLayers;

    public SoundEffectPlayerMode playMode;

    private bool HasSingleSoundLayer() {
        return soundLayers != null && soundLayers.Length == 1;
    }

    [ContextMenu("Init")]
    private void InitInEditor() {
        if(soundLayers != null) {
            foreach (var subSound in soundLayers)
            {
                subSound.SafeReset();
            }
        }
    }

    /**
     * Public, trigger the effect
     */
    [ContextMenu("Play")]
    public override void Trigger() {
        if(soundLayers != null) {
            foreach (var subSound in soundLayers)
            {
                subSound.Play(this, HasSingleSoundLayer()?playMode:SoundEffectPlayerMode.MODE_ONESHOT);
            }
        }
    }
}
