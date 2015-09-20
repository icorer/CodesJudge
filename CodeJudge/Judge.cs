using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CCWin;
using System.IO;
using System.Net;
using System.Threading;
using Qiniu.Conf;
using Qiniu.RS;
using System.Diagnostics;

namespace CodeJudge
{
    public partial class Judge : Skin_Mac
    {
        public  String Problem_ID = "";
        public  String Code = "";
        public String CodeType="cpp"; //代码类型初始化为C++
        public String CpuTime = "";
        public String MemoryMax = "";
        public String QiniuAK = "";
        public String QiniuSK = "";
        bool Cloud_type = true; //默认七牛云

        String Cloud_flag = "D:\\CodeJudge_Studio\\cloud_flag.dat";  //自定义云的标致符号 
        String CloudScriptUrl = "";
        //设计几个文件路径  部分在show事件中初始化
        String rootDir="D:\\CodeJudge_Studio\\Run_Data\\"; //根目录
        String problemDir = "";  //根目录下的题目目录
        String SourceDir = "";//题目目录下的源代码目录
        String JudgeStd = ""; //题目目录下的网络标准评测数据目录
        String JudgeCompile = "";//题目目录下的编译目录
        String JudgeRunTemp = "";//题目目录下的程序运行结果目录
        String CodeFile_Location = "";//源码文件完整路径
        String CloudDataUrl = "http://7xl54r.dl1.z0.glb.clouddn.com/data/";
        String JudgeStdFile = "";//云端数据文件本地保存路径
        String JudgeStdInDir = ""; //云端数据分割后的in数据文件夹
        String JudgeStdOutDir = "";//云端数据分割后的out数据文件夹
        Process compiler = new Process();  //定义编译器对象
        Process ExeRunner=null;  //定义程序执行体对象
        String ExeRunner_state = "ok";//实例运行后的状态
        DateTime ExeRunner_start_time ;//实例开始运行时间
        DateTime ExeRunner_end_time ;//实例终止运行时间
        TimeSpan ExeRunner_total_time;//实例实际运行时间
        double ExeRunner_max_time;//实例运行的最大时间
        double ExeRunner_sum_time;
        double ExeRunner_avg_time;
        double ExeRunner_min_time;

        long  ExeRunner_memory ; //获取进程内存大小

        public int TotalCkeckTime = 0; //系统总共需要评测的次数

        String Compiler_Error_output = "";//定义进程异常输出
        String compileTotalTime = "";

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


        void saveCode()
        {
            //MessageBox.Show(CodeFile_Location);
            File.WriteAllText(CodeFile_Location, Code,Encoding.Default);
            toolStripStatusLabel2.Text = "源码保存完毕！";
        }

        void JudgeDataComper() {
            int i;
            String RunOutFile = "";
            String StdOutFile = "";
            int flag = 1;
            for (i = 0; i < TotalCkeckTime; i++) //开启文件序号递归
            {
                flag = 1;//文件比对标志变量 初值1 代表文件相同
                RunOutFile = JudgeRunTemp + i.ToString() + ".out";
                StdOutFile = JudgeStdOutDir + i.ToString() + ".out";
                if (File.Exists(RunOutFile) && File.Exists(StdOutFile))// step1：两个文件均存在
                {
                    skinProgressBar1.Value = 86;
                    String[] RunOutData = File.ReadAllLines(RunOutFile); //获取用户out行数据
                    String[] StdOutData = File.ReadAllLines(StdOutFile);//获取标准out行数据
                    if (RunOutData.Length == StdOutData.Length) //两个数据数组条目数一样
                    {
                        int i2;
                        for (i2 = 0; i2 < RunOutData.Length; i2++) //比较文档内容是否相同
                        {
                            skinProgressBar1.Value = 96;
                            if (RunOutData[i2].Trim() != StdOutData[i2].Trim())
                                break;
                        }
                        if (i2 >= RunOutData.Length)
                            flag = 1;
                        else
                            flag = 0;
                    }
                    else
                    {
                        flag=0;
                    }

                }
                else {
                    flag = 0;
                }
                if (flag == 0)
                {
                    pictureBox5.Image = Image.FromFile("./res_pic/error.png");
                    toolStripStatusLabel2.Text = "数据匹配失败，错误点:" + i.ToString();
                    break;
                }
            }
            if (flag == 1)
            {
                pictureBox5.Image = Image.FromFile("./res_pic/ok.png");
                toolStripStatusLabel2.Text = "数据匹配完毕，评测通过！"+"  CPU时间消耗："+ExeRunner_avg_time.ToString("0.000")+"ms"+"  内存消耗:"+ (ExeRunner_memory/1024).ToString()+"KB"; 
                skinProgressBar1.Value = skinProgressBar1.Maximum;
                //MessageBox.Show("MEMORY: "+ExeRunner_memory.ToString());
            }
        }

