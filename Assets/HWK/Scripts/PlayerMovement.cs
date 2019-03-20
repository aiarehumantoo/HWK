using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// NOTES
//===========================



//===========================


// Contains the command the user wishes upon the character
struct Inputs
{
    public float forwardMove;
    public float rightMove;
    public float upMove;
}

public class PlayerMovement : MonoBehaviour
{
    float gravity = 25.0f; //20      // Gravity
    float friction = 6; //6        // Ground friction

    // Q3: players can queue the next jump just before he hits the ground
    private bool wishJump = false;

    // Used to display real time friction values
    private float playerFriction = 0.0f;

    // Player commands
    private Inputs _inputs;

    #region MouseControls
    [Header("Mouse")]
    //Camera
    public Transform playerView;            // Camera
    public float playerViewYOffset = 0.6f; // The height at which the camera is bound to
    public float xMouseSensitivity = 20.0f;
    public float yMouseSensitivity = 20.0f;

    // Camera rotations
    private float rotX = 0.0f;
    private float rotY = 0.0f;
    private Vector3 moveDirectionNorm = Vector3.zero;
    private Vector3 playerVelocity = Vector3.zero;
    private float playerTopVelocity = 0.0f;

    float mouseYaw = 0.022f;     //mouse yaw/pitch. Overwatch = 0.0066, Quake 0.022

    #endregion

    #region MovementVariables
    //Variables for movement

    // CPM / VQ3
    bool useCPM = false;                        // True = CPM, False = VQ3
    float moveSpeed = 7.0f; //7                     // Ground move speed
    float runAcceleration = 14.0f; //14         // Ground accel
    float runDeacceleration = 10.0f; //10       // Deacceleration that occurs when running on the ground
    float airAcceleration = 2.0f; //2          // Air accel
    float airDecceleration = 2.0f; //2         // Deacceleration experienced when ooposite strafing
    float airControl = 0.3f; //0.3                    // How precise air control is
    float sideStrafeAcceleration = 50.0f; //50  // How fast acceleration occurs to get up to sideStrafeSpeed when
    float sideStrafeSpeed = 1.0f; //1               // What the max speed to generate when side strafing
    float jumpSpeed = 8.0f; //8                // The speed at which the character's up axis gains when hitting jump

    #endregion

    // Abilities
    float dodgeSpeed = 40.0f;
    float dodgeCooldown = 1.5f;
    float dodgeTimer;

    private CharacterController _controller;


    // TESTING
    //========================
    bool boosting;
    float boostSpeed = 15;

    float debugWish;
    float debugSpeed;
    float debugAddSpeed;
    float debugAccelSpeed;
    float debugAngle;

    public GUIStyle style;

    //========================


