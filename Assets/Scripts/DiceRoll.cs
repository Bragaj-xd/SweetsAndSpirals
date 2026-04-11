using UnityEngine;
using TMPro;
using System;

public class DiceRoll : MonoBehaviour
{
    public TextMeshProUGUI wheelSpinText;
    public int wheelValue;
    public int cardWheelValue;
    public int wheelSpun = 0;
    public Camera mainCamera;

    void Start()
    {
        //wheelSpinAnimation = GetComponent<Animation>();
    }
    public void SpinTheWheel()
    {
        Debug.Log(wheelSpun);
        //wheelSpinAnimation.Play();
        wheelValue = UnityEngine.Random.Range(1, 7);
        //wheelValue = 3;     //debug
        Debug.Log("Rolled: " + wheelValue);
        wheelSpinText.text = wheelValue.ToString();
        wheelSpun++;
        
    }

    public void SpinTheWheelForCards()
    {
        Debug.Log(wheelSpun);
        //wheelSpinAnimation.Play();
        cardWheelValue = UnityEngine.Random.Range(1, 7);
        //cardWheelValue = 3;     //debug
        Debug.Log("Rolled for cards: " + cardWheelValue);
        wheelSpinText.text = cardWheelValue.ToString();
        
    }

}
