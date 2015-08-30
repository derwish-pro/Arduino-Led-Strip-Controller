﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class AdvancedControlPage : Page
    {
        private uint stripChangeColorTime = 1000;
        private uint stripEnableTime = 2000;

        public bool sendControls;


        public AdvancedControlPage()
        {
            this.InitializeComponent();

            if (App.ledStripController!=null)
                App.ledStripController.stateRecievedEvent += UpdateSliders;

            toggleSwitch.IsEnabled = false;
            slider1.IsEnabled = false;
            slider2.IsEnabled = false;
            slider3.IsEnabled = false;
        }



        private void toggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!sendControls) return;

            App.ledStripController.TurnOnOff(toggleSwitch.IsOn, stripEnableTime);
        }



        private void slider1_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!sendControls) return;

            App.ledStripController.Fade(
                (uint)slider1.Value,
                (uint)slider2.Value,
                (uint)slider3.Value,
                stripChangeColorTime);
        }

        private void UpdateSliders(uint r, uint g, uint b, bool isOn)
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
            App.ledStripController.StorePreset();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ControlPage));
        }





        private void button4_Click(object sender, RoutedEventArgs e)
        {
            if (App.ledStripController.IsStrobing())
            {
                button4.Content = "Start strobing";
                App.ledStripController.StopStrobe();
            }
            else
            {
                button4.Content = "Stop strobing";
                App.ledStripController.StartStrobe(20, (uint)slider4.Value);
            }
        }

        private void slider4_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (App.ledStripController == null) return;

            if (App.ledStripController.IsStrobing())
            {
                App.ledStripController.StartStrobe(10, (uint)slider4.Value);
            }
        }



        private void button3_Click(object sender, RoutedEventArgs e)
        {
            if (App.ledStripController.IsFaidingRainbow())
            {
                button3.Content = "Start faiding rainbow";
                App.ledStripController.StopFaidingRainbow();
            }
            else
            {
                button3.Content = "Stop faiding rainbow ";
                if (toggleSwitch2.IsOn)
                    App.ledStripController.StartFaidingRainbow768();
                else
                    App.ledStripController.StartFaidingRainbow1536();

            }

        }

        private async void toggleSwitch2_Toggled(object sender, RoutedEventArgs e)
        {
            if (App.ledStripController == null) return;

            if (App.ledStripController.IsFaidingRainbow())
            {
                App.ledStripController.StopFaidingRainbow();

                await Task.Delay(500);

                if (toggleSwitch2.IsOn)
                    App.ledStripController.StartFaidingRainbow768();
                else
                    App.ledStripController.StartFaidingRainbow1536();
            }
        }

        private async void button2_Click(object sender, RoutedEventArgs e)
        {
            if (App.ledStripController.IsFaidingRainbow())
            {
                App.ledStripController.StopFaidingRainbow();

                await Task.Delay(100);

                if (toggleSwitch2.IsOn)
                    App.ledStripController.FadeRandomHue768();
                else
                    App.ledStripController.FadeRandomHue1536();

                await Task.Delay(500);

                if (toggleSwitch2.IsOn)
                    App.ledStripController.StartFaidingRainbow768();
                else
                    App.ledStripController.StartFaidingRainbow1536();
            }
            else
            {
                if (toggleSwitch2.IsOn)
                    App.ledStripController.FadeRandomHue768();
                else
                    App.ledStripController.FadeRandomHue1536();
            }
        }
    }
}