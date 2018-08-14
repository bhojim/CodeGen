using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace CodeGen
{
    public class NativeTreeView : System.Windows.Forms.TreeView
    {
        // <copyright file="NativeTreeView" company="Dotnetcomp.com">
        // Copyright (c) 2016 All Rights Reserved
        // <author>Bernard Ho-Jim</author>
        // </copyright>
      
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private extern static int SetWindowTheme(IntPtr hWnd, string pszSubAppName,
                                                string pszSubIdList);

        protected override void CreateHandle()
        {
            base.CreateHandle();
            SetWindowTheme(this.Handle, "explorer", null);
        }
    }

}
