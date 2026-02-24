using DS.ScriptableObjects;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

abstract public class DQuestTriggerParent : MonoBehaviour
{
    [Header("Quests")]  
    [Space(5)]
    [SerializeField] public int startShow = 0;
    [SerializeField] public int endShow = int.MaxValue;
    [SerializeField] public int startTrigger = 0;
    [SerializeField] public int endTrigger = int.MaxValue;
    [Space(10)]
    [SerializeField] public DSDialogueContainerSO questContainer;
    [SerializeField] public List<int> answers = new();

    protected List<int> doneTriggers = new();
    protected Dictionary<int, Action> callbacks = new();
    GiantAI.GiantAI enemyParent;
    protected virtual void Start()
    {
        Debug.Assert(questContainer != null);
        TryGetComponent(out enemyParent);
        MainHandler.Instance.onAddQuestScript += triggerSmth;
        MainHandler.Instance.onQuestChange += questStateChange;
        
        // Use AllIntTriggers from DSDialogueContainerSO
        if (questContainer.AllIntTriggers != null)
        {
            foreach (var trig in questContainer.AllIntTriggers)
            {
                triggerSmth(questContainer, trig);
            }
        }

        int state;
        if(MainHandler.Instance.quests.ContainsKey(questContainer))
        {
            state = int.Parse(MainHandler.Instance.quests[questContainer].name);
        }
        else
        {
            state = int.Parse(questContainer.UngroupedDialogues[0].name);
        }
        checkVision(state);
    }
    void checkVision(int state)
    {
        if(startShow > state || endShow < state)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
            changeDialogActivity();
        }
    }
    public virtual DSDialogueSO checkPredicate()
    {
        return null;
    }
    void changeDialogActivity()
    {
        if (!enemyParent)
        {
            return;
        }
        enemyParent.checkQuestDialog();
    }
    private void OnDestroy()
    {
        MainHandler.Instance.onAddQuestScript -= triggerSmth;
        MainHandler.Instance.onQuestChange -= questStateChange;
    }
    void questStateChange(DSDialogueContainerSO quest, DSDialogueSO state)
    {
        if (quest != questContainer)
        {
            return;
        }
        int questDrin = int.Parse(state.name);
        checkVision(questDrin);
    }
    void triggerSmth(DSDialogueContainerSO quest, int state)
    {
        print($"START triggerSmth {state}");
        if(quest != questContainer)
        {
            return;
        }
        print("START triggerSmth1");
        if (doneTriggers.Contains(state))
        {
            return;
        }
        print("START triggerSmth2");
        if (callbacks.ContainsKey(state) && answers.Contains(state))
        {
            doneTriggers.Add(state);
            print("START triggerSmth4");
            callbacks[state].Invoke();
        }
    }
}