        void downloadDataFile()
        {
           // MessageBox.Show(JudgeStdFile);
            //MessageBox.Show(CloudDataUrl);
            //必须要进行云端文件判断 才能构成评测独立模块
            Qiniu.Conf.Config.ACCESS_KEY = QiniuAK;
            Qiniu.Conf.Config.SECRET_KEY = QiniuSK;
            String bucket = "codejudge";
            String bucketFileName = "data/" + Problem_ID + ".dat";
            RSClient client = new RSClient();
            Entry CloudJudgeInfoEntry = client.Stat(new EntryPath(bucket, bucketFileName));

            if (Cloud_type&&CloudJudgeInfoEntry.OK) //如果云端文件存在 且为 七牛云
            {
                WebClient wc1 = new WebClient();
                wc1.DownloadFileCompleted += new AsyncCompletedEventHandler(wc1_DownloadFileCompleted);
                wc1.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc1_DownloadProgressChanged);
                wc1.DownloadFileAsync(new Uri(CloudDataUrl), JudgeStdFile);
            }
            else if ((!Cloud_type) && (CheckCloudFileExist(CloudScriptUrl + "?problemid=" + Problem_ID.ToString()) == 3))
            {
              //  MessageBox.Show(CloudDataUrl);
                WebClient wc1 = new WebClient();
                wc1.DownloadFileCompleted += new AsyncCompletedEventHandler(wc1_DownloadFileCompleted);
                wc1.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc1_DownloadProgressChanged);
                wc1.DownloadFileAsync(new Uri(CloudDataUrl), JudgeStdFile);
            }
            else
            {  //云端无文件
                pictureBox6.Image = Image.FromFile("./res_pic/error.png");
                toolStripStatusLabel2.Text = "云端不存在此题号文件...";
                MessageBox.Show("云端不存在此序号的评测数据\n请与管理员联系！\n电子邮箱:admin@icore.com", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void wc1_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            pictureBox6.Image = Image.FromFile("./res_pic/run.png");
            skinProgressBar1.Maximum = (int)e.TotalBytesToReceive;
            skinProgressBar1.Value = (int)e.BytesReceived;
            toolStripStatusLabel2.Text = "正在同步云端数据...";
        }

        void wc1_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
           // Thread.Sleep(200);
            pictureBox6.Image = Image.FromFile("./res_pic/ok.png");
            skinProgressBar1.Value = 15;
            toolStripStatusLabel2.Text = "云端数据同步完毕！";

            toolStripStatusLabel2.Text = "开始分割数据块！";
            Thread.Sleep(120); //时间上缓冲一下文件I/O
            //云端数据同步完毕，开始分割数据；
            String[] data = File.ReadAllLines(JudgeStdFile, Encoding.Default);
            int arrayLen = data.Length;
            String strIdList = "";
            String temp = "";
            for (int i = 0; i < arrayLen; i++)
            {
                temp = data[i];
                temp = temp.Trim();
                if (temp == "#") //如果为标识符号 则添加数字进入数组 以空格分开
                {
                    strIdList += (" " + i.ToString());
                }
            }
            strIdList=strIdList.Trim();
            String[] arrIdList = strIdList.Split(new char[1] {' '}); //获取#号所在的行号 注意内容所在地址

            arrayLen = arrIdList.Length;
            if (arrayLen % 4 != 0)  //判断文件结构中#数量是否为4的倍数，如果为则文件结构正确 否则文件结构错误
            {
                pictureBox2.Image = Image.FromFile("./res_pic/error.png");
                toolStripStatusLabel2.Text = "云端数据存在结构错误！";
                MessageBox.Show("云端数据存在结构错误！\n请与管理员联系！\n电子邮箱:admin@icore.com", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                TotalCkeckTime = arrayLen / 4; //获取需要程序载入数据评测的次数
                pictureBox2.Image = Image.FromFile("./res_pic/run.png");
                for (int i1 = 0; i1 < TotalCkeckTime; i1++)  //文件分割操作 开始
                {
                    int[] i = new int[4];
                    for (int i2 = 0; i2 < 4; i2++)
                        i[i2] = Convert.ToInt32(arrIdList[i1 * 4 + i2]);
                    temp = JudgeStdInDir + i1.ToString() + ".in"; //构造out文件路径 带序号
                    //i[0] i[1] i[2] i[3]
                    String temp2 = "";
                    for (int i3 = i[0] + 1; i3 < i[1]; i3++)
                    {
                        if (temp2 == "") //此处判断 是为了去除开头换行
                            temp2 = data[i3];
                        else
                            temp2 += ("\r\n" + data[i3]);
                    }
                    File.WriteAllText(temp, temp2); //写入in文件

                    temp = JudgeStdOutDir + i1.ToString() + ".out"; //构造out文件路径 带序号
                    temp2 = "";
                    for (int i3 = i[2] + 1; i3 < i[3]; i3++)
                    {
                        if (temp2 == "")  //此处判断 是为了去除开头换行
                            temp2 = data[i3];
                        else
                            temp2 += ("\r\n" + data[i3]);
                    }
                    File.WriteAllText(temp, temp2); //写入out文件
                }  //文件分割操作 结束
                pictureBox2.Image = Image.FromFile("./res_pic/ok.png");
                skinProgressBar1.Value = 30;
                toolStripStatusLabel2.Text = "数据分割完毕！";

                //开始编译程序
                if (!File.Exists(CodeFile_Location)) //如果源码文件不存在
                {
                    pictureBox3.Image = Image.FromFile("./res_pic/error.png");
                    MessageBox.Show("源码文件不存在，无法执行编译操作！\n电子邮箱:admin@icore.com", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    if (File.Exists(JudgeCompile + "\\" + Problem_ID + ".exe")) //清空编译缓存文件
                        File.Delete(JudgeCompile + "\\" + Problem_ID + ".exe");
                    
                    compiler.EnableRaisingEvents = true;
                    compiler.StartInfo.UseShellExecute = false;
                    compiler.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    compiler.StartInfo.CreateNoWindow = true;
                    compiler.StartInfo.RedirectStandardError= true;//开启进程错误信息输出重定向
                    compiler.Exited += new EventHandler(compiler_Exited); //添加进程退出事件
                    compiler.StartInfo.Arguments = CodeFile_Location + " -o " + JudgeCompile + "\\" + Problem_ID + ".exe"; //运行参数
                    compiler.StartInfo.FileName = "D:\\CodeJudge_Studio\\GCC\\bin\\g++.exe "; //程序本体
                    compiler.Start();
                    Compiler_Error_output = compiler.StandardError.ReadToEnd().ToString(); //获取编译异常信息
                }
                

              //  MessageBox.Show("CPU时间："+compiler.TotalProcessorTime.TotalMilliseconds.ToString());
            }
        }

        void compiler_Exited(object sender, EventArgs e)
        {
            if (!File.Exists(JudgeCompile + "\\" + Problem_ID + ".exe"))
            {
                pictureBox3.Image = Image.FromFile("./res_pic/error.png");
                toolStripStatusLabel2.Text = "源代码编译失败,点击图标查看详情！";
                pictureBox3.Click += new EventHandler(pictureBox3_Click);
            }
            else
            {
                pictureBox3.Image = Image.FromFile("./res_pic/ok.png");
                toolStripStatusLabel2.Text = "源代码编译完成！";
                skinProgressBar1.Value = 50;
                compileTotalTime = compiler.TotalProcessorTime.TotalMilliseconds.ToString();//获取编译总共花费时间

                //编译完成 开始载入数据到程序中
                ExeRunner_sum_time = 0;
                ExeRunner_max_time = 0; //初始化实例运行最大时间
                ExeRunner_min_time = 10000;//初始化实例运行最小时间
                ExeRunner_memory = 0;//初始化实例运行最大内存
                
                for (int i = 0; i < TotalCkeckTime; i++) //根据此数字标识 判断程序执行几次 并载入数据
                { 
                    //构造参数 传统运行方式 不可行
                    //ExeRunner.StartInfo.Arguments = " << " + JudgeStdInDir + i.ToString() + ".in >>" + JudgeRunTemp + i.ToString()+".out";
                    //MessageBox.Show(ExeRunner.StartInfo.Arguments);
                    // ExeRunner.StartInfo.Arguments = " <" + JudgeStdInDir + i.ToString() + ".in ";

                    //清理以前程序运行所产生的out文件

                    if (ExeRunner_state == "ok")
                    {
                        toolStripStatusLabel2.Text = "正在运行第" + (i + 1).ToString() + "个实例！";
                        pictureBox4.Image = Image.FromFile("./res_pic/" + i.ToString() + ".png");
                        if (File.Exists(JudgeRunTemp + i.ToString() + ".out"))
                            File.Delete(JudgeRunTemp + i.ToString() + ".out");
                        ExeRunner = new Process();//创建数据管理进程
                        ExeRunner.StartInfo.UseShellExecute = false;
                        ExeRunner.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        ExeRunner.StartInfo.CreateNoWindow = true;
                        ExeRunner.StartInfo.FileName = JudgeCompile + "\\" + Problem_ID + ".exe"; //构造程序执行本体
                        ExeRunner.EnableRaisingEvents = true; //允许开启事件监听
                        ExeRunner.StartInfo.RedirectStandardOutput = true;
                        ExeRunner.StartInfo.RedirectStandardInput = true;
                        ExeRunner.Exited += new EventHandler(ExeRunner_Exited);
                        //ExeRunner.;      
                        ExeRunner.Start();
                        skinProgressBar1.Value = 60;

                          if (ExeRunner.WorkingSet64 > ExeRunner_memory) //获取物理内存使用情况 内存大小
                              ExeRunner_memory = ExeRunner.WorkingSet64;

                        String input = File.ReadAllText(JudgeStdInDir + i.ToString() + ".in");
                        ExeRunner.StandardInput.WriteLine(input);  //向进程标准输入系统中写入数据
                        ExeRunner_start_time = DateTime.Now;  //获取进程开始运行的时间            
                        if (!ExeRunner.WaitForExit(Convert.ToInt32(CpuTime.ToString())))//超时 无文件输出
                        {
                            // ExeRunner_state = "chaoshi"; //超时标记

                            if (!ExeRunner.HasExited)
                            {
                                ExeRunner.Kill();
                            }

                        }
                        else
                        {
                            
                            String output = ExeRunner.StandardOutput.ReadToEnd().ToString(); //获取控制台标准输出
                            if(output.Trim()!="")
                                 File.WriteAllText(JudgeRunTemp + i.ToString() + ".out", output); //把数据写入文件中  

                        }


                        if (File.Exists(JudgeRunTemp + i.ToString() + ".out") && ExeRunner_state == "ok")
                        {
                            pictureBox4.Image = Image.FromFile("./res_pic/ok.png");
                            // ExeRunner_state = "ok";
                        }
                        else if ((!File.Exists(JudgeRunTemp + i.ToString() + ".out")) && ExeRunner_state == "ok")
                        {
                            //运行时错误
                            ExeRunner_state = "runtimeerror"; //运行时错误
                            pictureBox4.Image = Image.FromFile("./res_pic/error.png");
                            break;
                        }
                        else
                        {
                            pictureBox4.Image = Image.FromFile("./res_pic/error.png");
                            break;
                        }
                    }
                    else
                    {
                        break; //不是OK状态了
                    }
                }


                while (!ExeRunner.HasExited) ;  //此空循环 是为了等待 进程关闭事件处理完毕 ExeRunner_state数据同步 千万不可以删除
                if (ExeRunner_state == "ok")
                {
                    skinProgressBar1.Value = 65;
                    //ExeRunner_avg_time = ExeRunner_sum_time / TotalCkeckTime;
                    if (TotalCkeckTime - 2 > 0)
                    {
                        ExeRunner_avg_time = (ExeRunner_sum_time - ExeRunner_max_time - ExeRunner_min_time) / (TotalCkeckTime - 2);
                        // MessageBox.Show("AVG Time：" + ExeRunner_avg_time.ToString("0.000"));
                    
                    //数据生成完毕 开始批对数据
                        JudgeDataComper();
 
                    }
                    else {
                        MessageBox.Show("错误：此评测数据量不足，请与管理员联系！\n电子邮箱:admin@icore.com", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (ExeRunner_state == "chaoshi")
                {
                    //  MessageBox.Show(ExeRunner_max_time.ToString());
                    toolStripStatusLabel2.Text = "错误：程序实例运行【超时】！";
                    MessageBox.Show("程序实例运行【超时】！", "运行超时", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    //MessageBox.Show(ExeRunner_state);
                    toolStripStatusLabel2.Text = "程序与系统环境发生【冲突】，请检查源码中【变量定义】部分！";
                    MessageBox.Show("错误：程序与系统环境发生【冲突】！", "系统冲突", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                
            }

        }

        void ExeRunner_Exited(object sender, EventArgs e)
        {
          //  throw new NotImplementedException();
           
           // MessageBox.Show("cpu time:" + CpuTime);
            if (ExeRunner.TotalProcessorTime.TotalMilliseconds > Convert.ToDouble(CpuTime)-30)
            {
                ExeRunner_state = "chaoshi"; //更新超时状态
             //   MessageBox.Show("run time:" + ExeRunner.TotalProcessorTime.TotalMilliseconds.ToString());
            }else  if (ExeRunner_state == "ok") //如果当前实例完美执行完后
                {
                    ExeRunner_end_time = DateTime.Now;
                    ExeRunner_total_time = ExeRunner_end_time - ExeRunner_start_time; //计算总共时间
                    ExeRunner_sum_time += Convert.ToDouble(ExeRunner_total_time.TotalMilliseconds.ToString());
                    // toolStripStatusLabel1.Text += (" "+ExeRunner_total_time.TotalMilliseconds.ToString("0.000"));
                    if (Convert.ToDouble(ExeRunner_total_time.TotalMilliseconds.ToString()) > ExeRunner_max_time)//获取实例运行最大时间
                    {
                        // MessageBox.Show(ExeRunner_max_time.ToString("0.000"));
                        ExeRunner_max_time = Convert.ToDouble(ExeRunner_total_time.TotalMilliseconds.ToString());
                    }
                    if (Convert.ToDouble(ExeRunner_total_time.TotalMilliseconds.ToString()) < ExeRunner_min_time)//获取实例运行最小时间
                    {
                        // MessageBox.Show(ExeRunner_max_time.ToString("0.000"));
                        ExeRunner_min_time = Convert.ToDouble(ExeRunner_total_time.TotalMilliseconds.ToString());
                    }


                }
            }


        void pictureBox3_Click(object sender, EventArgs e)
        {
            
         //   MessageBox.Show(Compiler_Error_output);
            frm_Info frmInfo = new frm_Info();
            frmInfo.infoType = "compileError";
            frmInfo.info = Compiler_Error_output;
            frmInfo.ShowDialog();
        }




        //初始化函数
        void Init()
        {
            saveCode();  //保存源码
            skinProgressBar1.Value = 5;
            downloadDataFile();
        }
        public Judge()
        {
            InitializeComponent();
        }

        private void Judge_Load(object sender, EventArgs e)
        {
            //获取云类型
            if (!File.Exists(Cloud_flag))
                Cloud_type = true; //默认七牛云
            else
            {
                Cloud_type = false;//自定义 私有云
                String[] temp = File.ReadAllLines(Cloud_flag, Encoding.Default);
                CloudDataUrl = temp[0].ToString()+"data/"; // 获取文件中保存的云URL链接
                CloudScriptUrl = temp[1].ToString();// 获取文件中保存的云端脚本URL链接

            }
        }

        private void Frm_Judge_unload(object sender, FormClosedEventArgs e)
        {
           
        }

        private void frm_Judge_Show(object sender, EventArgs e)
        {
            //构造工作必须目录 并创建目录
            problemDir = rootDir + Problem_ID+"\\"; //题目目录
            SourceDir = problemDir + "source\\";  //源码目录
            JudgeStd = problemDir + "judge_std\\";//网络标准评测数据目录
            JudgeCompile = problemDir + "judge_compile\\";//源码编译目录
            JudgeRunTemp = problemDir + "judge_runtemp\\";
            CodeFile_Location = SourceDir + Problem_ID + "." + CodeType; //构造源码文件保存完整路径
            CloudDataUrl = CloudDataUrl + Problem_ID + ".dat";//构造云端数据完整路径
            JudgeStdFile = JudgeStd + Problem_ID + ".dat";//标准评测数据完整本地路径 格式为.dat
            JudgeStdInDir = JudgeStd + "in\\"; //创建标准评测数据分割IN文件夹
            JudgeStdOutDir = JudgeStd + "out\\";//创建标准评测数据分割OUT文件夹


            if (!Directory.Exists(rootDir))
                Directory.CreateDirectory(rootDir);
            if (!Directory.Exists(problemDir))
                Directory.CreateDirectory(problemDir);
            if (!Directory.Exists(SourceDir))
                Directory.CreateDirectory(SourceDir);
            if (!Directory.Exists(JudgeStd))
                Directory.CreateDirectory(JudgeStd);
            if (!Directory.Exists(JudgeCompile))
                Directory.CreateDirectory(JudgeCompile);
            if (!Directory.Exists(JudgeRunTemp))
                Directory.CreateDirectory(JudgeRunTemp);
            if (!Directory.Exists(JudgeStdInDir))
                Directory.CreateDirectory(JudgeStdInDir);
            if (!Directory.Exists(JudgeStdOutDir))
                Directory.CreateDirectory(JudgeStdOutDir);

            //初始化环境
            Init();
        }
    }
}
