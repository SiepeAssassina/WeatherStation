using System;
using System.Windows;

namespace WeatherGUI
{
    public partial class MainWindow : Window, IMainWindow
    {
        comHandler modem;
        
        public MainWindow()
        {
            InitializeComponent();            
        }
                
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 20; i++) comSelectionBox.Items.Add("COM" + i);
            comSelectionBox.SelectedIndex = 0;
            appendDebug("Loaded");          
        }
        
        private void Window_Closed(object sender, EventArgs e)
        {
            modem.Close();
        }

        private void connectClick(object sender, RoutedEventArgs e)
        {
            modem = new comHandler(comSelectionBox.SelectedItem.ToString(), 600, (IMainWindow)this);            
        }

        public void appendDebug(string s)
        {
            Action action = new Action(() =>
            {
                debugBox.Items.Add("[" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "] -> " + s);
            });
            this.Dispatcher.Invoke(action, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }
}
