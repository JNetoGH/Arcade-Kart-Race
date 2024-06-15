using TMPro;
using UnityEngine;


public class GuiInputMethod : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _dropdown;
    [SerializeField] private CarController _carController;
    
    private void Start()
    {
        _dropdown.value = (int)_carController.InputModeProp;
    }

    public void SetInputMethod()
    {
        _carController.InputModeProp = (CarController.InputMode)_dropdown.value;
    }
}
