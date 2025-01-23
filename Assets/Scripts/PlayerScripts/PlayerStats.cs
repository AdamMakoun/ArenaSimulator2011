using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [SerializeField]
    private float health = 100;
    [SerializeField]
    private float maxHealth = 100;
    [SerializeField]
    private float damage = 10;

    [SerializeField]
    private float resistances = 0f;
  
    [SerializeField]
    private float rageBar = 10f;
    private float currentRage = 0;
    private PlayerAttackController playerAttackController;

    [SerializeField]
    private Image rageBarImage;
    [SerializeField]
    private Image healthBarImage;
    [SerializeField]
    private TMP_Text healthText;
    private void Start()
    {
        playerAttackController = GetComponent<PlayerAttackController>();
        UpdateBars();
    }
    private void Update()
    {
      
    }

    public float Health
    {
        get { return health; }
        set { health = value; }
    }
    public float MaxHealth
    {
        get { return maxHealth; }
        set { maxHealth = value; }
    }
    public float Damage
    {
        get { return damage; }
        set { damage = value; }
    }
 
    public float RageBar
    {
        get { return rageBar; }
        set { rageBar = value; }
    }
    public float CurrentRage
    {
        get { return currentRage; }
        set { currentRage = value; }
    }
    public void GetDamaged(float damage, EnemyBehavior attackingEnemy)
    {
        if (playerAttackController.IsBlocking)
        {
            if (playerAttackController.ParryTimer > 0)
            {
                Debug.Log("Parry successful");
                //todo add parry animation
                currentRage += 30;
                if (currentRage >= rageBar)
                {
                    currentRage = rageBar;
                }

                // Omráèit útoèícího nepøítele
                if (attackingEnemy != null)
                {
                    attackingEnemy.GetStunned(3.0f); // Omráèení na 3 sekundy
                }
                return;
            }
            if (damage >= 0)
            {
                damage *= 0.2f;
            }
        }
        if (damage >= 0)
        {
            health -= damage;
            UpdateBars();
            if (health <= 0)
            {
                Die();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Kontrola kolize s projektily
        ProjectileBehavior projectile = other.GetComponent<ProjectileBehavior>();
        if (projectile != null && playerAttackController.IsBlocking && playerAttackController.ParryTimer > 0)
        {
            Debug.Log("Parry successful - returning projectile");
            Transform nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                projectile.ReturnToSender(nearestEnemy);
            }
        }
    }

    Transform FindNearestEnemy()
    {
        EnemyBehavior[] enemies = FindObjectsOfType<EnemyBehavior>();
        Transform nearestEnemy = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (EnemyBehavior enemy in enemies)
        {
            float distance = Vector3.Distance(enemy.transform.position, currentPosition);
            if (distance < minDistance)
            {
                nearestEnemy = enemy.transform;
                minDistance = distance;
            }
        }

        return nearestEnemy;
    }




    private void Die()
    {
        Debug.Log("Player died");
        Time.timeScale = 0;
        //show death screen
    }

    private void UpdateBars()
    {
        rageBarImage.fillAmount = currentRage / rageBar;
        healthBarImage.fillAmount = health / maxHealth;
        healthText.text =  ((health / maxHealth) * 100) + "%";
    }
    public void DepleteRageBar()
    {
        if(currentRage >= 0)
        currentRage -= Time.deltaTime*10;
        else currentRage = 0;
        UpdateBars();
    }
    public void GetRageBar(float damageDone)
    {
        currentRage += damageDone/10;
        if (currentRage >= rageBar)
        {
            currentRage = rageBar;
        }
        UpdateBars();
    }
}
