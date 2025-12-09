using UnityEngine;
using System;

public class ColliderStarter : MonoBehaviour
{
    public event Action<Collider2D> OnEnter;
    public event Action<Collider2D> OnExit;
    public event Action<Collider2D> OnStay;

    private void OnTriggerEnter2D(Collider2D other) => OnEnter?.Invoke(other);
    private void OnTriggerExit2D(Collider2D other) => OnExit?.Invoke(other);
    private void OnTriggerStay2D(Collider2D other) => OnStay?.Invoke(other);

    private void OnCollisionEnter2D(Collision2D other) => OnEnter?.Invoke(other.collider);
    private void OnCollisionExit2D(Collision2D other) => OnExit?.Invoke(other.collider);
    private void OnCollisionStay2D(Collision2D other) => OnStay?.Invoke(other.collider);
}