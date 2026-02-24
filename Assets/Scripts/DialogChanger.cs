using UnityEngine;

public enum DialogType
{
    none,
    dialog,
    dialogPassive,
    dialogMain,
    dialogMainPassive,
    gold
}

public class DialogChanger : MonoBehaviour
{
    [SerializeField] RuntimeAnimatorController dialogActive;
    [SerializeField] RuntimeAnimatorController dialogPassive;
    [SerializeField] RuntimeAnimatorController dialogMainActive;
    [SerializeField] RuntimeAnimatorController dialogMainPassive;
    [SerializeField] RuntimeAnimatorController dialogGold;

    Animator animator;
    SpriteRenderer sprRender;
    public void setDialogPict(DialogType type)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (sprRender == null)
        {
            sprRender = GetComponent<SpriteRenderer>();
            sprRender.sprite = null;
        }
        switch (type)
        {
            case DialogType.none:
                animator.runtimeAnimatorController = null;
                sprRender.sprite = null;
                break;
            case DialogType.dialogPassive:
                animator.runtimeAnimatorController = dialogPassive;
                break;
            case DialogType.dialog:
                animator.runtimeAnimatorController = dialogActive;
                break;
            case DialogType.dialogMain:
                animator.runtimeAnimatorController = dialogMainActive;
                break;
            case DialogType.dialogMainPassive:
                animator.runtimeAnimatorController = dialogMainPassive;
                break;
            case DialogType.gold:
                animator.runtimeAnimatorController = dialogGold;
                break;
        }
        if (type == DialogType.none)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }
}
