using Unity.VisualScripting;
using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    public float speed = 10.0f; // Rychlost projektilu
    public int damage = 10; // Poškození zpùsobené projektilu
    private Transform target;
    private bool isReturning;

    // Nastavení cíle projektilu
    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    void Update()
    {
        if (target != null)
        {
            // Pohyb projektilu smìrem k cíli
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            // Kontrola, zda projektil zasáhl cíl
           
        }
        else
        {
            // Znièit projektil, pokud nemá cíl a není vracející se
            if (!isReturning)
            {
                Destroy(gameObject);
            }
        }
    }

    

    void HitTarget(string tag)
    {
        // Logika zásahu cíle
        if (tag == "Player")
        {
            PlayerStats playerStats = target.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.GetDamaged(damage, null); // Zpùsobit poškození hráèi a pøedat projektil
                Debug.Log("Player hit by projectile!");
            }
        }
        else if (tag == "Enemy" && isReturning)
        {
            EnemyBehavior enemyBehavior = target.GetComponent<EnemyBehavior>();
            if (enemyBehavior != null)
            {
                enemyBehavior.GetStunned(3.0f); // Omráèit nepøítele
            }
        }

        // Znièit projektil po zásahu
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
