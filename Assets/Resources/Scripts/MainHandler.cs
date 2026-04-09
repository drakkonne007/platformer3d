using DS.ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public float playerDamage = 10;

    [Header("Thrash")]
    int money = 0;    
    System.Random random_ = new();
    public GameLanguage gameLanguage = GameLanguage.ru;
    
    
    Dictionary<DSDialogueContainerSO, List<int>> questTriggers = new();
    public Dictionary<DSDialogueContainerSO, DSDialogueSO> quests = new();
    public event Action<DSDialogueContainerSO, int> onAddQuestScript;
    public event Action<DSDialogueContainerSO, DSDialogueSO> onQuestChange;
    PlayerGameLogic playerGameLogic_;
    ActionParent currentAction;
    public static MainHandler Instance;
    GameObject player_;

    public PlayerData playerData = new();

    //CHUNC
    Vector2Int currentChunc_ = new();
    Vector3 lastCamRotation = Vector3.zero;
    Vector2 chunkSize = new(50, 50);
    List<List<GameObject>> chunks = new();
    Vector2 minWorld;
    Vector2 maxWorld;
    HashSet<Vector2Int> activeChuncs = new();
    //~CHUNC

    public void KillPlayer()
    {
        print("Player killed!!!");
    }
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
        if(act == currentAction)
        {
            return;
        }
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
    }
    IEnumerator LoadNextScene(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
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
        activeChuncs.Clear();
        chunks.Clear();
    }

    public GameObject GetChunck(Vector3 position)
    {
        if (TryGetChunckInt(position, out Vector2Int indices))
        {
            return chunks[indices.x][indices.y];
        }
        throw new("Out of bounds!!!");
    }

    public Vector2Int GetChunckInt(Vector3 position)
    {
        if (TryGetChunckInt(position, out Vector2Int indices))
        {
            return indices;
        }
        throw new("Out of bounds!!!");
    }

    public bool TryGetChunckInt(Vector3 position, out Vector2Int indices)
    {
        int column = Mathf.FloorToInt((position.x - minWorld.x) / chunkSize.x);
        int row = Mathf.FloorToInt((position.z - minWorld.y) / chunkSize.y);

        if (column < 0 || row < 0 || column >= chunks.Count || row >= chunks[column].Count)
        {
            indices = Vector2Int.zero;
            return false;
        }
        indices = new(column, row);
        return true;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        minWorld = new(-5000, -5000);
        maxWorld = new(5000,5000);
        int chunksX = Mathf.CeilToInt((maxWorld.x - minWorld.x) / chunkSize.x);
        int chunksY = Mathf.CeilToInt((maxWorld.y - minWorld.y) / chunkSize.y);

        for (int x = 0; x < chunksX; x++)
        {
            chunks.Add(new());
            for (int y = 0; y < chunksY; y++)
            {
                var chunkObj = new GameObject($"Chunk_{x}_{y}");
                chunkObj.transform.position = new Vector3(
                    minWorld.x + x * chunkSize.x,
                    0,
                    minWorld.y + y * chunkSize.y
                );
                chunks[x].Add(chunkObj);
                chunkObj.SetActive(false);
            }
        }

        var myObjs = FindObjectsByType<ChunkChild>(FindObjectsSortMode.None);
        foreach (var obj in myObjs)
        {
            var trans = GetChunck(obj.transform.position).transform;
            obj.transform.SetParent(trans, worldPositionStays: true);
        }
        refreshChuncs(GetChunckInt(playerPosition()));

        var debugger = gameObject.AddComponent<ChuncDrawer>();
        debugger.chunks = chunks;
        debugger.chunkSize = chunkSize;
    }

    void Update()
    {
        if (Camera.main == null) return;

        var temp = GetChunckInt(playerPosition());
        float rotDiff = Vector3.Angle(lastCamRotation, Camera.main.transform.forward);
        
        if (temp != currentChunc_ || rotDiff > 5.0f)
        {
            refreshChuncs(temp);
        }
    }

    void refreshChuncs(Vector2Int currentChunc)
    {
        currentChunc_ = currentChunc;
        lastCamRotation = Camera.main.transform.forward;

        HashSet<Vector2Int> toActivate = new HashSet<Vector2Int>();

        for (int col = currentChunc_.x - 2; col < currentChunc_.x + 3; col++)
        {
            if (col < 0 || col >= chunks.Count) continue;
            for (int row = currentChunc_.y - 2; row < currentChunc_.y + 3; row++)
            {
                if (row < 0 || row >= chunks[col].Count) continue;
                toActivate.Add(new(col, row));
            }
        }

        Vector3 camPos = Camera.main.transform.position;
        Vector3 camFwd = Camera.main.transform.forward;
        float visionDistance = 500f;
        float stepSize = 25f;

        for (float d = 0; d < visionDistance; d += stepSize)
        {
            Vector3 samplePt = camPos + camFwd * d;
            if (TryGetChunckInt(samplePt, out Vector2Int visionIndex))
            {
                toActivate.Add(visionIndex);
            }
        }

        List<Vector2Int> toDeactivate = new List<Vector2Int>();
        foreach (var ch in activeChuncs)
        {
            if (!toActivate.Contains(ch))
            {
                chunks[ch.x][ch.y].SetActive(false);
                toDeactivate.Add(ch);
            }
        }
        foreach (var ch in toDeactivate)
        {
            activeChuncs.Remove(ch);
        }

        foreach (var ch in toActivate)
        {
            if (!activeChuncs.Contains(ch))
            {
                chunks[ch.x][ch.y].SetActive(true);
                activeChuncs.Add(ch);
            }
        }
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
            if(lives < 0 && false)
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
