using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace CodeGen
{
    public class QueryContents
    {
        // <copyright file="QueryContents" company="Dotnetcomp.com">
        // Copyright (c) 2016 All Rights Reserved
        // <author>Bernard Ho-Jim</author>
        // </copyright>

        public static void SearchFolder(string targetFolder, TreeNode parentNode, string searchTerm, bool matchCase, bool matchWholeWord)
        {
            // Take a snapshot of the file system.
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(targetFolder);

             // This method assumes that the application has discovery permissions
            // for all folders under the specified path.
            IEnumerable<System.IO.FileInfo> fileList = dir.GetFiles("*.*", System.IO.SearchOption.AllDirectories);

            // Search the contents of each file.
            // A regular expression created with the RegEx class
            // could be used instead of the Contains method.
            // queryMatchingFiles is an IEnumerable<string>.
            //where fileText.Contains(searchTerm)
            StringComparison stringComp;
            if (matchCase)
                stringComp = StringComparison.CurrentCulture;
            else
                stringComp = StringComparison.CurrentCultureIgnoreCase;

            // Return list of files where contents contain the search term or filename contains the search term
        var queryMatchingFiles =
                from file in fileList
                where file.Extension == ".tt"
                let fileContents = GetFileContents(file.FullName)
                let fileName = Path.GetFileName(file.FullName)
                where (fileContents.IndexOf(searchTerm, stringComp) >= 0 || fileName.Contains(searchTerm))
                select file.FullName;

            PopulateTreeView(parentNode, queryMatchingFiles, '\\');
        }

        private static void PopulateTreeView(TreeNode parentNode, IEnumerable<string> paths, char pathSeparator)
        {
            TreeNode lastNode = null;
            string subPathAgg;
            foreach (string path in paths)
            {
                subPathAgg = string.Empty;
                foreach (string subPath in path.Split(pathSeparator))
                {
                    subPathAgg += subPath + pathSeparator;
                    TreeNode[] nodes = parentNode.Nodes.Find(subPathAgg, true);
                    if (nodes.Length == 0)
                        if (lastNode == null)
                        {
                            lastNode = parentNode.Nodes.Add(subPathAgg, subPath);
                        }
                        else
                        {
                            lastNode = lastNode.Nodes.Add(subPathAgg, Path.GetFileNameWithoutExtension(subPath));
                            if (subPath == Path.GetFileName(path))
                            {
                                lastNode.Tag = path;
                                lastNode.ImageIndex = 1;
                                lastNode.SelectedImageIndex = 1;
                            }
                            else
                            {
                                lastNode.Tag = subPathAgg;
                            }
                        }
                    else
                    {
                        lastNode = nodes[0];
                    }
                }
            }
        }

            // Read the contents of the file.
        private static string GetFileContents(string name)
        {
            string fileContents = String.Empty;

            // If the file has been deleted since we took 
            // the snapshot, ignore it and return the empty string.
            if (System.IO.File.Exists(name))
            {
                fileContents = System.IO.File.ReadAllText(name);
            }
            return fileContents;
        }

    }
}
