using System.Runtime.Serialization.Json;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehavior : MonoBehaviour
{
    protected NavMeshAgent navMeshAgent;
    protected Transform player;
    public float attackRange = 2.0f; // Dosah útoku
    public float detectionRange = 10.0f; // Dosah detekce hráèe
    public float patrolRadius = 20.0f; // Polomìr pro náhodné body patrolování
    public float patrolSpeed = 2.0f; // Rychlost pøi patrolování
    public float chaseSpeed = 4.0f; // Rychlost pøi sledování hráèe
    public float attackCooldown = 2.0f; // Cooldown mezi útoky
    public float maxChaseTime = 5.0f; // Maximální doba sledování hráèe
    public float stopTimeAfterAttack = 1.0f; // Doba zastavení po útoku
    public int attackDamage = 10; // Poškození zpùsobené útokem
    protected float lastAttackTime;
    protected float chaseStartTime;
    protected bool isChasing;
    protected bool isStoppedAfterAttack;
    protected float stopEndTime;
    protected bool isStunned;
    protected float stunEndTime;
    protected bool isInHitStun = false;

    public NavMeshAgent NavMeshAgent
    {
        get { return navMeshAgent; }
    }

    [SerializeField]
    private float health = 100f;
    public float Health
    {
        get { return health; }
        set { health = value; }
    }


    // Start is called before the first frame update
    protected virtual void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastAttackTime = -attackCooldown; // Umožní okamžitý první útok
        SetRandomPatrolDestination();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        enableGravity(isStunned);
        if (stunEndTime > 0)
        {
            stunEndTime -= Time.deltaTime;
            if (stunEndTime <=0 )
            {
                isStunned = false;
                navMeshAgent.enabled = true;
                navMeshAgent.isStopped = false;
            }
            else
            {
                return;
            }
        }

        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (isStoppedAfterAttack)
            {
                if (Time.time >= stopEndTime)
                {
                    isStoppedAfterAttack = false;
                    navMeshAgent.isStopped = false;
                }
                else
                {
                    return;
                }
            }

            if (distanceToPlayer <= detectionRange)
            {
                if (!isChasing)
                {
                    isChasing = true;
                    chaseStartTime = Time.time;
                }

                if (distanceToPlayer <= attackRange)
                {
                    navMeshAgent.isStopped = true;
                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        AttackPlayer();
                        lastAttackTime = Time.time;
                        isStoppedAfterAttack = true;
                        stopEndTime = Time.time + stopTimeAfterAttack;
                    }
                }
                else
                {
                    navMeshAgent.isStopped = false;
                    navMeshAgent.SetDestination(player.position);
                    navMeshAgent.speed = chaseSpeed;
                }

                if (Time.time >= chaseStartTime + maxChaseTime)
                {
                    isChasing = false;
                    SetRandomPatrolDestination();
                }
            }
            else
            {
                isChasing = false;
                Patrol();
            }
        }
        else
        {
            isChasing = false;
            Patrol();
        }
    }
    protected void enableGravity(bool isInHitStun)
    {
        gameObject.GetComponent<Rigidbody>().useGravity = !isInHitStun;
        navMeshAgent.enabled = !isInHitStun;
    }
    protected virtual void Patrol()
    {
        navMeshAgent.speed = patrolSpeed;

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
        {
            SetRandomPatrolDestination();
        }
    }

    protected void SetRandomPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
        {
            navMeshAgent.SetDestination(hit.position);
        }
    }

    protected virtual void AttackPlayer()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var hitCollider in hitColliders)
        {
            CharacterController characterController = hitCollider.GetComponent<CharacterController>();
            if (characterController != null && hitCollider.transform.tag == player.tag)
            {

                PlayerStats playerHealth = hitCollider.GetComponent<PlayerStats>();
                if (playerHealth != null)
                {
                    playerHealth.GetDamaged(attackDamage, this);
                }
                else
                {
                    Debug.LogWarning("Komponenta PlayerStats nebyla nalezena na hráèi.");
                }
                break;
            }
        }
    }

    public void GetStunned(float stunDuration)
    {
        isStunned = true;
        if(navMeshAgent.enabled)
        navMeshAgent.isStopped = true;
        stunEndTime = stunDuration;
    }
    public void GetDamaged(float damage, float hitStun)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
            return;
        }
        GetStunned(hitStun);
    }
    private void Die()
    {
        Destroy(gameObject);
    }
    void OnDrawGizmosSelected()
    {
        // Zobrazí dosah útoku v editoru
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.EnemyDied();
        }
    }
}
