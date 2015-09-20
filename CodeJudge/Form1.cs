using System;
using System.Drawing;
using System.Windows.Forms;
using CCWin;
using System.Threading;
using System.IO;
using System.Net;
using SharpCompress.Archive;
using SharpCompress.Common;

namespace CodeJudge
{
    public partial class frm_init : Skin_Mac
    {
        public void  checkOther(){
            toolStripStatusLabel2.Text = "正在检测其余环境...";
            skinProgressBar1.Maximum = 2000;
            for (skinProgressBar1.Value = 30; skinProgressBar1.Value < 2000; skinProgressBar1.Value++)
                ;
            toolStripStatusLabel2.Text = "环境检测完毕";
        }

        public void unzip()  //解压函数
        {
            skinProgressBar1.Visible = false;
            var compressed = ArchiveFactory.Open("D:\\CodeJudge_Studio\\GCC\\gcc.zip");
            foreach (var entry in compressed.Entries)
            {
                if (!entry.IsDirectory)
                {
                    entry.WriteToDirectory("D:\\CodeJudge_Studio\\GCC", ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                    toolStripStatusLabel2.Text = entry.FilePath.ToString();
                }

            }
            skinProgressBar1.Visible = true;
            skinButton1.Text = "文件解压完毕";
            toolStripStatusLabel2.Text = "文件解压完毕";
            //Directory.CreateDirectory(Project_Dir + "\\finish_flag");
            skinButton1.Text = "文件配置完毕";
            //创建配置完毕标识  一定要在这里
            Directory.CreateDirectory(Project_Dir_flag);
            //恢复环境
            skinButton1.Enabled = true;
            skinButton1.BaseColor = Color.Red;
            skinButton1.Text = "重启软件";
        }


        String Project_Dir = "D:\\CodeJudge_Studio";
       // String Project_Dir_run = "D:\\CodeJudge_Studio\\Run_Data";
        String Project_Dir_flag = "D:\\CodeJudge_Studio\\finish_flag";
        public frm_init()
        {
            InitializeComponent();
        }

        private void frm_init_Load(object sender, EventArgs e)
        {
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false; //禁用线程调用检查 避免线程调用导致异常
            pictureBox1.Image = Image.FromFile("./res_pic/network.png");
            toolStripStatusLabel2.Text = "服务器网络...";
        }

        private void frm_Shown(object sender, EventArgs e)
        {
            Boolean netCheck = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            if (!netCheck)
            {
                toolStripStatusLabel2.Text = "服务器连接失败！";
                MessageBox.Show("服务器连接失败！", "运行状态", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            else
            {
                toolStripStatusLabel2.Text = "服务器连接成功";
                skinProgressBar1.Value = 30;
            }

            Thread.Sleep(100); //切换到编译环境检测部分
            if (!Directory.Exists(Project_Dir + "\\finish_flag")) //首次配置
            {
                pictureBox1.Visible = false;
                skinButton1.Visible = true;
                skinButton1.Text = "首次配置";
                skinButton1.BaseColor = Color.Red;
                toolStripStatusLabel2.Text = "请选择首次配置...";
            }
            else //非首次配置 进行系统常规检测
            {
                Thread check = new Thread(new ThreadStart(checkOther));
                check.IsBackground = true;
                check.Start();
            }

        }

        private void skinButton1_Click(object sender, EventArgs e)
        {
            int statue = 100;
            if (skinButton1.Text == "首次配置")
            {
                statue = 1;
            }
            else if (skinButton1.Text == "重启软件")
            {
                Application.Restart();
            }



            if (statue == 1)
            {
                toolStripStatusLabel2.Text = "正在进行首次配置...";
                skinProgressBar1.Value = 0;
                Directory.CreateDirectory(Project_Dir); //创建根目录
                Directory.CreateDirectory(Project_Dir+"\\Run_Data"); //运行目录
                Directory.CreateDirectory(Project_Dir + "\\GCC"); //运行目录
                toolStripStatusLabel2.Text = "创建目录完毕";
                //下载文件到根目录下
                WebClient wc = new WebClient();
                wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc_DownloadProgressChanged);
                wc.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(wc_DownloadFileCompleted);
                String gccZipLocation=Project_Dir + "\\GCC\\gcc.zip";
                FileInfo fi = new FileInfo(gccZipLocation);  //创建文件句柄 ，后续获取文件大小
                if (!(File.Exists(gccZipLocation) && (fi.Length >= 80000000)))
                {
                    skinButton1.Text = "正在下载GCC环境";
                    wc.DownloadFileAsync(new Uri("http://7xl54r.dl1.z0.glb.clouddn.com/setting/gcc.zip"), Project_Dir + "\\GCC\\gcc.zip");
                }
                else
                {
                    skinButton1.Text = "文件下载完毕";
                    toolStripStatusLabel2.Text = "文件下载完毕";
                }

                skinButton1.ForeColor = Color.Black;
                skinButton1.Enabled = false;

                if (!Directory.Exists(Project_Dir + "\\GCC\\bin") && (fi.Length >= 81619119))
                {
                    skinButton1.Text = "正在解压缩...";
                    toolStripStatusLabel2.Text = "解压Gcc编译包";
                    Thread unzipThread = new Thread(new ThreadStart(unzip));
                    unzipThread.Start();
                    unzipThread.IsBackground = true;
                }

            }
        }

        void wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
      {
          // throw new NotImplementedException();
            skinButton1.Text = "正在解压缩...";
            skinProgressBar1.Value = 0;
            if (!Directory.Exists(Project_Dir + "\\GCC\\bin"))
            {
                skinButton1.Text = "正在解压缩...";
                toolStripStatusLabel2.Text = "解压Gcc编译包";
                Thread unzipThread = new Thread(new ThreadStart(unzip));
                unzipThread.Start();
                unzipThread.IsBackground = true;
            }
        }

        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //throw new NotImplementedException();
            skinProgressBar1.Maximum = (int)e.TotalBytesToReceive;
            skinProgressBar1.Value = (int)e.BytesReceived;
            toolStripStatusLabel2.Text = e.BytesReceived.ToString() + "/" + e.TotalBytesToReceive.ToString();
            
        }

        private void PicBox_Click(object sender, EventArgs e)
        {
            if (toolStripStatusLabel2.Text == "环境检测完毕")
            { //环境检测完毕 执行逻辑代码 
                Main MainWindow = new Main();
                MainWindow.Show();
                this.Hide();
            }
        }
    }
}
