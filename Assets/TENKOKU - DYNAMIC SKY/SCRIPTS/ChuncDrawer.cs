using System.Collections.Generic;
using UnityEngine;

public class ChuncDrawer : MonoBehaviour
{
    public List<List<GameObject>> chunks;
    public Vector2 chunkSize;
    public bool draw = true;

    void OnDrawGizmos()
    {
        if (!draw || chunks == null)
            return;

        Gizmos.color = Color.red;

        foreach (var col in chunks)
        {
            foreach (var chunk in col)
            {
                if (chunk == null) continue;

                Vector3 pos = chunk.transform.position;
                Gizmos.DrawWireCube(
                    pos + new Vector3(chunkSize.x / 2f,MainHandler.Instance.playerPosition().y, chunkSize.y / 2f),
                    new Vector3(chunkSize.x, MainHandler.Instance.playerPosition().y + 0.1f, chunkSize.y)
                );
            }
        }
    }
}
