using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Cinemachine;
using EzySlice;
using UnityEngine.Rendering;
using DG.Tweening;
using System.ComponentModel;
using Unity.VisualScripting;


public class PlayerAttackController : MonoBehaviour
{
    public float parryWindow = 0.2f;
    private float parryTimer = 0f;
    public float parryCooldown = 1f;
    private float parryCooldownTimer = 0f;
    private Vector3 normalOffset;
    public Vector3 zoomOffset;
    private float normalFOV;
    public float zoomFOV = 15;
    public CinemachineFreeLook freeLook;
    private Animator animator;
    private bool isUptilting = false;
    private bool isDownslamming = false;
    private float cutCooldown = 0.1f;
    private float cutRemainingCD = 0;

    // List of attack animations or methods
    public List<AttackSO> comboAttacks;
    private int comboStep;
    float lastClickedTime;
    float lastComboEnd;
    

    [SerializeField]
    private BoxCollider hitbox;

    [SerializeField]
    private float hitstunTime = 0.5f;

    private PlayerStats playerStats;

    // Special attack variables
    public float specialAttackHeight = 5f;
    public float specialAttackSpeed = 10f;
    public float smashDownSpeed = 20f;
    private CharacterController characterController;
    private PlayerMovement playerMovement;
    private bool isInAirCombo;
    private bool isBlocking = false;
    private CinemachineComposer[] composers;

    private bool isInBladeMode = false;
    public bool IsBlocking => isBlocking;
    public float ParryTimer => parryTimer;

    public bool IsInAirCombo => isInAirCombo;

    // Blade Mode variables
    public float timeScaleInBladeMode = 0.1f;
    public GameObject bladeModeCuttingPlane;

    public LayerMask layerMask;
    public Material crossMaterial;

    float endComboTime = 0.7f;


    void Start()
    {
        hitbox.enabled = false;
        normalFOV = Camera.main.fieldOfView;
        characterController = GetComponent<CharacterController>();
        isInAirCombo = false;
        playerMovement = GetComponent<PlayerMovement>();
        normalFOV = freeLook.m_Lens.FieldOfView;
        bladeModeCuttingPlane.SetActive(false);
        composers = new CinemachineComposer[3];
        animator = GetComponentInChildren<Animator>();
        playerStats = GetComponent<PlayerStats>();
        
        for (int i = 0; i < 3; i++)
        {
            composers[i] = freeLook.GetRig(i).GetCinemachineComponent<CinemachineComposer>();
        }
        normalOffset = composers[0].m_TrackedObjectOffset;
    }

