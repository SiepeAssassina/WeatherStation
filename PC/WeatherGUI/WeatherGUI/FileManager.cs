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
                    w.WriteAttributeString("H", calib.gaugeHeight.ToString());
                    w.WriteAttributeString("W", calib.gaugeWeight.ToString());
                    w.WriteAttributeString("D", calib.gaugeArea.ToString());
                    w.WriteAttributeString("Z", calib.zero.ToString());

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
                    calib.gaugeHeight = float.Parse(r.Value);
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
    }
}
