using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenColorServer
{
    class Program
    {

        //SETTINGS
        const int PORT = 5544;
        const int CAPTURE_UPDATE_DELAY = 0;
        const float HEIGHT_FROM_TOP = 0.4f;





        
        static bool isWorking;
        static Color screenAvarageColor;


        static void Main(string[] args)
        {
            StartScreenCapture();
            Console.WriteLine("Screen capture running.");

            string serverURL = String.Format("http://localhost:{0}/", PORT);
            WebServer webServer = new WebServer(SendResponse, serverURL);
            webServer.Run();


            Console.WriteLine(String.Format("Server started at {0}.", serverURL));
            Console.WriteLine("Press any key to stop server.");
            Console.ReadKey();

            webServer.Stop();
        }

        public static string SendResponse(HttpListenerRequest request)
        {
            //Console.WriteLine(DateTime.Now + " : Send response to " + request.UserHostAddress);

            CalculateMsgPerSec();

            string response = "{ \"r\" : \"" + screenAvarageColor.R + "\" , \"g\" : \"" + screenAvarageColor.G + "\" , \"b\" : \"" + screenAvarageColor.B + "\" }";
            return response;
        }

        private static async void StartScreenCapture()
        {
            if (isWorking) return;

            isWorking = true;

            while (isWorking)
            {
                await Task.Delay(CAPTURE_UPDATE_DELAY);

                await Task.Run(() =>
                {
                    CalculateCapturesPerSec();

                    screenAvarageColor = ScreenCapture.GetScreenAverageColor(HEIGHT_FROM_TOP);

                });
            }

        }

        private static void StopScreenCapture()
        {
            isWorking = false;
        }




        private static DateTime msgsStartDate = DateTime.Now;
        private static int msgsCount;

        private static void CalculateMsgPerSec()
        {
            msgsCount++;

            DateTime now = DateTime.Now;
            TimeSpan elapsed = now.Subtract(msgsStartDate);

            if (elapsed.TotalSeconds<1)
                return;

            float msgPerSecond = msgsCount/ (float)elapsed.TotalSeconds;
            msgsStartDate = DateTime.Now;
            msgsCount = 0;

            Console.WriteLine("                                           Sended "+(int)msgPerSecond+" messages/second");
        }





        private static DateTime capturesStartDate = DateTime.Now;
        private static int capturesCount;

        private static void CalculateCapturesPerSec()
        {
            capturesCount++;

            DateTime now = DateTime.Now;
            TimeSpan elapsed = now.Subtract(capturesStartDate);

            if (elapsed.TotalSeconds < 1)
                return;

            float capturesPerSecond = capturesCount / (float)elapsed.TotalSeconds;

            capturesStartDate = DateTime.Now;
            capturesCount = 0;

            Console.WriteLine("Captured " + (int)capturesPerSecond + " screens/second");
        }

    }
}
