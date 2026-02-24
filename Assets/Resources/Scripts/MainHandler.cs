using DS.ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static UnityEditor.Experimental.GraphView.GraphView;

public enum DamageType
{
    Gold = 1,
    Silver = 2,
    Phys = 4
}

public enum GameHud
{
    GameHud,
    QuestDialog,
    PauseMenu,
}
public enum GameLanguage
{
    ru,
    en,
    kg
}

public class MainHandler : MonoBehaviour
{
    [Header("HUDS")]
    [SerializeField] public GameObject gameHud;
    [SerializeField] public GameObject pauseHud;
    [SerializeField] public GameObject dialogHud;
    [SerializeField] TextMeshProUGUI livesTxt;
    [SerializeField] TextMeshProUGUI scoresTxt;
    [SerializeField] TextMeshProUGUI healthTxt;
    [SerializeField] QuestDialog questGui;


    [SerializeField] int lives = 3;
    [SerializeField] float health = 100;

    [Header("Thrash")]
    int money = 0;    
    System.Random random_ = new();
    public GameLanguage gameLanguage = GameLanguage.ru;
    
    Dictionary<DSDialogueContainerSO, List<int>> questTriggers = new();
    public Dictionary<DSDialogueContainerSO, DSDialogueSO> quests = new();
    public event Action<DSDialogueContainerSO, int> onAddQuestScript;
    public event Action<DSDialogueContainerSO, DSDialogueSO> onQuestChange;
    PlayerGameLogic playerGameLogic_;
    public ActionParent currentAction;
    public static MainHandler Instance;
    GameObject player_;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        player_ = GameObject.FindGameObjectWithTag("Player");
        playerGameLogic_ = player_.GetComponent<PlayerGameLogic>();
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

    public void addQuestTrigger(DSDialogueContainerSO quest, int state)
    {
        if (!questTriggers.ContainsKey(quest))
        {
            questTriggers[quest] = new();
        }
        questTriggers[quest].Add(state);
        onAddQuestScript?.Invoke(quest, state);
    }
    public void removeQuestTrigger(DSDialogueContainerSO quest)
    {
        questTriggers.Remove(quest);
    }
    public List<int> findQuestTrigger(DSDialogueContainerSO quest)
    {
        if (questTriggers.ContainsKey(quest))
        {
            return questTriggers[quest];
        }
        return null;
    }
    public void StartQuestDialogGui(DSDialogueContainerSO quest, Mesh anim, DQuestTriggerParent checker)
    {
        questGui.activateDialog(quest, anim, checker);
    }
    public void StartInteractive()
    {
        if (currentAction != null)
        {
            currentAction.doAction();
        }
    }
    public void setActiveAction(ActionParent act)
    {
        if (currentAction != null)
        {
            currentAction.deactivate();
        }
        currentAction = act;
    }
    public ActionParent currentActiveAction()
    {
        return currentAction;
    }
    public void SetScreen(GameHud hud)
    {
        switch (hud)
        {
            case GameHud.GameHud:
                gameHud.SetActive(true);
                dialogHud.SetActive(false);
                break;
            case GameHud.PauseMenu:
                gameHud.SetActive(false);
                pauseHud.SetActive(true);
                break;
            case GameHud.QuestDialog:
                gameHud.SetActive(false);
                dialogHud.SetActive(true);
                break;
        }
    }
    public Vector3 playerPosition()
    {
        return player_.transform.position;
    }

    public void setQuestState(DSDialogueContainerSO quest, DSDialogueSO current, DSDialogueSO next)
    {
        quests[quest] = current;
        onQuestChange?.Invoke(quest, current);
        //quests[name].isDone = isDone ?? quests[name]?.isDone ?? false;
        //quests[name].currentState = state ?? quests[name]?.currentState ?? 0;
        //quests[name].desc = desc ?? quests[name]?.desc ?? "";
        //quests[name].descEn = descEn ?? quests[name]?.descEn ?? "";
        //quests[name].descKg = descKg ?? quests[name]?.descKg ?? "";
        //quests[name].needInventar = needInventar ?? quests[name]?.needInventar ?? false;        
        //dbHandler.setQuestState(name, quests[name]!.currentState, quests[name]!.isDone
        //, quests[name]?.desc
        //, quests[name]?.descEn
        //, quests[name]?.descKg
        //, needInventar);
    }
    IEnumerator LoadNextScene(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            // Можно показать прогресс asyncLoad.progress
            yield return null;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneUnloaded(Scene scene)
    {
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
    }
    public void addHealth(float value, DamageType type)
    {
        if(value < 0)
        {
            if((playerGameLogic_.isGold() && type == DamageType.Gold)
                 || (!playerGameLogic_.isGold() && type == DamageType.Silver))
            {
                return;
            }
            health += value;
        }
        else
        {
            health += value;
        }        
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
