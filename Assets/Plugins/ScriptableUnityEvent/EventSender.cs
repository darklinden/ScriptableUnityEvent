using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Reflection;
using System;

#if UNITY_EDITOR
using UnityEditor.Events;
using System.Linq;
#endif

public class EventSender : MonoBehaviour
{
    public ScriptableEvents EventsAsset;
    public List<UnityEvent> Events;

#if UNITY_EDITOR
    [ContextMenu("Save Events Asset")]
    public void SaveEventsAsset()
    {
        if (EventsAsset == null)
        {
            var save_path = UnityEditor.EditorUtility.SaveFilePanel("Save Event Data", Application.dataPath, "events.asset", "asset");
            if (string.IsNullOrEmpty(save_path)) return;
            var path = save_path.Substring(Application.dataPath.Length - 6);

            EventsAsset = ScriptableObject.CreateInstance<ScriptableEvents>();
            EventToScriptable();
            UnityEditor.AssetDatabase.CreateAsset(EventsAsset, path);
            Debug.Log("文件已保存到: " + path);
        }
        else
        {
            EventToScriptable();
            UnityEditor.EditorUtility.SetDirty(EventsAsset);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("文件已保存到: " + UnityEditor.AssetDatabase.GetAssetPath(EventsAsset));
        }
    }

    private void EventToScriptable()
    {
        for (int i = 0; i < Events.Count; i++)
        {
            var e = Events[i];
            var callCount = e.GetPersistentEventCount();

            for (int j = callCount - 1; j >= 0; j--)
            {
                var target = e.GetPersistentTarget(j);
                if (target == null)
                {
                    UnityEventTools.RemovePersistentListener(e, j);
                }
            }
        }

        EventsAsset.Events = new List<UnityEventSerial>();
        for (int i = 0; i < Events.Count; i++)
        {
            var e = Events[i];
            EventsAsset.Events.Add(EventToSerial(e));
        }
    }

