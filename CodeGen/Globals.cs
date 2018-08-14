using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Configuration;
using System.IO;
using System.Linq;

namespace CodeGen
{
    public static class Globals
    {
        // <copyright file="Globals" company="Dotnetcomp.com">
        // Copyright (c) 2016 All Rights Reserved
        // <author>Bernard Ho-Jim</author>
        // </copyright>
      
        public static int InRange(this int x, int lo, int hi)
        {
            Debug.Assert(lo <= hi);
            return x < lo ? lo : (x > hi ? hi : x);
        }
        public static bool IsInRange(this int x, int lo, int hi)
        {
            return x >= lo && x <= hi;
        }
        public static Color HalfMix(this Color one, Color two)
        {
            return Color.FromArgb(
                (one.A + two.A) >> 1,
                (one.R + two.R) >> 1,
                (one.G + two.G) >> 1,
                (one.B + two.B) >> 1);
        }

        public static string GetTemplateRootFolder()
        {
            return ConfigurationManager.AppSettings["TemplateRootFolder"];
        }

        //public static string GetOutputFolder()
        //{
        //    return ConfigurationManager.AppSettings["OutputFolder"];
        //}

        public static string GetFolderUnderRoot(string path)
        {
            string prevPath = "";
            // Let's remove drive or network name from path
            if ((path.IndexOf(":") == 1) || (path.IndexOf("\\\\") == 0))
            {
                path = path.Substring(2);
            }
            if (path.IndexOf("\\") > 0)
                path = path.Substring(path.IndexOf("\\"));  

            while (true)
            {
                string temp = Path.GetDirectoryName(path);
                if (String.IsNullOrEmpty(temp) || temp == "\\")
                    break;
                prevPath = path;
                path = temp;
            }
            if (!String.IsNullOrEmpty(prevPath))
            {
                path = prevPath;
            }
            path = path.TrimStart('\\');
            return path;
        }

        //public static void MoveDirectory(string newDirPath, string oldDirPath)
        //{
        //    String directoryName = newDirPath;
        //    DirectoryInfo dirInfo = new DirectoryInfo(directoryName);
        //    if (!dirInfo.Exists)
        //        Directory.CreateDirectory(directoryName);

        //    List<String> myFiles = Directory
        //                       .GetFiles(oldDirPath, "*.*", SearchOption.AllDirectories).ToList();

        //    foreach (string file in myFiles)
        //    {
        //        FileInfo mFile = new FileInfo(file);
        //        // to remove name collisions
        //        if (new FileInfo(dirInfo + "\\" + mFile.Name).Exists == false)
        //        {
        //            mFile.MoveTo(dirInfo + "\\" + mFile.Name);
        //        }
        //    }
        //}
    }
}
