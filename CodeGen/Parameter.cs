using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGen
{
    public class Parameter
    {
        // <copyright file="Parameter" company="Dotnetcomp.com">
        // Copyright (c) 2016 All Rights Reserved
        // <author>Bernard Ho-Jim</author>
        // </copyright>
      
        public string Type { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
        public Type ActualType { get; set; }
    }
}