    private UnityEventSerial EventToSerial(UnityEvent e)
    {
        var serial = new UnityEventSerial()
        {
            PersistentCalls = new List<UnityPersistentCallSerial>(),
        };
        var eventCount = e.GetPersistentEventCount();
        // use reflection to get UnityEngine.Events.UnityEventBase.m_PersistentCalls from e
        FieldInfo m_PersistentCalls = typeof(UnityEventBase).GetField("m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance);
        // Debug.Log("m_PersistentCalls FieldInfo:" + m_PersistentCalls);
        // PersistentCallGroup
        var persistentCalls = m_PersistentCalls.GetValue(e);
        // Debug.Log(persistentCalls);
        // PersistentCall GetListener(int index) from PersistentCallGroup
        PropertyInfo Count = persistentCalls.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
        var callCount = (int)Count.GetValue(persistentCalls);
        // Debug.Log("callCount:" + callCount);
        Debug.Assert(callCount == eventCount, "callCount != eventCount");

        MethodInfo GetListener = persistentCalls.GetType().GetMethod("GetListener", BindingFlags.Public | BindingFlags.Instance);
        // Debug.Log("GetListener MethodInfo:" + GetListener);
        for (var j = 0; j < callCount; j++)
        {
            var persistentCall = GetListener.Invoke(persistentCalls, new object[] { j });
            // PersistentCall

            // public Object target { get; }
            PropertyInfo targetProp = persistentCall.GetType().GetProperty("target", BindingFlags.Public | BindingFlags.Instance);
            var target = targetProp.GetValue(persistentCall);
            // Debug.Log("target:" + target);

            // public string methodName { get; }
            PropertyInfo methodNameProp = persistentCall.GetType().GetProperty("methodName", BindingFlags.Public | BindingFlags.Instance);
            var methodName = methodNameProp.GetValue(persistentCall);
            // Debug.Log("methodName:" + methodName);

            // public ArgumentCache arguments { get; }
            PropertyInfo argumentsProp = persistentCall.GetType().GetProperty("arguments", BindingFlags.Public | BindingFlags.Instance);
            var arguments = argumentsProp.GetValue(persistentCall);
            // ArgumentCache
            // Debug.Log("arguments:" + arguments);

            // public int intArgument { get; }
            PropertyInfo intArgumentProp = arguments.GetType().GetProperty("intArgument", BindingFlags.Public | BindingFlags.Instance);
            var intArgument = intArgumentProp.GetValue(arguments);
            // Debug.Log("intArgument:" + intArgument);

            // public float floatArgument { get; }
            PropertyInfo floatArgumentProp = arguments.GetType().GetProperty("floatArgument", BindingFlags.Public | BindingFlags.Instance);
            var floatArgument = floatArgumentProp.GetValue(arguments);
            // Debug.Log("floatArgument:" + floatArgument);

            // public string stringArgument { get; }
            PropertyInfo stringArgumentProp = arguments.GetType().GetProperty("stringArgument", BindingFlags.Public | BindingFlags.Instance);
            var stringArgument = stringArgumentProp.GetValue(arguments);
            // Debug.Log("stringArgument:" + stringArgument);

            // public bool boolArgument { get; }
            PropertyInfo boolArgumentProp = arguments.GetType().GetProperty("boolArgument", BindingFlags.Public | BindingFlags.Instance);
            var boolArgument = boolArgumentProp.GetValue(arguments);
            // Debug.Log("boolArgument:" + boolArgument);

            var targetGo = target is GameObject ? target as GameObject : (target as Component).gameObject;
            if (!targetGo.TryGetComponent<EventReceiver>(out var receiver))
            {
                receiver = targetGo.AddComponent<EventReceiver>();
            }

            var serialCall = new UnityPersistentCallSerial()
            {
                targetId = receiver.ReceiverID,
                targetType = target.GetType().FullName,
                methodName = methodName as string,
                intArgument = (int)intArgument,
                floatArgument = (float)floatArgument,
                stringArgument = stringArgument as string,
                boolArgument = (bool)boolArgument,
            };
            serial.PersistentCalls.Add(serialCall);
        }
        return serial;
    }

    private bool SerialToPersistentEvent(int index, UnityEventSerial serial)
    {
        bool loadSuccess = true;
        UnityEvent e;
        if (index >= 0 && index < Events.Count)
        {
            e = Events[index];
        }
        else
        {
            while (Events.Count <= index)
            {
                Events.Add(new UnityEvent());
            }
            e = Events[index];
        }
        e.RemoveAllListeners();

        var receivers = FindObjectsOfType<EventReceiver>(true);

        foreach (var call in serial.PersistentCalls)
        {
            var receiver = receivers.First(r => r.ReceiverID == call.targetId);
            if (receiver == null)
            {
                Debug.LogWarning("接收器未找到: " + call.targetId);
                loadSuccess = false;
                continue;
            }

            // Debug.Log("targetType name:" + call.targetType);
            var target = GetTarget(call.targetType, receiver);
            if (target == null)
            {
                Debug.LogWarning("目标未找到: " + call.targetType);
                loadSuccess = false;
                continue;
            }

            var method = target.GetType().GetMethod(call.methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // Debug.Log("method:" + method);

            if (method == null)
            {
                Debug.LogWarning("函数未找到: " + call.methodName);
                loadSuccess = false;
                continue;
            }
            // 只有 0 或 1 个参数
            var argTypes = method.GetParameters();
            if (argTypes.Length == 0)
            {
                UnityAction action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), target, method);
                UnityEventTools.AddVoidPersistentListener(e, action);
            }
            else
            {
                var firstArgType = argTypes[0].ParameterType;
                if (firstArgType == typeof(int))
                {
                    UnityAction<int> action = (UnityAction<int>)System.Delegate.CreateDelegate(typeof(UnityAction<int>), target, method);
                    UnityEventTools.AddIntPersistentListener(e, action, call.intArgument);
                }
                else if (firstArgType == typeof(float))
                {
                    UnityAction<float> action = (UnityAction<float>)System.Delegate.CreateDelegate(typeof(UnityAction<float>), target, method);
                    UnityEventTools.AddFloatPersistentListener(e, action, call.floatArgument);
                }
                else if (firstArgType == typeof(string))
                {
                    UnityAction<string> action = (UnityAction<string>)System.Delegate.CreateDelegate(typeof(UnityAction<string>), target, method);
                    UnityEventTools.AddStringPersistentListener(e, action, call.stringArgument);
                }
                else if (firstArgType == typeof(bool))
                {
                    UnityAction<bool> action = (UnityAction<bool>)System.Delegate.CreateDelegate(typeof(UnityAction<bool>), target, method);
                    UnityEventTools.AddBoolPersistentListener(e, action, call.boolArgument);
                }
                else
                {
                    loadSuccess = false;
                    Debug.LogWarning("不支持的参数类型: " + firstArgType);
                }
            }
        }

        return loadSuccess;
    }

#endif

    public static Type TypeByName(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse())
        {
            var tt = assembly.GetType(name);
            if (tt != null)
            {
                return tt;
            }
        }

        return null;
    }

    private UnityEngine.Object GetTarget(string targetTypeStr, EventReceiver receiver)
    {
        UnityEngine.Object target;
        if ("UnityEngine.GameObject".Equals(targetTypeStr))
        {
            target = receiver.gameObject;
        }
        else if ("UnityEngine.Transform".Equals(targetTypeStr))
        {
            target = receiver.transform;
        }
        else if ("UnityEngine.RectTransform".Equals(targetTypeStr))
        {
            target = receiver.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogWarning("使用反射获取, 会影响性能, 请在上面添加 if 处理:" + targetTypeStr);
            var targetType = TypeByName(targetTypeStr);
            if (targetType == null)
            {
                Debug.LogWarning("目标类型未找到! " + targetTypeStr);
                return null;
            }
            target = receiver.GetComponent(targetType);
        }
        return target;
    }

    private bool EventsLoaded { get; set; } = false;

