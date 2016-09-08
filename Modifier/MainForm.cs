using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Modifier
{
    public partial class MainForm : Form
    {
        int WFlag = WFlag_Busy;
        const int WFlag_Busy = 0;
        const int WFlag_Free = 1;

        private ModifierConfig modifierConfig;
        private Version version;

        public int PageIndex
        {
            get
            {
                return tabControl.SelectedIndex - 1;
            }
        }

        public MainForm()
        {
            InitializeComponent();
                              
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string fileName = SelectFile();
            if (fileName != null)
            {
                if (LoadFile(fileName))//读取文件初始化modifier
                {
                    InitApp();  //初始化应用    
                }              
            }
            else
            {
                MessageBox.Show("目录下没有找到脚本文件");
                Close();
            }

        }
        private void InitApp()
        {
            tabControl.TabPages.RemoveAt(1);//删除测试页


            //设置初始状态
            if (Program.IsProcessRunning(modifierConfig.ProcessName))
            {
                moduleStausLabel.ForeColor = Color.DarkGreen;
                moduleStausLabel.Text = modifierConfig.ProcessName + "正在运行";
            }
            else
            {
                moduleStausLabel.ForeColor = Color.DarkRed;
                moduleStausLabel.Text = modifierConfig.ProcessName + "未运行！";
            }

            this.Text += " - For " + modifierConfig.ProcessName;

            currentStatusStrip.Text = "当前状态：正在加载";
            versionLabel.Text = "版本号：" + Application.ProductVersion;

        }

        private void Event_ProcessRan()
        {
            //string fileName = APIHelper.GetProcessModule(modifierConfig.ProcessName, modifierConfig.ModuleName).FileName;
            //string md5 = Program.GetMD5HashFromFile(fileName);

            string md5 = "md5Test";
            //匹配版本
            if (AdaptVersion(md5))
            {
                LoadPages(version.Pages);
            }
            else
            {
                //版本匹配失败
                MessageBox.Show("版本匹配失败,没有合适的版本");
                Close();
            }

        }

        private string SelectFile()
        {
            string[] files = System.IO.Directory.GetFiles(Application.StartupPath, "*.xml", SearchOption.TopDirectoryOnly);
            if (files != null)
            {
                if (files.Length == 1)
                {
                    return files[0];
                }
                else
                {
                    return SelectFileBox.SelectFile(files);
                }
            }
            else
            {
                return null;
            }
            
        }   //寻找文件

        private bool LoadFile(string fileName)
        {
            try
            {
                if (fileName != null && fileName != "")
                {
                    StreamReader reader = new StreamReader(fileName);
                    if (reader != null)
                    {
                        System.Xml.Serialization.XmlSerializer xr = new System.Xml.Serialization.XmlSerializer(typeof(ModifierConfig));
                        modifierConfig = (ModifierConfig)xr.Deserialize(reader);
                        reader.Close();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取文件失败");
                Close();
            }
            return false;
        }   //读取文件

        private bool AdaptVersion(string md5)
        {
            foreach (var item in modifierConfig.Versions)
            {
                if (item.FileMd5 == md5)
                {
                    version = item;

                    return true;
                }
            }
            return false;
        }   //匹配版本

        private void LoadPages(List<FunctionPage> pages)
        {
            foreach (var page in pages)
            {
                TabPage tb = new TabPage();
                tb.Text = page.Name;
                tabControl.TabPages.Add(tb);
            }
        }

        private void LoadItems(List<DataGridViewRow> rows)
        {
            gridView.Rows.Clear();
            gridView.Rows.AddRange(rows.ToArray());
        }

        private List<DataGridViewRow> ReadItemsValue(List<FunctionItem> functionItems, bool isReload)
        {
            List<DataGridViewRow> rows = new List<DataGridViewRow>();
            foreach (var item in functionItems)
            {
                DataGridViewRow row = new DataGridViewRow();
                DataGridViewTextBoxCell name = new DataGridViewTextBoxCell();                
                DataGridViewTextBoxCell errorText = new DataGridViewTextBoxCell();
                DataGridViewCell value;
                object itemValue = -1;//初始值                

                name.Value = item.Name;

                try
                {
                    itemValue = item.Read(isReload);
                }
                catch (Exception ex)
                {
                    errorText.Value = ex.Message;
                }
                                               
                switch (item.FormStyle)
                {
                    case "文本框":
                        value = new DataGridViewTextBoxCell();
                        row.Cells.AddRange(new DataGridViewCell[] { name, value, errorText});

                        value.Value = itemValue;
                        value.ReadOnly = item.ReadOnly;
                        if (value.ReadOnly)
                        {
                            value.Style.BackColor = Color.WhiteSmoke;
                        }
                        break;
                    case "下拉列表":
                        value = new DataGridViewComboBoxCell();
                        row.Cells.AddRange(new DataGridViewCell[] { name, value, errorText });

                        value.ReadOnly = item.ReadOnly;
                        ((DataGridViewComboBoxCell)value).Items.AddRange(item.ValueStringMap.GetValueList().ToArray());
                        ((DataGridViewComboBoxCell)value).Value = item.ValueStringMap.GetValue((int)itemValue);
                        break;
                }
                rows.Add(row);
                
            }
            return rows;
        }


        //测试代码
        private void button1_Click(object sender, EventArgs e)
        {
            Event_ProcessRan();           
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //等待线程运行
            while (!Program.IsProcessRunning(modifierConfig.ProcessName))
            {
                System.Threading.Thread.Sleep(1000);
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Event_ProcessRan();
        }

        private void tabControl_Selected(object sender, TabControlEventArgs e)
        {
            int pageIndex = PageIndex;
            if (pageIndex > -1)
            {
                tabControl.SelectedTab.Controls.Add(gridView);

                WFlag = WFlag_Busy;
                LoadItems(ReadItemsValue(version.Pages[pageIndex].Items, true));
                WFlag = WFlag_Free;
            }
        }

        private void gridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            //设置编辑时背景色
            gridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.Gold;
            gridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = Color.Red;
        }

        private void gridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            //设置编辑完毕时背景色
            gridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
            gridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = Color.Black;
        }

        private void gridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (WFlag == WFlag_Free)
            {
                int rowIndex = e.RowIndex;
                int columnIndex = e.ColumnIndex;

                if (rowIndex >= 0 && columnIndex == 1)
                {
                    
                    FunctionItem item = version.Pages[PageIndex].Items[rowIndex];
                    DataGridViewRow currentRow = gridView.Rows[rowIndex];
                  
                    WFlag = WFlag_Busy;
                    if (item.FormStyle == "文本框")
                    {
                        string value = "0";
                        if (currentRow.Cells["value"].Value != null)
                        {
                            value = currentRow.Cells["value"].Value.ToString();
                        }

                        try
                        {
                            item.Write(value);
                            
                        }
                        catch (Exception ex)
                        {                           
                            currentRow.Cells["errorText"].Value = ex.Message;
                        }
                        currentRow.Cells["value"].Value = item.Read(false);
                    }
                    else if (item.FormStyle == "下拉列表")
                    {
                        string str = currentRow.Cells["value"].Value.ToString();
                        str = "";
                        int key = item.ValueStringMap.GetKey(str);
                        try
                        {
                            if (key != -1)
                            {
                                item.Write(key.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            currentRow.Cells["errorText"].Value = ex.Message;
                        }
                        currentRow.Cells["value"].Value = "";
                    }           
                    WFlag = WFlag_Free;
                }
            }
        }
    }
}