    // Update is called once per frame
    void Update()
    {
        if(cutRemainingCD > 0)
        cutRemainingCD -= Time.unscaledDeltaTime;
        animator.SetFloat("X", Mathf.Clamp(Camera.main.transform.GetChild(0).localPosition.x + 0.3f, -1, 1));
        animator.SetFloat("Y", Mathf.Clamp(Camera.main.transform.GetChild(0).localPosition.y + .18f, -1, 1));
        AnimatorStuff();
        if (Input.GetButtonDown("Blade Mode") && playerStats.CurrentRage > 0)
        {
            GetIntoBladeMode(true);
            Debug.Log("Blade Mode");
        }
        if (Input.GetButtonUp("Blade Mode"))
        {
            GetIntoBladeMode(false);
            Debug.Log("Blade Mode Off");
        }
        
        if (!isInBladeMode )
        {
            // Check for player input to trigger combo
            
            if (Input.GetButtonDown("Fire1") && !isBlocking)
            {
                Attack();
            }
            // Check for special attack input
            if (Input.GetButtonDown("Fire2") && playerMovement.IsGrounded && !isBlocking)
            {
                animator.SetBool("isUptilting", true);
            }

            // Check for smash down attack input
            if (Input.GetButtonDown("Fire2") && !playerMovement.IsGrounded && !isBlocking)
            {
                animator.SetBool("isDownSlamming", true);
            }

            // Check for block input
            if (Input.GetKeyDown(KeyCode.F))
            {
                StartBlocking();
            }
            else if (Input.GetKeyUp(KeyCode.F))
            {
                StopBlocking();
            }
            
        }
        ExitAttack();
        EndAirCombo();
        if (isInBladeMode)
        {
            if(playerStats.CurrentRage <= 0)
            {
                GetIntoBladeMode(false);
            }
            playerStats.DepleteRageBar();
            RotateBladeModePlane();
            if (Input.GetButtonDown("Fire1") && cutRemainingCD <= 0)
            {
                cutRemainingCD = cutCooldown;
                animator.SetFloat("X", animator.GetFloat("X")*-1);
                animator .SetFloat("Y", animator.GetFloat("Y") * -1);
                bladeModeCuttingPlane.transform.GetChild(0).DOComplete();
                bladeModeCuttingPlane.transform.GetChild(0).DOLocalMoveX(bladeModeCuttingPlane.transform.GetChild(0).localPosition.x * -1, .05f).SetEase(Ease.OutExpo);
                Slice();
            }

        }

        if (parryCooldownTimer > 0)
        {
            parryCooldownTimer -= Time.deltaTime;
        }
        if (parryTimer > 0)
        {
            parryTimer -= Time.deltaTime;
        }

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Uptilt") &&
         animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.7f && !isUptilting)
        {
            Debug.Log("Uptilt");    
            PerformSpecialAttack();
            isUptilting = true;

        }
        else if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Uptilt"))
        {
            isUptilting = false;
        }
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("DownSlam") &&
                    animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.7f && !isDownslamming)
        {
            PerformSmashDownAttack();
            isDownslamming = true;
        }
        else if (!animator.GetCurrentAnimatorStateInfo(0).IsName("DownSlam"))
        {
            isDownslamming = false;

        }
        

    }
    public void Attack()
    {
        if(Time.time - lastComboEnd > 0.7f && comboStep <= comboAttacks.Count)
        {
            CancelInvoke("endCombo");

            if(Time.time - lastClickedTime >= 0.7f)
            {
                animator.runtimeAnimatorController = comboAttacks[comboStep].AnimatorOV;
                animator.Play("Attack",0,0);
                CheckHitbox(comboStep);
                comboStep++;
                lastClickedTime = Time.time;
                endComboTime = 1.7f;
                if (comboStep >= comboAttacks.Count)
                {
                    comboStep = 0;
                    endComboTime = 0.5f;
                }
            }
        }
    }
    public void ExitAttack()
    {
        if(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.9f && animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            Invoke("endCombo", 1);
            isInAirCombo = false;
        }
    }
    public void EndCombo()
    {
        comboStep = 0;
        lastComboEnd = Time.time;
        isInAirCombo = false;
    }
    public void ChangeAirComboState(bool state)
    {
        isInAirCombo = state;
    }
    
    void EndAirCombo()
    {
        if(Time.time - lastClickedTime > endComboTime)
        {
            isInAirCombo = false;
        }
    }
    void PerformSpecialAttack()
    {
        Debug.Log("Special Attack");
        isInAirCombo = true;
        StartCoroutine(SpecialAttackRoutine());
        animator.SetBool("isUptilting", false);
        comboStep = 0;

        // Najít nepøátele v hitboxu
        hitbox.enabled = true;
        Collider[] hitColliders = Physics.OverlapBox(hitbox.bounds.center, hitbox.bounds.extents, hitbox.transform.rotation);
        foreach (var hitCollider in hitColliders)
        {
            EnemyBehavior enemy = hitCollider.GetComponent<EnemyBehavior>();
            if (enemy != null)
            {
                enemy.GetDamaged(playerStats.Damage, hitstunTime*3);
                StartCoroutine(MoveEnemyUp(enemy));
                Debug.Log("Enemy hit and sent up: " + hitCollider.name);
            }
        }
        endComboTime = 3.7f;
        hitbox.enabled = false;
    }

    IEnumerator MoveEnemyUp(EnemyBehavior enemy)
    {
        Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
        if(enemy.NavMeshAgent.enabled)
        enemy.NavMeshAgent.isStopped = true;
        enemy.NavMeshAgent.enabled = false;
        if (enemyRb != null)
        {
            enemyRb.isKinematic = true; 
            enemyRb.useGravity = false;
        }
        if(enemy != null)
        {
            Vector3 targetPosition = enemy.transform.position + Vector3.up * specialAttackHeight;
            
                while (enemy != null && enemy.transform.position.y < targetPosition.y)
                {
                    if (targetPosition != null && enemy != null)
                        enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, targetPosition, specialAttackSpeed * Time.deltaTime);
                    yield return null;
                }
           
        }
       
    }


    void PerformSmashDownAttack()
    {
        Debug.Log("Smash Down Attack");
        StartCoroutine(SmashDownRoutine());
        animator.SetBool("isDownSlamming", false);

        hitbox.enabled = true;
        // Najít nepøátele v hitboxu
        Collider[] hitColliders = Physics.OverlapBox(hitbox.bounds.center, hitbox.bounds.extents, hitbox.transform.rotation);
        foreach (var hitCollider in hitColliders)
        {
            EnemyBehavior enemy = hitCollider.GetComponent<EnemyBehavior>();
            if (enemy != null)
            {
                enemy.GetDamaged(playerStats.Damage, hitstunTime*3);
                StartCoroutine(MoveEnemyDown(enemy));
                Debug.Log("Enemy hit and slammed down: " + hitCollider.name);
            }
        }
        hitbox.enabled = false;
    }

    IEnumerator MoveEnemyDown(EnemyBehavior enemy)
    {
        Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
        if (enemy.NavMeshAgent.enabled)
            enemy.NavMeshAgent.isStopped = true;
        enemy.NavMeshAgent.enabled = false;
        if (enemyRb != null)
        {
            enemyRb.isKinematic = true; 
            enemyRb.useGravity = false;
        }

        Vector3 targetPosition = enemy.transform.position + Vector3.down * smashDownSpeed;
        while (!playerMovement.IsGrounded)
        {
            enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, targetPosition, smashDownSpeed * Time.deltaTime);
            yield return null;
        }

        if (enemyRb != null)
        {

        }
    }

    void StartBlocking()
    {
        isBlocking = true;
        if(parryCooldownTimer <= 0)
        {
            parryTimer = parryWindow;
            parryCooldownTimer = parryCooldown;
        }
        if(!animator.GetCurrentAnimatorStateInfo(0).IsName("StartBlock") || !animator.GetCurrentAnimatorStateInfo(0).IsName("Block"))
        {
            animator.SetBool("isBlocking", true);

            animator.Play("StartBlock");
        }
        Debug.Log("Blocking");
    }

    void StopBlocking()
    {
        isBlocking = false;
        Debug.Log("Stopped Blocking");
        animator.SetBool("isBlocking", false);
    }

    IEnumerator SpecialAttackRoutine()
    {
        isInAirCombo = true;
        characterController.enabled = false;
        
        Vector3 targetPosition = transform.position + Vector3.up * specialAttackHeight;


        while (transform.position.y < targetPosition.y)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, specialAttackSpeed * Time.deltaTime);
            yield return null;
        }
        characterController.enabled = true;
    }

    IEnumerator SmashDownRoutine()
    {
        characterController.enabled = false;

        Vector3 targetPosition = transform.position + Vector3.down * smashDownSpeed;

       
        while (!playerMovement.IsGrounded)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, smashDownSpeed * Time.deltaTime);
            yield return null;
        }

        characterController.enabled = true;
        animator.SetBool("isDownSlamming", false);

    }

    private void GetIntoBladeMode(bool state)
    {
        isInBladeMode = state;
        if(playerStats.CurrentRage <= 0)
        {
            state = false;
        }
        if (state)
        {
            Time.timeScale = timeScaleInBladeMode;
            bladeModeCuttingPlane.SetActive(true);
            playerMovement.ChangeStateBetweenBladeAndIdle(true);
            animator.SetBool("isInBladeMode", true);
        }
        else
        {
            Time.timeScale = 1f;
            bladeModeCuttingPlane.SetActive(false);
            playerMovement.ChangeStateBetweenBladeAndIdle(false);
            animator.SetBool("isInBladeMode", false);
        }
        ChangeCamera(state);
    }
    private void RotateBladeModePlane()
    {
        bladeModeCuttingPlane.transform.eulerAngles += new Vector3(0, 0, -Input.GetAxis("Horizontal") * 5);

        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0;

        if (cameraForward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(cameraForward);
        }
    }


    private void ChangeCamera(bool state)
    {
        if (state)
        {
            freeLook.m_Lens.FieldOfView = zoomFOV;
            for (int i = 0; i < 3; i++)
            {
                composers[i].m_TrackedObjectOffset = zoomOffset;
            }
        }
        else
        {
            freeLook.m_Lens.FieldOfView = normalFOV;
            for (int i = 0; i < 3; i++)
            {
                composers[i].m_TrackedObjectOffset = normalOffset;
            }
        }
    }
     public void Slice()
    {
        Collider[] hits = Physics.OverlapBox(bladeModeCuttingPlane.transform.position, new Vector3(5, 0.1f, 5), bladeModeCuttingPlane.transform.rotation, layerMask);
        if (hits.Length <= 0)
            return;

        for (int i = 0; i < hits.Length; i++)
        {
            SlicedHull hull = SliceObject(hits[i].gameObject, crossMaterial);
            if (hull != null)
            {
                GameObject bottom = hull.CreateLowerHull(hits[i].gameObject, crossMaterial);
                GameObject top = hull.CreateUpperHull(hits[i].gameObject, crossMaterial);
                AddHullComponents(bottom);
                AddHullComponents(top);
                Destroy(hits[i].gameObject);
            }
        }
        
    }

    public void AddHullComponents(GameObject go)
    {
        go.layer = 0;
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        MeshCollider collider = go.AddComponent<MeshCollider>();
        collider.convex = true;

        rb.AddExplosionForce(100, go.transform.position, 20);
    }

    public SlicedHull SliceObject(GameObject obj, Material crossSectionMaterial = null)
    {
        if (obj.GetComponent<MeshFilter>() == null)
            return null;

        return obj.Slice(bladeModeCuttingPlane.transform.position, bladeModeCuttingPlane.transform.up, crossSectionMaterial);
    }
    public void AnimatorStuff()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.8f)
        {
            animator.SetBool("isDownSlamming", false);
            animator.SetBool("isUptilting", false);
        }
    }
    private void CheckHitbox(int attackIndex)
    {
        hitbox.enabled = true;
        Collider[] hitColliders = Physics.OverlapBox(hitbox.bounds.center, hitbox.bounds.extents, hitbox.transform.rotation);
        foreach (var hitCollider in hitColliders)
        {
            EnemyBehavior enemy = hitCollider.GetComponent<EnemyBehavior>();
            if (enemy != null)
            {
                enemy.GetDamaged(comboAttacks[attackIndex].AttackDamage, hitstunTime);
                playerStats.GetRageBar(comboAttacks[attackIndex].AttackDamage);
                Debug.Log("Enemy hit: " + hitCollider.name);
            }
        }
        hitbox.enabled = false;
    }
    private void CheckHitbox()
    {
        hitbox.enabled = true;
        Collider[] hitColliders = Physics.OverlapBox(hitbox.bounds.center, hitbox.bounds.extents, hitbox.transform.rotation);
        foreach (var hitCollider in hitColliders)
        {
            EnemyBehavior enemy = hitCollider.GetComponent<EnemyBehavior>();
            if (enemy != null)
            {
                enemy.GetDamaged(playerStats.Damage, hitstunTime);
                playerStats.GetRageBar(playerStats.Damage);
                Debug.Log("Enemy hit: " + hitCollider.name);
            }
        }
        hitbox.enabled = false;
    }
}
