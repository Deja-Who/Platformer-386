using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Parameters")]
    [SerializeField] private float speed;
    [SerializeField] private float jumpPower;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteTime; //How much time the player can hang in the air before jumping
    private float coyoteCounter; //How much time passed since the player ran off the edge

    [Header("Multiple Jumps")]
    [SerializeField] private int extraJumps;
    private int jumpCounter;

    [Header("Wall Jumping")]
    [SerializeField] private float wallJumpX; //Horizontal wall jump force
    [SerializeField] private float wallJumpY; //Vertical wall jump force

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Sounds")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip landingSound;

    [Header("Dash")]
    [SerializeField] private float dashForce = 10f;
    [SerializeField] private float dashCooldown = 1f; // Time before player can dash again


    private float dashCooldownRemaining = 0f;
    private float tapTimeLeft = -1f;
    private float tapTimeRight = -1f;
    private bool isLeftButtonReleased = true;
    private bool isRightButtonReleased = true;
    private float doubleTapTime = 0.25f;
    private float currentDirection = 0f;  // 1 for right, -1 for left, 0 for not moving

    private bool isDashing;
    private float dashTime = 0.2f; // Time taken to perform the dash
    private float dashTimeLeft; // Remaining time of the current dash
    private float landCountdown = 0;
    private float countDownTime = 1;
   

    private Rigidbody2D body;
    //private Animator anim;
    private BoxCollider2D boxCollider;
    private float wallJumpCooldown;
    private float horizontalInput;

    private void Awake()
    {
        //Grab references for rigidbody and animator from object
        body = GetComponent<Rigidbody2D>();
       // anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");

        //Flip player when moving left-right
        if (horizontalInput > 0.01f)
            transform.localScale = Vector3.one;
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);

        //Set animator parameters
       // anim.SetBool("run", horizontalInput != 0);
       // anim.SetBool("grounded", isGrounded());

       
        if (Mathf.Abs(horizontalInput) > 0.01f)
{
    currentDirection = Mathf.Sign(horizontalInput);
    Debug.Log("Current Direction: " + currentDirection);
}

        
       
        
        //Jump
        if (Input.GetKeyDown(KeyCode.Space))
            Jump();

        //Adjustable jump height
        if (Input.GetKeyUp(KeyCode.Space) && body.velocity.y > 0)
            body.velocity = new Vector2(body.velocity.x, body.velocity.y / 2);
           

        if (onWall())
        {
            body.gravityScale = 0;
            body.velocity = Vector2.zero;
        }
        else
        {
            body.gravityScale = 7;
            body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);
            //body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);

            if (isGrounded())
            {
                if (landCountdown < 1)
                {
                    SoundManager.instance.PlaySound(landingSound);
                    landCountdown = countDownTime;
                }
                coyoteCounter = coyoteTime; //Reset coyote counter when on the ground
                jumpCounter = extraJumps; //Reset jump counter to extra jump value
            }
            else
            {
                coyoteCounter -= Time.deltaTime; //Start decreasing coyote counter when not on the ground
                if (landCountdown > 0)
                {
                    landCountdown -= Time.deltaTime;
                }
            }
        }
         //double-tapping for dashing
         if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            CheckDoubleTap(ref tapTimeLeft, ref isLeftButtonReleased);
        }
        
        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            isLeftButtonReleased = true;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            CheckDoubleTap(ref tapTimeRight, ref isRightButtonReleased);
        }
        
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            isRightButtonReleased = true;
        }
         if (isDashing)
        {
            Dash();
        }
        if (dashCooldownRemaining > 0)
    {
        dashCooldownRemaining -= Time.deltaTime;
    }
    }

private void StartDash()
{
    isDashing = true;
    dashTimeLeft = dashTime;
    SoundManager.instance.PlaySound(dashSound);
    }
   private void CheckDoubleTap(ref float tapTime, ref bool isButtonReleased)
    {
        if (dashCooldownRemaining > 0) return; // Return if dash is on cooldown
        if (isButtonReleased && Time.time - tapTime < doubleTapTime)
        {
            StartDash();
            isButtonReleased = false;
            dashCooldownRemaining = dashCooldown;
        }
        else
        {
            tapTime = Time.time;
        }
    }

    private void Dash()
{
    body.gravityScale = 3;
    body.mass = 1;
    body.drag = 0;
    body.velocity = new Vector2(dashForce * currentDirection, body.velocity.y);
    dashTimeLeft -= Time.deltaTime;
    if (dashTimeLeft <= 0)
    {
        body.gravityScale = 7;
        body.mass = 6;
        isDashing = false;
    }
}

    private void Jump()
    {
        if (coyoteCounter <= 0 && !onWall() && jumpCounter <= 0) return; 
        //If coyote counter is 0 or less and not on the wall and don't have any extra jumps don't do anything

        SoundManager.instance.PlaySound(jumpSound);

        if (onWall())
            WallJump();
        else
        {
            if (isGrounded())
            {
                //body.drag = 0;
                body.velocity = new Vector2(body.velocity.x, jumpPower);
            }
            else
            {
                //If not on the ground and coyote counter bigger than 0 do a normal jump
                if (coyoteCounter > 0)
                    body.velocity = new Vector2(body.velocity.x, jumpPower);
                else
                {
                    if (jumpCounter > 0) //If we have extra jumps then jump and decrease the jump counter
                    {
                        body.velocity = new Vector2(body.velocity.x, jumpPower);
                        jumpCounter--;
                    }
                }
            }

            //Reset coyote counter to 0 to avoid double jumps
            coyoteCounter = 0;
        }
    }

       
    private void WallJump()
    {
        body.AddForce(new Vector2(-Mathf.Sign(transform.localScale.x) * wallJumpX, wallJumpY));
        wallJumpCooldown = 0;
    }


    private bool isGrounded()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
        return raycastHit.collider != null;
    }
    private bool onWall()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, new Vector2(transform.localScale.x, 0), 0.1f, wallLayer);
        return raycastHit.collider != null;
    }
    public bool canAttack()
    {
        return horizontalInput == 0 && isGrounded() && !onWall();
    }
}