using System.Collections.Generic;
using UnityEngine;

namespace DS.ScriptableObjects
{
    public class DSDialogueContainerSO : ScriptableObject
    {
        [field: SerializeField] public string FileName { get; set; }
        [field: SerializeField] public bool MainQuest { get; set; }
        [field: SerializeField] public SerializableDictionary<DSDialogueGroupSO, List<DSDialogueSO>> DialogueGroups { get; set; }
        [field: SerializeField] public List<DSDialogueSO> UngroupedDialogues { get; set; }
        [field: SerializeField] public List<int> AllIntTriggers { get; set; } = new List<int>();
        [field: SerializeField] public List<DSDialogueContainerSO> AllQuests { get; set; } = new List<DSDialogueContainerSO>();

        public void Initialize(string fileName)
        {
            FileName = fileName;
            MainQuest = false;
            DialogueGroups = new SerializableDictionary<DSDialogueGroupSO, List<DSDialogueSO>>();
            UngroupedDialogues = new List<DSDialogueSO>();
            AllIntTriggers = new List<int>();
            AllQuests = new List<DSDialogueContainerSO>();
        }

        public List<string> GetDialogueGroupNames()
        {
            List<string> dialogueGroupNames = new List<string>();

            foreach (DSDialogueGroupSO dialogueGroup in DialogueGroups.Keys)
            {
                dialogueGroupNames.Add(dialogueGroup.GroupName);
            }

            return dialogueGroupNames;
        }

        public List<string> GetGroupedDialogueNames(DSDialogueGroupSO dialogueGroup, bool startingDialoguesOnly)
        {
            List<DSDialogueSO> groupedDialogues = DialogueGroups[dialogueGroup];

            List<string> groupedDialogueNames = new List<string>();

            foreach (DSDialogueSO groupedDialogue in groupedDialogues)
            {
                if (startingDialoguesOnly && !groupedDialogue.IsStartingDialogue)
                {
                    continue;
                }

                groupedDialogueNames.Add(groupedDialogue.DialogueName);
            }

            return groupedDialogueNames;
        }

        public List<string> GetUngroupedDialogueNames(bool startingDialoguesOnly)
        {
            List<string> ungroupedDialogueNames = new List<string>();

            foreach (DSDialogueSO ungroupedDialogue in UngroupedDialogues)
            {
                if (startingDialoguesOnly && !ungroupedDialogue.IsStartingDialogue)
                {
                    continue;
                }

                ungroupedDialogueNames.Add(ungroupedDialogue.DialogueName);
            }

            return ungroupedDialogueNames;
        }
    }
}