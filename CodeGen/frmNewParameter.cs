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
    public partial class frmNewParameter : Form
    {
        // <copyright file="frmNewParameter" company="Dotnetcomp.com">
        // Copyright (c) 2016 All Rights Reserved
        // <author>Bernard Ho-Jim</author>
        // </copyright>
      
        public string ParameterString { get; set; }

        public frmNewParameter()
        {
            InitializeComponent();
        }

        private void frmNewParameter_Load(object sender, EventArgs e)
        {
            cboType.Items.Add(new ListItem("String", "String"));
            cboType.Items.Add(new ListItem("Integer", "Integer"));
            cboType.Items.Add(new ListItem("Decinal", "Decimal"));
            cboType.Items.Add(new ListItem("Boolean","Boolean"));
            cboType.Items.Add(new ListItem("Enum", "Enum"));
            cboType.SelectedIndex = 0;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            ListItem listItem;

            // Build the parameter string
            string name = txtName.Text;
            string value = txtValue.Text;
            listItem = (ListItem)cboType.SelectedItem;
            switch (listItem.ItemData.ToString())
            {
                case "String":
                    ParameterString = String.Format(@"<#@ parameter type=""System.String"" name=""{0}"" value=""{1}"" #>",name, value);
                    break;
                case "Integer":
                    ParameterString = String.Format(@"<#@ parameter type=""System.Int32"" name=""{0}"" value=""{1}"" #>", name, value);
                    break;
                case "Decinal":
                    ParameterString = String.Format(@"<#@ parameter type=""System.Decimal"" name=""{0}"" value=""{1}"" #>", name, value);
                    break;
                case "Boolean":
                    ParameterString = String.Format(@"<#@ parameter type=""System.Boolean"" name=""{0}"" value=""{1}"" #>", name, value);
                    break;
                default:
                    break;
            }
                  

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
