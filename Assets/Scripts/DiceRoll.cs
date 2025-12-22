using UnityEngine;
using TMPro;

public class DiceRoll : MonoBehaviour
{
    public TextMeshProUGUI wheelSpinText;
    public int wheelValue;
    public int wheelSpun = 0;
    public void SpinTheWheel()
    {

        
        Debug.Log(wheelSpun);
        wheelValue = Random.Range(1, 7);
        wheelValue = 3;     //debug
        Debug.Log("Rolled: " + wheelValue);
        wheelSpinText.text = (wheelValue.ToString());
        wheelSpun++;
        
    }

    
}
