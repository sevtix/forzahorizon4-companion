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
    public GameObject noConnectionPanel;

    public Image rpmNeedle;

    public Image limiterView;
    public Image dialView;

    public Text speedView;
    public Text gearView;
    public Text ipView;

    public Text torqueView;
    public Text powerView;

    public InputField portInput;

    bool isGameMode = false;

    int port = 4096;

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

        if(PlayerPrefs.HasKey("udp.port"))
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

        float rpm = floatConversion(byteCollection(16, 20, message));
        float speed = (floatConversion(byteCollection(256, 260, message)) * 3.6f);

        float torque_fl = floatConversion(byteCollection(264, 268, message));
        float power_fl = floatConversion(byteCollection(260, 264, message)) / 1000.0f;

        float max_rpm = floatConversion(byteCollection(8, 12, message));
        float max_rpm_dial = (float) Math.Ceiling(max_rpm / 1000.0f) * 1000;

        byte gear = message[319];

        int torque = Mathf.RoundToInt(torque_fl);
        int power = Mathf.RoundToInt(power_fl);

        if(power < 0)
        {
            power = 0;
        }

        if (torque < 0)
        {
            torque = 0;
        }

        int speed_int = Mathf.RoundToInt(speed);

        rpmNeedle.transform.eulerAngles = new Vector3(0, 0, ((360 / max_rpm_dial) * rpm) * -1.0f);

        limiterView.fillAmount = ((max_rpm_dial / max_rpm) - 1.0f) * 2.5f;
        dialView.fillAmount = 1.0f - (((max_rpm_dial / max_rpm) - 1.0f) * 2.5f);


        string gear_str = "";

        if (gear > 0)
        {
            gear_str = gear.ToString();
        } else
        {
            gear_str = "R";
        }

        gearView.text = gear_str;
        speedView.text = speed_int.ToString();
        torqueView.text = torque + " Nm";
        powerView.text = power + " kW";

        /*
         
        speed = 512, 520
        max_rpm = 16, 24
        rpm = 32, 40
         
         */
    }

    public void changePort()
    {
        try
        {
            if(!string.IsNullOrEmpty(portInput.text))
            {
                port = Int32.Parse(portInput.text);
                PlayerPrefs.SetInt("udp.port", port);
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