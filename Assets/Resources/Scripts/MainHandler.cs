using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class MainHandler : MonoBehaviour
{
    public static MainHandler Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void addMoney(float value)
    {
        Debug.Log($"Add {value}");
    }
}
