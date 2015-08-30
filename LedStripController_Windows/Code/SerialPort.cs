﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Text.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace LedStripController_Windows.Code
{
    public delegate void ReceivedDataEventHandler(string message);

    public class SerialPort
    {

        private bool isConnected;

        public event ReceivedDataEventHandler ReceivedDataEvent;
        public event EventHandler OnConnectedEvent;
        public event EventHandler OnDisconnectedEvent;

        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;

        private ObservableCollection<DeviceInformation> listOfDevices;
        private CancellationTokenSource ReadCancellationTokenSource;



  


        public SerialPort()
        {
            listOfDevices = new ObservableCollection<DeviceInformation>();
            FindDevices();
        }

        public bool IsConnected()
        {
            return isConnected;
        }


        public async Task<List<string>> GetDevicesList()
        {

            await FindDevices();

            List<string> devicesList = new List<string>();

            if (listOfDevices != null)
                foreach (var device in listOfDevices)
                    devicesList.Add(device.Name);

            return devicesList;
        }


        public async Task FindDevices()
        {
            try
            {
                string selector = SerialDevice.GetDeviceSelector();
                var devices = await DeviceInformation.FindAllAsync(selector);

                listOfDevices.Clear();

                for (int i = 0; i < devices.Count; i++)
                {
                    listOfDevices.Add(devices[i]);
                }
            }
            catch (Exception ex)
            {

            }
        }


        public async Task Connect(int deviceIndex)
        {

            DeviceInformation entry = listOfDevices[deviceIndex];

            try
            {
                serialPort = await SerialDevice.FromIdAsync(entry.Id);


                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;

                isConnected = true;

                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();
                StartReading();

                if (OnConnectedEvent != null) OnConnectedEvent(this,null);
            }
            catch (Exception ex)
            {

            }
        }


        public async void SendMessage(string message)
        {
            try
            {
                if (serialPort != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    dataWriteObject = new DataWriter(serialPort.OutputStream);

                    //Launch the WriteAsync task to perform the write
                    await WriteAsync(message);
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }


        private async Task WriteAsync(string message)
        {
            Debug.WriteLine("Writing to serial: "+ message);

            Task<UInt32> storeAsyncTask;

            if (!string.IsNullOrEmpty(message))
            {
                // Load the text from the sendText input text box to the dataWriter object
                dataWriteObject.WriteString(message);

                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
            }
        }


        private async void StartReading()
        {
            try
            {
                while (isConnected)
                {
                    if (serialPort != null)
                    {
                        dataReaderObject = new DataReader(serialPort.InputStream);
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    CloseDevice();
                }
                else
                {

                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }


        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                SendDataRecievedEvents(dataReaderObject.ReadString(bytesRead));
            }
           

        }

        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }


        private void CloseDevice()
        {
            isConnected = false;
            if (OnDisconnectedEvent != null) OnDisconnectedEvent(this, null);

            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;
        }


        public void Disconnect()
        {
            try
            {
                CancelReadTask();
                CloseDevice();
            }
            catch (Exception ex)
            {

            }
        }


        private void SendDataRecievedEvents(string receivedText)
        {

            string[] messages = Regex.Split(receivedText, "\r\n");

            foreach (var message in messages)
            {
                Debug.WriteLine("Readed from serial: " + message);


                if (ReceivedDataEvent != null)
                    ReceivedDataEvent(message);
            }
        }

    }
}
