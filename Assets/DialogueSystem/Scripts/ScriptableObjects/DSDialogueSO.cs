using System.Collections.Generic;
using UnityEngine;

namespace DS.ScriptableObjects
{
    using Data;
    using Enumerations;
    using UnityEngine.UIElements;

    public class DSDialogueSO : ScriptableObject
    {
        [field: SerializeField] public string DialogueName { get; set; }
        [field: SerializeField][field: TextArea()] public string TextRu { get; set; }
        [field: SerializeField][field: TextArea()] public string TextEn { get; set; }
        [field: SerializeField][field: TextArea()] public string TextKg { get; set; }
        [field: SerializeField][field: TextArea()] public string TextCh { get; set; }
        [field: SerializeField][field: TextArea()] public string TextGm { get; set; }
        [field: SerializeField] public int DoSmth { get; set; }
        [field: SerializeField] public bool Predicate { get; set; }
        [field: SerializeField] public bool Final { get; set; }
        [field: SerializeField] public List<DSDialogueChoiceData> Choices { get; set; }
        [field: SerializeField] public DSDialogueType DialogueType { get; set; }
        [field: SerializeField] public bool IsStartingDialogue { get; set; }

        public void Initialize(string dialogueName, string textRu, string textEn,
        string textKg, string textCh, string textGm, List<DSDialogueChoiceData> choices
        , DSDialogueType dialogueType, bool isStartingDialogue, int doSmth, bool final, bool predicate)
        {
            DialogueName = dialogueName;
            TextRu = textRu;
            TextEn = textEn;
            TextKg = textKg;
            TextCh = textCh;
            TextGm = textGm;
            DoSmth = doSmth;
            Choices = choices;
            DialogueType = dialogueType;
            IsStartingDialogue = isStartingDialogue;
            Final = final;
            Predicate = predicate;
        }
    }
}