using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(LazyMonoSingletonBase), true)]
public class LazyMonoSingletonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("This object use lazy singleton initialization and should not be attached to any gameobject", MessageType.Error);
        DrawDefaultInspector();
    }
}

#endif

public abstract class LazyMonoSingletonBase : MonoBehaviour { }


public class LazyMonoSingleton<T> : LazyMonoSingletonBase where T : LazyMonoSingleton<T>
{
    public static T Instance
    {
        get
        {
            if (_instnce == null)
            {
                var go = new GameObject(typeof(T).Name);
                go.hideFlags = HideFlags.HideInHierarchy;
                _instnce = go.AddComponent<T>();
                //Debug.Log($"Singleton [{go.name}] Created");
            }
            return _instnce;
        }
    }
    private static T _instnce;

    protected virtual void OnDestroy()
    {
        _instnce = null;
        Debug.Log($"Singleton [{gameObject.name}] Destroyed");
    }
}