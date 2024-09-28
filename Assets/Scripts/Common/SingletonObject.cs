using UnityEngine;

public abstract class SingletonObject<T> : MonoBehaviour
    where T : MonoBehaviour
{
    public static T Instance { get; protected set; }

    public static void EnsureInitialized()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject(typeof(T).Name);
            Instance = obj.AddComponent<T>();
            DontDestroyOnLoad(obj);
        }
    }
    public static void EnsureDestroyed()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }
    }
}
