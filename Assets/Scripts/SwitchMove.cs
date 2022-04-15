using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwitchMove : MonoBehaviour
{
    public Toggle toggle;
    public Image swImage;
    public Vector3 offButtonPosition;
    public Vector3 onButtonPosition;
    public float speed = 1.0F;
    private float startTime;
    private float journeyLength;

    private Vector3 targetPos;
    public Color offColor;
    public Color onColor;
    public Color offBackGroundColor;
    public Color onBackGroundColor;

    // Start is called before the first frame update
    void Start()
    {
        this.offColor.a = 1;
        this.onColor.a = 1;
        this.offBackGroundColor.a = 1;
        this.onBackGroundColor.a = 1;
        journeyLength = Vector3.Distance(offButtonPosition, onButtonPosition);
        onValueChange();
    }

    // Update is called once per frame
    void Update()
    {
        float distCovered = (Time.time - startTime) * speed;
        float fractionOfJourney = distCovered / journeyLength;
        swImage.transform.localPosition = Vector3.Lerp(swImage.transform.localPosition, targetPos, fractionOfJourney);
    }

    public void onValueChange()
    {
        startTime = Time.time;
        Color targetColor;
        Color targetBgColor;
        if (this.toggle.isOn == true)
        {
            targetPos = this.onButtonPosition;
            targetColor = this.onColor;
            targetBgColor = this.onBackGroundColor;
        } else 
        {
            targetPos = this.offButtonPosition;
            targetColor = this.offColor;
            targetBgColor = this.offBackGroundColor;
        }
        // swImage.transform.localPosition = targetPos;
        swImage.color = targetColor;
        this.GetComponentInChildren<Image>().color = targetBgColor;
    }
}
