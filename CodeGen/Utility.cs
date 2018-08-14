using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

namespace CodeGen
{
    class Utility
    {
        // <copyright file="Utility" company="Dotnetcomp.com">
        // Copyright (c) 2016 All Rights Reserved
        // <author>Bernard Ho-Jim</author>
        // </copyright>
      
        static string pattern = @"
(?:<)(?<Tag>[^\s/>]+)       # Extract the tag name.
(?![/>])                    # Stop if /> is found
                     # -- Extract Attributes Key Value Pairs  --
 
((?:\s+)             # One to many spaces start the attribute
 (?<Key>[^=]+)       # Name/key of the attribute
 (?:=)               # Equals sign needs to be matched, but not captured.
 
(?([\x22\x27])              # If quotes are found
  (?:[\x22\x27])
  (?<Value>[^\x22\x27]+)    # Place the value into named Capture
  (?:[\x22\x27])
 |                          # Else no quotes
   (?<Value>[^\s/>]*)       # Place the value into named Capture
 )
)+                  # -- One to many attributes found!";

        private static Parameter GetParameter(string node)
        {
            object value = null;

            var attributes = (from Match mt in Regex.Matches(node, pattern, RegexOptions.IgnorePatternWhitespace)
                              select new
                              {
                                  Name = mt.Groups["Tag"],
                                  Attrs = (from cpKey in mt.Groups["Key"].Captures.Cast<Capture>().Select((a, i) => new { a.Value, i })
                                           join cpValue in mt.Groups["Value"].Captures.Cast<Capture>().Select((b, i) => new { b.Value, i }) on cpKey.i equals cpValue.i
                                           select new KeyValuePair<string, string>(cpKey.Value, cpValue.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                              }).First().Attrs;

            string name = attributes["name"];
            string type = attributes["type"];
            if (attributes["value"] != null)
            {
                value = attributes["value"];
                if (Type.GetType(type)== null)
                {
                    //value = Convert.ToInt32(value);
                    //value = value.ToString();
                }
                else if (Type.GetType(type).Equals(typeof(System.Int32)))
                {
                    value = Convert.ToInt32(value);
                }
                else if (Type.GetType(type).Equals(typeof(System.Boolean)))
                {
                    value = Convert.ToBoolean(value);
                }
                else
                {
                    value = value.ToString();
                }

            }
            Parameter param = new Parameter
            {
                Name = name,
                Type = type,
                Value = value
            };
            return param;
        }

       

        /// <summary>
        /// Return a list of parameters
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<Parameter> GetParameters(string filePath)
        {
            // example:  <#@ parameter type="System.Int32" name="TimesToRepeat" #>
            string line;
            Parameter parameter;
            List<Parameter> paramList = new List<Parameter>();

            // Read the file
            System.IO.StreamReader file = new System.IO.StreamReader(filePath);
            while((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("<#@ parameter"))
                {
                    //line = line.Replace("<#@ parameter", "");
                    //line = line.Replace("#>", "");
                    line = line.Replace("parameter", "");
                    parameter = GetParameter(line);
                    paramList.Add(parameter);
                }
            }
            file.Close();
            return paramList;
        }

        /// <summary>
        /// Returns entry in Properties matching given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static CustomProperty GetProperty(CustomClass properties, string name)
        {
            CustomProperty retval = null;
            foreach (CustomProperty prop in properties)
            {
                if (prop.Name == name)
                {
                    retval = prop;
                }
            }
            return retval;
        }

        /// <summary>
        /// Populate a hash table from list of parameters and properties from property grid
        /// </summary>
        /// <param name="paramList"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static Hashtable getParameterList(List<Parameter> paramList, CustomClass properties)
        {
            CustomProperty prop;
            Hashtable tbl = new Hashtable();
            foreach (Parameter param in paramList)
            {
                prop = GetProperty(properties, param.Name);
                tbl.Add(param.Name, prop.Value);
            }
            return tbl;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetDynamicTemplates(string filePath, List<Parameter> paramList)
        {
            string codeBlock;
            string startTag = @"<#+";
            string endTag = "#>";
            Dictionary<string, string> templates = new Dictionary<string, string>();

            string readText = File.ReadAllText(filePath);
            if (readText.Contains(startTag))
            {
                codeBlock = ExtractString(readText, startTag, endTag);
                if (codeBlock != "")
                {
                    // loop through the paramter list and find which parameter this code block refers to.
                    foreach (Parameter param in paramList)
                    {
                        if (codeBlock.Contains(param.Type))
                        {
                            templates.Add(param.Type, codeBlock);
                        }
                    }
                }
            }
            return templates;
        }

        public static string ExtractString(string s, string startTag, string endTag)
        {
            // You should check for errors in real-world code, omitted for brevity
            //var startTag = "<" + tag + ">";
            if (s.IndexOf(startTag) != -1)
            {
                int startIndex = s.IndexOf(startTag) + startTag.Length;
                int endIndex = s.IndexOf(endTag, startIndex);
                if (endIndex != -1)
                {
                    return s.Substring(startIndex, endIndex - startIndex);
                }
                else return "";
            }
            else return "";
        }

        public static string RemoveUserDefinedScripts(string str)
        {
            string startTag = @"<script runat=""template"">";
            string endTag = "</script>";
            string strToRemove = str.Substring(str.IndexOf(startTag), str.IndexOf(endTag) - str.IndexOf(startTag) + endTag.Length);
            str = str.Replace(strToRemove, "");
            return str;
        }

        public static string RemoveSpecial(string str)
        {
            string strToRemove;
            string startTag = @"<script runat=""template"">";
            string endTag = "</script>";
            if (str.IndexOf(startTag) != -1)
            {
                strToRemove = str.Substring(str.IndexOf(startTag), startTag.Length);
                str = str.Replace(strToRemove, "");
            }
            if (str.IndexOf(endTag) != -1)
            {
                strToRemove = str.Substring(str.IndexOf(endTag), endTag.Length);
                str = str.Replace(strToRemove, "");
            }

            return str;
        }

        public static bool IsFileReady(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return inputStream.Length > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

