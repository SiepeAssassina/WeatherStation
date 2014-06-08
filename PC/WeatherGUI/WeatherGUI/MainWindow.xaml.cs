using System;
using System.Windows;
using System.Threading;

namespace WeatherGUI
{
    public partial class MainWindow : Window, IMainWindow
    {
        comHandler modem;
        sensorData data = new sensorData();
        bool isOnline = false;
        public  string[] poolRate = new string[] {"1s", "3s", "10s", "1m", "3m", "10m", "30m", "1h", "2h", "3h"};
        public int pooling = 10000;
        float tempK = 0;
        public MainWindow()
        {
            InitializeComponent();           
        }
                
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string s in poolRate)
                poolBox.Items.Add(s);
            CRadioBtn.IsChecked = true;
            rawDataListBox.Items.Add("Temp -> ");
            rawDataListBox.Items.Add("Pres -> ");
            rawDataListBox.Items.Add("Humi -> ");
            rawDataListBox.Items.Add("Rain -> ");
            rawDataListBox.Items.Add("Vcc -> ");
            poolBox.SelectedIndex = 5;
            rainAcqBtn.IsEnabled = false;
            calibrateLdCellBtn.IsEnabled = false;
            clbTempBtn.IsEnabled = false;
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
                modem.pooling = pooling;
                modem.Open();
            }
            else if (!isOnline)
            {
                modem.Close();
                Thread.Sleep(10);
                modem = new comHandler(comSelectionBox.SelectedItem.ToString(), 600, (IMainWindow)this);
                modem.pooling = pooling;
                modem.Open();
            }
            else
            {
                modem.Close();                
            }
            button1.IsEnabled = true;
            Thread.Sleep(10);
        }

        public void updateState(bool b)
        {            
            Action action = new Action(() =>
            {
                isOnline = b;
                appendDebug(button1.Content + "ed!");
                button1.Content = b ? "Disconnect" : "Connect";               
                rstButton.IsEnabled = b;      
                clbTempBtn.IsEnabled = b;
                calibrateLdCellBtn.IsEnabled = b;
            });
            this.Dispatcher.BeginInvoke(action, System.Windows.Threading.DispatcherPriority.Send);
        }

        public void appendDebug(string s)
        {
            Action action = new Action(() =>
            {
                string buffer = "[" + string.Format("{0:HH:mm:sstt}", DateTime.Now) + "] -> " + s;
                debugBox.Items.Add(buffer);
                debugBox.ScrollIntoView(buffer);
            });
            this.Dispatcher.BeginInvoke(action, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        public void updateRawData(sensorData buffer)
        { 
            Action action = new Action(() =>
            {
                float bandGap = buffer.bandGap / 100F;
                float temp = (buffer.value[0] * tempK) - 273;
                float P = (float)((buffer.value[1] / (1024 * 0.009)) + (0.095 / 0.009));
                int RH = (int)((buffer.value[2] / (1024 * 0.00636)) - (0.1515 / 0.00636));
                rawDataListBox.Items[0] = "Temp -> " + buffer.value[0] + " (" + (int)temp+ " °C)";
                rawDataListBox.Items[1] = "Pres -> " + buffer.value[1] + " (" + P + " kPa)";
                rawDataListBox.Items[2] = "Humi -> " + buffer.value[2] + " (" + RH + " %RH)";
                rawDataListBox.Items[3] = "Rain -> " + buffer.value[3] + " (" + buffer.value[3] * (1500F / 1023F) + " g)";
                rawDataListBox.Items[4] = "Vcc -> " + buffer.bandGap + " (" + bandGap + " V)";
                currentTempTxtBlk.Text = "Current temp: " + (int)temp + "°C";
                data = buffer;
            });
            this.Dispatcher.BeginInvoke(action, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (modem != null)
            {               
                modem.Close();
            }
            Environment.Exit(0x00);
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
            pooling = index * Int16.Parse(item.Remove(item.Length - 1)) * 1000;
            if (isOnline) modem.pooling = pooling;   
        }

        private void calibrateLdCellBtnClick(object sender, RoutedEventArgs e)
        {
            int userWeight;            
            try
            {
                userWeight = Int16.Parse(rainSampleWTxtBox.Text);
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show("Please insert a value before calibrating");
                return;
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter a valid int");
                return;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
                return;
            }
            MessageBox.Show("Unload the cell, then press OK");
            sensorData buffer = modem.asyncronousUpdate();
            int zeroWeigh = buffer.value[4];
            MessageBox.Show("Now put the weight on the cell and presso OK");
            int sampleWeight = buffer.value[4];

        }

        private void clbTempBtnClick(object sender, RoutedEventArgs e)
        {
            float sampleTemp;           
            try
            {                
                sampleTemp = float.Parse(userTempTxtBox.Text) + 273;
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show("Please insert a value before calibrating");
                return;
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter a valid float");
                return;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
                return;
            }
            sensorData buffer =  modem.asyncronousUpdate();            
            tempK = sampleTemp / buffer.value[0];           
        }     

        private void rainSaveBtnClick(object sender, RoutedEventArgs e)
        {

        }                  
    }
}
