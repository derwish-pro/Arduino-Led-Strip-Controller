using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace LedStripController_Windows
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConnectPage : Page
    {
        public ConnectPage()
        {
            this.InitializeComponent();

            GetSerialList();

        }

        private DeviceInformationCollection devices;

        public async void GetSerialList()
        {


            string selector = SerialDevice.GetDeviceSelector();
            devices = await DeviceInformation.FindAllAsync(selector);


            foreach (var device in devices)
                listbox1.Items.Add(device.Name);

        }



        private async void buttonConnect_Click(object sender, RoutedEventArgs e)
        {

            int selection = listbox1.SelectedIndex;
            DeviceInformation device = devices[selection];

            App.serialPort = await SerialDevice.FromIdAsync(device.Id);

            if (App.serialPort == null)
            {
                var dialog = new MessageDialog("Cannot connect to device.");
                await dialog.ShowAsync();
                return;
            }

            App.serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
            App.serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
            App.serialPort.BaudRate = 9600;
            App.serialPort.Parity = SerialParity.None;
            App.serialPort.StopBits = SerialStopBitCount.One;
            App.serialPort.DataBits = 8;

            Frame.Navigate(typeof(RemoteControlPage));
        }
    }
}
