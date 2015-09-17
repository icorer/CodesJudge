using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CCWin;
using System.IO;
using Qiniu.RS;
using System.Net;

namespace CodeJudge
{
    public partial class Main : Skin_Mac
    {
        String Cloud_ACCESS_KEY = "V6UKn_EQAB0sNo8WC7N7cXwBABDIhnb6F6YjzkxS";
        String Cloud_SECRET_KEY = "KKvicgXXzQMba_JuoP_rBsCQeeHMWxQ5k3i2t3oA";
        String Cloud_bucket = "codejudge";
        String problemJudgeInfo = "";
        String problemJudgeDate = "";
        String problemId = "";
        String RootDir = "D:\\CodeJudge_Studio\\Run_Data\\";
        String urlLocation = "http://7xl54r.dl1.z0.glb.clouddn.com/";
        String judgeInfoLocation = "";
        String judgeDataLocation = "";
        String ProblemDir = "";
        String CloudScriptUrl = "";

        String Cloud_flag = "D:\\CodeJudge_Studio\\cloud_flag.dat";
        Boolean Cloud_type; //云类型 true：七牛云，false：自我组织的云
        public void CloudInit() {
            Qiniu.Conf.Config.ACCESS_KEY = Cloud_ACCESS_KEY;
            Qiniu.Conf.Config.SECRET_KEY = Cloud_SECRET_KEY;
        }

        /*
        public Boolean CheckCloudExist(Entry e)
        {
            if (e.OK)
                return true;
            else
                return false;
        }
        */

        int CheckCloudFileExist(String ApiRequestUrl) //检测脚本网页API接口是否完整
        {

            try
            {
                //Creating the HttpWebRequest
                HttpWebRequest request = WebRequest.Create(ApiRequestUrl) as HttpWebRequest;
                request.Timeout = 800;
                //Setting the Request method HEAD, you can also use GET too.
                request.Method = "HEAD";
                //Getting the Web Response.
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //Returns TRUE if the Status code == 200

                if (response.StatusCode == HttpStatusCode.OK) 
                { //有文件存在响应 开始读文件
                    WebClient wc1 = new WebClient();
                    String WebSource = wc1.DownloadString(ApiRequestUrl); //获取请求后的网页源码
                    WebSource = WebSource.Trim();
                    if (WebSource == "yes")
                        return 3; //云端所有文件均存在
                    else
                        return 2; //云端文件不全存在
                }
                else
                {
                    return 1;//1 云端请求无法响应
                }


            }
            catch
            {
                //Any exception will returns false.
                return 0; //0 云端请求无法响应
            }
        }

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            //获取云类型
            if (!File.Exists(Cloud_flag))
                Cloud_type = true; //默认七牛云
            else
            { 
                 Cloud_type = false;//自定义 私有云
                 String[] temp = File.ReadAllLines(Cloud_flag, Encoding.Default);
                 urlLocation = temp[0].ToString(); // 获取文件中保存的云URL链接
                 CloudScriptUrl = temp[1].ToString();// 获取文件中保存的云端脚本URL链接

            }
               

        }

        private void MainFrm_Closed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void Menu_OpenFile_Click(object sender, EventArgs e)
        {
            String fileLocation="";
            OpenFileDialog a = new OpenFileDialog();
            a.Filter = "(C++源文件)|*.cpp|(C语言源文件)|*.c|(文本文件)|*.txt";
            a.ShowDialog();
            if (a.FileName != "")
            {
                fileLocation = a.FileName;
                String data = File.ReadAllText(fileLocation, Encoding.Default); //读取文本 注意 编码方式
                fastColoredTextBox1.Text = data;
            }  
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void Splitter_Moved(object sender, SplitterEventArgs e)
        {

            int Splitter_right_width = skinSplitContainer1.Panel2.Width;
            skinGroupBox3.Width = (int)(Splitter_right_width-30);
            fastColoredTextBox1.Width = (int)(Splitter_right_width - 50);
           
        }

        private void ToolStrip_cpp_Click(object sender, EventArgs e)
        {
            skinButton3.Text = "语言：C++";
        }

        private void ToolStrip_c_Click(object sender, EventArgs e)
        {
            skinButton3.Text = "语言：C";
        }

        private void skinButton1_Click(object sender, EventArgs e)
        {
            if (Text_JudgeId.Text != "")
            {
                problemId = Text_JudgeId.Text;
                //MessageBox.Show(problemId);
                CloudInit();
                RSClient client = new RSClient();
                problemJudgeInfo = "data/" + problemId + ".info";
                problemJudgeDate = "data/" + problemId + ".data";
                //MessageBox.Show(problemJudgeInfo);
                //MessageBox.Show(problemJudgeDate);
                Entry CloudJudgeInfoEntry = client.Stat(new EntryPath(Cloud_bucket, problemJudgeInfo));
               // Entry CloudJudgeDataEntry = client.Stat(new EntryPath(Cloud_bucket, problemJudgeDate)); //创建data检测

                if (Cloud_type&&!(CloudJudgeInfoEntry.OK))// 如果七牛云 而且七牛云数据不存在
                {
                    MessageBox.Show("云端不存在此序号的题目信息！","操作提示",MessageBoxButtons.OK,MessageBoxIcon.Error);
                }
                else if ((!Cloud_type) && (CheckCloudFileExist(CloudScriptUrl+"?problemid="+problemId.ToString()) != 3)) //如果是私有云 而且数据不完全存在
                {
                   // MessageBox.Show(CloudScriptUrl + "?problemid=" + problemId.ToString());
                    MessageBox.Show("私有云不存在此序号的完整题目信息！", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    //创建题目目录
                    Text_Judge_id.Text = problemId;
                    ProblemDir = RootDir + problemId;
                    if (!Directory.Exists(ProblemDir))
                        Directory.CreateDirectory(ProblemDir);
                    //下载资源文件
                    judgeInfoLocation = ProblemDir + "\\" + problemId + ".info";
                    judgeDataLocation = ProblemDir + "\\" + problemId + ".data";

                    //清理旧的题目INFO DATA数据
                    if (File.Exists(judgeInfoLocation))
                        File.Delete(judgeInfoLocation);
                    if (File.Exists(judgeDataLocation))
                        File.Delete(judgeDataLocation);

                    WebClient wc1 = new WebClient(); //info
                    wc1.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc1_DownloadProgressChanged);
                    wc1.DownloadFileCompleted += new AsyncCompletedEventHandler(wc1_DownloadFileCompleted);
                    wc1.DownloadFileAsync(new Uri(urlLocation + problemJudgeInfo), judgeInfoLocation);//下载题目评测要求到目录

                    // skinButton1.Enabled = false; //锁定题目获取按钮
                }

            }
        }

        void wc1_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            skinButton1.Enabled = false;
            skinButton1.BaseColor = Color.Blue;
            toolStripStatusLabel2.Text = "正在同步云端INFO数据...";
           
        }

