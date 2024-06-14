using System;
using NaughtyAttributes;
using UnityEngine;


/// <summary>
/// Mainly moves the Rigidbody in the fixed update, and then makes the attached car follow in the update.
/// But the rotation is made on the car, otherwise the rigidbody would fly all around.
/// </summary>
public class CarController : MonoBehaviour
{
    
    public enum InputMode
    {
        Keyboard,
        Controller,
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
    [SerializeField, Tooltip(TipTf)] private float _turnStrength = 120;
    private const string TipTf = "How fast the car turns";
    
    [Header("Gravity/Ground Check")]
    [SerializeField, Tooltip(TipGf)] private float _gravityForce = 10;
    [SerializeField] private float _gravityMultiplier = 100f;
    [ReadOnly, SerializeField] private bool _isGrounded;
    [SerializeField] private float _groundCheckerRayLength = 0.5f;
    [SerializeField] private Transform _groundCheckerRayStartPoint;
    [SerializeField] private LayerMask _groundLayers;
    private const string TipGf = "Applied only when the rigid body is off the ground";
    private RaycastHit _groundRayHit;
    
    [Header("Drag")]
    [SerializeField, Tooltip(TipDwg)] private float _dragWhenOnGround = 3;
    [SerializeField, Tooltip(TipDwa)] private float _dragWhenOnAir = 0.1f;
    private const string TipDwg = "How much the car stops moving when on the ground, around 3 is mostly fine";
    private const string TipDwa = "How much the car stops moving when on the air, it should be low, like 0.1, " +
                                  "if it's too high, the car will suddenly stop once it's off the ground.";

    [Header("Slope")] 
    [SerializeField] private float _angleOnSlopeChangingSpeed = 100f;
    
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

    private void Start()
    {
        // Deparents the physics model so the car movement post doesn't move it.
        _physicsModel.transform.parent = null;
    }
    
    private void Update()
    {
        UpdateInputs();
        UpdateIncrements();
        UpdateTurnRotation();
        UpdateCarPosition();
    }
    
    private void FixedUpdate()
    {
        UpdateMaxVelocity();
        UpdateIsGrounded();
        UpdateSlope();
        UpdateDrag();
        UpdatePhysicsModel();
    }
    
    private void UpdateInputs()
    {
        switch (_inputMode)
        {
            case InputMode.Keyboard:
                _verticalInput = Input.GetAxis("Vertical");
                _horizontalInput = Input.GetAxis("Horizontal");
                break;
            case InputMode.Controller:
                // The forward has priority over the reverse.
                _verticalInput = Input.GetButton("Fire2") ? 1 : 0;
                if (_verticalInput == 0) 
                    _verticalInput = Input.GetButton("Fire1") ? -1 : 0;
                _horizontalInput = Input.GetAxis("Horizontal");
                break;
        }
    } 
    
    private void UpdateIncrements()
    {
        // Clears the forward/backwards increment and then, updates it.
        _verticalIncrement = 0;
        if (_verticalInput > 0) _verticalIncrement = _verticalInput * _forwardAcceleration * _accelerationMultiplier;
        else if (_verticalInput < 0) _verticalIncrement = _verticalInput * _reverseAcceleration * _accelerationMultiplier;
        
        // Updates the turning by adding on the amount of turning to be done.
        _turnIncrement = _horizontalInput * _turnStrength * Time.deltaTime;
        // The car should not turn when stopped, only when moving, to do so, the _turnIncrement is multiplied
        // by the _verticalInput, when the car is stopped, this value is 0, and therefore the turn increment will be zero.
        // When moving backwards the rotation keys/axis will be inverted, just like in a real car.
        _turnIncrement *= _verticalInput;
    } 
    
    private void UpdateCarPosition()
    {
	    transform.position = _physicsModel.transform.position;
    }
    
    private void UpdateTurnRotation()
    {
        // Can only Rotate when grounded.
        if (!_isGrounded)
            return;
        
        // Updates the turning by adding on the amount of turning to be done.
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, _turnIncrement, 0));
    }
    
    private void UpdateMaxVelocity()
    {
        // Tolerance checking
        if (Math.Abs(_physicsModel.maxLinearVelocity - _maxSpeed) > 0.0001f)
            _physicsModel.maxLinearVelocity = _maxSpeed;
    }
    
    private void UpdateIsGrounded()
    {
        _isGrounded = false;
        // it needs to be in a direction relative to the car, and there is no transform.down, that's why the - transform.up.
        if (Physics.Raycast(_groundCheckerRayStartPoint.position, - transform.up , out _groundRayHit, _groundCheckerRayLength, _groundLayers))
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
        _physicsModel.drag = _isGrounded ? _dragWhenOnGround : _dragWhenOnAir;
    }

    private void UpdatePhysicsModel()
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
    
}
