using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CCWin;
using System.Net;
using System.IO;

//基于api模式的云端检测模式 提高速度  和 降低并发数

namespace CodeJudge
{
    public partial class Frm_AddCloud : Skin_Mac
    {

        String Cloud_Url = "";
        String Cloud_Script_Url = "";
        String Cloud_flag = "D:\\CodeJudge_Studio\\cloud_flag.dat";

        int CheckUrlExist(String ApiScriptUrl) //检测脚本网页API接口是否完整
        {
            try
            {
                //Creating the HttpWebRequest
                HttpWebRequest request = WebRequest.Create(ApiScriptUrl) as HttpWebRequest;
                //request.Timeout = 800;
                //Setting the Request method HEAD, you can also use GET too.
                request.Method = "HEAD";
                //Getting the Web Response.
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //Returns TRUE if the Status code == 200

                if (response.StatusCode == HttpStatusCode.OK)
                { //有文件存在响应 开始读文件
                    WebClient wc1 = new WebClient();
                    String WebSource = wc1.DownloadString(ApiScriptUrl); //获取网页源码
                    if (WebSource.Trim() == "LinkOk")
                        return 3;//云端配置验证通过
                    else
                        return 2; //云端脚本文件配置错误
                }
                else
                {
                    return 1;//云端脚本文件不存在
                }
                

            }
            catch
            {
                //Any exception will returns false.
                return 0; //0 代表网络不可达
            }
        }


        public Frm_AddCloud()
        {
            InitializeComponent();
        }

        private void Frm_AddCloud_Load(object sender, EventArgs e)
        {

        }

        private void skinButton1_Click(object sender, EventArgs e)
        {
            Cloud_Script_Url = "";
            Cloud_Url = skinTextBox1.Text;
            Cloud_Url = Cloud_Url.Trim();
            char[] charsToTrim = { '/'};
            Cloud_Url = Cloud_Url.TrimEnd(charsToTrim); //去除结尾的反斜杠

            int check_intFlag = Cloud_Url.IndexOf("http://");
            if(check_intFlag!=0)
            {
                MessageBox.Show("请提供以http://开头的链接地址！", "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                toolStripStatusLabel2.Text = "请提供以http://开头的链接地址！";
                skinTextBox1.Text = "";
            }
            else
            {
                toolStripStatusLabel2.Text = "运行正常";
                Cloud_Script_Url = Cloud_Url+"/script.php";
               // MessageBox.Show(Cloud_Url);
                //检测 云端脚本的返回值 参数为check 返回值为LinkOk

                switch (CheckUrlExist(Cloud_Script_Url))
                { 
                    case 0:
                        MessageBox.Show("提供的云端链接不可到达！", "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        toolStripStatusLabel2.Text = "提供的云端链接不可达！";
                        break;
                    case 1:
                        MessageBox.Show("云端脚本文件不存在！", "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        toolStripStatusLabel2.Text = "云端脚本文件不存在！";
                        break;
                    case 2:
                        MessageBox.Show("云端脚本文件配置错误！", "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        toolStripStatusLabel2.Text = "云端脚本文件配置错误！";
                        break;
                    default:
                        MessageBox.Show("新云端环境配置完毕，确认重启！", "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        File.WriteAllText(Cloud_flag, Cloud_Url+"/"+Environment.NewLine+Cloud_Script_Url); //写入云端路径
                        Application.Restart();
                        break;
                }


            }
        }
    }
}