        void wc2_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {

            skinButton1.Enabled = false;
            skinButton1.BaseColor = Color.Blue;
            toolStripStatusLabel2.Text = "正在同步云端DATA数据...";
        }

        void wc2_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {

            skinButton1.BaseColor = Color.Maroon;
            skinButton1.Enabled = true;
            Text_ProInfo.Text = File.ReadAllText(judgeDataLocation, Encoding.UTF8); //评测data文件采用的是UTF-8编码
            toolStripStatusLabel2.Text = "运行正常";  
        }

        void wc1_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            string[] lines = File.ReadAllLines(judgeInfoLocation);
            Label_cpuTime.Text = lines[0];
            Label_Memory.Text = lines[1];
            WebClient wc2 = new WebClient(); //data
            wc2.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc2_DownloadProgressChanged);
            wc2.DownloadFileCompleted += new AsyncCompletedEventHandler(wc2_DownloadFileCompleted);
            wc2.DownloadFileAsync(new Uri(urlLocation + problemJudgeDate), judgeDataLocation);//下载题目内容到达目录
            skinButton1.Enabled = true;
        }

        private void skinButton2_Click(object sender, EventArgs e)
        {
            //MessageBox.Show(skinCode1.CodeStr);
            if (Text_yanzhengma.Text.ToString().ToUpper()== skinCode1.CodeStr)
            {
                if (Text_JudgeId.Text == "")
                    MessageBox.Show("请输入评测序号获取题目信息！", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else if (fastColoredTextBox1.Text.Length <=20)
                {
                    MessageBox.Show("请输入或导入源代码！", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else {
                    if (MessageBox.Show("评测序号为：" + Text_JudgeId.Text + "\n您是否开始评测?", "选择提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        Text_Judge_id.Text = Text_JudgeId.Text;
                       // MessageBox.Show("开始评测");
                        skinCode1.NewCode(); //刷新验证码
                        Judge CodeJudge = new Judge();
                        CodeJudge.Problem_ID = Text_Judge_id.Text.ToString();
                        CodeJudge.Code = fastColoredTextBox1.Text.ToString();
                        CodeJudge.CpuTime = Label_cpuTime.Text;
                        CodeJudge.MemoryMax = Label_Memory.Text;
                        CodeJudge.QiniuAK = Cloud_ACCESS_KEY;
                        CodeJudge.QiniuSK = Cloud_SECRET_KEY;
                        CodeJudge.ShowDialog();
                       
                    }
                
                }

            }
            else
            {
                MessageBox.Show("验证码输入错误！", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void Menu_Save_Click(object sender, EventArgs e)
        {
            SaveFileDialog Menu_SaveFile = new SaveFileDialog();
          //  Menu_SaveFile.ShowDialog();
            String CodeType = skinButton3.Text;
            Menu_SaveFile.Title = "源代码 另保存";
            if (CodeType == "语言: C++")
            {
                Menu_SaveFile.Filter = "(C++源文件)|*.cpp";
                CodeType = "cpp";
            }
            else if (CodeType == "语言：C")
            {
                Menu_SaveFile.Filter = "(C语言源文件)|*.c";
                CodeType = "c";
            }
                
            Menu_SaveFile.ShowDialog();
            String fileLocation = Menu_SaveFile.FileName;
            if (fileLocation != "")
            {
                File.WriteAllText(fileLocation, fastColoredTextBox1.Text, Encoding.Default); //保存代码文件;

            }
               

        }

        private void Menu_AddCloud_Click(object sender, EventArgs e)
        {
            //弹出输入框
            Frm_AddCloud Frm_addCloud = new Frm_AddCloud();
            Frm_addCloud.ShowDialog();
        }

        private void Menu_Cloud_Reset(object sender, EventArgs e)
        {
            if (MessageBox.Show("您是否确认连接默认的数据云？", "操作提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                File.Delete(Cloud_flag);
                MessageBox.Show("云端重置成功，确认后重启系统！", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Restart();
            }
        }

    }
}


