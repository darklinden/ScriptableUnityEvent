using System.Collections.Generic;
using UnityEngine;

public class PlayAnim : MonoBehaviour
{
    public Animation Anim;

#if UNITY_EDITOR
    public List<string> Clips = new List<string>();

    private void Reset()
    {
        Anim = GetComponent<Animation>();
        ResetClips();
    }

    private void OnValidate()
    {
        // will cause [Release of invalid GC handle. 
        // The handle is from a previous domain. 
        // The release operation is skipped.]
        // ResetClips();
    }

    [ContextMenu("Reset Clips")]
    private void ResetClips()
    {
        if (Application.isPlaying) return;

        Clips.Clear();
        if (Anim != null)
        {
            foreach (AnimationState state in Anim)
            {
                Clips.Add(state.clip.name);
            }
        }
    }
#endif

    public void Play(string clip)
    {
        if (Anim == null)
        {
            Debug.LogError("Anim is null");
            return;
        }

        if (Anim.isPlaying)
        {
            Anim.Stop();
        }

        if (string.IsNullOrEmpty(clip))
        {
            Anim.Play();
        }
        else
        {
            Anim.Play(clip);
        }
    }
}
