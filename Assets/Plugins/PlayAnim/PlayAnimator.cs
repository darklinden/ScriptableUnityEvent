using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

public class PlayAnimator : MonoBehaviour
{
    public Animator Anim;

#if UNITY_EDITOR
    public List<string> States = new List<string>();

    private void Reset()
    {
        TryGetComponent(out Anim);
        ResetStates();
    }

    private void OnValidate()
    {
        // will cause [Release of invalid GC handle. 
        // The handle is from a previous domain. 
        // The release operation is skipped.]
        // ResetStates();
    }

    [ContextMenu("Reset States")]
    private void ResetStates()
    {
        if (Application.isPlaying) return;

        States.Clear();
        if (Anim != null)
        {
            AnimatorController controller = Anim.runtimeAnimatorController as AnimatorController;
            if (controller != null)
            {
                foreach (var layer in controller.layers)
                {
                    foreach (var state in layer.stateMachine.states)
                    {
                        States.Add(state.state.name);
                    }
                }
            }
        }
    }
#endif

    public void Play(string state)
    {
        if (Anim == null)
        {
            Debug.LogError("Anim is null");
            return;
        }

        if (string.IsNullOrEmpty(state))
        {
            Debug.LogError("state is null or empty");
            return;
        }

#if UNITY_EDITOR
        if (States.IndexOf(state) < 0)
        {
            Debug.LogError($"state {state} not found");
            return;
        }
#endif

        Anim.Play(state, -1, 0);
    }
}
