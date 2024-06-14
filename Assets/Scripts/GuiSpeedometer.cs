using System.Globalization;
using TMPro;
using UnityEngine;

public class GuiSpeedometer : MonoBehaviour
{
    
    [SerializeField] private CarController _carController;
    [SerializeField] private TextMeshProUGUI _speedText;
    [SerializeField] private float _meterPerSecToBugsPerHourConversion = 4f;
    
    private void Update()
    {
        // if the conversion rate is 4, 1 bugs has 900 meters.
        _speedText.text = Mathf.Ceil(_carController.CurrentSpeed * _meterPerSecToBugsPerHourConversion).ToString(CultureInfo.InvariantCulture);
    }
    
}
