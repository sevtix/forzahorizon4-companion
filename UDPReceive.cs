using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.UI;

public class UDPReceive : MonoBehaviour
{
    [Header("Panels")]

    public GameObject helpPanel;
    public GameObject noConnectionPanel;

    [Space]
    [Header("Image views")]

    public Image rpmNeedle;
    public Transform g_blip;

    [Space]
    [Header("Dials")]

    public Image limiterView;
    public Image dialView;
    public Image torqueView;
    public Image powerView;

    [Space]
    [Header("Text views")]

    public Text speedView;
    public Text gearView;
    public Text ipView;

    public Text x_acc_view;
    public Text z_acc_view;

    public Text torqueView_percent;
    public Text powerView_percent;

    [Space]
    [Header("Settings")]
    public InputField portInput;

    int port = 4096;

    float reference_torque = 0;
    float reference_power = 0;


    UdpClient socket;

    byte[] message = new byte[322];

    void OnUdpData(IAsyncResult result)
    {
        UdpClient socket = result.AsyncState as UdpClient;
        IPEndPoint source = new IPEndPoint(0, 0);
        message = socket.EndReceive(result, ref source);
        socket.BeginReceive(new AsyncCallback(OnUdpData), socket);
    }

    void Start()
    {
        Application.targetFrameRate = 60;

        if (!PlayerPrefs.HasKey("app.run"))
        {
            helpPanel.SetActive(true);
            PlayerPrefs.SetInt("app.run", 1);
            PlayerPrefs.Save();
        }

        if (PlayerPrefs.HasKey("udp.port"))
        {
            port = PlayerPrefs.GetInt("udp.port");
        }

        portInput.placeholder.GetComponent<Text>().text = port + "";

        startUDPSocket();

        ipView.text = LocalIPAddress();
    }

    string LocalIPAddress()
    {
        IPHostEntry host;
        string localIP = "0.0.0.0";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }



    void Update()
    {

        byte gamemode = message[0];

        if(gamemode == 0)
        {
            noConnectionPanel.SetActive(true);
        } else
        {
            noConnectionPanel.SetActive(false);
        }

          /*float x = floatConversion(byteCollection(244, 248, message));
            float y = floatConversion(byteCollection(248, 252, message));
            float z = floatConversion(byteCollection(252, 256, message));
            */

        // DATA INPUT

        float rpm = floatConversion(byteCollection(16, 20, message));
        float speed = (floatConversion(byteCollection(256, 260, message)) * 3.6f);

        float torque_fl = floatConversion(byteCollection(264, 268, message));
        float power_fl = floatConversion(byteCollection(260, 264, message)) / 1000.0f;

        float max_rpm = floatConversion(byteCollection(8, 12, message));
        float max_rpm_dial = (float) Math.Ceiling(max_rpm / 1000.0f) * 1000;

        byte gear = message[319];

        int speed_int = Mathf.RoundToInt(speed);

        // GEAR

        string gear_str = "";
        if (gear > 0){
            gear_str = gear.ToString();
        }else{
            gear_str = "R";
        }

        // G SENSOR

        float side_acceleration = floatConversion(byteCollection(32, 36, message));
        float forward_acceleration = floatConversion(byteCollection(28, 32, message));
        x_acc_view.text = side_acceleration + "";
        z_acc_view.text = forward_acceleration + "";
        g_blip.position = new Vector3(side_acceleration, forward_acceleration, 0);

        // CHECK IF < 0

        if (torque_fl < 0)
        {
            torque_fl = 0;
        }

        if (power_fl < 0)
        {
            power_fl = 0;
        }

        // CHECK IF REFERENCE

        if (torque_fl > reference_torque)
        {
            reference_torque = torque_fl;
        }

        if (power_fl > reference_power)
        {
            reference_power = power_fl;
        }

        // APPLY

        torqueView.fillAmount = torque_fl / reference_torque;
        powerView.fillAmount = power_fl / reference_power;

        torqueView_percent.text = Mathf.RoundToInt((torque_fl / reference_torque) * 100.0f) +"";
        powerView_percent.text = Mathf.RoundToInt((power_fl / reference_power) * 100.0f) + "";

        gearView.text = gear_str;
        speedView.text = speed_int.ToString();

        // NEEDLE

        rpmNeedle.transform.eulerAngles = new Vector3(0, 0, ((360 / max_rpm_dial) * rpm) * -1.0f);

        // DIAL & LIMITER

        limiterView.fillAmount = ((max_rpm_dial / max_rpm) - 1.0f) * 2.5f;
        dialView.fillAmount = 1.0f - (((max_rpm_dial / max_rpm) - 1.0f) * 2.5f);
    }

    public void changePort()
    {
        try
        {
            if(!string.IsNullOrEmpty(portInput.text))
            {
                port = Int32.Parse(portInput.text);
                PlayerPrefs.SetInt("udp.port", port);
                PlayerPrefs.Save();
                Debug.Log("changed port to: " + port);
                stopUDPSocket();
                startUDPSocket();
            }
        } catch (Exception ex)
        {
        }
    }

    void startUDPSocket()
    {
        socket = new UdpClient(port);
        socket.BeginReceive(new AsyncCallback(OnUdpData), socket);
    }

    void stopUDPSocket()
    {
        socket.Close();
    }

    public void clearReferences()
    {
        reference_power = 0;
        reference_torque = 0;
    }

    byte[] byteCollection(int from, int to, byte[] source)
    {

        List<byte> arrayList = new List<byte>();


        for (int i = from; i < to; i++)
        {
            arrayList.Add(source[i]);
        }

        return (byte[]) arrayList.ToArray();
    }

    float floatConversion(byte[] bytes)
    {
        //Array.Reverse(bytes);
        float myFloat = BitConverter.ToSingle(bytes, 0);
        return myFloat;
    }
}