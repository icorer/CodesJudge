using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CCWin;

namespace CodeJudge
{
    public partial class frm_Info : Skin_Mac
    {
        public String info = "";
        public String infoType = "1";//1代表初始值
                                     //compileError :编译错误信息
        public frm_Info()
        {
            InitializeComponent();
        }

        private void frm_Info_Load(object sender, EventArgs e)
        {
            if (infoType == "compileError")
            {
                fastColoredTextBox1.Text = info;
                this.Text = "编译异常";
            }
            
        }
    }
}
