using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace WeatherGUI
{
    class FileManager
    {
        string path;
        FileStream file;
        public FileManager(string path)
        {
            this.path = path;
            if (!File.Exists(@path + @"\calibData.xml"))
            {                             
                writeCalibrationData(new calibrationData());
            }

            if (!File.Exists(@path + @"\Data.xml"))
            {
                File.Create(@path + @"\Data.xml");
            }
        }

        public void writeCalibrationData(calibrationData calib)
        {
            try
            {
                using (file = File.Open(@path + @"\calibData.xml", FileMode.OpenOrCreate))
                {
                    XmlWriterSettings xmlSet = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
                    XmlWriter w = XmlWriter.Create(file, xmlSet);
                    w.WriteStartElement("LM335");
                    w.WriteAttributeString("K", calib.tempK.ToString());
                    w.WriteEndElement();

                    w.WriteStartElement("LoadCell");
                    w.WriteAttributeString("K", calib.weightK.ToString());                    
                    w.WriteAttributeString("W", calib.gaugeWeight.ToString());
                    w.WriteAttributeString("D", calib.gaugeArea.ToString());
                    w.WriteAttributeString("Z", calib.zero.ToString());
                    w.WriteEndElement();

                    w.Flush();
                    w.Close();

                    file.Flush();
                    file.Close();
                }
            }
            catch (FileNotFoundException fnfe) { MessageBox.Show("File Not Found \n" + fnfe.ToString()); }
            catch (FileLoadException fle) { MessageBox.Show("Can't load file \n" + fle.ToString()); }
            catch (AccessViolationException ave) { MessageBox.Show("File blocked \n" + ave.ToString()); }
            catch (UnauthorizedAccessException uae) { MessageBox.Show("Unauthorized access\n" + uae.ToString()); }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public calibrationData readCalibrationData() 
        {
            calibrationData calib = new calibrationData();
            try
            {
                using (file = File.Open(@path + @"\calibData.xml", FileMode.Open))
                {
                    XmlReaderSettings xmlSet = new XmlReaderSettings {ConformanceLevel = ConformanceLevel.Fragment };
                    XmlReader r = XmlReader.Create(file, xmlSet);
                    r.ReadToFollowing("LM335");
                    r.MoveToFirstAttribute();
                    calib.tempK = float.Parse(r.Value);
                    r.ReadToFollowing("LoadCell");
                    r.MoveToFirstAttribute();
                    calib.weightK = float.Parse(r.Value);
                    r.MoveToNextAttribute();                   
                    calib.gaugeWeight = float.Parse(r.Value);
                    r.MoveToNextAttribute();
                    calib.gaugeArea = float.Parse(r.Value);
                    r.MoveToNextAttribute();
                    calib.zero = float.Parse(r.Value);
                    return calib;
                }
            }
            catch (FileNotFoundException fnfe) { MessageBox.Show("File Not Found \n" + fnfe.ToString()); }
            catch (FileLoadException fle) { MessageBox.Show("Can't load file \n" + fle.ToString()); }
            catch (AccessViolationException ave) { MessageBox.Show("File blocked \n" + ave.ToString()); }
            catch (UnauthorizedAccessException uae) { MessageBox.Show("Unauthorized access\n" + uae.ToString()); }
            throw new Exception();
        }

        public void writeWeatherData(weatherData weather)
        {
            try
            {
                using (file = File.Open(@path + @"\Data.xml", FileMode.Append))
                {               
                    XmlWriterSettings xmlSet = new XmlWriterSettings { Indent = true, NewLineChars = "\r\n", NewLineHandling = NewLineHandling.Replace, IndentChars = "\t" , Encoding = Encoding.UTF8, OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Document};
                    XmlWriter w = XmlWriter.Create(file, xmlSet);

                    w.WriteRaw(Environment.NewLine);
                    
                    w.WriteStartElement("Timestamp");
                    w.WriteAttributeString("Day", DateTime.Now.Date.ToShortDateString());
                    w.WriteAttributeString("Time", string.Format("{0:HH:mm:sstt}", DateTime.Now));

                    w.WriteStartElement("Data");
                    w.WriteStartElement("Temperature");
                    w.WriteAttributeString("Value", weather.temp.ToString("0.0"));
                    w.WriteEndElement();
                    
                    w.WriteStartElement("RH");
                    w.WriteAttributeString("Value", weather.humi.ToString("0.0"));
                    w.WriteEndElement();

                    w.WriteStartElement("Pressure");
                    w.WriteAttributeString("Value", weather.press.ToString("0.0"));
                    w.WriteEndElement();

                    w.WriteStartElement("mm");
                    w.WriteAttributeString("Value", weather.mm.ToString("0.0"));
                    w.WriteEndElement();

                    w.WriteEndElement();
                                       
                    w.Flush();
                    w.Close();                 

                    file.Flush();
                    file.Close(); 
                }
            }
            catch (FileNotFoundException fnfe) { MessageBox.Show("File Not Found \n" + fnfe.ToString()); }
            catch (FileLoadException fle) { MessageBox.Show("Can't load file \n" + fle.ToString()); }
            catch (AccessViolationException ave) { MessageBox.Show("File blocked \n" + ave.ToString()); }
            catch (UnauthorizedAccessException uae) { MessageBox.Show("Unauthorized access\n" + uae.ToString()); }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
    }
}
