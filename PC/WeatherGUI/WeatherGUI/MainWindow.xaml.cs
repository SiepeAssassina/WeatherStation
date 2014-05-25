using System;
using System.Windows;

namespace WeatherGUI
{
    public partial class MainWindow : Window, IMainWindow
    {
        comHandler modem;
        bool isOnline = false;
        public MainWindow()
        {
            InitializeComponent();           
        }
                
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 1; i < 20; i++) comSelectionBox.Items.Add("COM" + i);
            comSelectionBox.SelectedIndex = 0;
            appendDebug("Loaded");           
        }
        
        private void Window_Closed(object sender, EventArgs e)
        {            
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
                modem = new comHandler(comSelectionBox.SelectedItem.ToString(), 600, (IMainWindow)this);
                modem.connect();
            }
            else
            {
                modem.Close();
                modem = null;
            }
            button1.IsEnabled = true;
            System.Threading.Thread.Sleep(100);
        }

        public void updateState(bool b)
        {
            Action action = new Action(() =>
            {
                isOnline = b;
                button1.Content = b ? "Disconnect" : "Connect";
                if (b) appendDebug("Connected!");
                else appendDebug("Disconnected!");
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
            this.Dispatcher.Invoke(action, System.Windows.Threading.DispatcherPriority.Send);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (modem != null) 
                modem.Close();
            this.Close();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("WeatherGUI for ARDUINO \n Brought to you by Lorenzo Tomasin, 2014");
        }
    }
}
