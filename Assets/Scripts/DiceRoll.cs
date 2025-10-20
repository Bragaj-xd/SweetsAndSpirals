using UnityEngine;
using TMPro;

public class DiceRoll : MonoBehaviour
{
    public TextMeshProUGUI wheelSpinText;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int WheelValue;

    public void SpinTheWheel() 
    {
        WheelValue = Random.Range( 0, 8);
        Debug.Log("Rolled: " + WheelValue);
        wheelSpinText.text = (WheelValue.ToString());
    }

    
}
