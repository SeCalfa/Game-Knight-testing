using UnityEngine;

namespace Game.Code.AI
{
    public class ImmortalEnemy : MonoBehaviour
    {
        [SerializeField] private GameObject impactPrefab;
        [Space]
        [SerializeField] private Transform[] hitPoints;

        public void SpawnImpact()
        {
            var targetHitPoint = hitPoints[Random.Range(0, hitPoints.Length)].position;

            var impact = Instantiate(impactPrefab);
            impact.transform.position = targetHitPoint;
            impact.transform.localScale = new Vector3(Random.Range(0.8f, 1.2f), Random.Range(0.8f, 1.2f), 1f);
        }
    }
}
