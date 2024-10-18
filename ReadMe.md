# Scriptable Unity Event

一个简单的工具脚本, 用于解决在 Inspector 中设置 UnityEvent 时, 如果 UnityEvent 的目标 GameObject 存在于不同的 Prefab 中, Prefab 加载后 UnityEvent 会失效的问题

使用反射读取当前设置的 UnityEvent, 将参数序列化存储到 ScriptableObject 中, 这时如果 GameObject 分别存储在不同的 Prefab 中, 可以通过 ScriptableObject 保存的参数, 在 Prefab 加载后重新设置 UnityEvent

-   在 Editor 非运行状态下, 加载 UnityEvent 使用的是 Persistent Listener 以便调试
-   在运行状态下, 加载 UnityEvent 使用的是 Dynamic Listener, Inspector 中不会显示, 但是可以在运行时正常调用
