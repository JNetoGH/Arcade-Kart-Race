using System;
using NaughtyAttributes;
using UnityEngine;
using Range = UnityEngine.SocialPlatforms.Range;


/// <summary>
/// Mainly moves the Rigidbody in the fixed update, and then makes the attached car follow in the update.
/// But the rotation is made on the car, otherwise the rigidbody would fly all around.
/// </summary>
public class CarController : MonoBehaviour
{
    
    public enum InputMode
    {
        Controller,
        Keyboard,
    }

    [Header("General")] 
    [SerializeField] private InputMode _inputMode = InputMode.Controller;
    [SerializeField] private Rigidbody _physicsModel;
    
    [Header("Acceleration")]
    [SerializeField] private float _forwardAcceleration = 8f;
    [SerializeField] private float _reverseAcceleration = 4f;
    [SerializeField] private float _accelerationMultiplier = 1000f;
    [SerializeField] private float _maxSpeed = 30f;
   
    [Header("Turning")]
    [SerializeField, Tooltip(TipTurn)] private float _turnStrengthWhenOnGround = 120;
    [SerializeField, Tooltip(TipTurn)] private float _turnStrengthWhenInAir = 180;
    [SerializeField, Tooltip(TipZScale), Range(0, 1)] private float _turnZAxisScale = 0.5f;
    private const string TipTurn = "How fast the car turns.";
    private const string TipZScale = "The amount of rotation in the Z axis." +
                                     "This Z axis modifier can vary from 0 to 1, 0 being nothing and 1 the same " +
                                     "amount in the Z axis as Y axis.";
    
    [Header("Gravity/Ground Check")]
    [SerializeField, Tooltip(TipGf)] private float _gravityForce = 10;
    [SerializeField] private float _gravityMultiplier = 100f;
    [ReadOnly, SerializeField] private bool _isGrounded;
    [SerializeField] private float _groundCheckerRayLength = 0.5f;
    [SerializeField] private Transform _groundCheckerRayStartPoint;
    [SerializeField] private LayerMask _groundLayers;
    private const string TipGf = "Applied only when the rigid body is off the ground.";
    private RaycastHit _groundRayHit;
    
    [Header("Drag")]
    [SerializeField, Tooltip(TipDwg)] private float _dragWhenOnGround = 3;
    [SerializeField, Tooltip(TipDwa)] private float _dragWhenInAir = 0.1f;
    private const string TipDwg = "How much the car stops moving when on the ground, around 3 is mostly fine";
    private const string TipDwa = "How much the car stops moving when on the air, it should be low, like 0.1, " +
                                  "if it's too high, the car will suddenly stop once it's off the ground.";

    [Header("Slope")] 
    [SerializeField] private float _angleOnSlopeChangingSpeed = 100f;
    
    [Header("Jumping")] 
    [SerializeField] private float _jumpForce = 35f;
    [SerializeField] private float _jumpMultiplier = 1000;
    [SerializeField, Range(0, 1), Tooltip(TipFrd)] private float _jumpForwardness = 0.5f;
    private bool _jumpInputBuffer = false;
    private const string TipFrd = "The amount of force applied forward." +
                                  "The jump direction can have some amount of forwardness in it, varying from 0 to 1, " +
                                  "0 being nothing and 1 the same amount of forwards as upwards.";
    
    [Header("Debugging")] 
    [ReadOnly, SerializeField] private float _currentSpeed;
    [ReadOnly, SerializeField] private float _verticalInput;
    [ReadOnly, SerializeField] private float _verticalIncrement;
    [ReadOnly, SerializeField] private float _horizontalInput;
    [ReadOnly, SerializeField] private float _turnIncrement;
    
    // Communication Properties
    public float CurrentSpeed => _currentSpeed;
    public bool IsGrounded => _isGrounded;
    public float VerticalInput => _verticalInput;
    public float HorizontalInput => _horizontalInput;
    public InputMode InputModeProp { get => _inputMode; set => _inputMode = value; }
    
    private void Update()
    {
        UpdateInputs();
        UpdateIncrements();
        TryJump();
        UpdateTurnRotation();
        UpdateCarPosition();
    }
    
    private void FixedUpdate()
    {
        UpdateMaxVelocity();
        UpdateIsGrounded();
        UpdateSlope();
        UpdateDrag();
        UpdatePhysicsModelMotion();
    }

    private void UpdateInputs()
    {
        switch (_inputMode)
        {
            case InputMode.Keyboard:
                _verticalInput = Input.GetAxis("Vertical");
                _horizontalInput = Input.GetAxis("Horizontal");
                if (_isGrounded && Input.GetButtonDown("Jump"))
                    _jumpInputBuffer = true;
                break;
            case InputMode.Controller:
                // The forward has priority over the reverse.
                _verticalInput = Input.GetButton("Fire2") ? 1 : 0;
                if (_verticalInput == 0) 
                    _verticalInput = Input.GetButton("Fire1") ? -1 : 0;
                _horizontalInput = Input.GetAxis("Horizontal");
                if (_isGrounded && Input.GetButtonDown("Jump"))
                    _jumpInputBuffer = true;
                break;
        }
    } 
    
