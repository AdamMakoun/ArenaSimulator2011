using Unity.VisualScripting;
using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    public float speed = 10.0f; // Rychlost projektilu
    public int damage = 10; // Po�kozen� zp�soben� projektilu
    private Transform target;
    private bool isReturning;

    // Nastaven� c�le projektilu
    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    void Update()
    {
        if (target != null)
        {
            // Pohyb projektilu sm�rem k c�li
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            // Kontrola, zda projektil zas�hl c�l
           
        }
        else
        {
            // Zni�it projektil, pokud nem� c�l a nen� vracej�c� se
            if (!isReturning)
            {
                Destroy(gameObject);
            }
        }
    }

    

    void HitTarget(string tag)
    {
        // Logika z�sahu c�le
        if (tag == "Player")
        {
            PlayerStats playerStats = target.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.GetDamaged(damage, null); // Zp�sobit po�kozen� hr��i a p�edat projektil
                Debug.Log("Player hit by projectile!");
            }
        }
        else if (tag == "Enemy" && isReturning)
        {
            EnemyBehavior enemyBehavior = target.GetComponent<EnemyBehavior>();
            if (enemyBehavior != null)
            {
                enemyBehavior.GetStunned(3.0f); // Omr��it nep��tele
            }
        }

        // Zni�it projektil po z�sahu
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player" && !isReturning)
        {
            other.GetComponent<PlayerStats>().GetDamaged(damage, null);
            if(other.GetComponent<PlayerAttackController>().ParryTimer > 0)
            {
                return;
            }
            Destroy(gameObject);
        }
        else if(other.tag == "Enemy" && isReturning)
        {
            other.GetComponent<EnemyBehavior>().GetDamaged(damage, 3.0f);
            Destroy(gameObject);
        }
        else if(other.tag == "Enemy" && !isReturning)
        {
            return;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ReturnToSender(Transform newTarget)
    {
        target = newTarget;
        isReturning = true;
    }
}
