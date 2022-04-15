using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;
using M2MqttUnity;
[Serializable]
public class DataControl{
    public string devices;
    public string status;
    public DataControl(string name, string is_ON){
        this.devices = name;
        this.status = is_ON;
    }
}
[Serializable]
public class Data_ss{
    public string ss_name;
    public string ss_unit;
    public string ss_value;
}

[Serializable]
public class DataCollection{
    public string project_id;
    public string project_name;
    public string station_id;
    public string station_name;
    public string longitude;
    public string latitude;
    public string volt_battery;
    public string volt_solar;
    public List<Data_ss> data_ss;
    public string device_status;

}
public class mqttUnityClient : M2MqttUnityClient
{
    // Start is called before the first frame update
    public Text errorMessage;
    public Text Temperature;
    public Text humidity;
    
    public Toggle led;
    public Toggle pump;

    public InputField addressInputField;
    public InputField usernameInputField;
    public InputField passwordInputField;
    public Button ConnectButton;

    public List<string> Topics = new List<string>();

    private List<string> eventMessage = new List<string>();
    private bool updateUI = false;

    public void PublishTopics(String topic, String msg){
        client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(msg),MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
        AddUiMessage("Test messagge published");
    }

    public void OnLedChange(){
        led.interactable = false;
        string msg = JsonUtility.ToJson(new DataControl("LED", this.led.isOn ? "ON" : "OFF"));
        PublishTopics(Topics[1], msg);
    }

    public void OnPumpChange(){
        pump.interactable = false;
        string msg = JsonUtility.ToJson(new DataControl("PUMP", this.pump.isOn ? "ON" : "OFF"));
        PublishTopics(Topics[2], msg);
    }
    public void SetBrokerAddress(string brokerAddress)
    {
        if (addressInputField && !updateUI)
        {
            this.brokerAddress = brokerAddress;
        }
    }
    public void SetUsername(string username)
    {
        if (usernameInputField && !updateUI)
        {
            this.mqttUserName = username;
        }
    }
    public void SetPassword(string password)
    {
        if (passwordInputField && !updateUI)
        {
            this.mqttPassword = password;
        }
    }
    public void SetUiMessage(string msg)
    {
        if (errorMessage != null)
        {
            errorMessage.text = msg;
            updateUI = true;
        }
    }

    public void AddUiMessage(string msg)
    {
        if (errorMessage != null)
        {
            errorMessage.text += msg + "\n";
            updateUI = true;
        }
    }
    protected override void OnConnecting()
    {
        base.OnConnecting();
        SetUiMessage("Connecting to broker on " + brokerAddress + ":" + brokerPort.ToString() + "...\n");
    }
    protected override void SubscribeTopics(){
        client.Subscribe(new string[] {Topics[0]}, new byte[]{MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE});
    }
    protected override void OnConnectionFailed(string errorMessage)
    {
        AddUiMessage("CONNECTION FAILED! " + errorMessage);
    }
    protected override void OnDisconnected()
    {
        AddUiMessage("Disconnected.");
    }
    protected override void OnConnectionLost()
    {
        AddUiMessage("CONNECTION LOST!");
    }
    protected override void OnConnected()
    {
        base.OnConnected();
        SetUiMessage("Connected to broker on " + brokerAddress + "\n");
        SubscribeTopics();

        SwitchScene();
    }


    private void UpdateUI()
    {
        if (client == null)
        {
            if (ConnectButton != null)
            {
                ConnectButton.interactable = true;
            }
        }
        else
        {
            if (ConnectButton != null)
            {
                ConnectButton.interactable = !client.IsConnected;
            }
        }
        if (addressInputField != null && ConnectButton != null)
        {
            addressInputField.interactable = ConnectButton.interactable;
            addressInputField.text = brokerAddress;
        }
        if (usernameInputField != null && ConnectButton != null)
        {
            usernameInputField.interactable = ConnectButton.interactable;
            usernameInputField.text = mqttUserName;
        }
        if (passwordInputField != null && ConnectButton != null)
        {
            passwordInputField.interactable = ConnectButton.interactable;
            passwordInputField.text = mqttPassword;
        }
        updateUI = false;
    }