    private void UpdateIncrements()
    {
        // Clears the forward/backward increment and then, updates it: (direction * acceleration * multiplier).
        _verticalIncrement = 0;
        bool goingForward = _verticalInput > 0;
        bool goingBackward = _verticalInput > 0;
        if (goingForward) _verticalIncrement = _verticalInput * _forwardAcceleration * _accelerationMultiplier;
        else if (goingBackward) _verticalIncrement = _verticalInput * _reverseAcceleration * _accelerationMultiplier;
        
        // Updates the turning increment by adding on the amount of turning to be done:
        // (direction * strength * △T)
        _turnIncrement = 0;
        float turnStrength = (_isGrounded ? _turnStrengthWhenOnGround : _turnStrengthWhenInAir);
        _turnIncrement = _horizontalInput * turnStrength * Time.deltaTime;
        
        // When grounded (not in the air, for air controlling):
        // - The car should not turn when stopped, only when moving, to do so, the _turnIncrement is multiplied by the
        //   _verticalInput, when the car is stopped, this value is 0, and therefore the turn increment will be zero.
        // - When moving backward the rotation keys/axis will be inverted, just like in a real car.
        if (_isGrounded)
            _turnIncrement *= _verticalInput;
    } 
    
    private void UpdateCarPosition()
    {
	    transform.position = _physicsModel.transform.position;
    }
    
    private void UpdateTurnRotation()
    {
        // Updates the turning by adding on the amount of turning to be done, varying from grounded to in air.
        if (_isGrounded)
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, _turnIncrement, 0));
        else
            // Turn increment in the Z axis needs to be negative.
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, _turnIncrement, -_turnIncrement * _turnZAxisScale));
    }
    
    private void UpdateMaxVelocity()
    {
        // Tolerance checking
        if (Math.Abs(_physicsModel.maxLinearVelocity - _maxSpeed) > 0.0001f)
            _physicsModel.maxLinearVelocity = _maxSpeed;
    }
    
    private void UpdateIsGrounded()
    {
        // Clears the previously cached value.
        _isGrounded = false;
        // it needs to be in a direction relative to the car, and there is no transform.down, that's why the - transform.up.
        if (Physics.Raycast(_groundCheckerRayStartPoint.position, -transform.up , out _groundRayHit, _groundCheckerRayLength, _groundLayers))
            _isGrounded = true;
    }
    
    private void UpdateSlope()
    {
        // - Rotates the car, based on what the ground ray checker hits while grounded,
        //   it enables teh car to rotate over slopes in the X axis.
        // - This method will make the transform.Up tho be the same as the normal of the surface hit.
        // - Which is the angle what whatever the physics model is hitting against.
        // - Multiplying by the current rotation, will allow it to be affected by the current Turn rotation (y-axis),
        //   otherwise the car would always override the turn rotation and look only forward.
        if (!_isGrounded)
            return;
        
        // Moves gradually towards the slope, otherwise it would just teleport.
        Quaternion targetSlopeRot = Quaternion.FromToRotation(transform.up, _groundRayHit.normal) * transform.rotation;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetSlopeRot, _angleOnSlopeChangingSpeed * Time.deltaTime);
    }
    
    private void UpdateDrag()
    {
        _physicsModel.drag = _isGrounded ? _dragWhenOnGround : _dragWhenInAir;
    }

    private void UpdatePhysicsModelMotion()
    {
        // THe car can only move when grounded, otherwise it will have some extra gravity applied.
        if (_isGrounded)
        {
            if (_verticalInput != 0)
                // This forward is the car's forward, not the physics model.
                _physicsModel.AddForce(transform.forward * _verticalIncrement);
        }
        else
        {
            // Applies some extra gravity when not grounded.
            _physicsModel.AddForce(Vector3.down * _gravityForce * _gravityMultiplier);
        }
        
        // It's not used for nothing in the motion, but it's sent to stuff like GUI.
        _currentSpeed = _physicsModel.velocity.magnitude;
    }
    
    private void TryJump()
    {
        if (_jumpInputBuffer) _jumpInputBuffer = false;
        else return;
        
        // Blocks the player from jumping twice,
        // by allowing jump only when not jumping already (velocity.y not going up).
        // The 0.2 instead of 0 is to avoid mistakes from possible noises in the rigidbody.
        bool isGoingUp = _physicsModel.velocity.y > 0.2;
        if (isGoingUp)
            return;
        
        Debug.Log($"Payer Jumped: velocity.y ({_physicsModel.velocity.y})");
        Vector3 jumpDirection = transform.up + (transform.forward * _jumpForwardness);
        _physicsModel.AddForce(jumpDirection * (_jumpForce * _jumpMultiplier));
    
    }
    
    private void OnDrawGizmos()
    {
        DrawGroundCheckerRayOnGizmos();
    }
    
    private void DrawGroundCheckerRayOnGizmos() 
    {
        if (_groundCheckerRayStartPoint == null) 
            return;
        
        Vector3 rayDirection = -transform.up * _groundCheckerRayLength;
        // Set gizmo color based on grounded state
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(_groundCheckerRayStartPoint.position, _groundCheckerRayStartPoint.position + rayDirection);
        Gizmos.DrawSphere(_groundCheckerRayStartPoint.position + rayDirection, 0.1f);
    }
    
}
