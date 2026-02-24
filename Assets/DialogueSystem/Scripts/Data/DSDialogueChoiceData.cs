using System;
using UnityEngine;

namespace DS.Data
{
    using ScriptableObjects;

    [Serializable]
    public class DSDialogueChoiceData
    {
        [field: SerializeField] public string TextRu { get; set; }
        [field: SerializeField] public string TextEn { get; set; }
        [field: SerializeField] public string TextKg { get; set; }
        [field: SerializeField] public string TextCh { get; set; }
        [field: SerializeField] public string TextGm { get; set; }
        [field: SerializeField] public int DoSmth { get; set; }
        [field: SerializeField] public bool End { get; set; }
        [field: SerializeField] public bool Final { get; set; }
        [field: SerializeField] public DSDialogueSO NextDialogue { get; set; }
    }
}