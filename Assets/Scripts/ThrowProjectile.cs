using KinematicCharacterController.Examples;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class ThrowProjectile : MonoBehaviour
{
    [SerializeField] DamageType damageType = DamageType.Phys;
    [SerializeField] float damage = 1;
    [SerializeField] float rotateSpeed = 300;
    [SerializeField] float pushForce = 5f;
    [SerializeField] ParticleSystem effect;
    Quaternion startQuaternion;
    Vector3 startPos;
    Vector3 randomRotate = Vector3.zero;

    private void OnTriggerEnter(Collider other) => Enter(other);
    private void Start()
    {
        startPos = transform.position;
        startQuaternion = transform.rotation;
        randomRotate = new Vector3(UnityEngine.Random.value * 2 - 1, UnityEngine.Random.value * 2 - 1
            , UnityEngine.Random.value * 2 - 1);
    }
    private void OnDestroy()
    {
        Instantiate(effect, transform.position, startQuaternion * Quaternion.Euler(0, 200, 0));
    }
    private void Enter(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            MainHandler.Instance.addHealth(-damage, damageType);
            
            var controller = other.transform.root.GetComponent<ExampleCharacterController>();
            if (controller != null)
            {
                Vector3 pushDirection = (transform.position - startPos).normalized;
                pushDirection.y = 0;
                controller.AddVelocity(pushDirection.normalized * pushForce);
            }

            Destroy(gameObject);
            return;
        }

        if (!other.transform.root.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        transform.Rotate(randomRotate * rotateSpeed * Time.deltaTime);
    }
}
