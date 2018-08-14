using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGen
{
    public class ListItem
    {
        // <copyright file="ListItem" company="Dotnetcomp.com">
        // Copyright (c) 2016 All Rights Reserved
        // <author>Bernard Ho-Jim</author>
        // </copyright>
      
        public Object ItemData { get; set; }
        public string Text { get; set; }

        public ListItem(Object NewValue, string NewDescription)
        {
            ItemData = NewValue;
            Text = NewDescription;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
