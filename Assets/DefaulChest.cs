using System.Collections.Generic;
using UnityEngine;

public enum KryshkaState
{
    Opened,
    Closed,
    Up,
    Down
}
public class DefaulChest : ActionParent
{
    [SerializeField] Transform kryshka;
    [SerializeField] float rotateSpeed = 5;
    [SerializeField] List<GameObject> loots = new();
    [SerializeField] Collider placeForLoot;
    [SerializeField] ColliderStarter objBox;
    [SerializeField] GameObject coin;

    bool isOpen = false;
    KryshkaState kryskaState;
    float _currentAngle = 0f;

    void Start()
    {
        if (!isOpen)
        {
            doSmth = Open;
            kryskaState = KryshkaState.Closed;
            _currentAngle = 0f;
            if(loots.Count == 0)
            {
                for(int i=0;i<UnityEngine.Random.Range(5, 10); i++)
                {
                    loots.Add(coin);
                }
            }
        }
        else
        {
            kryskaState = KryshkaState.Opened;
            _currentAngle = -70f; // -70 is the Opened resting angle.
        }
        objBox.OnEnter += SetActive;
        objBox.OnStay += SetActive;
        objBox.OnExit += SetDeactive;
    }
    private void OnDestroy()
    {
        objBox.OnEnter -= SetActive;
        objBox.OnExit -= SetDeactive;
    }
    void SetActive(Collider other)
    {
        MainHandler.Instance.setActiveAction(this);
    }
    void SetDeactive(Collider other)
    {
        if(MainHandler.Instance.currentActiveAction() == this)
        {
            MainHandler.Instance.setActiveAction(null);
        }
    }
    void Open()
    {
        kryskaState = KryshkaState.Up;
        objBox.GetComponent<Collider>().enabled = false;
        SetDeactive(null);
    }
    // Update is called once per frame
    void Update()
    {
        if(kryskaState == KryshkaState.Up)
        {
            _currentAngle -= rotateSpeed * Time.deltaTime;
            kryshka.localRotation = Quaternion.Euler(_currentAngle, 0, 0);
            if(_currentAngle < -100f)
            {
                _currentAngle = -100f;
                kryskaState = KryshkaState.Down;
                SpawnLoot();
            }
        }else if(kryskaState == KryshkaState.Down)
        {
            _currentAngle += rotateSpeed * Time.deltaTime;
            kryshka.localRotation = Quaternion.Euler(_currentAngle, 0, 0);
            if (_currentAngle > -70f)
            {
                _currentAngle = -70f;
                kryskaState = KryshkaState.Opened;
            }
        }
    }

    void SpawnLoot()
    {
        if (placeForLoot == null) return;

        BoxCollider box = placeForLoot as BoxCollider;
        foreach (var loot in loots)
        {
            if (loot == null) continue;

            Vector3 spawnPos;
            if (box != null)
            {
                Vector3 randomLocal = new Vector3(
                    UnityEngine.Random.Range(-box.size.x / 2f, box.size.x / 2f),
                    UnityEngine.Random.Range(-box.size.y / 2f, box.size.y / 2f),
                    UnityEngine.Random.Range(-box.size.z / 2f, box.size.z / 2f)
                ) + box.center;
                
                spawnPos = placeForLoot.transform.TransformPoint(randomLocal);
            }
            else
            {
                // Fallback for non-box colliders
                Bounds bounds = placeForLoot.bounds;
                spawnPos = new Vector3(
                    UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                    UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                    UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
                );
            }
            
            Instantiate(loot, spawnPos, Quaternion.identity);
        }
    }
}
