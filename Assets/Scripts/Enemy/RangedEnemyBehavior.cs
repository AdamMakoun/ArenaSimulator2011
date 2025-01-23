using UnityEngine;

public class RangedEnemyBehavior : EnemyBehavior
{
    public float rangedAttackRange = 15.0f; // Dosah útoku na dálku
    public GameObject projectilePrefab; // Prefab projektilu
    public Transform firePoint; // Místo, odkud se støílí projektil

    protected override void Start()
    {
        base.Start();
        attackRange = rangedAttackRange; // Nastavení dosahu útoku na dálku
    }

    protected override void AttackPlayer()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            // Vytvoøení projektilu a jeho vystøelení smìrem k hráèi
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
