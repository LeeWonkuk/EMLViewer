using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid;
using DevExpress.XtraRichEdit;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using DevExpress.XtraTreeList.Nodes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using static DevExpress.XtraEditors.Mask.MaskSettings;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using MimeKit;
using DevExpress.Utils.CommonDialogs;
using DevExpress.XtraEditors;
using DevExpress.XtraExport.Helpers;
using DevExpress.Utils;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraRichEdit.Model;
using System.Collections;

namespace EmlViewer
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        private List<string> emlFiles; // List of EML files
        private DataTable dataTable; // Data table to hold the mail list
        private string currentFolderPath;

        public Form1()
        {
            InitializeComponent();
            flowLayoutPanel1.Visible = false;
            mailListGrid.UseEmbeddedNavigator = true;

            //LoadMailList(@"C:\Users\Cowintech\Desktop\MailSave\Backup_Mail");
        }

        private void treeList1_AfterFocusNode(object sender, DevExpress.XtraTreeList.NodeEventArgs e)
        {
            //LoadFolderTree(treeList1);
        }

        private void mailListView_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            try
            {
                if (e.FocusedRowHandle >= 0)
                {
                    GridColumn column = mailListView.FocusedColumn;
                    if (column.Caption == "Selection")
                    {
                        return;
                    }

                    if (!splashScreenManager1.IsSplashFormVisible) // Wait Form이 이미 실행중인지 확인
                        splashScreenManager1.ShowWaitForm(); // Wait Form 열기

                    DataRowView selRow = (DataRowView)(((GridView)mailListGrid.MainView).GetRow(e.FocusedRowHandle));
                    string path = selRow["Path"].ToString();
                    richEditControl1.LoadDocument(path, DocumentFormat.Mht);

                    var mail = LoadMail(path);
                    flowLayoutPanel1.Controls.Clear();
                    if (mail.Attachments == null || mail.Attachments.Count == 0)
                    {
                        flowLayoutPanel1.Visible = false;
                    }
                    else
                    {
                        flowLayoutPanel1.Visible = true;
                        foreach (var attachment in mail.Attachments)
                        {
                            var attachmentButton = new SimpleButton
                            {
                                AutoSize = true,
                                //Width = 90,
                                //Height = 60,
                                Text = attachment.FileName, // Display file name as button text
                                ImageOptions = { Image = GetAttachmentIcon(attachment.FileName), ImageToTextAlignment = ImageAlignToText.TopCenter }, // Set button image
                                Tag = attachment,
                                Cursor = Cursors.Hand,
                                ToolTip = attachment.FileName // Tooltip to display file name
                            };
                            attachmentButton.Click += AttachmentButton_Click;
                            flowLayoutPanel1.Controls.Add(attachmentButton);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                if (splashScreenManager1.IsSplashFormVisible) // Wait Form이 이미 실행중인지 확인
                    splashScreenManager1.CloseWaitForm(); // Wait Form 닫기
            }
        }

        private Image GetAttachmentIcon(string fileName)
        {
            // 파일 확장자에 따른 아이콘 이미지 생성
            var fileExtension = Path.GetExtension(fileName).ToLower();
            switch (fileExtension)
            {
                case ".doc":
                case ".docx":
                    return Properties.Resources.doc;

                case ".xls":
                case ".xlsx":
                    return Properties.Resources.xlsx;

                case ".pdf":
                    return Properties.Resources.pdf;

                case ".zip":
                case ".rar":
                    return Properties.Resources.zip;

                case ".dwg":
                    return Properties.Resources.dwg;

                case ".ppt":
                case ".pptx":
                    return Properties.Resources.ppt;

                case ".hwp":
                    return Properties.Resources.hwp;

                case ".txt":
                    return Properties.Resources.txt;

                default:
                    return Properties.Resources.default_icon2;
            }
        }

        private void AttachmentButton_Click(object sender, EventArgs e)
        {
            var button = (SimpleButton)sender;
            var attachment = (MimePart)button.Tag;

            // 첨부파일 다운로드 기능 구현
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = attachment.FileName;
            saveFileDialog.Filter = "모든 파일|*.*";
            saveFileDialog.FilterIndex = 1;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = attachment.FileName;

                using (var fileStream = File.Create(saveFileDialog.FileName))
                {
                    attachment.Content.DecodeTo(fileStream);
                }
            }
            MessageBox.Show(attachment.FileName + " DownLoad OK!!!");
        }

        private void LoadFolderTree(string rootPath)
        {
            treeList1.BeginUpdate();

            // Create three columns
            TreeListColumn col1 = treeList1.Columns.Add();
            col1.FieldName = "Path";
            col1.Caption = "Path";
            col1.VisibleIndex = 0;

            treeList1.EndUpdate();

            TreeListNode parentForRootNodes = treeList1.AppendNode(
                new object[] { "Root", Tag = "Root" },
                null);
            TreeListNode rootNode = treeList1.AppendNode(
                new object[] { rootPath },
                parentForRootNodes);
            TreeListNode root2Node = treeList1.AppendNode(
                new object[] { Path.GetFileName("C:\\Users\\Cowintech\\Desktop") },
                parentForRootNodes);
            //        TreeListNode root3Node = treeList1.AppendNode(
            //new object[] { Path.GetFileName("S:\\") },
            //parentForRootNodes);

            rootNode.Tag = rootPath; // 경로를 태그로 저장
            root2Node.Tag = "C:\\Users\\Cowintech\\Desktop";
            //root3Node.Tag = "S:\\";
            AddDummyNode(rootNode);
            AddDummyNode(root2Node);
            //AddDummyNode(root3Node);
        }

        private void AddDummyNode(TreeListNode node)
        {
            node.Nodes.Add(new object[] { "Loading..." });
        }

        private void treeList1_AfterExpand(object sender, NodeEventArgs e)
        {
            var node = e.Node;
            //if (node.Nodes.Count == 1 && node.Nodes[0].GetValue(0).ToString() == "Loading...")
            //{
            //    node.Nodes.Clear();
            //    LoadSubDirectories(node, node.Tag.ToString());
            //}
            if (node.Tag != null)
            {
                node.Nodes.Clear();
                LoadSubDirectories(node, node.Tag.ToString());
            }
        }

        private void LoadSubDirectories(TreeListNode node, string path)
        {
            try
            {
                var directories = Directory.GetDirectories(path);
                foreach (var directory in directories)
                {
                    var subNode = node.TreeList.AppendNode(new object[] { Path.GetFileName(directory) }, node);
                    subNode.Tag = directory; // 경로를 태그로 저장
                    AddDummyNode(subNode);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 접근할 수 없는 디렉터리일 경우 예외 처리
                node.TreeList.AppendNode(new object[] { "Access Denied" }, node);
            }
        }

        private void LoadMailList(string folderPath)
        {
            if (!splashScreenManager1.IsSplashFormVisible) // Wait Form이 이미 실행중인지 확인
                splashScreenManager1.ShowWaitForm(); // Wait Form 열기

            emlFiles = Directory.GetFiles(folderPath, "*.eml").ToList();
            dataTable = new DataTable();
            dataTable.Columns.Add("Date");
            dataTable.Columns.Add("Attach");
            dataTable.Columns.Add("Subject");
            dataTable.Columns.Add("From");
            dataTable.Columns.Add("To");
            dataTable.Columns.Add("Cc");
            dataTable.Columns.Add("Path");
            dataTable.Columns.Add("Content");

            foreach (var emlFile in emlFiles)
            {
                var mail = LoadMail(emlFile);
                dataTable.Rows.Add(mail.Date, mail.Attachment, mail.Subject, mail.From, mail.To, mail.Cc, emlFile.ToString(), mail.Body);
            }

            dataTable.DefaultView.Sort = "Date DESC";

            mailListGrid.DataSource = dataTable;

            mailListView.Columns["Date"].Width = 150;
            //mailListView.Columns["Date"].DisplayFormat.FormatType = FormatType.DateTime;
            //mailListView.Columns["Date"].DisplayFormat.FormatString = "D";
            mailListView.Columns["Attach"].Width = 30;
            mailListView.Columns["Subject"].Width = 500;
            mailListView.Columns["From"].Width = 500;
            mailListView.Columns["To"].Width = 500;
            mailListView.Columns["Cc"].Width = 500;
            mailListView.Columns["Path"].Width = 300;
            mailListView.Columns["Content"].Width = 1000;

            //mailListView.Columns["Date"].SortMode = ColumnSortMode.Custom;
            //mailListView.Columns["Subject"].SortMode = ColumnSortMode.Custom;

            //mailListView.BestFitColumns();

            if (splashScreenManager1.IsSplashFormVisible) // Wait Form이 이미 실행중인지 확인
                splashScreenManager1.CloseWaitForm(); // Wait Form 닫기
        }

        //private void LoadNextBatch()
        //{
        //    if (currentIndex >= emlFiles.Count)
        //        return;

        //    int endIndex = Math.Min(currentIndex + batchSize, emlFiles.Count);

        //    for (int i = currentIndex; i < endIndex; i++)
        //    {
        //        var mail = LoadMail(emlFiles[i]);
        //        dataTable.Rows.Add(mail.Date, mail.Attachment, mail.Subject, mail.From, mail.To, mail.Cc, emlFiles[i], mail.Body);
        //    }

        //    currentIndex = endIndex;
        //}

        private MailMessage LoadMail(string emlFilePath)
        {
            using (var reader = new StreamReader(emlFilePath))
            {
                var mail = new MailMessage();
                mail.Load(reader);
                return mail;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 기본 폴더 경로 설정
            string rootPath = @"C:\";
            LoadFolderTree(rootPath);

            // 기본 폴더의 메일 리스트 로드
            //LoadMailList(rootPath);
        }

        private void treeList1_FocusedNodeChanged(object sender, FocusedNodeChangedEventArgs e)
        {
            if (e.Node.Tag != null)
            {
                LoadMailList(e.Node.Tag.ToString());
                currentFolderPath = e.Node.Tag.ToString();
            }
        }

        private void mailListView_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            //var view = sender as GridView;
            //if (view != null)
            //{
            //    // 현재 표시된 마지막 행 인덱스를 얻습니다.
            //    int lastVisibleRowIndex = view.RowCount - 1;

            //    // 현재 행이 마지막 표시된 행인지 확인합니다.
            //    if (e.RowHandle == lastVisibleRowIndex)
            //    {
            //        if (!splashScreenManager1.IsSplashFormVisible) // Wait Form이 이미 실행중인지 확인
            //            splashScreenManager1.ShowWaitForm(); // Wait Form 열기

            //        LoadNextBatch();

            //        if (splashScreenManager1.IsSplashFormVisible) // Wait Form이 이미 실행중인지 확인
            //            splashScreenManager1.CloseWaitForm(); // Wait Form 닫기
            //    }
            //}
        }

        private void mailListView_SelectionChanged(object sender, DevExpress.Data.SelectionChangedEventArgs e)
        {
            //if (check == false)
            //{
            //    check = true;
            //}
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select the destination folder";

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string destinationFolderPath = folderBrowserDialog.SelectedPath; // 선택한 목적지 폴더 경로

                    MoveSelectedMails(destinationFolderPath);
                }
            }
        }

        private void MoveSelectedMails(string destinationFolderPath)
        {
            List<int> selectedRows = mailListView.GetSelectedRows().ToList();
            if (selectedRows == null || selectedRows.Count == 0)
            {
                MessageBox.Show($"선택된 편지가 없습니다.");
                return;
            }

            foreach (var rowIndex in selectedRows)
            {
                var row = (DataRowView)mailListView.GetRow(rowIndex);
                string sourceFilePath = row["Path"].ToString();
                string fileName = Path.GetFileName(sourceFilePath);
                string destinationFilePath = Path.Combine(destinationFolderPath, fileName);

                try
                {
                    // 파일 이동
                    File.Move(sourceFilePath, destinationFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error moving file: {ex.Message}");
                }
            }

            selectedRows = selectedRows.OrderByDescending(i => i).ToList();
            foreach (var rowIndex in selectedRows)
            {
                mailListView.DeleteRow(rowIndex);
            }
            MessageBox.Show("Selected mails have been moved.");
            //LoadMailList(currentFolderPath); // 현재 폴더 경로를 재설정해야 합니다.
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("선택된 메일을 정말로 삭제하겠습니까? 복구 할 수 없습니다.",
                                     "Confirm Delete",
                                     MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                DeleteSelectedMails();
            }
        }

        private void DeleteSelectedMails()
        {
            List<int> selectedRows = mailListView.GetSelectedRows().ToList();
            if (selectedRows == null || selectedRows.Count == 0)
            {
                MessageBox.Show($"선택된 편지가 없습니다.");
                return;
            }

            foreach (var rowIndex in selectedRows)
            {
                var row = (DataRowView)mailListView.GetRow(rowIndex);
                string filePath = row["Path"].ToString();

                try
                {
                    // 파일 삭제
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting file: {ex.Message}");
                }
            }

            selectedRows = selectedRows.OrderByDescending(i => i).ToList();
            foreach (var rowIndex in selectedRows)
            {
                mailListView.DeleteRow(rowIndex);
            }
            MessageBox.Show("Selected mails have been deleted.");
        }
    }
}