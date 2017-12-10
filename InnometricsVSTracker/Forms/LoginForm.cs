using System;
using System.Windows.Forms;

namespace InnometricsVSTracker.Forms
{
    public partial class LoginForm : Form
    {
        private readonly ConfigFile _innoMetricsConfigFile;
        public LoginForm()
        {
            InitializeComponent();
            _innoMetricsConfigFile = new ConfigFile();
            _innoMetricsConfigFile.Read();
        }

        private void ApiKeyForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtUserName.Text = _innoMetricsConfigFile.Username;
                txtPassword.Text = _innoMetricsConfigFile.Password;
                txtUrl.Text = _innoMetricsConfigFile.Url;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                var username = txtUserName.Text.Trim();
                var password = txtPassword.Text;
                var url = txtUrl.Text.Trim();
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(url))
                {
                    var auth = new Sender(url);
                    var token = auth.GetToken(username, password);
                    if (token != null)
                    {
                        _innoMetricsConfigFile.Username = username;
                        _innoMetricsConfigFile.Password = password;
                        _innoMetricsConfigFile.Url = url;
                        _innoMetricsConfigFile.Token = token;

                        _innoMetricsConfigFile.Save();
                    }
                }
                else
                {
                    MessageBox.Show(@"Please enter valid credentials");
                    DialogResult = DialogResult.None; // do not close dialog box
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