    void MouseControls()
    {
        /* Ensure that the cursor is locked into the screen */
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetButtonDown("Fire1"))
                Cursor.lockState = CursorLockMode.Locked;
        }

        /* Camera rotation stuff, mouse controls this shit */
        rotX -= Input.GetAxisRaw("Mouse Y") * xMouseSensitivity * mouseYaw;
        rotY += Input.GetAxisRaw("Mouse X") * yMouseSensitivity * mouseYaw;

        // Clamp the X rotation
        if (rotX < -90)
            rotX = -90;
        else if (rotX > 90)
            rotX = 90;

        this.transform.rotation = Quaternion.Euler(0, rotY, 0); // Rotates the collider
        playerView.rotation = Quaternion.Euler(rotX, rotY, 0); // Rotates the camera
    }


    private void Start()
    {
        // Hide the cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Put the camera inside the capsule collider
        playerView.position = new Vector3(transform.position.x, transform.position.y + playerViewYOffset, transform.position.z);

        _controller = GetComponent<CharacterController>();
    }


    private void QueueThrusters()
    {
        if (Input.GetButtonDown("Boost") && !boosting)
        {
            boosting = true;
        }
        if (Input.GetButtonUp("Boost"))
        {
            boosting = false;
        }
    }

    // Mech movement when thrusters are on. Mix of Hawken and quake to make it more skill based and engaging? Maybe some sort of "skating" or momentum based.
    // Momentum, Forward boost, left/right dodges?
    // increased movement speed, test with strafing etc
    // or simply increase movement speed while boosting and let friction to handle rest     <--actually this wouldnt work since momentum needs to affect movement instead of having suddent direction changes. groundmove with airacceleration?
    private void BoostMovement() 
    {       
        Vector3 wishdir;
        SetMovementDir();                       

        // Wished direction for movement
        wishdir = new Vector3(_inputs.rightMove, 0, _inputs.forwardMove);
        wishdir = transform.TransformDirection(wishdir);
        wishdir.Normalize();
        moveDirectionNorm = wishdir;

        //length of the wish vector * movement speed
        float wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed;
            debugWish = wishspeed;  //display wishspeed         // 0 or movement speed (7)

        // Wished direction, speed, how fast speed can change
        //Accelerate(wishdir, wishspeed, airAcceleration);

        //========
        float thrusterAcceleration = 7.5f;
        Momentum(wishdir, wishspeed, thrusterAcceleration);

        // Reset the gravity velocity           
        playerVelocity.y = -gravity * Time.deltaTime;


        //======================
        //Testing
        /*
         * current movement vector + wish vector
         *      -> faster movement speed == harder to change direction. Normalize current & wish vector and speed wont change how fast movement direction changes
         * .normalized * desired movement speed
         *      for constant movement speed (alternatively normal speed + ways to temporarily exceed that speed +friction)
         * 
         */

        /*
        // Wished direction for movement
        Vector3 wishdir;
        SetMovementDir();
        wishdir = new Vector3(_inputs.rightMove, 0, _inputs.forwardMove);
        wishdir = transform.TransformDirection(wishdir);

        Vector3 newSpeed;
        newSpeed = wishdir + playerVelocity;    //wished direction + current direction
        newSpeed.Normalize();   // only direction
        newSpeed *= boostSpeed; // multiply with desired velocity

        playerVelocity = newSpeed;
        */

        //float airacc = 10;
        //playerVelocity += wishdir * airacc * Time.deltaTime;




    }

    // TODO:
    // Deacceleration based on degree of turn

    private void Momentum(Vector3 wishdir, float wishspeed, float accel)
    {
        /*
        // New direction
        Vector3 newDirection = playerVelocity + (wishdir * accel * Time.deltaTime);
        newDirection.Normalize();

        // New speed
        float newSpeed;
        if( Vector3.Dot(playerVelocity, wishdir) > 0) // If same direction, accelerate
        {
            // New speed of the player
            newSpeed = playerVelocity.magnitude + (wishdir * accel * Time.deltaTime).magnitude;

            // Speed cap
            if(newSpeed > boostSpeed)
            {
                newSpeed = boostSpeed;
            }
        }
        else //deaccelerate
        {
            newSpeed = playerVelocity.magnitude - (wishdir * accel * Time.deltaTime).magnitude;
        }

        // Set speed of the player
        playerVelocity = newDirection * newSpeed;
        */

        //========================

        Vector3 newVelocity = playerVelocity + (wishdir * accel * Time.deltaTime);
        if(newVelocity.magnitude > boostSpeed)   // If speed is over cap
        {
            // Change direction but limit velocity to speed cap
            newVelocity.Normalize();
            newVelocity *= boostSpeed;
            playerVelocity = newVelocity;
        }
        else
        {
            // Accelerate towards wished direction
            playerVelocity += wishdir * accel * Time.deltaTime;
        }

        //===========================

        // Deacceleration:
        // Does Hawken even have speed reduction on <90 degree turns?

        // Angle between current movement direction & wished direction
        float angle = Vector3.Angle(wishdir, playerVelocity.normalized);

        // Reduce speed based on angle





        //=======================

        //debug
        float currentspeed = Vector3.Dot(playerVelocity, wishdir);    // Dot of movement vector and wished direction. + for same direction, - for opposite directions
        float addspeed = wishspeed - currentspeed;
        float accelspeed = accel * Time.deltaTime * wishspeed;  // airaccel * movementspeed * deltatime

        debugSpeed = currentspeed;
        debugAddSpeed = addspeed;
        debugAccelSpeed = accelspeed;
        debugAngle = angle;             // Not exactly 0 degrees for some reason but movement is still straight forward
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 200, 400, 100), "========", style);
        GUI.Label(new Rect(10, 220, 400, 100), "Wish Speed: " + debugWish + "ups", style);
        GUI.Label(new Rect(10, 240, 400, 100), "Dot Speed: " + debugSpeed + "ups", style);
        GUI.Label(new Rect(10, 260, 400, 100), "Add speed: " + debugAddSpeed + "ups", style);
        GUI.Label(new Rect(10, 280, 400, 100), "Accel speed: " + debugAccelSpeed + "ups", style);
        GUI.Label(new Rect(10, 300, 400, 100), "Angle: " + debugAngle + "degrees", style);
    }






    private void Update()
    {
        MouseControls();
        QueueJump();
        QueueThrusters();       // merge all input thingies?

        //thrusters
            //ground
                //air (gravity)                 no aircontrol, or heavily reduced
                    //walking movement
                        //^^air


        if (_controller.isGrounded)
        {
            if (boosting) // Using thrusters
            {
                BoostMovement();
            }
            else // normal walking movement
            {
                GroundMove();
            }
        }
        else
        {
            AirMove(); // redo air movement. less aircontrol, simple momentum loss?
        }

        // Move the controller
        _controller.Move(playerVelocity * Time.deltaTime);

        //Need to move the camera after the player has been moved because otherwise the camera will clip the player if going fast enough and will always be 1 frame behind.
        // Set the camera's position to the transform
        playerView.position = new Vector3(transform.position.x, transform.position.y + playerViewYOffset, transform.position.z);
    }

    private void SetMovementDir()
    {
        _inputs.forwardMove = Input.GetAxisRaw("Vertical");
        _inputs.rightMove = Input.GetAxisRaw("Horizontal");
    }

    private void QueueJump()
    {
        if (Input.GetButtonDown("Jump") && !wishJump)
        {
            wishJump = true;
        }
        if (Input.GetButtonUp("Jump"))
        {
            wishJump = false;
        }
    }

    private void GroundMove()
    {
        Vector3 wishdir;

        // Do not apply friction if the player is queueing up the next jump
        if (!wishJump)
            ApplyFriction(1.0f);
        else
            ApplyFriction(0);

        SetMovementDir();

        wishdir = new Vector3(_inputs.rightMove, 0, _inputs.forwardMove);
        wishdir = transform.TransformDirection(wishdir);
        wishdir.Normalize();
        moveDirectionNorm = wishdir;

        var wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed;

        Accelerate(wishdir, wishspeed, runAcceleration);

        // Reset the gravity velocity           
        playerVelocity.y = -gravity * Time.deltaTime;

        if (wishJump)
        {
            playerVelocity.y = jumpSpeed;
            wishJump = false;
        }
    }

    private void AirMove()
    {
        //AirMoveVQ3();
        //AirMoveSource(playerVelocity);
        //return;

        Vector3 wishdir;
        float wishvel = airAcceleration;
        float accel;

        SetMovementDir();

        wishdir = new Vector3(_inputs.rightMove, 0, _inputs.forwardMove);
        wishdir = transform.TransformDirection(wishdir);

        float wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed;

        wishdir.Normalize();
        moveDirectionNorm = wishdir;

        // CPM: Aircontrol
        if (useCPM)
        {
            float wishspeed2 = wishspeed;
            if (Vector3.Dot(playerVelocity, wishdir) < 0)
                accel = airDecceleration;
            else
                accel = airAcceleration;
            // If the player is ONLY strafing left or right
            if (_inputs.forwardMove == 0 && _inputs.rightMove != 0)
            {
                if (wishspeed > sideStrafeSpeed)
                    wishspeed = sideStrafeSpeed;
                accel = sideStrafeAcceleration;
            }

            Accelerate(wishdir, wishspeed, accel);
            if (airControl > 0)
                AirControl(wishdir, wishspeed2);
            // !CPM: Aircontrol
        }
        else // VQ3
        {
            Accelerate(wishdir, wishspeed, airAcceleration);
        }

        // Apply gravity
        playerVelocity.y -= gravity * Time.deltaTime;
    }

    private void AirMoveVQ3() //Q3 PM_AirMove
    {
        Vector3 wishdir;

        SetMovementDir();

        wishdir = new Vector3(_inputs.rightMove, 0, _inputs.forwardMove);
        wishdir = transform.TransformDirection(wishdir);

        // Changing the order here results in different acceleration!!!
        // merge both so that acceleration is the same and only difference is in air control
        // Different for VQ3 and CPM?
        // double check source codes for order of input calculations. Groundmove, Airmove CPM & VQ3
        wishdir.Normalize();
        moveDirectionNorm = wishdir;
        float wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed;
        //=============

        Accelerate(wishdir, wishspeed, airAcceleration);

        // Apply gravity
        playerVelocity.y -= gravity * Time.deltaTime;

    }

    private void AirControl(Vector3 wishdir, float wishspeed)
    {
        float zspeed;
        float speed;
        float dot;
        float k;

        // Can't control movement if not moving forward or backward
        if (Mathf.Abs(_inputs.forwardMove) < 0.001 || Mathf.Abs(wishspeed) < 0.001)
            return;
        zspeed = playerVelocity.y;
        playerVelocity.y = 0;
        /* Next two lines are equivalent to idTech's VectorNormalize() */
        speed = playerVelocity.magnitude;
        playerVelocity.Normalize();

        dot = Vector3.Dot(playerVelocity, wishdir);
        k = 32;
        k *= airControl * dot * dot * Time.deltaTime;

        // Change direction while slowing down
        if (dot > 0)
        {
            playerVelocity.x = playerVelocity.x * speed + wishdir.x * k;
            playerVelocity.y = playerVelocity.y * speed + wishdir.y * k;
            playerVelocity.z = playerVelocity.z * speed + wishdir.z * k;

            playerVelocity.Normalize();
            moveDirectionNorm = playerVelocity;
        }

        playerVelocity.x *= speed;
        playerVelocity.y = zspeed; // Note this line
        playerVelocity.z *= speed;
    }

    private void ApplyFriction(float t)
    {
        Vector3 vec = playerVelocity; // Equivalent to: VectorCopy();
        float speed;
        float newspeed;
        float control;
        float drop;

        vec.y = 0.0f;
        speed = vec.magnitude;
        drop = 0.0f;

        /* Only if the player is on the ground then apply friction */
        if (_controller.isGrounded)
        {
            control = speed < runDeacceleration ? runDeacceleration : speed;
            drop = control * friction * Time.deltaTime * t;
        }

        newspeed = speed - drop;
        playerFriction = newspeed;
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        playerVelocity.x *= newspeed;
        playerVelocity.z *= newspeed;
    }

    private void Accelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        float addspeed;
        float accelspeed;
        float currentspeed;

        currentspeed = Vector3.Dot(playerVelocity, wishdir);
        addspeed = wishspeed - currentspeed;
        if (addspeed <= 0)
            return;
        accelspeed = accel * Time.deltaTime * wishspeed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        playerVelocity.x += accelspeed * wishdir.x;
        playerVelocity.z += accelspeed * wishdir.z;
    }
}