using UnityEditor;
using UnityEngine;

public class EventReceiverRoot : MonoBehaviour
{
    private void Awake()
    {
        RegisterReceivers();
    }

    private void Start()
    {
        RegisterReceivers();
    }

    private void OnEnable()
    {
        RegisterReceivers();
    }

    public void RegisterReceivers()
    {
        if (Application.isPlaying)
        {
            var receivers = transform.GetComponentsInChildren<EventReceiver>(true);
            foreach (var receiver in receivers)
            {
                receiver.RegisterReceiver();
            }
        }
    }
}