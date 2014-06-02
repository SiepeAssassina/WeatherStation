using System;
using System.Windows;
using System.Threading;

namespace WeatherGUI
{
    public partial class MainWindow : Window, IMainWindow
    {
        comHandler modem;
        bool isOnline = false;
        public  string[] poolRate = new string[] {"1s", "3s", "10s", "1m", "3m", "10m", "30m", "1h", "2h", "3h"};
        public MainWindow()
        {
            InitializeComponent();           
        }
                
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string s in poolRate)
                poolBox.Items.Add(s);
            rawDataListBox.Items.Add("Temp -> ");
            rawDataListBox.Items.Add("Pres -> ");
            rawDataListBox.Items.Add("Humi -> ");
            rawDataListBox.Items.Add("Rain -> ");
            rawDataListBox.Items.Add("Vcc -> ");
            for (int i = 1; i < 20; i++) comSelectionBox.Items.Add("COM" + i);
            comSelectionBox.SelectedIndex = 0;
            appendDebug("Loaded");           
        }
        
        private void Window_Closing(object sender, EventArgs e)
        {
            if (modem != null) 
                modem.Close();   
        }

        private void connectClick(object sender, RoutedEventArgs e)
        {
            button1.IsEnabled = false;
            if (!isOnline && modem == null)
            {
                modem = new comHandler(comSelectionBox.SelectedItem.ToString(), 600, (IMainWindow)this);
                modem.connect();
            }
            else if (!isOnline)
            {
                modem.Close();
                Thread.Sleep(10);
                modem = new comHandler(comSelectionBox.SelectedItem.ToString(), 600, (IMainWindow)this);
                modem.connect();
            }
            else
            {
                modem.disconnect();                
            }
            button1.IsEnabled = true;
            Thread.Sleep(1000);
        }

        public void updateState(bool b)
        {            
            Action action = new Action(() =>
            {
                isOnline = b;
                appendDebug(button1.Content + "ed!");
                button1.Content = b ? "Disconnect" : "Connect";               
                rstButton.IsEnabled = b;
                poolBtn.IsEnabled = b;
            });
            this.Dispatcher.Invoke(action, System.Windows.Threading.DispatcherPriority.Send);
        }

        public void appendDebug(string s)
        {
            Action action = new Action(() =>
            {
                string buffer = "[" + string.Format("{0:HH:mm:sstt}", DateTime.Now) + "] -> " + s;
                debugBox.Items.Add(buffer);
                debugBox.ScrollIntoView(buffer);
            });
            this.Dispatcher.Invoke(action, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        public void updateRawData(string s, int index)
        { 
            Action action = new Action(() =>
            {
                rawDataListBox.Items[index] = s;
            });
            this.Dispatcher.Invoke(action, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (modem != null)
            {               
                modem.Close();
            }
            this.Close();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("WeatherGUI for ARDUINO \n Brought to you by Lorenzo Tomasin, 2014");
        }

        private void rstClick(object sender, RoutedEventArgs e)
        {
            modem.reset();
        }

        private void poolBtnClick(object sender, RoutedEventArgs e)
        {
            int index = 1000;
            string item = poolBox.SelectedItem.ToString();
            if (item.Contains("s")) index = 1;
            else if (item.Contains("m")) index = 60;
            else if (item.Contains("h")) index = 3600;
            modem.pooling = index * Int16.Parse(item.Remove(item.Length - 1)) * 1000;
            MessageBox.Show(modem.pooling.ToString());
        }
    }
}
