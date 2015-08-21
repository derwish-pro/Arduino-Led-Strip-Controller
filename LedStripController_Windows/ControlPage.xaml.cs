using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LedStripController_Windows
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ControlPage : Page
    {
        private int stripChangeColorTime = 1000;
        private int stripEnableTime = 2000;

        private LedStripController ledStripController = new LedStripController();

        public bool sendControls;


        public ControlPage()
        {
            this.InitializeComponent();

            ledStripController.stateRecievedEvent += UpdateSliders;

            toggleSwitch.IsEnabled = false;
            slider1.IsEnabled = false;
            slider2.IsEnabled = false;
            slider3.IsEnabled = false;
        }



        private void toggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!sendControls) return;

            ledStripController.EnableLedStrip(toggleSwitch.IsOn, stripEnableTime);
        }



        private void slider1_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!sendControls) return;

            ledStripController.FadeLed(
                (int)slider1.Value,
                (int)slider2.Value,
                (int)slider3.Value,
                stripChangeColorTime);
        }

        private void UpdateSliders(int r,int g,int b, bool isOn)
        {
            sendControls = false;

            slider1.Value = r;
            slider2.Value = g;
            slider3.Value = b;
            toggleSwitch.IsOn = isOn;

            toggleSwitch.IsEnabled = true;
            slider1.IsEnabled = true;
            slider2.IsEnabled = true;
            slider3.IsEnabled = true;

            sendControls = true;

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            ledStripController.StorePreset();
        }
    }
}
