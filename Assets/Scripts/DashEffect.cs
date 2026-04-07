using UnityEngine;

public class DashEffect : MonoBehaviour
{
    [SerializeField] TrailRenderer dashRender;

    public void StartDashEffect()
    {
        dashRender.emitting = true;
    }

    public void StopDashEffect()
    {
        dashRender.emitting = false;
    }
}
