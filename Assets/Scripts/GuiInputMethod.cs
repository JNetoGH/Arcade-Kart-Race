using TMPro;
using UnityEngine;


public class GuiInputMethod : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _dropdown;

    private void Start()
    {
        _dropdown.value = (int)FindObjectOfType<CarController>().InputModeProp;
    }

    public void SetInputMethod()
    {
        FindObjectOfType<CarController>().InputModeProp = (CarController.InputMode)_dropdown.value;
    }
}
