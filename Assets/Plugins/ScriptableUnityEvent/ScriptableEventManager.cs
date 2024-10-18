using System;
using System.Collections.Generic;
using UnityEngine;

public class ScriptableEventManager : MonoBehaviour
{
    private static ScriptableEventManager _instance;
    public static ScriptableEventManager Instance
    {
        get
        {
            if (_instance == null)
            {
                if (_isQuitting)
                {
                    return null;
                }
                _instance = new GameObject("ScriptableEventManager").AddComponent<ScriptableEventManager>();
            }
            return _instance;
        }
    }

    public Action OnReceiverChanged { get; internal set; }

#if UNITY_EDITOR
    [Serializable]
    public class ReceiverInfo
    {
        public string id;
        public EventReceiver receiver;
    }
    [SerializeField]
    private List<ReceiverInfo> _receivers = new List<ReceiverInfo>();
#endif

    private void Awake()
    {
        if (_instance == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    static bool _isQuitting = false;
    private void OnApplicationQuit()
    {
        _isQuitting = true;
        _instance = null;
    }

    private Dictionary<string, EventReceiver> EventReceivers = new Dictionary<string, EventReceiver>();

    public EventReceiver GetReceiver(string id)
    {
        if (EventReceivers.TryGetValue(id, out var eventReceiver))
        {
            return eventReceiver;
        }
        return null;
    }

    public static void RegisterReceiver(string id, EventReceiver receiver)
    {
        var inst = Instance;
        if (inst == null)
        {
            return;
        }

        if (!inst.EventReceivers.ContainsKey(id))
        {
            inst.EventReceivers.Add(id, receiver);
#if UNITY_EDITOR
            inst._receivers.Add(new ReceiverInfo { id = id, receiver = receiver });
#endif
            inst.OnReceiverChanged?.Invoke();
        }
    }

    public static void UnregisterReceiver(string id)
    {
        var inst = Instance;
        if (inst == null)
        {
            return;
        }

        if (inst.EventReceivers.ContainsKey(id))
        {
            inst.EventReceivers.Remove(id);
#if UNITY_EDITOR
            inst._receivers.RemoveAll(x => x.id == id);
#endif
            inst.OnReceiverChanged?.Invoke();
        }
    }
}