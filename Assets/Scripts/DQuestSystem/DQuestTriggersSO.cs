using DS.ScriptableObjects;
using System.Collections.Generic;
using UnityEngine;

public class DQuestTriggersSO : ScriptableObject
{
    [SerializeField] public List<int> allIntTriggers = new();
    [SerializeField] public List<DSDialogueContainerSO> allQuests = new();
    [SerializeField] public DSDialogueContainerSO quest;
}
