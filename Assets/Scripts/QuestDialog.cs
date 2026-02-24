using DS.ScriptableObjects;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QuestDialog : MonoBehaviour
{
    [SerializeField] GameObject answersParent;
    [SerializeField] TextMeshProUGUI questText;
    [SerializeField] Image spriteImage;
    [SerializeField] GameObject answerPrefab;
    DSDialogueContainerSO currentQuest_;
    DSDialogueSO currentDialog_;
    Coroutine animCoroutine;
    AspectRatioFitter aspectFitter;
    public void activateDialog(DSDialogueContainerSO quest, Mesh anim, DQuestTriggerParent checker /* Добавить сюда картинку аватара */)
    {
        currentQuest_ = quest;
        if (aspectFitter == null) aspectFitter = spriteImage.GetComponent<AspectRatioFitter>();
        UpdateAspectRatio();

        spriteImage.sprite = null;
        currentDialog_ = MainHandler.Instance.quests[quest];
        if (currentDialog_.Predicate)
        {
            var checkDialog = checker.checkPredicate();
            if (checkDialog != null)
            {
                currentDialog_ = checkDialog;
            }
        }
        updateDialogues();

        MainHandler.Instance.SetScreen(GameHud.QuestDialog);

        // Пауза игры и запуск корутины анимации
        Time.timeScale = 0;
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateCharacter());
    }

    IEnumerator AnimateCharacter()
    {
        while (true)
        {
            spriteImage.sprite = null;
            UpdateAspectRatio();
            // Используем Realtime, так как Time.timeScale = 0
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    void UpdateAspectRatio()
    {
        if (aspectFitter != null && spriteImage.sprite != null)
        {
            float ratio = (float)spriteImage.sprite.rect.width / spriteImage.sprite.rect.height;
            aspectFitter.aspectRatio = ratio;
        }
    }
    void updateDialogues()
    {
        questText.text = currentDialog_.TextRu;
        clearAnswers();
        foreach (var choice in currentDialog_.Choices)
        {
            var tt = Instantiate(answerPrefab, answersParent.transform);
            tt.GetComponent<DialogueChooseButton>().init(choice, this);
        }
    }
    void clearAnswers()
    {
        foreach (Transform child in answersParent.transform)
        {
            // Не удаляем Спейсер, чтобы он продолжал прижимать кнопки к низу
            if (child.name == "Spacer") continue;

            Destroy(child.gameObject);
        }
    }
    public void clickNextButton(DS.Data.DSDialogueChoiceData choosedAnswer)
    {
        if (currentDialog_.DoSmth != 0)
        {
            print($"Start addQuestTrigger {currentDialog_.DoSmth}");
            MainHandler.Instance.addQuestTrigger(currentQuest_, currentDialog_.DoSmth);
        }
        if (choosedAnswer.DoSmth != 0)
        {
            print($"Start addQuestTrigger {choosedAnswer.DoSmth}");
            MainHandler.Instance.addQuestTrigger(currentQuest_, choosedAnswer.DoSmth);
        }
        if (choosedAnswer.NextDialogue != null)
        {
            MainHandler.Instance.quests[currentQuest_] = choosedAnswer.NextDialogue;
        }
        if (choosedAnswer.End)
        {
            // Продолжение игры и остановка анимации
            Time.timeScale = 1;
            if (animCoroutine != null)
            {
                StopCoroutine(animCoroutine);
            }
            MainHandler.Instance.SetScreen(GameHud.GameHud);
        }
        else if (choosedAnswer.NextDialogue != null)
        {
            currentDialog_ = choosedAnswer.NextDialogue;
            updateDialogues();
        }
    }


}
