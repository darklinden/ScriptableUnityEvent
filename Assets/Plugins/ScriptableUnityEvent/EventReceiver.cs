using UnityEditor;
using UnityEngine;

public class EventReceiver : MonoBehaviour
{
    [SerializeField] public string _receiverID;

    public string ReceiverID
    {
        get
        {
            if (string.IsNullOrEmpty(_receiverID))
            {
                _receiverID = GUID.Generate().ToString();
            }
            return _receiverID;
        }
    }

    private void Awake()
    {
        RegisterReceiver();
    }

    private void Start()
    {
        RegisterReceiver();
    }

    private void OnEnable()
    {
        RegisterReceiver();
    }

    public void RegisterReceiver()
    {
        if (Application.isPlaying)
        {
            ScriptableEventManager.RegisterReceiver(ReceiverID, this);
        }
    }

    public void UnregisterReceiver()
    {
        if (Application.isPlaying)
        {
            ScriptableEventManager.UnregisterReceiver(ReceiverID);
        }
    }

    private void OnDestroy()
    {
        UnregisterReceiver();
    }
}