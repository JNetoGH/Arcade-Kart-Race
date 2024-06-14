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
    [SerializeField] private float _maxSpeed = 25f;
   
    [Header("Turning")]
    [SerializeField, Tooltip(TipTf)] private float _turnStrength = 150;
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
    
    [Header("Wheels")]
    [SerializeField, Tooltip(TipWheels)] private Transform _leftFrontWheel;
    [SerializeField, Tooltip(TipWheels)] private Transform _rightFrontWheel;
    [SerializeField, Tooltip(TipWheels)] private float _maxWheelsTurnAngle = 40;
    [SerializeField, Tooltip(TipWheels)] private float _wheelsTurnAngleChangingSpeed = 250;
    private const string TipWheels = "Just for the aethetics";

    [Header("Dust")] 
    [SerializeField, Tooltip(TipDust)] private ParticleSystem[] _dustTrail;
    [SerializeField, Tooltip(TipDust)] private float _dustMaxEmissionAmount = 25;
    [ReadOnly, SerializeField] private float _dustEmissionRate;
    private const string TipDust = "Just for the aethetics, stops then not grounded";

    [Header("Debugging")] 
    [ReadOnly, SerializeField] private float _currentSpeed;
    [ReadOnly, SerializeField] private float _verticalInput;
    [ReadOnly, SerializeField] private float _verticalIncrement;
    [ReadOnly, SerializeField] private float _horizontalInput;
    [ReadOnly, SerializeField] private float _turnIncrement;
    
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
        UpdateWheelsTurnRotation();
        UpdateCarPosition();
    }
    
    private void FixedUpdate()
    {
        UpdateMaxVelocity();
        UpdateIsGrounded();
        UpdateSlope();
        UpdateDrag();
        UpdateDustEmission();
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
    
    private void UpdateWheelsTurnRotation()
    {
        // Uses the _horizontalInput * _maxWheelsTurn to get a new target rotation in the Y-axis for the wheel.
        // Then, goes gradually rotating the wheel in the Y axis towards the new rotation.
        Vector3 rightWheelRot = _rightFrontWheel.localRotation.eulerAngles;
        Quaternion rightWheelTargetRot =  Quaternion.Euler(rightWheelRot.x, (_horizontalInput * _maxWheelsTurnAngle), rightWheelRot.z);
        _rightFrontWheel.localRotation = Quaternion.RotateTowards(
            _rightFrontWheel.localRotation, 
            rightWheelTargetRot, 
            _wheelsTurnAngleChangingSpeed * Time.deltaTime);
        
        // The left wheel is already rotated in 180 degrees, it's just flipped, so, needs to be inverted by subtracting 180. 
        Vector3 leftWheelRot = _leftFrontWheel.localRotation.eulerAngles;
        Quaternion leftWheelTargetRot = Quaternion.Euler(leftWheelRot.x, (_horizontalInput * _maxWheelsTurnAngle) - 180, leftWheelRot.z);
        _leftFrontWheel.localRotation = Quaternion.RotateTowards(
            _leftFrontWheel.localRotation, 
            leftWheelTargetRot, 
            _wheelsTurnAngleChangingSpeed * Time.deltaTime);
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
        // - This method will make the transform.Up tho be the same as the normal of the ray hit.
        // - Which is the angle what whatever the physics model is hitting against.
        // - Multiplying by the current rotation, will allow it to be affected by the current Turn rotation (y-axis),
        //   otherwise the car would always override the turn rotation and look only forward.
        if (_isGrounded)
            transform.rotation = Quaternion.FromToRotation(transform.up, _groundRayHit.normal) * transform.rotation;
    }
    
    private void UpdateDrag()
    {
        _physicsModel.drag = _isGrounded ? _dragWhenOnGround : _dragWhenOnAir;
    }
    
    private void UpdateDustEmission()
    {
        _dustEmissionRate = 0;
        // If the car is grounded and is moving, starts emitting the dust particles.
        if (_isGrounded && _verticalInput != 0)
            _dustEmissionRate = _dustMaxEmissionAmount;
        foreach (ParticleSystem particle in _dustTrail)
        {
            // The particles. can't be changed directly, requires this workaround.
            ParticleSystem.EmissionModule module = particle.emission;
            module.rateOverTime = _dustEmissionRate;
        }
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

        _currentSpeed = _physicsModel.velocity.magnitude;
    }
    
}
