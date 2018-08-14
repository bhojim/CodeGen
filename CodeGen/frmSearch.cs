using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace CodeGen
{
    public partial class frmSearch : Form
    {
        // <copyright file="frmSearch" company="Dotnetcomp.com">
        // Copyright (c) 2016 All Rights Reserved
        // <author>Bernard Ho-Jim</author>
        // </copyright>
      
        public string TargetFolder { get; set; }
        public string SearchTerm { get; set; }
        public bool MatchCase { get; set; }
        public bool MatchWholeWord { get; set; }

        private string rootFolder;
        private string _selectedFolder;

        public frmSearch(string selectedFolder)
        {
            _selectedFolder = selectedFolder;
            InitializeComponent();
        }

        private void frmSearch_Load(object sender, EventArgs e)
        {
            string folderName = string.Empty;
            rootFolder = Globals.GetTemplateRootFolder();
            // Check if entry exists.
            // If so, check if root folder exists
            // if it does not, 
            // 1. create one under the assembly folder and name it MyTemplates
            // 2. Update app.config

            cboFolders.Items.Add(new ListItem(rootFolder, "<any folder>"));

/*
            //string[] folders = System.IO.Directory.GetDirectories(rootFolder);
            DirectoryInfo dinfo = new DirectoryInfo(rootFolder);
            DirectoryInfo[] directorys = dinfo.GetDirectories();
            //foreach (string folder in folders)
            foreach (DirectoryInfo directory in directorys)
            {
                //folderName = System.IO.Path.GetFileName(folder);
                //cboFolders.Items.Add(new ListItem(folder, folderName));
                cboFolders.Items.Add(new ListItem(directory.FullName, directory.Name));
            }
*/
            ScanFolder(String.Empty, rootFolder);

            if (string.IsNullOrEmpty(_selectedFolder))
            {
                cboFolders.SelectedIndex = 0;
            }
            else
            {
                cboFolders.SelectedIndex = cboFolders.FindStringExact(_selectedFolder);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            // Set form properties before closing form so they can be retrieved from caller

            ListItem listItem;
            SearchTerm = txtSearch.Text;

            if (cboFolders.SelectedIndex <= 0)
            {
                TargetFolder = rootFolder;
            }
            else
            {
                listItem = (ListItem)cboFolders.SelectedItem;
                TargetFolder = listItem.ItemData.ToString();
            }
            MatchCase = chkMatchCase.Checked;
            MatchWholeWord = chkMatchWholeWord.Checked;
   
            // Close form
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ScanFolder(String prefix, String path)
        {
            foreach (var dir in new DirectoryInfo(path).GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                //cboFolders.Items.Add(prefix + dir.Name);
                cboFolders.Items.Add(new ListItem(dir.FullName, prefix + dir.Name));
                ScanFolder(prefix + "---", dir.FullName);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            btnSearch.Enabled = !string.IsNullOrWhiteSpace(txtSearch.Text);
        }
    }
}
