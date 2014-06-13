using System;
using System.Windows;
using System.Threading;
using System.IO;
using System.Resources;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;

namespace WeatherGUI
{
    public partial class MainWindow : Window, IMainWindow
    {
        bool isOnline = false;       
        public int pooling = 60000;
        public float maxTemp = -20, minTemp = 50;
        public int lastmm = 0;
        public string[] poolRate = new string[] { "1s", "3s", "10s", "1m", "3m", "10m", "30m", "1h", "2h", "3h" };
        comHandler modem;  
        FileManager file = new FileManager(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
        sensorData data = new sensorData();
        calibrationData calibration = new calibrationData();        
        BitmapImage sunny, sunnyDes, lightrain, lightrainDes, rain, rainDes;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {            
            try
            {
                sunny = new BitmapImage();
                sunny.BeginInit();
                sunny.UriSource = new Uri(@"pack://application:,,,/WeatherGUI;component/Resources/Sunny.png", UriKind.Absolute);
                sunny.EndInit();

                sunnyDes = new BitmapImage();
                sunnyDes.BeginInit();
                sunnyDes.UriSource = new Uri(@"pack://application:,,,/WeatherGUI;component/Resources/SunnyDes.png", UriKind.Absolute);
                sunnyDes.EndInit();

                lightrain = new BitmapImage();
                lightrain.BeginInit();
                lightrain.UriSource = new Uri(@"pack://application:,,,/WeatherGUI;component/Resources/SlightDrizzle.png", UriKind.Absolute);
                lightrain.EndInit();

                lightrainDes = new BitmapImage();
                lightrainDes.BeginInit();
                lightrainDes.UriSource = new Uri(@"pack://application:,,,/WeatherGUI;component/Resources/SlightDrizzleDes.png", UriKind.Absolute);
                lightrainDes.EndInit();

                rain = new BitmapImage();
                rain.BeginInit();
                rain.UriSource = new Uri(@"pack://application:,,,/WeatherGUI;component/Resources/Drizzle.png", UriKind.Absolute);
                rain.EndInit();

                rainDes = new BitmapImage();
                rainDes.BeginInit();
                rainDes.UriSource = new Uri(@"pack://application:,,,/WeatherGUI;component/Resources/DrizzleDes.png", UriKind.Absolute);
                rainDes.EndInit(); 
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); this.Close(); }
            sunnyImg.Source = sunnyDes;
            lightrainImg.Source = lightrainDes;
            heavyrainImg.Source = rainDes;
            NOWDay.Text = DateTime.Now.DayOfWeek.ToString();
            NOWDate.Text = DateTime.Now.Date.ToString();

            foreach (string s in poolRate)
                poolBox.Items.Add(s);
            CRadioBtn.IsChecked = true;
            NOWMAXTempTxtBlk.Foreground = System.Windows.Media.Brushes.Red;
            NOWMINTempTxtBlk.Foreground = System.Windows.Media.Brushes.Blue;
            rawDataListBox.Items.Add("Temp -> ");
            rawDataListBox.Items.Add("Pres -> ");
            rawDataListBox.Items.Add("Humi -> ");
            rawDataListBox.Items.Add("Rain -> ");
            rawDataListBox.Items.Add("Vcc -> ");
            poolBox.SelectedIndex = 5;
            calibrateLdCellBtn.IsEnabled = false;
            acqWGBtn.IsEnabled = false;
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
                acqWGBtn.IsEnabled = b;
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
                float temp = (buffer.value[0] * calibration.tempK) - 273.15F;
                float P = (float)((buffer.value[1] / (1024 * 0.009)) + (0.095 / 0.009));
                int RH = (int)((buffer.value[2] / (1024 * 0.00636)) - (0.1515 / 0.00636));
                float weight = (buffer.value[3] - calibration.zero) * calibration.weightK;
                rawDataListBox.Items[0] = "Temp -> " + buffer.value[0] + " (" + temp.ToString("0.0") + " °C)";
                rawDataListBox.Items[1] = "Pres -> " + buffer.value[1] + " (" + P + " kPa)";
                rawDataListBox.Items[2] = "Humi -> " + buffer.value[2] + " (" + RH + " %RH)";
                rawDataListBox.Items[3] = "Rain -> " + buffer.value[3] + " (" + weight + " g)";
                rawDataListBox.Items[4] = "Vcc -> " + buffer.bandGap + " (" + bandGap + " V)";
                currentTempTxtBlk.Text = "Current temp: " + (int)temp + "°C";
                NOWTempTxtBlk.Text = temp.ToString("0.0") + "°C";
                NOWPrssTxtBlk.Text = (int)(P * 10F) + "mbar";
                NOWHumTxtBlk.Text = (int)RH + "%RH";
                NOWRainTxtBlk.Text = (int)(((weight - calibration.gaugeWeight) * 1000F) / calibration.gaugeArea) + "mm";
                maxTemp = maxTemp < temp ? temp : maxTemp;
                minTemp = minTemp > temp ? temp : minTemp;
                NOWMAXTempTxtBlk.Text = (int)maxTemp + "°C";
                NOWMINTempTxtBlk.Text = (int)minTemp + "°C";
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
                MessageBox.Show("Unload the cell, then press OK");
                sensorData buffer = modem.asyncronousUpdate();
                calibration.zero = buffer.value[3];
                MessageBox.Show("Zeroed. Now put the sample weight on the cell and press OK");
                buffer = modem.asyncronousUpdate();
                calibration.weightK = userWeight / (buffer.value[3] - calibration.zero);
                file.writeCalibrationData(calibration);
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

        }

        private void clbTempBtnClick(object sender, RoutedEventArgs e)
        {
            float sampleTemp;
            try
            {
                sampleTemp = float.Parse(userTempTxtBox.Text) + 273.15F;
                sensorData buffer = modem.asyncronousUpdate();
                calibration.tempK = (sampleTemp) / buffer.value[0];
                file.writeCalibrationData(calibration);
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
        }

        private void rainSaveBtnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                calibration.gaugeArea = float.Parse(rainDTxtBox.Text);
                calibration.gaugeWeight = float.Parse(rainWTxtBox.Text);
                file.writeCalibrationData(calibration);
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
        }

        private void acqWGBtnClick(object sender, RoutedEventArgs e)
        {
            sensorData buffer = modem.asyncronousUpdate();
            rainWTxtBox.Text = ((buffer.value[3] - calibration.zero) * calibration.weightK).ToString();
        }
    }       
}
