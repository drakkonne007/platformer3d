using TMPro;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class MainHandler : MonoBehaviour
{

    [SerializeField] int lives = 3;
    [SerializeField] float health = 100;
    int money = 0;

    [SerializeField] TextMeshProUGUI livesTxt;
    [SerializeField] TextMeshProUGUI scoresTxt;
    [SerializeField] TextMeshProUGUI healthTxt;


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

    private void Start()
    {
        livesTxt.text = lives.ToString();
        scoresTxt.text = money.ToString();
        healthTxt.text = health.ToString();
    }

    public void addMoney(int value)
    {
        money += value;
        scoresTxt.text = money.ToString();
    }
    public void addHealth(float value)
    {
        health += value;
        if(health <= 0)
        {
            lives--;
            if(lives < 0)
            {
                Time.timeScale = 0;
                Debug.Log("Game is end");
            }
            else
            {
                health = 100;
            }
            livesTxt.text = lives.ToString();
        }
        healthTxt.text = health.ToString();
    }
}
