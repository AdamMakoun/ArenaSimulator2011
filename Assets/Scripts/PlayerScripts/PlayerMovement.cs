using System.Collections;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public enum PlayerState
{
    Idle,
    Running,
    Sprinting,
    Jumping,
    Dashing,
    Vaulting,
    WallJumping,
    BladeMode
}
public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform cameraTransform;
    public float walkSpeed = 6f;
    public float sprintSpeed = 12f;
    public float dashSpeed = 20f;
    public float jumpHeight = 3f;
    public float gravity = -9.81f;
    public float dashDuration = 0.2f;
    public float rotationSpeed = 10f;
    public float vaultDuration = 0.5f;
    public float wallJumpBackForce = 2f;
    public float wallJumpUpForce = 5f;
    public float Iframes = 0.2f;
    public float dashCooldown = 1f;
    private float dashCooldownTimer = 0f;
    private bool canMove = true;
    private int maxWallJumps = 3;
    private int currentWallJumps = 0;

    private float IframesTimer = 0f;
    public float IFramesTimer => IframesTimer;
    
    public int maxJumps = 2;

    private Vector3 velocity;
    private bool isGrounded;
    private int jumpCount;
    private float dashTime;
    private float wallRunTime;
    private float wallRunCooldownTime;
    private Animator animator;
    private PlayerAttackController playerAttackController;
    [SerializeField] private LayerMask groundLayer;

    public bool IsGrounded => isGrounded;


    private PlayerState currentState;
    public PlayerState CurrentState => currentState;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        animator = GetComponentInChildren<Animator>();
        playerAttackController = GetComponent<PlayerAttackController>();
        currentState = PlayerState.Idle;
    }

    private void Update()
    {
        CheckGroundStatus();
        HandleMovement();
        HandleStateTransitions();
        ApplyGravity();
        HandleAnims();
        //Debug.Log(currentState);

        controller.Move(velocity * Time.deltaTime);
        if(dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }
    private void HandleAnims()
    {
        if(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f && animator.GetCurrentAnimatorStateInfo(0).IsName("Jump") && jumpCount == 0 )
        {
            Jump();
            animator.SetBool("isJumping", false);   
        }

        
        if(isGrounded)
        {
            animator.SetBool("isDoubleJumping", false);

        }
        if(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f && animator.GetCurrentAnimatorStateInfo(0).IsName("DoubleJump") )
        {
            animator.SetBool("isDoubleJumping", false);
        }
    }
    public void SetIsInAirCombo(bool value)
    {
    }
    private void CheckGroundStatus()
    {
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, 1.1f, groundLayer);
        if (isGrounded && velocity.y < 0)
        {
            
                velocity.y = -2f;
                jumpCount = 0; // Reset jump count when grounded
                currentWallJumps = 0;
            Debug.Log("reset jumps");
            
        }
    }
    public void isInCombo(bool value)
    {
        canMove = !value;
    }
    private void HandleMovement()
    {
        if(currentState == PlayerState.BladeMode || playerAttackController.IsInAirCombo)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isSprinting", false);
            animator.SetBool("isDashing", false);
            animator.SetBool("isVaulting", false);

            return;
        }
        
        
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = (cameraTransform.right * moveX + cameraTransform.forward * moveZ).normalized;
        move.y = 0;

        if (move != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        switch (currentState)
        {
            case PlayerState.Idle:
                animator.SetBool("isWalking", false);
                animator.SetBool("isSprinting", false);
                animator.SetBool("isDashing", false);
                animator.SetBool("isVaulting", false);

                break;
            case PlayerState.Running:
                controller.Move(move * walkSpeed * Time.deltaTime);
                animator.SetBool("isWalking", true);
                animator.SetBool("isSprinting", false);
                animator.SetBool("attack1", false);
                animator.SetBool("attack2", false);
                animator.SetBool("attack3", false);
                break;
            case PlayerState.Sprinting:
                controller.Move(move * sprintSpeed * Time.deltaTime);
                animator.SetBool("isSprinting", true);
                animator.SetBool("isWalking", false);
                animator.SetBool("attack1",false);
                animator.SetBool("attack2",false);
                animator.SetBool("attack3",false);
                break;
            case PlayerState.Dashing:
                animator.SetBool("isWalking", false);
                animator.SetBool("isSprinting", false);
                controller.Move(move * dashSpeed * Time.deltaTime);
                dashTime -= Time.deltaTime;
                IframesTimer -= Time.deltaTime;
                if (dashTime <= 0)
                {
                    currentState = PlayerState.Idle;
                }
                break;
            case PlayerState.Vaulting:
                // Do nothing while vaulting
                break;
            case PlayerState.Jumping:
                break;
            
        }
    }

    private void HandleStateTransitions()
    {
        if(currentState == PlayerState.BladeMode)
        {
            return;
        }
        if (Input.GetKey(KeyCode.LeftShift) && currentState != PlayerState.Vaulting)
        {
            currentState = PlayerState.Sprinting;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift) && currentState == PlayerState.Sprinting)
        {
            currentState = PlayerState.Running;
        }

        if (Input.GetButtonDown("Jump"))
        {
            playerAttackController.ChangeAirComboState(false);
            
            if (!isGrounded && CheckForWallJump())
            {
                WallJump();
            }
            if (jumpCount < maxJumps)
            {
                if(jumpCount == 0)
                {
                    animator.SetBool("isJumping", true);
                }
                else if(jumpCount == 1)
                {
                    Jump();
                }
                
            }
            Debug.Log(jumpCount);
        }

        if (Input.GetButtonDown("Dash") && currentState != PlayerState.Dashing && currentState != PlayerState.Vaulting && dashCooldownTimer <= 0)
        {
            currentState = PlayerState.Dashing;
            dashTime = dashDuration;
            IframesTimer = Iframes;
            dashCooldownTimer = dashCooldown;
        }

        if (Input.GetKey(KeyCode.LeftShift) && currentState != PlayerState.Vaulting)
        {
            Vault();
        }

        if (currentState != PlayerState.Sprinting && currentState != PlayerState.Dashing && currentState != PlayerState.Vaulting)
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            if (moveX == 0 && moveZ == 0)
            {
                currentState = PlayerState.Idle;
            }
            else
            {
                currentState = PlayerState.Running;
            }
        }

       
    }
    private void Jump()
    {
        if(jumpCount < maxJumps)
        {   
            if(jumpCount == 1)
            {
                animator.SetBool("isDoubleJumping", true);

            }
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;
            
        }
    }

    private void ApplyGravity()
    {
        if(playerAttackController.IsInAirCombo)
        {
            velocity.y = 0;
        }
        else if (currentState != PlayerState.Vaulting)
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    private void Vault()
    {
        if(currentState == PlayerState.BladeMode)
        {
            return;
        }
        Vector3[] rayOrigins = new Vector3[]
        {
            transform.position + Vector3.up * 0.2f, // Legs
            transform.position + Vector3.up * (controller.height / 2), // Body
            transform.position + Vector3.up * (controller.height - 0.2f) // Head
        };

        foreach (var rayOrigin in rayOrigins)
        {
            if (Physics.Raycast(rayOrigin, transform.forward, out var firstHit, 1f))
            {
                if (Physics.Raycast(firstHit.point + (transform.forward * 0.5f) + (Vector3.up * 0.6f * controller.height), Vector3.down, out var secondHit, controller.height))
                {
                    Vector3 targetPosition = secondHit.point + Vector3.up * (controller.height / 2);
                    StartCoroutine(LerpVault(targetPosition, vaultDuration));
                    break;
                }
            }
        }
    }

    private IEnumerator LerpVault(Vector3 targetPosition, float duration)
    {
        currentState = PlayerState.Vaulting;
        controller.enabled = false;
        animator.Play("Vaulting", 0, 0);
        float time = 0;
        Vector3 startPosition = transform.position;
        while (time < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        controller.enabled = true;
        currentState = PlayerState.Idle;

    }

    private bool CheckForWallJump()
    {
        if (currentState == PlayerState.BladeMode)
        {
            return false;
        }
        Vector3[] rayOrigins = new Vector3[]
        {
            transform.position + Vector3.up * 0.2f, // Legs
            transform.position + Vector3.up * (controller.height / 2), // Body
            transform.position + Vector3.up * (controller.height - 0.2f) // Head
        };

        foreach (var rayOrigin in rayOrigins)
        {
            if (Physics.Raycast(rayOrigin, transform.forward, out var hit, 1f))
            {
                if (!CanVault(hit))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool CanVault(RaycastHit hit)
    {
        return Physics.Raycast(hit.point + (transform.forward * 0.5f) + (Vector3.up * 0.6f * controller.height), Vector3.down, out var secondHit, controller.height);
    }

    private void WallJump()
    {
        if(currentState == PlayerState.BladeMode)
        {
            return;
        }
        if(currentWallJumps < maxWallJumps)
        {
            animator.SetBool("isDoubleJumping", true);
            currentWallJumps++;
            StartCoroutine(LerpWallJump(wallJumpBackForce, wallJumpUpForce, vaultDuration));
        }
    }
      

    private IEnumerator LerpWallJump(float backForce, float upForce, float duration)
    {
        currentState = PlayerState.WallJumping;
        controller.enabled = false;
        float time = 0;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + (-transform.forward * backForce) + (Vector3.up * upForce);

        while (time < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        animator.SetBool("isWallJumping", false);
        transform.position = targetPosition;
        controller.enabled = true;
        velocity = Vector3.zero; // Reset velocities
        currentState = PlayerState.Idle;
    }
    public void ChangeStateBetweenBladeAndIdle(bool state)
    {
        if (state)
        {
            currentState = PlayerState.BladeMode;
        }
        else
        {
            currentState = PlayerState.Idle;
        }
    }
}