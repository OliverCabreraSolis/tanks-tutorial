using UnityEngine;
using System.Collections.Generic;

public class PowerUpSpawner : MonoBehaviour
{
    public float m_SpawnInterval = 10f;
    public int m_MaxItems = 4;
    private float m_Timer = 3f; // First spawn after 3 seconds
    private List<GameObject> m_ActiveItems = new List<GameObject>();

    private void Update()
    {
        // Clean up collected/destroyed items
        m_ActiveItems.RemoveAll(item => item == null);

        m_Timer -= Time.deltaTime;
        if (m_Timer <= 0f)
        {
            m_Timer = m_SpawnInterval;
            if (m_ActiveItems.Count < m_MaxItems)
            {
                SpawnRandomPowerUp();
            }
        }
    }

    public void SpawnRandomPowerUp()
    {
        // Random position within combat arena (avoiding central direct line)
        float x = Random.Range(-15f, 15f);
        float z = Random.Range(-15f, 15f);
        Vector3 pos = new Vector3(x, 0.5f, z);

        PowerUpType randomType = (PowerUpType)Random.Range(0, 4);
        GameObject powerUp = PowerUpItem.Create(pos, randomType);
        m_ActiveItems.Add(powerUp);
    }

    public void ClearAll()
    {
        foreach (var item in m_ActiveItems)
        {
            if (item != null) Destroy(item);
        }
        m_ActiveItems.Clear();
    }
}
