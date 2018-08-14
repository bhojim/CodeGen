using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodeGen
{
    public partial class frmCreateFolder : Form
    {
        // <copyright file="frmCreateFolder" company="Dotnetcomp.com">
        // Copyright (c) 2016 All Rights Reserved
        // <author>Bernard Ho-Jim</author>
        // </copyright>
      
        public frmCreateFolder()
        {
            InitializeComponent();
        }

        public string TextEntered { get; set; }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
            TextEntered = txtName.Text;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }
    }
}
