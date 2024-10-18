using System;
using System.Collections.Generic;
using UnityEngine;

public class ScriptableEvents : ScriptableObject
{
    public long LastModified;
    public List<UnityEventSerial> Events;
}

[Serializable]
public class UnityPersistentCallSerial
{
    public string targetId;
    public string targetType;
    public string methodName;
    public int intArgument;
    public float floatArgument;
    public string stringArgument;
    public bool boolArgument;
}

[Serializable]
public class UnityEventSerial
{
    public List<UnityPersistentCallSerial> PersistentCalls;
}
