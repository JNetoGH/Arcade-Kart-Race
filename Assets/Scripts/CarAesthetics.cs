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
    [SerializeField] private Transform _leftFrontWheel;
    [SerializeField] private Transform _rightFrontWheel;
    [SerializeField] private float _maxWheelsTurnAngle = 40;
    [SerializeField] private float _wheelsTurnAngleChangingSpeed = 250;
    
    [Header("Dust")] 
    [SerializeField] private ParticleSystem[] _dustTrail;
    [SerializeField] private float _dustMaxEmissionAmount = 25;
    [ReadOnly, SerializeField] private float _dustEmissionRate;
  
    private void Update()
    {
        UpdateWheelsTurnRotation();
        // UpdateWheelsSpinRotation();
    }

    private void FixedUpdate()
    {
        UpdateDustEmission();
    }
    
    private void UpdateWheelsTurnRotation()
    {
        // Uses the _horizontalInput * _maxWheelsTurn to get a new target rotation in the Y-axis for the wheel.
        // Then, goes gradually rotating the wheel in the Y axis towards the new rotation.
        Vector3 rightWheelRot = _rightFrontWheel.localRotation.eulerAngles;
        Quaternion rightWheelTargetRot =  Quaternion.Euler(rightWheelRot.x, (_carController.HorizontalInput * _maxWheelsTurnAngle), rightWheelRot.z);
        _rightFrontWheel.localRotation = Quaternion.RotateTowards(
            _rightFrontWheel.localRotation, 
            rightWheelTargetRot, 
            _wheelsTurnAngleChangingSpeed * Time.deltaTime);
        
        // The left wheel is already rotated in 180 degrees, it's just flipped, so, needs to be inverted by subtracting 180. 
        Vector3 leftWheelRot = _leftFrontWheel.localRotation.eulerAngles;
        Quaternion leftWheelTargetRot = Quaternion.Euler(leftWheelRot.x, (_carController.HorizontalInput * _maxWheelsTurnAngle) - 180, leftWheelRot.z);
        _leftFrontWheel.localRotation = Quaternion.RotateTowards(
            _leftFrontWheel.localRotation, 
            leftWheelTargetRot, 
            _wheelsTurnAngleChangingSpeed * Time.deltaTime);
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
