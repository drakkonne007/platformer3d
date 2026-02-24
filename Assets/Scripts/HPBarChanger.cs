using UnityEngine;
using UnityEngine.UI;

public class HPBarChanger : MonoBehaviour
{
    [SerializeField] RectTransform CanvasParent;
    [SerializeField] GameObject HpBar;
    [SerializeField] GameObject BackgroundBar;
    [SerializeField] float secsToHide;

    RectTransform hpRect;
    Image hpImage;
    Image backImage;
    bool isNeed = false;
    float percent_ = 0;
    float currentSecs = 0;
    bool needForce = false;
    bool isLoad = false;
    private void Start()
    {
        if (isLoad)
        {
            return;
        }
        gameObject.SetActive(false);
        currentSecs = secsToHide;
        hpRect = HpBar.GetComponent<RectTransform>();
        hpImage = HpBar.GetComponent<Image>();
        backImage = BackgroundBar.GetComponent<Image>();
        isLoad = true;
    }
    public void Init(float percent)
    {
        percent_ = percent;
        if (!isLoad)
        {
            gameObject.SetActive(false);
            currentSecs = secsToHide;
            hpRect = HpBar.GetComponent<RectTransform>();
            hpImage = HpBar.GetComponent<Image>();
            backImage = BackgroundBar.GetComponent<Image>();
        }
        hpRect.sizeDelta = new Vector2(CanvasParent.sizeDelta.x * percent_, hpRect.sizeDelta.y);
    }
    public void SetHealth(float percent)
    {
        percent_ = percent;
        hpRect.sizeDelta = new Vector2(CanvasParent.sizeDelta.x * percent_, hpRect.sizeDelta.y);
        var col = hpImage.color;
        col.a = 1;
        hpImage.color = col;
        col = backImage.color;
        col.a = 1;
        backImage.color = col;
        isNeed = true;
        currentSecs = 0;
        gameObject.SetActive(true);
    }
    public void SetVisible(bool need)
    {
        needForce = need;
        if (needForce)
        {
            var col = hpImage.color;
            col.a = 1;
            hpImage.color = col;
            col = backImage.color;
            col.a = 1;
            backImage.color = col;
            isNeed = true;
            currentSecs = 0;
            gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (isNeed && !needForce)
        {
            currentSecs += Time.deltaTime;
            float percentNow = currentSecs / secsToHide;
            if (percentNow > 1)
            {
                isNeed = false;
                percentNow = 1;
                gameObject.SetActive(false);
                return;
            }
            var col = hpImage.color;
            col.a = 1 - currentSecs / secsToHide;
            hpImage.color = col;
            col = backImage.color;
            col.a = 1 - currentSecs / secsToHide;
            backImage.color = col;
        }
    }
}
