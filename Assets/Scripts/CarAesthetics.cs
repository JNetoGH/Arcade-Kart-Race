using NaughtyAttributes;
using UnityEngine;


/// <summary>
/// Handles the car model components, like wheels and dust, these are only aesthetic and don't interfere with the motion.
/// </summary>
public class CarAesthetics : MonoBehaviour
{
    
    [Header("General")] 
    [SerializeField] private CarController _carController;
    
    [Header("Wheels Turn")]
    [SerializeField] private Transform _wrapperFrontLeftWheel;
    [SerializeField] private Transform _wrapperFrontRightWheel;
    [SerializeField] private float _maxWheelsTurnAngle = 40;
    [SerializeField] private float _wheelsTurnAngleChangingSpeed = 250;
    
    [Header("Wheels Spin")]
    [SerializeField] private Transform _frontLeftWheel;
    [SerializeField] private Transform _frontRightWheel;
    [SerializeField] private Transform _backLeftWheel;
    [SerializeField] private Transform _backRightWheel;
    [SerializeField] private float _wheelSpinSpeed = 1500;
    
    [Header("Dust")] 
    [SerializeField] private ParticleSystem[] _dustTrail;
    [SerializeField] private float _dustMaxEmissionAmount = 25;
    [ReadOnly, SerializeField] private float _dustEmissionRate;
  
    private void Update()
    {
        UpdateWheelsTurnRotation();
        UpdateWheelsSprinRotation();
    }
    
    private void FixedUpdate()
    {
        UpdateDustEmission();
    }
    
    private void UpdateWheelsTurnRotation()
    {
        // It's made on the Wrappers parents, because it would get all wrecked if made together with the spin.
        
        // Uses the _horizontalInput * _maxWheelsTurn to get a new target rotation in the Y-axis for the wheel.
        // Then, goes gradually rotating the wheel in the Y axis towards the new rotation.
        Vector3 rightWheelRot = _wrapperFrontRightWheel.localRotation.eulerAngles;
        Quaternion rightWheelTargetRot =  Quaternion.Euler(rightWheelRot.x, (_carController.HorizontalInput * _maxWheelsTurnAngle), rightWheelRot.z);
        _wrapperFrontRightWheel.localRotation = Quaternion.RotateTowards(
            _wrapperFrontRightWheel.localRotation, 
            rightWheelTargetRot, 
            _wheelsTurnAngleChangingSpeed * Time.deltaTime);
        
        // The left wheel is already rotated in 180 degrees, it's just flipped, so, needs to be inverted by subtracting 180. 
        Vector3 leftWheelRot = _wrapperFrontLeftWheel.localRotation.eulerAngles;
        Quaternion leftWheelTargetRot = Quaternion.Euler(leftWheelRot.x, (_carController.HorizontalInput * _maxWheelsTurnAngle) - 180, leftWheelRot.z);
        _wrapperFrontLeftWheel.localRotation = Quaternion.RotateTowards(
            _wrapperFrontLeftWheel.localRotation, 
            leftWheelTargetRot, 
            _wheelsTurnAngleChangingSpeed * Time.deltaTime);
    }
    
    private void UpdateWheelsSprinRotation()
    {
        if (_carController.VerticalInput == 0) 
            return;
        
        // The left one is, rotated in Y 180 degrees.
        _frontLeftWheel.Rotate(- _wheelSpinSpeed * Time.deltaTime, 0 ,0);
        _frontRightWheel.Rotate(_wheelSpinSpeed * Time.deltaTime, 0 ,0);
        
        // Can only spin the back wheels when grounded, because the engine is at the front.
        if (!_carController.IsGrounded)
            return;
            
        // The left one is, rotated in Y 180 degrees.
        _backLeftWheel.Rotate(- _wheelSpinSpeed * Time.deltaTime, 0 ,0);
        _backRightWheel.Rotate(_wheelSpinSpeed * Time.deltaTime, 0 ,0);
    }
    
    private void UpdateDustEmission()
    {
        // Resets the value.
        _dustEmissionRate = 0;
        
        // If the car is grounded and is moving, starts emitting the dust particles.
        if (_carController.IsGrounded && _carController.VerticalInput != 0)
            _dustEmissionRate = _dustMaxEmissionAmount;
        
        foreach (ParticleSystem particle in _dustTrail)
        {
            // The particles. can't be changed directly, requires this workaround.
            ParticleSystem.EmissionModule module = particle.emission;
            module.rateOverTime = _dustEmissionRate;
        }
    }
    
}
