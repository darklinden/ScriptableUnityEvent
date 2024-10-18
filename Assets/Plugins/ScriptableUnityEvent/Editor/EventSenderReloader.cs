using UnityEditor;
using UnityEngine;

public class EventSenderReloader
{
    [MenuItem("Tools/Reload EventSenders %#l")]
    static void SpecialCommand()
    {
        Debug.Log("收到快捷键, 重新加载所有 EventReceiver");
        var receivers = GameObject.FindObjectsOfType<EventReceiver>(true);
        foreach (var receiver in receivers)
        {
            Debug.Log("重新加载 EventReceiver: " + receiver.name);
            receiver.RegisterReceiver();
        }

        Debug.Log("收到快捷键, 重新加载所有 EventSender");
        var senders = GameObject.FindObjectsOfType<EventSender>(true);
        foreach (var sender in senders)
        {
            Debug.Log("重新加载 EventSender: " + sender.name);
            sender.LoadFromAsset();
        }
    }
}