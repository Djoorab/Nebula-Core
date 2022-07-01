using System;
using UnityEngine;
using System.IO.Ports;
using System.Threading;

//Class allowing to use serial communication while using Unity Editor or Unity built project

public static class MAO_OtherPlatformInitializer
{
    //Initialize the serial port.
    public static SerialPort serial;
    public static int baudRate = 115200;
    //By default the correct port is defined automaticaly by using a single handshake (FindPort method)
    //You can manually providethe correct port using the bool and the string bellow 
    public static bool defineManuallyCOMPort = false;
    public static string MAOPort = "COM8";

    //Thread used to read and print on the console everything trhat your MAO is writing on it
    public static Thread thread;

    public static bool InitUSBSerial()
    {
        if (!defineManuallyCOMPort) MAOPort = FindPort("MAOConnect");
        serial = new SerialPort(MAOPort, baudRate);
        serial.Parity = Parity.None;
        serial.StopBits = StopBits.One;
        serial.DataBits = 8;
        serial.DtrEnable = true;
        serial.Open();
        Debug.Log("Serial port opened");
        thread = new Thread (ThreadLoop);
        thread.Start();
        return true;
    }

    //Method looking for the MAO, given the handshake necessary => here "MAOConnect"
    private static string FindPort(string handShake)
    {
        string[] portList = SerialPort.GetPortNames();
        foreach (string port in portList)
        {
            if (port != "COM1")
            {
                try
                {
                    SerialPort currentPort = new SerialPort(port, baudRate);
                    currentPort.Parity = Parity.None;
                    currentPort.StopBits = StopBits.One;
                    currentPort.DataBits = 8;
                    currentPort.DtrEnable = true;
                    if (!currentPort.IsOpen)
                    {
                        currentPort.Open();
                        currentPort.WriteLine(handShake);
                        string received = currentPort.ReadLine();
                        currentPort.Close();
                        if (received.Equals(handShake))
                        {
                            Debug.Log("MAO Found on " + port);
                            return port;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            }

        }
        return null;
    }

    public static void ThreadLoop()
    {
        while (true)
        {

            if (serial.BytesToRead > 0)
            {
                string data = serial.ReadTo("\n"); //gathering working return from MAO
                Debug.Log(data);
            }

        }
    }

}
