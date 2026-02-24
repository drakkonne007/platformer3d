using TMPro;
using UnityEngine;

public class DialogueChooseButton : MonoBehaviour
{
    DS.Data.DSDialogueChoiceData data_;
    QuestDialog questUi_;
    [SerializeField] TextMeshProUGUI text;
    public void init(DS.Data.DSDialogueChoiceData data, QuestDialog questUI)
    {
        data_ = data;
        questUi_ = questUI;
        text.text = data_.TextRu;
    }

    public void onClick()
    {
        questUi_.clickNextButton(data_);
    }
}
