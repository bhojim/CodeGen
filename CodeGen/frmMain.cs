using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.CodeDom.Compiler;
using Microsoft.VisualStudio.TextTemplating;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using mshtml;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace CodeGen
{

    public partial class frmMain : Form
    {
        // <copyright file="TextEditorForm" company="Dotnetcomp.com">
        // Copyright (c) 2016 All Rights Reserved
        // <author>Bernard Ho-Jim</author>
        // </copyright>
      
        private const string SEARCH_KEY = "Search";
        private const string SEARCH_RESULTS = "Search Results";
        private string templateFileName;
        private bool sortByDateDescending = false;

        // Global variables
        private TreeNode _selectedNode;
        private TreeNode _selectedFolder;
        private Dictionary<string, string> dynamicTemplates;
        ITextEditorProperties _editorSettings;
        TextEditorControl templateEditor;
        TextEditorControl resultEditor;
        FindAndReplaceForm _findForm = new FindAndReplaceForm();
        FileSystemWatcher watcher;
        List<string> filenames;
        string searchTerm;
        bool matchCase;
        bool matchWholeWord;
        Dictionary<TextEditorControl, HighlightGroup> _highlightGroups = new Dictionary<TextEditorControl, HighlightGroup>();

        public frmMain()
        {
            InitializeComponent();
            //templateEditor = AddNewTextEditor("New file", fileTabs.TabPages["TabTemplate"]);
            templateEditor = AddNewTextEditor("Template");
            resultEditor = AddNewTextEditor("Result");
            this.KeyPreview = true;
        }

        private void TextEditorForm_Load(object sender, EventArgs e)
        {
            RefreshTreeView();
        }

        private TextEditorControl AddNewTextEditor(string title)
        {
            TextEditorControl editor = null;
            try
            {
                var tab = new TabPage(title);
                tab.Name = title;
                fileTabs.TabPages.Add(tab);
                //var tab = tabControl1.TabPages["TabTemplate"];
                editor = new TextEditorControl();
                editor.Name = "TextEditor";
                editor.Dock = System.Windows.Forms.DockStyle.Fill;
                editor.IsReadOnly = false;
                editor.Document.DocumentChanged +=
                    new DocumentEventHandler((sender, e) => { SetModifiedFlag(editor, true); });
                // When a tab page gets the focus, move the focus to the editor control
                // instead when it gets the Enter (focus) event. I use BeginInvoke 
                // because changing the focus directly in the Enter handler doesn't 
                // work.
                tab.Enter +=
                    new EventHandler((sender, e) =>
                    {
                        var page = ((TabPage)sender);
                        page.BeginInvoke(new Action<TabPage>(p => p.Controls[0].Focus()), page);
                    });
                tab.Controls.Add(editor);
                //fileTabs.Controls.Add(tab);

                if (_editorSettings == null)
                {
                    _editorSettings = editor.TextEditorProperties;
                    OnSettingsChanged();
                }
                else
                    editor.TextEditorProperties = _editorSettings;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message,"Unexpected error",MessageBoxButtons.OK ,MessageBoxIcon.Error);
            }
            return editor;
        }

        /// <summary>Show current settings on the Options menu</summary>
        /// <remarks>We don't have to sync settings between the editors because 
        /// they all share the same DefaultTextEditorProperties object.</remarks>
        private void OnSettingsChanged()
        {
            //menuShowSpacesTabs.Checked = _editorSettings.ShowSpaces;
            //menuShowNewlines.Checked = _editorSettings.ShowEOLMarker;
            //menuHighlightCurrentRow.Checked = _editorSettings.LineViewerStyle == LineViewerStyle.FullRow;
            //menuBracketMatchingStyle.Checked = _editorSettings.BracketMatchingStyle == BracketMatchingStyle.After;
            //menuEnableVirtualSpace.Checked = _editorSettings.AllowCaretBeyondEOL;
            //menuShowLineNumbers.Checked = _editorSettings.ShowLineNumbers;
        }

        /// <summary>Returns a list of all editor controls</summary>
        private IEnumerable<TextEditorControl> AllEditors
        {
            get
            {
                return from t in fileTabs.Controls.Cast<TabPage>()
                       from c in t.Controls.OfType<TextEditorControl>()
                       select c;
            }
        }

        /// <summary>Returns the currently displayed editor, or null if none are open</summary>
        private TextEditorControl ActiveEditor
        {
            get
            {
                if (fileTabs.TabPages.Count == 0) return null;
                return fileTabs.SelectedTab.Controls.OfType<TextEditorControl>().FirstOrDefault();
            }
        }

        /// <summary>Gets whether the file in the specified editor is modified.</summary>
        /// <remarks>TextEditorControl doesn't maintain its own internal modified 
        /// flag, so we use the '*' shown after the file name to represent the 
        /// modified state.</remarks>
        private bool IsModified(TextEditorControl editor)
        {
            // TextEditorControl doesn't seem to contain its own 'modified' flag, so 
            // instead we'll treat the "*" on the filename as the modified flag.
            return editor.Parent.Text.EndsWith("*");
        }

        private void SetModifiedFlag(TextEditorControl editor, bool flag)
        {
            if (IsModified(editor) != flag)
            {
                var p = editor.Parent;
                if (IsModified(editor))
                    p.Text = p.Text.Substring(0, p.Text.Length - 1);
                else
                    p.Text += "*";
            }
        }

        private void OpenFile(string filename, TabPage tab)
        {
            //var tab = tabControl1.TabPages["TabTemplate"];
            var editor = tab.Controls.Find("TextEditor", false).FirstOrDefault() as TextEditorControl;
            try
            {
                editor.LoadFile(filename);
                // Modified flag is set during loading because the document 
                // "changes" (from nothing to something). So, clear it again.
                SetModifiedFlag(editor, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name);
                //RemoveTextEditor(editor);
                return;
            }
            // ICSharpCode.TextEditor doesn't have any built-in code folding
            // strategies, so I've included a simple one. Apparently, the
            // foldings are not updated automatically, so in this demo the user
            // cannot add or remove folding regions after loading the file.
            //editor.Document.FoldingManager.FoldingStrategy = new RegionFoldingStrategy();
            //editor.Document.FoldingManager.UpdateFoldings(null, null);
        }

        private void OpenFiles(string[] fns)
        {
            // Close default untitled document if it is still empty
            if (fileTabs.TabPages.Count == 1
                && ActiveEditor.Document.TextLength == 0
                && string.IsNullOrEmpty(ActiveEditor.FileName))
                RemoveTextEditor(ActiveEditor);

            // Open file(s)
            foreach (string fn in fns)
            {
                while (!Utility.IsFileReady(fn))
                {
                    System.Threading.Thread.Sleep(100);
                }
                var editor = AddNewTextEditor(Path.GetFileName(fn));
                try
                {
                    editor.LoadFile(fn);
                    // Modified flag is set during loading because the document 
                    // "changes" (from nothing to something). So, clear it again.
                    SetModifiedFlag(editor, false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name);
                    RemoveTextEditor(editor);
                    return;
                }

                // ICSharpCode.TextEditor doesn't have any built-in code folding
                // strategies, so I've included a simple one. Apparently, the
                // foldings are not updated automatically, so in this demo the user
                // cannot add or remove folding regions after loading the file.
                //editor.Document.FoldingManager.FoldingStrategy = new RegionFoldingStrategy();
                //editor.Document.FoldingManager.UpdateFoldings(null, null);
            }
        }



        private void RefreshTreeView()
        {
            TreeNode aNode = null;

            string rootDir = Globals.GetTemplateRootFolder();
            treeView1.Nodes.Clear();
            //Add this drive as a root node
            aNode = treeView1.Nodes.Add(rootDir);
            aNode.Tag = rootDir;
            //Populate this root node
            PopulateTreeView(rootDir, treeView1.Nodes[0]);
            ExpandFirstLevel();

        }

        private void PopulateTreeView(string folderPath, TreeNode parentNode)
        {
            string folder = string.Empty;
            //string[] files;
            FileInfo[] fileinfos;
            DirectoryInfo dir;
            try
            {
                //Add the files to treeview
                if (sortByDateDescending )
                {
                    dir = new DirectoryInfo(folderPath);
                    fileinfos = dir.GetFiles("*.tt").OrderByDescending(p => p.CreationTime).ToArray();
                }
                else
                { 
                    //files = Directory.GetFiles(folderPath, "*.tt");
                    dir = new DirectoryInfo(folderPath);
                    fileinfos = dir.GetFiles("*.tt");
                }
                //if (files.Length != 0)
                if (fileinfos.Length != 0)
                    {
                    TreeNode fileNode = null;
                    //foreach (string file in files)
                    foreach (FileInfo file in fileinfos)
                        {
                        //fileNode = parentNode.Nodes.Add(System.IO.Path.GetFileName(file));
                        fileNode = parentNode.Nodes.Add(Path.GetFileNameWithoutExtension(file.Name));   // Do not display the file extension
                        //fileNode.Tag = file;
                        fileNode.Tag = file.FullName;
                        fileNode.ImageIndex = 1;
                        fileNode.SelectedImageIndex = 1;
                    }
                }
                //Add folders to treeview
                string[] folders = System.IO.Directory.GetDirectories(folderPath);
                if (folders.Length != 0)
                {
                    TreeNode folderNode = null;
                    string folderName = string.Empty;
                    foreach (string folder_loopVariable in folders)
                    {
                        folder = folder_loopVariable;
                        folderName = System.IO.Path.GetFileName(folder);
                        folderNode = parentNode.Nodes.Add(folderName);
                        folderNode.Tag = folder;
                        folderNode.ImageIndex = 0;
                        folderNode.SelectedImageIndex = 0;
                        PopulateTreeView(folder, folderNode);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                parentNode.Nodes.Add("Access Denied");
            }
        }

        private void ExpandFirstLevel()
        {
            foreach (TreeNode tn in treeView1.Nodes)
            {
                tn.Expand();
            }
        }

        /// <summary>
        /// Generate the code from templateFileName
        /// </summary>
        private void GenerateCode()
        {
            string temp = _selectedNode.Tag.ToString();
            // Ensure it's not a directory
            FileAttributes attr = File.GetAttributes(temp);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                MessageBox.Show("Please select a template", "Code Generation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                templateFileName = _selectedNode.Tag.ToString();

                OpenFile(templateFileName, fileTabs.TabPages["Template"]);

                CustomClass properties;
                Hashtable ParameterList = new Hashtable();
                bool bContinue = true;

                //templateFileName = txtTemplate.Text;

                // Parse for parameters
                System.Collections.Generic.List<Parameter> paramList = Utility.GetParameters(templateFileName);

                // Parse for "<script runat="template">.. </script> block
                //TODO: Store the content into a dictionary
                dynamicTemplates = Utility.GetDynamicTemplates(templateFileName, paramList);

                if (dynamicTemplates.Count > 0)
                {
                    var provider = CodeDomProvider.CreateProvider("CSharp");
                    var options = new CompilerParameters();
                    foreach (KeyValuePair<string, string> entry in dynamicTemplates)
                    {
                        var results = provider.CompileAssemblyFromSource(options, entry.Value);
                        var t = results.CompiledAssembly.GetType(entry.Key);
                        var param = paramList.Where(x => x.Type == entry.Key).FirstOrDefault();
                        if (param != null)
                        {
                            param.ActualType = t;
                            param.Type = null;
                        }
                    }

                }

                // If exist, open the Properties dialog to prompt for parameter values
                if (paramList.Count > 0)
                {
                    PropertyForm propForm = new PropertyForm(paramList);
                    bContinue = false;
                    if (propForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        bContinue = true;
                        // Retrieve values from the propertygrid
                        properties = propForm.Properties;
                        ParameterList = Utility.getParameterList(paramList, properties);
                    }
                    propForm.Dispose();
                }

                if (bContinue)
                {
                    GenerateCode(ParameterList);
                }
            }
        }


        private void GenerateCode(Hashtable ParameterList)
        {
            StringBuilder strErrors;
            CustomCmdLineHost host = new CustomCmdLineHost();

            host.TemplateFileValue = templateFileName;
            //host.TemplateFileValue = "";    // templateFileName;
            TextTemplatingSession session = new TextTemplatingSession();

            // Pass the parameters to the session dictionary
            foreach (DictionaryEntry entry in ParameterList)
            {
                session[(string)entry.Key] = entry.Value;
            }

            var sessionHost = (ITextTemplatingSessionHost)host;
            sessionHost.Session = session;

            filenames = new List<string>();
            // monitor changes in the containing folder. When files are created, an event is raised and the content of each file will be displayed on a tab.
            _selectedFolder = _selectedNode.Parent;
            Watch(_selectedFolder.Tag.ToString());

            //Read the text template.
            string input = File.ReadAllText(templateFileName);

            //Transform the text template.
            Engine engine = new Engine();
            string generatedContent = engine.ProcessTemplate(input, host);

            //string outputFileName = Path.GetFileNameWithoutExtension(templateFileName);
            // Save into _temp.tt in the output folder.
            //string outputFileName = Path.Combine(Globals.GetOutputFolder(), "_temp.tt");
            string outputFileName = Path.Combine(Path.GetTempPath(), "_temp.tt");

            //outputFileName = Path.Combine(Path.GetDirectoryName(templateFileName), outputFileName);
            //outputFileName = outputFileName + host.FileExtension;
            File.WriteAllText(outputFileName, generatedContent, host.FileEncoding);

            //AddNewTextEditor("Result");
            OpenFile(outputFileName, fileTabs.TabPages["Result"]);

            fileTabs.SelectedTab = fileTabs.TabPages["Result"];

            // Check if errors encountered during generation
            if (host.Errors.Count > 0)
            {
                strErrors = new StringBuilder();
                foreach (CompilerError error in host.Errors)
                {
                    strErrors.AppendLine(error.ToString());
                }
                if (fileTabs.TabPages["Errors"] == null)
                {
                    var editor = AddNewTextEditor("Errors");
                    editor.Document.TextContent = strErrors.ToString();
                }
                fileTabs.SelectedTab = fileTabs.TabPages["Errors"];
            }
            else
            {
                // some code template output multiple files. Therefore we want to display their contents.
                // filenames list has been populated by an event handler.
                // This list of filenames may contain duplicates. Let's remove them
                filenames = filenames.Distinct().ToList();
                if (filenames.Contains(outputFileName))
                {
                    filenames.Remove(outputFileName);
                }
                if (filenames.Count > 0)
                {
                    OpenFiles(filenames.ToArray());     // display them in different tabs
                }
            }

        }

        /// <summary>
        /// Display contents of a file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            Debug.WriteLine("AfterSelect");
            if (treeView1.SelectedNode != null && treeView1.SelectedNode.Tag != null)
            {
                _selectedNode = treeView1.SelectedNode;
                _selectedFolder = _selectedNode.Parent;
                Debug.WriteLine("AfterSelect selectedNode:" + _selectedNode + " _selectedFolder:" + _selectedFolder);
                
                string tag = treeView1.SelectedNode.Tag.ToString();
                StatusLabel.Text = tag;

                // Check whether it's a directory or file
                //FileAttributes attr = File.GetAttributes(tag);
                //if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                if (Directory.Exists(tag))
                {
                    // clear template area
                    //$$$
                }
                else
                {
                    // Check if previous template has been modified
                    var editor = templateEditor;
                    if (IsModified(editor))
                    {
                        var r = MessageBox.Show(string.Format("Save changes to {0}?", editor.FileName ?? "new file"),
                            "Save?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        if (r == DialogResult.Yes)
                            DoSave(editor);
                    }


                    // Open selected code template
                    templateFileName = tag;
                    OpenFile(templateFileName, fileTabs.TabPages["Template"]);
                    fileTabs.SelectedTab = fileTabs.TabPages["Template"];

                    // if clicking on a file under search results, highlight the search term
                    if (GetRootNode(_selectedNode).Text == SEARCH_RESULTS)
                    {
                        if (!string.IsNullOrEmpty(searchTerm))
                        {
                            // Highlight search word
                            SearchFor.Text = "Search for: " + searchTerm;        // display search term in status label
                            _findForm.Editor = ActiveEditor;
                            if (_findForm.Editor == null) return;
                            int count;
                            _findForm.HighlightAll(searchTerm, matchCase, matchWholeWord, out count);
                            _findForm.Editor.Refresh();
                        }
                    }

                }
            }
        }

        /// <summary>
        /// "Create Folder" context menu has been selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CreateFolder();
            //RefreshTreeView();
        }

        /// <summary>
        /// "Create template" context menu has been selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            CreateTemplate();

        }


        //private TreeNode FindTreeNode(TreeNodeCollection nodes, string templateName)
        //{
        //    TreeNode result = null;
        //    foreach (TreeNode node in nodes)
        //    {
        //        if (node.Text.Contains(templateName))
        //        {
        //            result = node;
        //            break;
        //        }
        //        if (node.Nodes.Count > 0)
        //        {
        //            result = FindTreeNode(node.Nodes, templateName);
        //            if (result != null)
        //            {
        //                break;
        //            }
        //        }
        //    }
        //    return result;
        //}

        /// <summary>
        /// Attach contextMenuStrip1 onto a tree node that is a folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            _selectedNode = e.Node;
            SearchFor.Text = string.Empty;

            // check if it's a root node and not a Search Results node
            if (e.Node.Parent == null && GetRootNode(_selectedNode).Text != SEARCH_RESULTS)
            {
                // Display context menu
                e.Node.ContextMenuStrip = contextMenuStrip1;
            }
            else
            {
                string nodeTag = (string)e.Node.Tag;
                if (nodeTag != null)
                {
                    //FileAttributes attr = File.GetAttributes(nodeTag);
                    //if (attr.HasFlag(FileAttributes.Directory))
                    // Check whether it's a directory. The following code is simpler. Is it identical than the two lines above?
                    if (Directory.Exists(nodeTag))
                    {
                        // Arrive here if right click on a directory
                        // Display context menu
                        if (GetRootNode(_selectedNode).Text != SEARCH_RESULTS)
                        { 
                            e.Node.ContextMenuStrip = contextMenuStrip1;
                            _selectedNode = e.Node;
                        }
                    }
                    else
                    {
                        // It's a file. Allow Generate 
                        e.Node.ContextMenuStrip = ctxFileMenu;
                        _selectedNode = e.Node;

                        // if clicking on a file under search results, highlight the search term
                        if (GetRootNode(_selectedNode).Text == SEARCH_RESULTS)
                        {
                            if (!string.IsNullOrEmpty(searchTerm))
                            {
                                // Highlight search word
                                SearchFor.Text = "Search for: " + searchTerm;        // display search term in status label
                                //_findForm.Editor = ActiveEditor;
                                //if (_findForm.Editor == null) return;
                                //int count;
                                //_findForm.HighlightAll(searchTerm, false, false, out count);
                            }
                        }

                    }
                }
            }
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            string temp = _selectedNode.Tag.ToString();
            // Ensure it's not a directory
            FileAttributes attr = File.GetAttributes(temp);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                MessageBox.Show("Please select a template", "Code Generation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Save the template
                // What's the name of the template?
                templateFileName = _selectedNode.Tag.ToString();
                if (!string.IsNullOrEmpty(templateFileName))
                {
                    try
                    {
                        templateEditor.FileName = templateFileName;
                        templateEditor.SaveFile(templateEditor.FileName);
                        SetModifiedFlag(templateEditor, false);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.GetType().Name);
                    }
                }
            }

        }

        private bool DoSaveAs(TextEditorControl editor)
        {
            string temp = _selectedNode.Tag.ToString();
            // Ensure it's not a directory
            FileAttributes attr = File.GetAttributes(temp);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                MessageBox.Show("Please select a template", "Code Generation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                saveFileDialog.FileName = editor.FileName;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        editor.SaveFile(saveFileDialog.FileName);
                        editor.Parent.Text = Path.GetFileName(editor.FileName);
                        SetModifiedFlag(editor, false);

                        // The syntax highlighting strategy doesn't change
                        // automatically, so do it manually.
                        editor.Document.HighlightingStrategy =
                            HighlightingStrategyFactory.CreateHighlightingStrategyForFile(editor.FileName);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.GetType().Name);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Call a function to create a new folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdNewFolder_Click(object sender, EventArgs e)
        {
            // Create new folder
            CreateFolder();
            RefreshTreeView();
        }

        /// <summary>
        /// Call a function to create a new template
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdNewTemplate_Click(object sender, EventArgs e)
        {
            // Create new template
            CreateTemplate();
        }

        /// <summary>
        /// Prompt for the folder name, create the folder and add a node to the treeview.
        /// </summary>
        private void CreateFolder()
        {
            string subDir;
            // Create a folder
            frmCreateFolder frm = new frmCreateFolder();
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                subDir = frm.TextEntered;

                // Create a reference to the selected directory
                string selFolder = _selectedNode.Tag.ToString();
                DirectoryInfo di = new DirectoryInfo(selFolder);

                // Create a subdirectory in the selected directory
                DirectoryInfo dis = di.CreateSubdirectory(subDir);

                TreeNode newNode = new TreeNode(subDir);
                string newPath = Path.Combine(selFolder, subDir);
                newNode.Tag = newPath;
                _selectedNode.Nodes.Add(newNode);
                //this is new. Let's see if it works
                _selectedNode = newNode;
                this.treeView1.SelectedNode = _selectedNode;
            }
        }

        private void CreateTemplate()
        {
            string templateName;
            string newPath;

            // Create a template
            frmTemplateName frm = new frmTemplateName();
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                templateName = frm.TextEntered;
                // Create a file under the selected directory
                string selFolder = _selectedNode.Tag.ToString();
                newPath = Path.Combine(selFolder, templateName + ".tt");
                FileStream fs = File.Create(newPath);
                fs.Dispose();

                TreeNode newNode = new TreeNode(templateName);
                newNode.Tag = newPath;
                newNode.ImageIndex = 1;
                newNode.SelectedImageIndex = 1;
                _selectedNode.Nodes.Add(newNode);
                _selectedNode.Expand();

                _selectedFolder = _selectedNode;
                _selectedNode = newNode;
                // Set the focus to the newly created node. This will trigger an AfterSelect event which will open the template for editing.
                this.treeView1.SelectedNode = _selectedNode;
            }
        }

        private void DeleteFolder()
        {
            // Create a reference to the selected directory
            string selFolder = _selectedNode.Tag.ToString();

            if (MessageBox.Show("Are you sure you want to delete this folder: " + selFolder, "Delete a folder", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(selFolder))
                {
                    Directory.Delete(selFolder);
                }
                treeView1.Nodes.Remove(_selectedNode);
            }
        }

        /// <summary>
        /// Search for text in templates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdSearch_Click(object sender, EventArgs e)
        {
            TreeNode resultsNode;
            string targetFolder;

            // Pass the currently selected folder to the search form
            string folderPath = Globals.GetFolderUnderRoot(_selectedNode.Tag.ToString());
            string folderName = new DirectoryInfo(folderPath).Name;
            frmSearch frm = new frmSearch(folderName);
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                targetFolder = frm.TargetFolder;
                searchTerm = frm.SearchTerm;
                matchCase = frm.MatchCase;
                matchWholeWord = frm.MatchWholeWord;

                TreeNode[] nodes = treeView1.Nodes.Find(SEARCH_KEY, false);
                if (nodes.Length > 0)
                {
                    // remove it
                    resultsNode = nodes[0];
                    treeView1.Nodes.Remove(resultsNode);
                }

                resultsNode = treeView1.Nodes.Add(SEARCH_KEY, SEARCH_RESULTS);
                QueryContents.SearchFolder(targetFolder, resultsNode, searchTerm, matchCase, matchWholeWord);
                
                // Expand search results
                resultsNode.ExpandAll();
            }

        }

        /// <summary>
        /// ItemDrag: This event is raised from the source TreeView control as soon as the user starts to drag the tree node.
        /// When this occurs, call the DoDragDrop method to initiate the drag-and-drop procedure.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            // Disallow drag and drop from Search Results
            //// Disallow drag and drop if it's a folder
            var node = (TreeNode)e.Item;
            //if (GetRootNode(node).Text != SEARCH_RESULTS && !IsDirectory(node))
            if (GetRootNode(node).Text != SEARCH_RESULTS)
                {
                    DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        /// <summary>
        /// DragEnter: After you initiate the drag-and-drop operation, you must handle the DragEnter event in the destination TreeView control.
        /// This event occurs when the user drags the TreeNode object from the source TreeView control to a point in the bounds of the destination TreeView control. 
        /// The DragEnter event enables the destination TreeView control to specify whether the drag operation is valid for this control.
        /// In this example, only the move operation is allowed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        /// <summary>
        /// This is the final operation
        /// DragDrop: The last event to handle is the DragDrop event of the destination TreeView control.
        /// This event occurs when the TreeNode object that is dragged has been dropped on the destination TreeView control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            string newPath = string.Empty;
            string sourcePath;
            string destPath;

            // Retrieve the client coordinates of the drop location.
            Point targetPoint = treeView1.PointToClient(new Point(e.X, e.Y));

            // Retrieve the node at the drop location.
            TreeNode targetNode = treeView1.GetNodeAt(targetPoint);

            // Retrieve the node that was dragged.
            TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));

            // Confirm that the node at the drop location is not 
            // the dragged node and that target node isn't null
            // (for example if you drag outside the control)
            if (!draggedNode.Equals(targetNode) && targetNode != null)
            {
                sourcePath = draggedNode.Tag.ToString();
                destPath = targetNode.Tag.ToString();

                // If dragged node is a directory and target node is not a directory, exit
                if (Directory.Exists(sourcePath) && !Directory.Exists(destPath))
                    return;

                // Remove the node from its current 
                // location and add it to the node at the drop location.
                draggedNode.Remove();
                // Are we dragging a file or folder?
                if (Directory.Exists(sourcePath))     // Check if dragging a directory
                {
                    // Arrive here if dragging a directory
                    string folderName = new DirectoryInfo(sourcePath).Name;
                    newPath = Path.Combine(destPath, folderName);

                    // Move the directory
                    Directory.Move(sourcePath, newPath);

                    //TODO: Reload folders and files under the target node
                    TreeNode folderNode = targetNode.Nodes.Add(folderName);
                    folderNode.Tag = newPath;
                    folderNode.ImageIndex = 0;
                    folderNode.SelectedImageIndex = 0;
                    PopulateTreeView(newPath, folderNode);
                    targetNode.Expand();
                }
                else
                {
                    // Move the file
                    destPath = targetNode.Tag.ToString();
                    newPath = Path.Combine(destPath, Path.GetFileName(sourcePath));
                    File.Move(sourcePath, newPath);

                    // update the node's tag containing the actual file path
                    draggedNode.Tag = newPath;
                    targetNode.Nodes.Add(draggedNode);

                    // Expand the node at the location 
                    // to show the dropped node.
                    targetNode.Expand();
                }

            }

        }

        /// <summary>
        /// Validate the treeNOde as the cursor passes over it by handling the DragOver event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_DragOver(object sender, DragEventArgs e)
        {
            // Check that there is a TreeNode being dragged 
            if ((e.Data.GetDataPresent("System.Windows.Forms.TreeNode", true) == false))
            {
                return;
            }
            Point pt = ((TreeView)(sender)).PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeView1.GetNodeAt(pt);

            // See if the targetNode is currently selected, 
            // if so no need to validate again
            if (!(treeView1.SelectedNode == targetNode))
            {
                // Select the node currently under the cursor
                treeView1.SelectedNode = targetNode;
                // Check that the selected node is not the dropNode and
                // also that it is not a child of the dropNode and 
                // therefore an invalid target
                TreeNode dropNode = ((TreeNode)(e.Data.GetData("System.Windows.Forms.TreeNode")));

                if (!IsDirectory(targetNode))
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }

                do
                {
                    if ((targetNode == dropNode))
                    {
                        e.Effect = DragDropEffects.None;
                        return;
                    }
                    targetNode = targetNode.Parent;
                }
                while ((targetNode == null));

                // Currently selected node is a suitable target
                e.Effect = DragDropEffects.Move;
            }
        }

        /// <summary>
        /// Function to check if node is a directory or file
        /// </summary>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        private bool IsDirectory(TreeNode targetNode)
        {
            string tag = targetNode.Tag.ToString();
            // Check whether it's a directory or file
            FileAttributes attr = File.GetAttributes(tag);
            bool retVal = ((attr & FileAttributes.Directory) == FileAttributes.Directory);
            return retVal;
        }

        private void generateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                RemoveTabs();
                GenerateCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unexpected error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void menuFileClose_Click(object sender, EventArgs e)
        {
            // TODO
        }

        private bool DoSave(TextEditorControl editor)
        {
            if (string.IsNullOrEmpty(editor.FileName))
                return DoSaveAs(editor);
            else
            {
                try
                {
                    editor.SaveFile(editor.FileName);
                    SetModifiedFlag(editor, false);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name);
                    return false;
                }
            }
        }

        private void menuFileSaveAs_Click(object sender, EventArgs e)
        {
            var editor = templateEditor;    // fileTabs.TabPages["TabTemplate"].Controls.Find("TextEditor", false).FirstOrDefault() as TextEditorControl;
            if (editor != null)
                DoSaveAs(editor);
        }

        private bool HaveSelection()
        {
            var editor = ActiveEditor;
            return editor != null &&
                editor.ActiveTextAreaControl.TextArea.SelectionManager.HasSomethingSelected;
        }

        #region Code related to Edit menu

        /// <summary>Performs an action encapsulated in IEditAction.</summary>
        /// <remarks>
        /// There is an implementation of IEditAction for every action that 
        /// the user can invoke using a shortcut key (arrow keys, Ctrl+X, etc.)
        /// The editor control doesn't provide a public funciton to perform one
        /// of these actions directly, so I wrote DoEditAction() based on the
        /// code in TextArea.ExecuteDialogKey(). You can call ExecuteDialogKey
        /// directly, but it is more fragile because it takes a Keys value (e.g.
        /// Keys.Left) instead of the action to perform.
        /// <para/>
        /// Clipboard commands could also be done by calling methods in
        /// editor.ActiveTextAreaControl.TextArea.ClipboardHandler.
        /// </remarks>
        private void DoEditAction(TextEditorControl editor, ICSharpCode.TextEditor.Actions.IEditAction action)
        {
            if (editor != null && action != null)
            {
                var area = editor.ActiveTextAreaControl.TextArea;
                editor.BeginUpdate();
                try
                {
                    lock (editor.Document)
                    {
                        action.Execute(area);
                        if (area.SelectionManager.HasSomethingSelected && area.AutoClearSelection /*&& caretchanged*/)
                        {
                            if (area.Document.TextEditorProperties.DocumentSelectionMode == DocumentSelectionMode.Normal)
                            {
                                area.SelectionManager.ClearSelection();
                            }
                        }
                    }
                }
                finally
                {
                    editor.EndUpdate();
                    area.Caret.UpdateCaretPosition();
                }
            }
        }

        private void menuEditCopy_Click(object sender, EventArgs e)
        {
            if (HaveSelection())
                DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.Copy());
        }

        private void menuEditPaste_Click(object sender, EventArgs e)
        {
            DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.Paste());
        }

        private void menuEditDelete_Click(object sender, EventArgs e)
        {
            if (HaveSelection())
                DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.Delete());
        }

        private void TextEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Ask user to save changes
            foreach (var editor in AllEditors)
            {
                if (IsModified(editor))
                {
                    var r = MessageBox.Show(string.Format("Save changes to {0}?", editor.FileName ?? "new file"),
                        "Save?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (r == DialogResult.Cancel)
                        e.Cancel = true;
                    else if (r == DialogResult.Yes)
                        if (!DoSave(editor))
                            e.Cancel = true;
                }
            }
        }

        private void menuEditFind_Click(object sender, EventArgs e)
        {
            TextEditorControl editor = ActiveEditor;
            if (editor == null) return;
            _findForm.ShowFor(editor, false);
        }

        private void menuEditReplace_Click(object sender, EventArgs e)
        {
            TextEditorControl editor = ActiveEditor;
            if (editor == null) return;
            _findForm.ShowFor(editor, true);
        }

        private void menuFindAgain_Click(object sender, EventArgs e)
        {
            _findForm.FindNext(true, false,
                string.Format("Search text «{0}» not found.", _findForm.LookFor));
        }

        private void menuFindAgainReverse_Click(object sender, EventArgs e)
        {
            _findForm.FindNext(true, true,
                string.Format("Search text «{0}» not found.", _findForm.LookFor));
        }

        private void menuToggleBookmark_Click(object sender, EventArgs e)
        {
            var editor = ActiveEditor;
            if (editor != null)
            {
                DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.ToggleBookmark());
                editor.IsIconBarVisible = editor.Document.BookmarkManager.Marks.Count > 0;
            }
        }

        private void menuGoToNextBookmark_Click(object sender, EventArgs e)
        {
            DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.GotoNextBookmark
                (bookmark => true));
        }

        private void menuGoToPrevBookmark_Click(object sender, EventArgs e)
        {
            DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.GotoPrevBookmark
                (bookmark => true));
        }

        private void RemoveTextEditor(TextEditorControl editor)
        {
            ((TabControl)editor.Parent.Parent).Controls.Remove(editor.Parent);
        }

        private void menuFileSave_Click(object sender, EventArgs e)
        {
            TextEditorControl editor = ActiveEditor;
            if (editor != null)
                DoSave(editor);
        }

        private void Watch(string folderPath)
        {
            watcher = new FileSystemWatcher();
            watcher.Path = folderPath;

            //watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
            //watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*.*";
            //watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            string filePath = e.FullPath;
            string filename = Path.GetFileName(filePath);
            if (Path.GetExtension(filePath) != "tt")      // Exclude .tt extension
            {
                filenames.Add(filePath);
            }
        }

        private void RemoveTabs()
        {
            if (fileTabs.TabPages.Count > 2)
            {
                foreach (TabPage tab in fileTabs.TabPages)
                {
                    if (tab.Name != "Template" && tab.Name != "Result")
                    {
                        fileTabs.TabPages.Remove(tab);
                    }
                }
            }
        }

        #endregion

        private void menuInsertParam_Click(object sender, EventArgs e)
        {
            // Prompt for parameter name and initial value
            frmNewParameter frm = new frmNewParameter();
            if (frm.ShowDialog() == DialogResult.OK)
            {
                string paramString = frm.ParameterString;
                Clipboard.SetText(paramString);
                DoEditAction(ActiveEditor, new ICSharpCode.TextEditor.Actions.Paste());

                //$$$ Do Text replacement
            } 
        }

        private TreeNode GetRootNode(TreeNode node)
        {
            TreeNode parentNode = null;
            while (node != null)
            {
                parentNode = node;
                node = node.Parent;
            }
            return parentNode;
        }

        private void menuDeleteFile_Click(object sender, EventArgs e)
        {
            // Delete file
            templateFileName = _selectedNode.Tag.ToString();
            if (MessageBox.Show("Are you sure you want to delete this file: " + templateFileName,"Delete a file",  MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
            {
                File.Delete(templateFileName);
                treeView1.Nodes.Remove(_selectedNode);
            }
        }

        private void menuRenameFile_Click(object sender, EventArgs e)
        {
            // Rename file
            treeView1.SelectedNode = _selectedNode;
            treeView1.LabelEdit = true;
            if(!_selectedNode.IsEditing)
            {
                _selectedNode.BeginEdit();
            }
        }

        /// <summary>
        /// Event triggered after filename or directory name was changed on the treeview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            string newPath = null;
            if (e.Label != null)
            {
                if (e.Label.Length > 0)
                {
                    if (e.Label.IndexOfAny(new char[] { '@', ',', '!' }) == -1)
                    {
                        // Stop editing without canceling the label change.
                        e.Node.EndEdit(false);
                        string oldPath = _selectedNode.Tag.ToString();
                        // Are we modifying a directory or filename?
                        FileAttributes attr = File.GetAttributes(oldPath);
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            string selFolder = _selectedNode.Parent.Tag.ToString();
                            newPath = Path.Combine(selFolder, e.Label);
                            if (Directory.Exists(newPath))
                            {
                                MessageBox.Show("The new directory already exists", "Renaming a Directory", MessageBoxButtons.OK);
                            }
                            Directory.Move(oldPath, newPath);
                            RefreshTreeView();
                        }
                        else
                        {
                            // Ensure the filename ends with .tt. If not append .tt
                            //$$$
                            string selFolder = _selectedNode.Parent.Tag.ToString();
                            newPath = Path.Combine(selFolder, e.Label);
                            if (Path.GetExtension(newPath) != ".tt")
                            {
                                newPath = newPath + ".tt";
                            }
                            if (oldPath != newPath)
                            {
                                if (File.Exists(newPath))
                                {
                                    System.IO.File.Delete(newPath);
                                }
                                File.Move(oldPath, newPath);
                            }
                        }
                        _selectedNode.Tag = newPath;
                    }
                    else
                    {
                        /* Cancel the label edit action, inform the user, and 
                           place the node in edit mode again. */
                        e.CancelEdit = true;
                        MessageBox.Show("Invalid tree node label.\n" +
                           "The invalid characters are: '@', ',', '!'",
                           "Node Label Edit");
                        e.Node.BeginEdit();
                    }
                }
                else
                {
                    /* Cancel the label edit action, inform the user, and 
                       place the node in edit mode again. */
                    e.CancelEdit = true;
                    MessageBox.Show("Invalid tree node label.\nThe label cannot be blank",
                       "Node Label Edit");
                    e.Node.BeginEdit();
                }
            }

        }

        private void cmdGenerate_Click(object sender, EventArgs e)
        {
            RemoveTabs();
            GenerateCode();
        }

        private void addWebBrowserControl()
        {

        }

        private void aboutCodeGenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmAbout frm = new frmAbout();
            frm.ShowDialog();
        }

        private void frmMain_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                string temp = _selectedNode.Tag.ToString();
                treeView1.SelectedNode = _selectedNode;
                treeView1.LabelEdit = true;
                if (!_selectedNode.IsEditing)
                {
                    _selectedNode.BeginEdit();
                }
            }
        }

        private void deleteAFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteFolder();
        }
    }
}
