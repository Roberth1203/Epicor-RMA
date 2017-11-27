using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Windows.Forms;

namespace Utilities
{
    public class Config
    {
        public void CreateNewOption(string newKey, string newValue)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings.Add(newKey, newValue);
                config.Save(ConfigurationSaveMode.Modified);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public void UpdateSettings(string key, string value)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings[key].Value = value;
                config.Save(ConfigurationSaveMode.Modified);

                MessageBox.Show("Configuración actualizada correctamente", "SaveSettingsAlert", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception settingError)
            {
                MessageBox.Show("Ocurrió un error al guardar la configuración: \n" + settingError.Message, "SaveSettingsException", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
