using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class NPCHandler : MonoBehaviour
{
    [Header("Add coliders")]
    [SerializeField] List<ColliderStarter> colliders;
    private string enemyTag = "Enemy";
    private string playerTag = "Player";

    private List<GameObject> enemies = new List<GameObject>();
    private bool isPlayerHere = false;
    private BoxCollider boxCollider;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        
        // Safety check to ensure it functions as a Trigger volume
        if (!boxCollider.isTrigger)
        {
            Debug.LogWarning("NPCHandler: BoxCollider is not set to IsTrigger. Forcing it to true!");
            boxCollider.isTrigger = true;
        }
        foreach(var coll in colliders)
        {
            coll.OnEnter += EnterNew;
            coll.OnExit += ExitNew;
        }
        SetupInitialState();
    }
    private void OnDestroy()
    {
        foreach (var coll in colliders)
        {
            coll.OnEnter -= EnterNew;
            coll.OnExit -= ExitNew;
        }
    }
    void ExitNew(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerHere = false;
            UpdateEnemiesState();
        }
    }
    void EnterNew(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerHere = true;
            UpdateEnemiesState();
        }
    }
    void SetupInitialState()
    {
        // 1. Find all colliders inside the box bounds at the very start.
        // We MUST account for scale and rotation in world space.
        Vector3 center = transform.TransformPoint(boxCollider.center);
        Vector3 halfExtents = Vector3.Scale(boxCollider.size, transform.lossyScale) / 2;
        halfExtents = new Vector3(Mathf.Abs(halfExtents.x), Mathf.Abs(halfExtents.y), Mathf.Abs(halfExtents.z));

        Collider[] colliders = Physics.OverlapBox(
            center, 
            halfExtents, 
            transform.rotation
        );

        // 2. Filter found colliders to identify enemies and if player is already inside
        foreach (var col in colliders)
        {
            if (col.CompareTag(enemyTag))
            {
                enemies.Add(col.gameObject);
            }
            else if (col.CompareTag(playerTag))
            {
                isPlayerHere = true;
            }
        }

        // 3. Set the initial state for enemies
        UpdateEnemiesState();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.CompareTag(playerTag))
        {
            isPlayerHere = true;
            UpdateEnemiesState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.root.CompareTag(playerTag))
        {
            isPlayerHere = false;
            UpdateEnemiesState();
        }
    }

    private void UpdateEnemiesState()
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null) // Safety check: enemy could be destroyed
            {
                enemy.SetActive(isPlayerHere);
            }
        }
    }
}
