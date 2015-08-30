using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
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

          //  GetSerialList();
        }


        public void GetSerialList()
        {
            List<string> devices = App.serialPort.GetSerialDevicesList();
       
            if (listbox1.Items != null)
                listbox1.Items.Clear();

            foreach (var device in devices)
                listbox1.Items.Add(device);
        }



        private async void buttonConnect_Click(object sender, RoutedEventArgs e)
        {

            int selection = listbox1.SelectedIndex;
            try
            {
                App.serialPort.Connect(selection);
            }
            catch
            {
                var dialog = new MessageDialog("Connecting failed.");
                await dialog.ShowAsync();
                return;
            }

            App.ledStripController.Connect();

            Frame.Navigate(typeof(ControlPage));
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            GetSerialList();
        }
    }
}
