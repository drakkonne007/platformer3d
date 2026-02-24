using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DashEffect : MonoBehaviour
{
    [SerializeField] private Material _ghostMaterial;
    [SerializeField] private float _ghostDuration = 0.5f;
    [SerializeField] private float _ghostInterval = 0.05f;
    [SerializeField] private GameObject _meshRoot;

    private Coroutine _dashCoroutine;

    public void StartDashEffect()
    {
        StopDashEffect();
        _dashCoroutine = StartCoroutine(DashCoroutine());
    }

    public void StopDashEffect()
    {
        if (_dashCoroutine != null)
        {
            StopCoroutine(_dashCoroutine);
            _dashCoroutine = null;
        }
    }

    private IEnumerator DashCoroutine()
    {
        while (true)
        {
            CreateGhost();
            yield return new WaitForSeconds(_ghostInterval);
        }
    }

    private void CreateGhost()
    {
        // Create a new ghost object
        GameObject ghostObj = new GameObject("DashGhost");
        ghostObj.transform.position = _meshRoot.transform.position;
        ghostObj.transform.rotation = _meshRoot.transform.rotation;
        ghostObj.transform.localScale = _meshRoot.transform.lossyScale;

        // Copy meshes
        SkinnedMeshRenderer[] smrs = _meshRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var smr in smrs)
        {
            if (!smr.gameObject.activeInHierarchy) continue;

            GameObject ghostPart = new GameObject(smr.name);
            ghostPart.transform.position = smr.transform.position;
            ghostPart.transform.rotation = smr.transform.rotation;
            ghostPart.transform.localScale = smr.transform.lossyScale;
            ghostPart.transform.SetParent(ghostObj.transform);

            MeshFilter mf = ghostPart.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            smr.BakeMesh(mesh);
            mf.mesh = mesh;

            MeshRenderer mr = ghostPart.AddComponent<MeshRenderer>();
            mr.material = _ghostMaterial;
        }

        MeshRenderer[] mrs = _meshRoot.GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in mrs)
        {
            if (!mr.gameObject.activeInHierarchy || mr.GetComponent<SkinnedMeshRenderer>() != null) continue;

            GameObject ghostPart = new GameObject(mr.name);
            ghostPart.transform.position = mr.transform.position;
            ghostPart.transform.rotation = mr.transform.rotation;
            ghostPart.transform.localScale = mr.transform.lossyScale;
            ghostPart.transform.SetParent(ghostObj.transform);

            MeshFilter mf = ghostPart.AddComponent<MeshFilter>();
            MeshFilter sourceMf = mr.GetComponent<MeshFilter>();
            if (sourceMf != null) mf.mesh = sourceMf.sharedMesh;

            MeshRenderer ghostMr = ghostPart.AddComponent<MeshRenderer>();
            ghostMr.material = _ghostMaterial;
        }

        ghostObj.AddComponent<DashGhost>().Init(_ghostDuration, _ghostMaterial);
    }
}
