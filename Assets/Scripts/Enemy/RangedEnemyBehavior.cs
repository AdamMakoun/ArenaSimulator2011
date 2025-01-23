using UnityEngine;

public class RangedEnemyBehavior : EnemyBehavior
{
    public float rangedAttackRange = 15.0f; // Dosah �toku na d�lku
    public GameObject projectilePrefab; // Prefab projektilu
    public Transform firePoint; // M�sto, odkud se st��l� projektil

    protected override void Start()
    {
        base.Start();
        attackRange = rangedAttackRange; // Nastaven� dosahu �toku na d�lku
    }

    protected override void AttackPlayer()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            // Vytvo�en� projektilu a jeho vyst�elen� sm�rem k hr��i
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            ProjectileBehavior projectileBehavior = projectile.GetComponent<ProjectileBehavior>();
            if (projectileBehavior != null)
            {
                projectileBehavior.SetTarget(player);
            }
            Debug.Log("Ranged attack on player!");
        }
    }
}
