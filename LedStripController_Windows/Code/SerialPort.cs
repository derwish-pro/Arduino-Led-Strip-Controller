using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Text.Core;

namespace LedStripController_Windows.Code
{
    public delegate void ReceivedDataEventHandler(string message);

    public class SerialPort
    {
        const int INCOMING_DATA_SIZE = 2014;

        private SerialDevice serialPort;
        private DeviceInformationCollection devices;
        DataReader dataReaderObject;
        private bool isConnected;

        public event ReceivedDataEventHandler ReceivedDataEvent;


        public async void FindAllDevices()
        {

            string selector = SerialDevice.GetDeviceSelector();

            devices = await DeviceInformation.FindAllAsync(selector);

        }


        public List<string> GetSerialDevicesList()
        {

            FindAllDevices();

            List<string> devicesList = new List<string>();

            if (devices!=null)
            foreach (var device in devices)
                devicesList.Add(device.Name);

            return devicesList;
        }


        public async void Connect(int deviceIndex)
        {
            DeviceInformation device = devices[deviceIndex];

            serialPort = await SerialDevice.FromIdAsync(device.Id);

            if (App.serialPort == null)
                throw new Exception("Serial connecting failed.");

            serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
            serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
            serialPort.BaudRate = 9600;
            serialPort.Parity = SerialParity.None;
            serialPort.StopBits = SerialStopBitCount.One;
            serialPort.DataBits = 8;

            isConnected = true;

            StartReading();
        }

        public void Disconnect()
        {
            isConnected = false;
            StopReading();
        }

        public async void WriteAsync(string message)
        {
            if (!isConnected) return;

            Debug.WriteLine("Send: " + message);

            DataWriter dataWriteObject = new DataWriter(serialPort.OutputStream);

            dataWriteObject.WriteString(message);

            await dataWriteObject.StoreAsync();

            dataWriteObject.DetachStream();
        }

        private async void StartReading()
        {
            dataReaderObject = new DataReader(serialPort.InputStream);

            while (isConnected)
                await ReadAsync();

        }

        private void StopReading()
        {
            dataReaderObject.DetachStream();
        }

        private async Task ReadAsync()
        {
            //if (dataReaderObject == null) return;

            uint bytesRead = await dataReaderObject.LoadAsync(INCOMING_DATA_SIZE);

            string receivedText = dataReaderObject.ReadString(bytesRead);
            string[] messages = Regex.Split(receivedText, "\r\n");

            foreach (var message in messages)
            {
                Debug.WriteLine("Recieved: " + message);

                if (ReceivedDataEvent != null)
                    ReceivedDataEvent(message);
            }
        }
    }
}
