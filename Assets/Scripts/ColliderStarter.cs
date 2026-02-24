using UnityEngine;
using System;

public class ColliderStarter : MonoBehaviour
{
    public event Action<Collider> OnEnter;
    public event Action<Collider> OnExit;
    public event Action<Collider> OnStay;

    private void OnTriggerEnter(Collider other) => OnEnter?.Invoke(other);
    private void OnTriggerExit(Collider other) => OnExit?.Invoke(other);
    private void OnTriggerStay(Collider other) => OnStay?.Invoke(other);

    private void OnCollisionEnter(Collision other) => OnEnter?.Invoke(other.collider);
    private void OnCollisionExit(Collision other) => OnExit?.Invoke(other.collider);
    private void OnCollisionStay(Collision other) => OnStay?.Invoke(other.collider);
}