    protected override void Awake()
    {
        DontDestroyOnLoad(gameObject);
        base.Awake();
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + scene.name);
        Debug.Log(mode);
    }

    protected override void Start()
    {
        SetUiMessage("Ready.");
        updateUI = true;
        base.Start();
    }

    public void connectBtnOnClickStart()
    {
        autoConnect = true;
        SetBrokerAddress(this.addressInputField.text);
        SetUsername(this.usernameInputField.text);
        SetPassword(this.passwordInputField.text);
        base.Start();
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string msg = System.Text.Encoding.UTF8.GetString(message);
        Debug.Log("Received: " + msg);
        if ( String.Equals(Topics[0], topic)){
            StoreMessage(msg);
        }
    }

    private void StoreMessage(string eventMsg)
    {
        eventMessage.Add(eventMsg);
    }

    private void ProcessMessage(string msg)
    {
        AddUiMessage("Received: " + msg);
        DataCollection _status_data = JsonUtility.FromJson<DataCollection>(msg);
        List<float> data = new List<float>(new float[]{0.0f, 0.0f});
        foreach(Data_ss _data in _status_data.data_ss)
        {
            switch (_data.ss_name)
            {
                case "temperature": 
                    if (this.Temperature != null)
                    {
                        float value = float.Parse(_data.ss_value);
                        this.Temperature.text = ((int) value).ToString() + "°C";
                        data[0] = value;
                    }
                    break;
                case "humidity": 
                    if (this.humidity != null)
                    {
                        float value = float.Parse(_data.ss_value);
                        this.humidity.text = ((int) value).ToString() + "%";
                        data[1] = value;
                    }
                    break;
                case "led_status":
                    if (_status_data.device_status == "1" && !led.interactable)
                    {
                        bool isTrue = _data.ss_value == "ON" ^ led.isOn;
                        // if not the same (true) publish again
                        if (!isTrue)
                            led.interactable = true;
                        else
                            OnLedChange();
                    }
                    break;
                case "pump_status":
                    if (_status_data.device_status == "1" && !pump.interactable)
                    {
                        bool isTrue = _data.ss_value  == "ON" ^ pump.isOn;
                        // if not the same (true) publish again
                        if (!isTrue)
                            pump.interactable = true;
                        else
                            OnPumpChange();
                    }
                    break;
            }
        }
        // graphController.UpdateData(data);
    }

    protected override void Update()
    {
        base.Update(); // call ProcessMqttEvents()

        if (eventMessage.Count > 0)
        {
            foreach (string msg in eventMessage)
            {
                ProcessMessage(msg);
            }
            eventMessage.Clear();
        }
        if (updateUI)
        {
            UpdateUI();
        }
        if (updateScene == true)
        {
            SwitchOut(Scene_1);
            SwitchIn(Scene_2);
        }
        else if (updateScene == false)
        {
            SwitchOut(Scene_2);
            SwitchIn(Scene_1);
        }
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    // train canvas
    public CanvasGroup Scene_1 , Scene_2;
    public int offsetPosX, offsetPosY;
    public float speed = 1.0f;
    private float startTime;
    private bool updateScene = false;

    void SwitchIn(CanvasGroup _canvas){
        Vector3 targetPos = new Vector3(0, offsetPosY, 0);
        float distCovered = (Time.time - startTime)*speed;
        float fractionOfJourney = distCovered / offsetPosX;
        _canvas.transform.localPosition = Vector3.Lerp(_canvas.transform.localPosition, targetPos, fractionOfJourney);
    }
    void SwitchOut(CanvasGroup _canvas)
    {
        Vector3 targetPos;
        if (string.Equals(_canvas.name, "Scene1"))
            targetPos = new Vector3(-offsetPosX, offsetPosY, 0);
        else
            targetPos = new Vector3(offsetPosX, offsetPosY, 0);
        float distCovered = (Time.time - startTime) * speed;
        float fractionOfJourney = distCovered / offsetPosX;
        _canvas.transform.localPosition = Vector3.Lerp(_canvas.transform.localPosition, targetPos, fractionOfJourney);
    }
    void SwitchScene()
    {
        startTime = Time.time;
        updateScene = !updateScene;
        if (Scene_1.interactable == true)
        {
            Scene_1.interactable = false;
            Scene_1.blocksRaycasts = false;
            Scene_2.interactable = true;
            Scene_2.blocksRaycasts = true;
        }
        else
        {
            Scene_2.interactable = false;
            Scene_2.blocksRaycasts = false;
            Scene_1.interactable = true;
            Scene_1.blocksRaycasts = true;
        }
    }
}