#if UNITY_EDITOR
    [ContextMenu("Load Events")]
#endif
    public void LoadFromAsset()
    {
        if (EventsAsset == null) return;

        bool allLoadSuccess = true;
        Events = new List<UnityEvent>();
        for (var i = 0; i < EventsAsset.Events.Count; i++)
        {

            bool loaded = false;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                allLoadSuccess = SerialToPersistentEvent(i, EventsAsset.Events[i]) && allLoadSuccess;
                loaded = true;
            }
#endif
            if (!loaded)
            {
                allLoadSuccess = SerialToEvent(i, EventsAsset.Events[i]) && allLoadSuccess;
            }

            if (!allLoadSuccess) break;
        }

        if (allLoadSuccess)
        {
            Debug.Log("EventSender 加载成功: " + name);
            EventsLoaded = true;
        }
        else
        {
            Debug.LogWarning("EventSender 尚未加载成功: " + name);
        }
    }

    private bool SerialToEvent(int index, UnityEventSerial serial)
    {
        bool loadSuccess = true;
        UnityEvent e;
        if (index >= 0 && index < Events.Count)
        {
            e = Events[index];
        }
        else
        {
            while (Events.Count <= index)
            {
                Events.Add(new UnityEvent());
            }
            e = Events[index];
        }
        e.RemoveAllListeners();

        var sem = ScriptableEventManager.Instance;

        foreach (var call in serial.PersistentCalls)
        {
            var receiver = sem.GetReceiver(call.targetId);
            if (receiver == null)
            {
                Debug.LogWarning("接收器未找到: " + call.targetId);
                loadSuccess = false;
                continue;
            }

            var target = GetTarget(call.targetType, receiver);
            if (target == null)
            {
                Debug.LogWarning("目标未找到: " + call.targetType);
                loadSuccess = false;
                continue;
            }

            var method = target.GetType().GetMethod(call.methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // Debug.Log("method:" + method);

            if (method == null)
            {
                Debug.LogWarning("函数未找到: " + call.methodName);
                loadSuccess = false;
                continue;
            }
            // 只有 0 或 1 个参数
            var argTypes = method.GetParameters();
            if (argTypes.Length == 0)
            {
                UnityAction action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), target, method);
                e.AddListener(action);
            }
            else
            {
                var firstArgType = argTypes[0].ParameterType;
                if (firstArgType == typeof(int))
                {
                    UnityAction<int> action = (UnityAction<int>)System.Delegate.CreateDelegate(typeof(UnityAction<int>), target, method);
                    e.AddListener(() => action(call.intArgument));
                }
                else if (firstArgType == typeof(float))
                {
                    UnityAction<float> action = (UnityAction<float>)System.Delegate.CreateDelegate(typeof(UnityAction<float>), target, method);
                    e.AddListener(() => action(call.floatArgument));
                }
                else if (firstArgType == typeof(string))
                {
                    UnityAction<string> action = (UnityAction<string>)System.Delegate.CreateDelegate(typeof(UnityAction<string>), target, method);
                    e.AddListener(() => action(call.stringArgument));
                }
                else if (firstArgType == typeof(bool))
                {
                    UnityAction<bool> action = (UnityAction<bool>)System.Delegate.CreateDelegate(typeof(UnityAction<bool>), target, method);
                    e.AddListener(() => action(call.boolArgument));
                }
                else
                {
                    Debug.LogWarning("不支持的参数类型: " + firstArgType);
                    loadSuccess = false;
                }
            }
        }

        return loadSuccess;
    }

    public void ZzCallEvent(int index = 0)
    {
        if (index >= 0 && index < Events.Count)
        {
            Events[index]?.Invoke();
        }
    }

    private void LoadEvents()
    {
        if (Application.isPlaying)
        {
            if (!EventsLoaded)
            {
                LoadFromAsset();
            }
        }
    }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            LoadEvents();
            ScriptableEventManager.Instance.OnReceiverChanged += LoadFromAsset;
        }
    }

    private void Start()
    {
        LoadEvents();
    }

#if UNITY_EDITOR

    [ContextMenu("Call 0")]
    public void Call0()
    {
        ZzCallEvent(0);
    }

    [ContextMenu("Call 1")]
    public void Call1()
    {
        ZzCallEvent(1);
    }

    [ContextMenu("Call 2")]
    public void Call2()
    {
        ZzCallEvent(2);
    }

#endif
}