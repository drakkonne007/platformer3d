using KinematicCharacterController.Examples;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyThrowBullet : TriggerAction
{
    [SerializeField] DamageType damageType = DamageType.Phys;
    [SerializeField] float damage = 1;
    [SerializeField] float rotateSpeed = 1;
    Vector3 randomRotate = Vector3.zero;

    private void Start()
    {
        randomRotate = new Vector3(UnityEngine.Random.value * 2 - 1, UnityEngine.Random.value * 2 - 1
            , UnityEngine.Random.value * 2 - 1);
    }
    public override void Action(Collider other)
    {
        // Проверяем корень объекта, чтобы не менять тэги всем вложенным хитбоксам
        if (other.transform.root.CompareTag("Player"))
        {
            MainHandler.Instance.addHealth(-damage, damageType);
            Destroy(gameObject);
            return;
        }

        // Если попали не во врага (в стену или декорации) - пуля исчезает
        if (!other.transform.root.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        transform.Rotate(randomRotate * rotateSpeed * Time.deltaTime);
    }

    //public override void ActionStay(Collider other, float time)
    //{
    //    var obj = other.GetComponentsInParent<ExampleCharacterController>();
    //    if (obj != null)
    //    {
    //        MainHandler.Instance.addHealth(-damage * time, damageType);
    //    }

    //}
}
