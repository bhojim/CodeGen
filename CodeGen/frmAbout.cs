using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

namespace CodeGen
{
    public partial class frmAbout : Form
    {
        public frmAbout()
        {
            InitializeComponent();
        }

        private void frmAbout_Load(object sender, EventArgs e)
        {
            //AssemblyInfo entryAssemblyInfo = new AssemblyInfo(Assembly.GetEntryAssembly());
            //lblProduct.Text = entryAssemblyInfo.Product;
            //lblDescription.Text = entryAssemblyInfo.Description;
            //lblVersion.Text = "Version " + entryAssemblyInfo.Version;
            //lblCopyright.Text = entryAssemblyInfo.Copyright;

            //var assembly = Assembly.GetExecutingAssembly();
            //// Display assembly description
            //var descriptionAttribute = assembly
            //    .GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)
            //    .OfType<AssemblyDescriptionAttribute>()
            //    .FirstOrDefault();

            //if (descriptionAttribute != null)
            //    lblDescription.Text = descriptionAttribute.Description;

            //// display version number
            //lblVersion.Text = "Version " + Application.ProductVersion;

        }

        private void frmAbout_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            MessageBox.Show("Help clicked");
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
