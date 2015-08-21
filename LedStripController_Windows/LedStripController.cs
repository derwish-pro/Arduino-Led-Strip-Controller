/*
* Led Strip Controller
*
* Copyright (C) 2015 Derwish <derwish.pro@gmail.com>
* License: http://opensource.org/licenses/MIT
*/


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;
using Windows.Storage.Streams;
using Windows.UI.Xaml;

namespace LedStripController_Windows
{
    public delegate void StateRecievedEventHandler(int r, int g, int b,bool isOn);

    public class LedStripController
    {
        DataReader dataReaderObject;

        public bool connected;
        private bool ledIsOn;
        private int r, g, b;
        private DispatcherTimer sendTimer = new DispatcherTimer();
        private DispatcherTimer connectTimer = new DispatcherTimer();
        string sendMessage;

        public event StateRecievedEventHandler stateRecievedEvent;

        public LedStripController()
        {
            StartReading();

            sendTimer.Interval = TimeSpan.FromMilliseconds(100);
            sendTimer.Tick += SendMessage;
            sendTimer.Start();

            connectTimer.Interval = TimeSpan.FromMilliseconds(3000);
            connectTimer.Tick += TryToConnect;
            connectTimer.Start();

            GetState();
        }

        public void TryToConnect(object sender, object e)
        {
            if (!connected)
                GetState();
        }


        public void FadeToPreset(int fadeTime)
        {
             sendMessage = string.Format("fadetopreset {0}\r\n", fadeTime);
        }

        public void FadeToZero(int fadeTime)
        {
            sendMessage = string.Format("fadetozero {0}\r\n", fadeTime);
        }

        public void  StorePreset()
        {
            sendMessage = string.Format("storepreset\r\n");
        }

        public void Strobe(int onDuration, int offDuration, int times)
        {
            sendMessage = string.Format("fade {0} {1} {2}\r\n", onDuration, offDuration, times);
        }

        public void EnableLedStrip(bool enabled)
        {
            ledIsOn = enabled;

            if (ledIsOn)
                sendMessage = string.Format("on\r\n");
            else
                sendMessage = string.Format("off\r\n");
        }

        public void EnableLedStrip(bool enabled, int fadeTime)
        {
            ledIsOn = enabled;

            if (ledIsOn)
                sendMessage = string.Format("on {0}\r\n", fadeTime);
            else
                sendMessage = string.Format("off {0}\r\n", fadeTime);
        }

        public void FadeLed(int r, int g, int b, int fadeTime)
        {
            this.r = r;
            this.g = g;
            this.b = b;

            sendMessage = string.Format("fade {0} {1} {2} {3}\r\n", r, g, b, fadeTime);
        }

        public void SetColorLed(int r, int g, int b)
        {
            this.r = r;
            this.g = g;
            this.b = b;

            sendMessage = string.Format("color {0} {1} {2} \r\n", r, g, b);
        }

        public void GetState()
        {
            WriteAsync("state\r\n");
        }

        private void SendMessage(object sender, object e)
        {
            if (sendMessage != null)
            {
                WriteAsync(sendMessage);
                sendMessage = null;
            }

        }

        private async void WriteAsync(string message)
        {

            Debug.WriteLine("Send: "+ message);


            DataWriter dataWriteObject = new DataWriter(App.serialPort.OutputStream);

            dataWriteObject.WriteString(message);

            await dataWriteObject.StoreAsync();

            dataWriteObject.DetachStream();
        }



        private async void StartReading()
        {

            dataReaderObject = new DataReader(App.serialPort.InputStream);

            while (true)
                await ReadAsync();

        }

        private async Task ReadAsync()
        {
            uint bytesRead = await dataReaderObject.LoadAsync(1024);

            string receivedText = dataReaderObject.ReadString(bytesRead);
            string[] messages = Regex.Split(receivedText, "\r\n");

            foreach (var m in messages)
                ReadRecievedMessage(m);

        }

        private void ReadRecievedMessage(string recievedMessage)
        {
            Debug.WriteLine("Recieved: " + recievedMessage);

            string[] arguments = recievedMessage.Split(' ');

            if (arguments[0] == "State:") ReadState(arguments);
            else if (arguments[0] == "Switch:") ReadSwitch(arguments);
        }


        private void ReadState(string[] arguments)
        {
            int new_r =  Int32.Parse(arguments[1]);
            int new_g =  Int32.Parse(arguments[2]);
            int new_b =  Int32.Parse(arguments[3]);
            ledIsOn = arguments[4] == "1";

            r = new_r;
            g = new_g;
            b = new_b;

            connectTimer.Stop();

            if (stateRecievedEvent != null) stateRecievedEvent(r,g,b,ledIsOn);

            connected = true;
        }

        private void ReadSwitch(string[] arguments)
        {
            bool switchState = arguments[1]=="1";
            if (stateRecievedEvent != null) stateRecievedEvent(r, g, b, switchState);
        }
    }
}
