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
    public partial class PropertyForm : Form
    {
        // <copyright file="PropertyForm" company="Dotnetcomp.com">
        // Copyright (c) 2016 All Rights Reserved
        // <author>Bernard Ho-Jim</author>
        // </copyright>
      
        CustomClass myProperties = new CustomClass();
        List<Parameter> paramList;

        public CustomClass Properties
        {
            get
            {
                return myProperties;
            }
        }

        public PropertyForm(List<Parameter> paramList)
        {
            this.paramList = paramList;
            InitializeComponent();
        }

        private void PropertyForm_Load(object sender, EventArgs e)
        {
            foreach (Parameter param in paramList)
            {
                if (param.Type != null)
                {
                    myProperties.Add(new CustomProperty(param.Name, param.Value, Type.GetType(param.Type), false, true));
                }
                else
                {
                    myProperties.Add(new CustomProperty(param.Name, Enum.Parse(param.ActualType, param.Value.ToString()), param.ActualType, false, true));
                }
            }
            propertyGrid1.SelectedObject = myProperties;
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
