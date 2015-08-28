/*
* Led Strip Controller
*
* Copyright (C) 2015 Derwish <derwish.pro@gmail.com>
* License: http://opensource.org/licenses/MIT
*/


/*

The maximum frequency of messages when connecting 9600 - once every 20 MS.
If you send faster, some messages will be lost.

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;
using Windows.Storage.Streams;
using Windows.UI.Xaml;

namespace LedStripController_Windows
{
    public delegate void StateRecievedEventHandler(uint r, uint g, uint b, bool isOn);

    public class LedStripController
    {
        DataReader dataReaderObject;

        bool connected;
        bool ledIsOn;
        uint r, g, b;
        DispatcherTimer sendTimer = new DispatcherTimer();
        DispatcherTimer connectTimer = new DispatcherTimer();
        string sendMessage;
        bool isStrobing;
        bool isFaidingRainbow;

        public event StateRecievedEventHandler stateRecievedEvent;

        public LedStripController()
        {
            StartReading();

            sendTimer.Interval = TimeSpan.FromMilliseconds(5);
            sendTimer.Tick += SendMessageTimer;
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



        public void GetState()
        {
            SendMessage("state\r\n");
        }

        private void SendMessageTimer(object sender, object e)
        {
            if (sendMessage != null)
            {
                WriteAsync(sendMessage);
                sendMessage = null;
            }

        }


        private void SendMessage(string message, bool isImportant = true)
        {
            if (isImportant)
                WriteAsync(message);
            else
                sendMessage = message;

        }

        private async void WriteAsync(string message)
        {
            //           if(message==null)return;

            Debug.WriteLine("Send: " + message);


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
            uint new_r = UInt32.Parse(arguments[1]);
            uint new_g = UInt32.Parse(arguments[2]);
            uint new_b = UInt32.Parse(arguments[3]);
            ledIsOn = arguments[4] == "1";

            r = new_r;
            g = new_g;
            b = new_b;

            connectTimer.Stop();

            if (stateRecievedEvent != null) stateRecievedEvent(r, g, b, ledIsOn);

            connected = true;
        }

        private void ReadSwitch(string[] arguments)
        {
            bool switchState = arguments[1] == "1";
            if (stateRecievedEvent != null) stateRecievedEvent(r, g, b, switchState);
        }




        public void FadeToPreset(uint fadeTime)
        {
            SendMessage(string.Format("fadetopreset {0}\r\n", fadeTime));
            if (stateRecievedEvent != null) stateRecievedEvent(r, g, b, ledIsOn);
        }

        public void StorePreset()
        {
            SendMessage(string.Format("storepreset\r\n"));
        }

        public void Strobe(uint onDuration = 100, uint offDuration = 900, uint times = 0)
        {
            SendMessage(string.Format("strobe {0} {1} {2}\r\n", onDuration, offDuration, times), false);
        }

        public void TurnOnOff(bool enabled)
        {
            ledIsOn = enabled;

            if (ledIsOn)
                SendMessage(string.Format("on\r\n"));
            else
                SendMessage(string.Format("off\r\n"));
        }

        public void TurnOnOff(bool enabled, uint fadeTime)
        {
            ledIsOn = enabled;

            if (ledIsOn)
                SendMessage(string.Format("on {0}\r\n", fadeTime));
            else
                SendMessage(string.Format("off {0}\r\n", fadeTime));
        }

        public void Fade(uint r, uint g, uint b, uint fadeTime)
        {
            r = Clamp(r, 0, 255);
            g = Clamp(g, 0, 255);
            b = Clamp(b, 0, 255);

            this.r = r;
            this.g = g;
            this.b = b;

            SendMessage(string.Format("fade {0} {1} {2} {3}\r\n", r, g, b, fadeTime), false);
            if (stateRecievedEvent != null) stateRecievedEvent(r, g, b, ledIsOn);

        }

        public void SetColor(uint r, uint g, uint b)
        {
            r = Clamp(r, 0, 255);
            g = Clamp(g, 0, 255);
            b = Clamp(b, 0, 255);

            this.r = r;
            this.g = g;
            this.b = b;

            SendMessage(string.Format("color {0} {1} {2} \r\n", r, g, b), false);
            if (stateRecievedEvent != null) stateRecievedEvent(r, g, b, ledIsOn);

        }






        public void FadeRandom(uint fadeTime = 500)
        {
            Random rand = new Random();
            uint r = (uint)rand.Next(0, 255);
            uint g = (uint)rand.Next(0, 255);
            uint b = (uint)rand.Next(0, 255);
            Fade(r, g, b, fadeTime);
        }


        public void FadeToRandom768Hue(uint fadeTime = 500)
        {
            Random rand = new Random();
            uint hue = (uint)rand.Next(0, 767);
            FadeHue768(hue, 1, 500);
        }


        public async void StartFaidingRainbow768(uint fadeTime = 20000, float brightness = 1)
        {
            isFaidingRainbow = true;

            brightness = Clamp(brightness, 0f, 1f);

            //fade to closest color
            if (r < g && r < b)
            {
                r = 0;
            }
            else if (g < r && g < b)
            {
                g = 0;
            }
            else
            {
                b = 0;
            }
            Fade(r, g, b, 500);
            Task.Delay(TimeSpan.FromMilliseconds(500));


            while (isFaidingRainbow)
            {
                await FadeRainbow768(fadeTime, brightness);
            }
        }

        public async void StartFaidingRainbow1536(uint fadeTime = 20000, float brightness = 1)
        {
            isFaidingRainbow = true;

            brightness = Clamp(brightness, 0f, 1f);

            //fade to closest color
            if (r < g && r < b)
            {
                r = 0;
            }
            else if (g < r && g < b)
            {
                g = 0;
            }
            else
            {
                b = 0;
            }
            Fade(r, g, b, 500);
            Task.Delay(TimeSpan.FromMilliseconds(500));


            while (isFaidingRainbow)
            {
                await FadeRainbow1536(fadeTime, brightness);
            }
        }


        public void StopFaidingRainbow()
        {
            isFaidingRainbow = false;
        }

        public async Task FadeRainbow768(uint fadeTime = 20000, float brightness = 1)
        {
            isFaidingRainbow = true;

            brightness = Clamp(brightness, 0f, 1f);

            uint delay = fadeTime / 768;
            uint stepFadeTime = delay;

            for (uint i = 0; i < 768; i++)
            {
                if (!isFaidingRainbow) return;

                FadeHue768(i, brightness, stepFadeTime);
                await Task.Delay(TimeSpan.FromMilliseconds(delay));
            }
        }

        public async Task FadeRainbow1536(uint fadeTime = 20000, float brightness = 1)
        {
            isFaidingRainbow = true;

            brightness = Clamp(brightness, 0f, 1f);

            uint delay = fadeTime / 1536;
            uint stepFadeTime = delay;

            for (uint i = 0; i < 1536; i++)
            {
                if (!isFaidingRainbow) return;

                FadeHue1536(i, brightness, stepFadeTime);
                await Task.Delay(TimeSpan.FromMilliseconds(delay));
            }
        }



        /// <summary>
        /// Fade to hue
        /// </summary>
        /// <param name="hue">0-767, 0-red,256-blue, 512-green, 767-almost red</param>
        /// <param name="brightness">0-1</param>
        /// <param name="fadeTime">0-... ms</param>
        public void FadeHue768(uint hue, float brightness = 1, uint fadeTime = 1000)
        {
            uint r = 0, g = 0, b = 0;

            brightness = Clamp(brightness, 0f, 1f);
            hue = Clamp(hue, 0, 767);


            if (hue < 256)
            {
                r = 256 - hue - 1;
                g = hue;
            }
            else if (hue >= 256 && hue < 512)
            {
                g = 512 - hue - 1;
                b = hue - 256;
            }
            else
            {
                b = 768 - hue - 1;
                r = hue - 512;
            }

            r = (uint)(brightness * r);
            g = (uint)(brightness * g);
            b = (uint)(brightness * b);

            Fade(r, g, b, fadeTime);
        }




        /// <summary>
        /// Fade to hue
        /// </summary>
        /// <param name="hue">0-767, 0-red,256-blue, 512-green, 767-almost red</param>
        /// <param name="brightness">0-1</param>
        public void SetColorHue768(uint hue, float brightness = 1)
        {
            uint r = 0, g = 0, b = 0;

            brightness = Clamp(brightness, 0f, 1f);
            hue = Clamp(hue, 0, 767);

            if (hue < 256)
            {
                r = 256 - hue - 1;
                g = hue;
            }
            else if (hue >= 256 && hue < 512)
            {
                g = 512 - hue - 1;
                b = hue - 256;
            }
            else
            {
                b = 768 - hue - 1;
                r = hue - 512;
            }

            r = (uint)(brightness * r);
            g = (uint)(brightness * g);
            b = (uint)(brightness * b);

            SetColor(r, g, b);
        }


        /// <summary>
        /// Fade to hue
        /// </summary>
        /// <param name="hue">0-1536, 0-red,512-blue, 1024-green, 1535-almost red</param>
        /// <param name="brightness">0-1</param>
        /// <param name="fadeTime">0-... ms</param>
        public void FadeHue1536(uint hue, float brightness = 1, uint fadeTime = 1000)
        {
            uint r = 0, g = 0, b = 0;

            brightness = Clamp(brightness, 0f, 1f);
            hue = Clamp(hue, 0, 1535);

            /*
            255    0-255
            >0-255
            0

            <0-255  256-511
            255
            0

            0      512-767
            255  
            >0-255

            0      758-1023
            <0-255
            255

            >0-255  1024-1279
            0
            255

            255  1280-1536
            0
           <0-255
            */


            if (hue < 256)
            {
                r = 255;
                g = hue;
                b = 0;
            }
            else if (hue >= 256 && hue < 512)
            {
                r = 512 - hue - 1;
                g = 255;
                b = 0;
            }
            else if (hue >= 512 && hue < 768)
            {
                r = 0;
                g = 255;
                b = hue - 512;
            }
            else if (hue >= 768 && hue < 1024)
            {
                r = 0;
                g = 1024 - hue - 1;
                b = 255;
            }
            else if (hue >= 1024 && hue < 1280)
            {
                r = hue - 1024;
                g = 0;
                b = 255;
            }
            else
            {
                r = 255;
                g = 0;
                b = 1536 - hue - 1;
            }


            r = (uint)(brightness * r);
            g = (uint)(brightness * g);
            b = (uint)(brightness * b);

            Fade(r, g, b, fadeTime);
        }



        /// <summary>
        /// Fade to hue
        /// </summary>
        /// <param name="hue">0-1536, 0-red,512-blue, 1024-green, 1535-almost red</param>
        /// <param name="brightness">0-1</param>
        public void SetColorHue1536(uint hue, float brightness = 1)
        {
            uint r = 0, g = 0, b = 0;

            brightness = Clamp(brightness, 0f, 1f);
            hue = Clamp(hue, 0, 1535);
            
            if (hue < 256)
            {
                r = 255;
                g = hue;
                b = 0;
            }
            else if (hue >= 256 && hue < 512)
            {
                r = 512 - hue - 1;
                g = 255;
                b = 0;
            }
            else if (hue >= 512 && hue < 768)
            {
                r = 0;
                g = 255;
                b = hue - 512;
            }
            else if (hue >= 768 && hue < 1024)
            {
                r = 0;
                g = 1024 - hue - 1;
                b = 255;
            }
            else if (hue >= 1024 && hue < 1280)
            {
                r = hue - 1024;
                g = 0;
                b = 255;
            }
            else
            {
                r = 255;
                g = 0;
                b = 1536 - hue - 1;
            }


            r = (uint)(brightness * r);
            g = (uint)(brightness * g);
            b = (uint)(brightness * b);

            SetColor(r, g, b);
        }




        public void StartStrobe(uint onDuration = 100, uint offDuration = 900)
        {
            isStrobing = true;
            Strobe(onDuration, offDuration, 10000);
        }

        public void StopStrobe()
        {
            isStrobing = false;
            SendMessage(string.Format("stopstrobe\r\n"));
        }

        public bool IsStrobing()
        {
            return isStrobing;
        }

        public bool IsFaidingRainbow()
        {
            return isFaidingRainbow;
        }


        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static uint Clamp(uint value, uint min, uint max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }




        public uint ConvertRGBToHue1536(uint r, uint g, uint b)
        {
            /*
            255    0-255
           >0-255
            0

           <0-255  256-511
            255
            0

            0      512-767
            255  
           >0-255

            0      758-1023
           <0-255
            255

           >0-255  1024-1279
            0
            255

            255  1280-1536
            0
           <0-255
            */

            uint hue = 0;

            if (r == 255 && g >= 0 && b == 0)
            {
                hue = 0 + g;
            }
            else if (r >= 0 && g == 255 && b == 0)
            {
                hue = 256 + (255 - r);
            }
            else if (r == 0 && g == 255 && b >= 0)
            {
                hue = 512 + b;
            }
            else if (r == 0 && g >= 0 && b == 255)
            {
                hue = 758 + (255 - g);
            }
            else if (r >= 0 && g == 0 && b == 255)
            {
                hue = 1024 + r;
            }
            else if (r == 255 && g == 0 && b >= 0)
            {
                hue = 1536 + (255 - b);
            }

            return hue;
        }


    }
}
