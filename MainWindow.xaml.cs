using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.ComponentModel;                            //包含事件接口，如：INotifyPropertyChanged。
using System.Windows.Threading;                         //包含时钟中断，DispatcherTimer。
using System.Text.RegularExpressions;                   //支持正则表达式。
using System.Security.Permissions;                      //支持DoEvents()。

namespace DownFy4File
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>设定一个绑定显示文本框的字符串，用于中断处理时对显示框的显示状态操作。中断状态下无法获得文本框的控制权。</summary>
        ViewTxt dispStr;
        /// <summary>设定一个绑定显示文本框的绑定</summary>
        System.Windows.Data.Binding dispTxtBinding;
        /// <summary>设置滚动播放显示的时间中断</summary>
        DispatcherTimer timer = new DispatcherTimer();
        /// <summary>记忆启动程序的目录,要求连带的动态链接库,数据库必须与应用程序同目录.</summary>
        string curDir;
        /// <summary>标记当前状态是否是自动检索下载状态.</summary>
        bool bSelfActing;
        /// <summary>定义当前Ftp类的实例.</summary>
        FtpClassFileDownUp.FtpClassFileDownUP theCurFtp;
        /// <summary>定义当前ftp路径.</summary>
        string curFtppath;
        /// <summary>定义当前ftp地址IP.</summary>
        string curFtpip;
        /// <summary>定义当前ftp用户名.</summary>
        string curUsername;
        /// <summary>定义当前ftp用户密码.</summary>
        string curPassword;
        /// <summary>上下载文件本地路径.</summary>
        string localFilePath;
        /// <summary>当前时钟触发检索的时间间隔</summary>
        int timeLag;
        /// <summary>下载文件在本地路径保留的天数</summary>
        int dayNum;
        /// <summary>ftp文件名识别特征正则表达式</summary>
        Regex regFileID;
        /// <summary>当前ftp访问路径下的较新(一小时内)符合甄别特征正则表达式的文件名列表</summary>
        List<string> theFilenameLst;
        /// <summary>记录前一次下载文件中最晚的时间，新的下载从这个时间之后的文件下载</summary>
        DateTime dtLastFileTime;
        /// <summary>记录前一次清理过期文件的时间，增加一个判断，以免随时钟中断频繁检查文件目录</summary>
        DateTime dtDayBase;
        /// <summary>记录Mouse的当前状态</summary>
        Cursor mouseMap;
        Brush btnBackground;

        DirectoryInfo checkPath;
        FileInfo[] checkPathfiles;

        public MainWindow()
        {
            InitializeComponent();
            curDir = Environment.CurrentDirectory;
            mouseMap = Mouse.OverrideCursor;
            btnBackground = btnAutoStart.Background.Clone();

            dispStr = new ViewTxt();
            dispTxtBinding = new Binding();
            dispTxtBinding.Source = dispStr;
            dispTxtBinding.Path = new PropertyPath("txtLongStr");
            BindingOperations.SetBinding(this.txtDisp, System.Windows.Controls.TextBox.TextProperty, dispTxtBinding);

            bool bInit = getIni();

            timer.Tick += new EventHandler(NewFileSearching);
            bool btmp = int.TryParse(txtTimeLag.Text, out timeLag);
            timer.Interval = (btmp) ? TimeSpan.FromMinutes(timeLag) : TimeSpan.FromMinutes(3);   //设置刷新的间隔时间
            dtLastFileTime = DateTime.Now.AddMinutes(-20);
            dtDayBase = DateTime.Now.AddDays(-1);
            bSelfActing = false;
            timer.Stop();
        }

        private void btnAutoStart_Click(object sender, RoutedEventArgs e)
        {
            bool btmp = false;
            System.Windows.Media.Color cSalmon = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("Salmon");
            if (!bSelfActing)
            {
                Regex regTmp = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$");
                Match mtchTmp = regTmp.Match(txtFtpIP.Text);
                btmp = mtchTmp.Success;
                curFtpip = (btmp) ? mtchTmp.Value : "";
                regTmp = new Regex(@"^\d{1,2}$");
                mtchTmp = regTmp.Match(txtTimeLag.Text);
                btmp = (btmp && mtchTmp.Success);
                btmp = (btmp && int.TryParse(txtTimeLag.Text, out timeLag));
                btmp = (btmp && ((timeLag > 0) && (timeLag <= 60)));
                mtchTmp = regTmp.Match(txtDayNum.Text);
                btmp = (btmp && mtchTmp.Success);
                btmp = (btmp && int.TryParse(txtDayNum.Text, out dayNum));
                btmp = (btmp && (dayNum >= 1));
                btmp = (btmp && !(txtFtpPath.Text == "") && !(txtUserName.Text == "") && !(txtPassWord.Text == "") && 
                    !(txtFileRegex.Text == "") && !(txtDownPath.Text == ""));
                if (!btmp)
                {
                    dispStr.txtLongStr = "Ftp参数不合理，请核查，无法启动自动侦测下载！！！\r\n";
                    timer.Stop();
                    return;
                }
                curFtppath = txtFtpPath.Text;
                curUsername = txtUserName.Text;
                curPassword = txtPassWord.Text;
                regFileID = new Regex(txtFileRegex.Text);
                localFilePath = txtDownPath.Text;           //启动FTP访问前，再次检查调用参数的合理性,特别是本地存储路径和已有文件列表。
                checkPath = new DirectoryInfo(localFilePath);

                Mouse.OverrideCursor = Cursors.Wait;
                txtFtpIP.IsEnabled = false;
                txtFtpPath.IsEnabled = false;
                txtUserName.IsEnabled = false;
                txtPassWord.IsEnabled = false;
                txtFileRegex.IsEnabled = false;
                txtDownPath.IsEnabled = false;
                txtTimeLag.IsEnabled = false;
                txtDayNum.IsEnabled = false;
                btnAutoStart.Content = "停止自动侦测下载";
                btnAutoStart.Background = new SolidColorBrush(cSalmon);
                theCurFtp = new FtpClassFileDownUp.FtpClassFileDownUP(curFtpip, curFtppath, curUsername, curPassword);
                dtLastFileTime = DateTime.Now.AddMinutes(-20);
                dtDayBase = DateTime.Now.AddDays(-1);
                DispatcherHelper.DoEvents();
                btmp = timerSub();

                if (btmp)
                {
                    bSelfActing = true;
                    timer.Start();
                }
                else
                {
                    txtFtpIP.IsEnabled = true;
                    txtFtpPath.IsEnabled = true;
                    txtUserName.IsEnabled = true;
                    txtPassWord.IsEnabled = true;
                    txtFileRegex.IsEnabled = true;
                    txtDownPath.IsEnabled = true;
                    txtTimeLag.IsEnabled = true;
                    txtDayNum.IsEnabled = true;
                    btnAutoStart.Background = btnBackground;
                    btnAutoStart.Content = "启动自动侦测下载";
                    Mouse.OverrideCursor = mouseMap;
                    dispStr.txtLongStr = "启动FTP访问和下载文件失败！！！\r\n";
                    timer.Stop();
                    return;
                }

            }
            else
            {
                timer.Stop();
                bSelfActing = false;
                txtFtpIP.IsEnabled = true;
                txtFtpPath.IsEnabled = true;
                txtUserName.IsEnabled = true;
                txtPassWord.IsEnabled = true;
                txtFileRegex.IsEnabled = true;
                txtDownPath.IsEnabled = true;
                txtTimeLag.IsEnabled = true;
                txtDayNum.IsEnabled = true;
                btnAutoStart.Background = btnBackground;
                btnAutoStart.Content = "启动自动侦测下载";
                Mouse.OverrideCursor = mouseMap;
            }
        }

        /// <summary>
        /// 按照时钟触发执行新文件检索下载。
        /// </summary>
        private void NewFileSearching(object sender, EventArgs e)
        {
            dispStr.txtLongStr = "定时触发启动FTP访问和下载文件启动中......！\r\n" + dispStr.txtLongStr;
            //checkPathfiles = checkPath.GetFiles();
            DispatcherHelper.DoEvents();

            bool bTmp = timerSub();
            if (!bTmp)
            {
                timer.Stop();
                bSelfActing = false;
                txtFtpIP.IsEnabled = true;
                txtFtpPath.IsEnabled = true;
                txtUserName.IsEnabled = true;
                txtPassWord.IsEnabled = true;
                txtFileRegex.IsEnabled = true;
                txtDownPath.IsEnabled = true;
                txtTimeLag.IsEnabled = true;
                txtDayNum.IsEnabled = true;
                btnAutoStart.Background = btnBackground;
                btnAutoStart.Content = "启动自动侦测下载";
                Mouse.OverrideCursor = mouseMap;
                dispStr.txtLongStr = "定时触发启动FTP访问和下载文件失败！！！\r\n" + dispStr.txtLongStr;
            }
            else
            {
                dispStr.txtLongStr = "定时触发启动FTP访问和下载文件成功！\r\n" + dispStr.txtLongStr;
            }

        }

        /// <summary>
        /// 为时钟触发侦测下载设立的子程序，便于操作。
        /// </summary>
        private bool timerSub()
        {
            bool bReturnValue = false;
            DateTime dtTmp;
            DateTime dtTmpStart = dtLastFileTime.AddMinutes(-30);      //这个下载不执行30分钟以前的任务，这样的原因是避免下载量太大。
            DateTime dtTmpMax =  dtTmpStart;
            //dtLastFileTime = (dtTmpMax > dtLastFileTime) ? dtTmpMax : dtLastFileTime;
            List<string> filesCorrespond = new List<string>();
            Regex regTime = new Regex(@"\d{14}");

            Match mtchTime;

            theFilenameLst = theCurFtp.GetFiles(regFileID);
            bReturnValue = theCurFtp.bCurProcStatus;
            dispStr.txtLongStr = theCurFtp.strStatusView + dispStr.txtLongStr;
            foreach (string strTmp in theFilenameLst) 
            {
                mtchTime = regTime.Match(strTmp);
                dtTmp = DateTime.ParseExact(mtchTime.Value, "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
                if ((dtTmp > dtTmpStart) && (!(File.Exists(localFilePath + "\\" + strTmp))))
                {
                    filesCorrespond.Add(strTmp);
                    dtTmpMax = (dtTmp > dtTmpMax) ? dtTmp : dtTmpMax;   //将最晚的文件名内时间，也就是最晚观测时间筛选记录下来。
                }
            }

            theCurFtp.Download(localFilePath, filesCorrespond);
            bReturnValue = theCurFtp.bCurProcStatus;
            dtLastFileTime = (bReturnValue) ? dtTmpMax : dtLastFileTime;        //确认下载执行没毛病，则记录下下载文件中最晚的文件名内时间。
            dispStr.txtLongStr = theCurFtp.strStatusView + "\r\n当前下载文件最后观测时间：" + dtLastFileTime.ToShortDateString() + "日" 
               + dtLastFileTime.ToShortTimeString() + "时\r\n" + dispStr.txtLongStr;

            TimeSpan tsDayTmp = new TimeSpan(0, 0, 0, 0);
            dtTmp = DateTime.Now;
            tsDayTmp = dtTmp.Subtract(dtDayBase);
            if (tsDayTmp.TotalHours > 6)
            {
                bool bCancel = false;
                //checkPath = new DirectoryInfo(localFilePath);
                checkPathfiles = checkPath.GetFiles();
                foreach (FileInfo tmpFile in checkPathfiles)
                {
                    tsDayTmp = dtTmp.Subtract(tmpFile.CreationTime);
                    if (tsDayTmp.TotalDays > dayNum)
                    {
                        try
                        {
                            tmpFile.Delete();
                            bCancel = true;
                        }
                        catch (Exception ecpt)
                        {
                            dispStr.txtLongStr = "清除过期文件时发生意外：" + ecpt.Message + "\r\n" + dispStr.txtLongStr;
                            bCancel = false;
                            break;
                        }
                    }
                }
                dtDayBase = bCancel ? dtTmp : dtDayBase;
            }

            return bReturnValue;
        }

        /// <summary>处理程序启动后初始参数的初始化，方法是读入与运行程序同目录下的一个初始化文本文件。</summary>
        private bool getIni()
        {
            bool btmp = false;
            Regex regTmp;
            Match mtchTmp;
            try
            {
                btmp = File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Ftp操作类程序初始化参数.ini");
                if (!btmp)
                {
                    dispStr.txtLongStr = "程序初始化文件有错误，请退出查找原因，不建议继续执行！！！\r\n";
                    return btmp;
                }
                else
                {
                    string strLine = string.Empty;
                    List<string> iniFileContent = new List<string>();
                    using (StreamReader r_ini = new StreamReader("Ftp操作类程序初始化参数.ini"))
                    {
                        strLine = r_ini.ReadLine();
                        while (strLine != null)
                        {
                            iniFileContent.Add(strLine);
                            strLine = r_ini.ReadLine();
                        }
                    }
                    int iTmp = 0;
                    foreach (string strTmp in iniFileContent)
                    {
                        iTmp = strTmp.IndexOf("=") + 1;
                        if (strTmp.Contains("address ip="))
                        {
                            txtFtpIP.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                            regTmp = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$");
                            mtchTmp = regTmp.Match(txtFtpIP.Text);
                            btmp = mtchTmp.Success;
                            curFtpip = (btmp) ? mtchTmp.Value : "";
                        }
                        else if (strTmp.Contains("sourcefile path="))
                        {
                            txtFtpPath.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                            curFtppath = txtFtpPath.Text;
                        }
                        else if (strTmp.Contains("user name="))
                        {
                            txtUserName.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                            curUsername = txtUserName.Text;
                        }
                        else if (strTmp.Contains("user password="))
                        {
                            txtPassWord.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                            curPassword = txtPassWord.Text;
                        }
                        else if (strTmp.Contains("filename regex="))
                        {
                            txtFileRegex.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                            regFileID = new Regex(txtFileRegex.Text);
                        }
                        else if (strTmp.Contains("aimfile path="))
                        {
                            txtDownPath.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                            localFilePath = txtDownPath.Text;
                        }
                        else if (strTmp.Contains("time interval="))
                        {
                            txtTimeLag.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                            regTmp = new Regex(@"^\d{1,2}$");
                            mtchTmp = regTmp.Match(txtTimeLag.Text);
                            btmp = mtchTmp.Success;
                            btmp = (btmp && int.TryParse(txtTimeLag.Text, out timeLag));
                            btmp = (btmp && ((timeLag > 0) && (timeLag <= 60)));
                        }
                        else if (strTmp.Contains("file retention="))
                        {
                            txtDayNum.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                            regTmp = new Regex(@"^\d{1,2}$");
                            mtchTmp = regTmp.Match(txtDayNum.Text);
                            btmp = mtchTmp.Success;
                            btmp = (btmp && int.TryParse(txtTimeLag.Text, out dayNum));
                            btmp = (btmp && (dayNum > 0));
                        }
                    }
                }
            }
            catch (Exception excp)
            {
                dispStr.txtLongStr = "程序初始化文件有错误，请退出查找原因，不建议继续执行！！！\r\n" + excp.Message + "\r\n";
                btmp = false;
                return btmp;
            }

            if (!btmp) dispStr.txtLongStr = "初始化参数是不合理的参数，请核查，不建议继续执行！！！\r\n";
            return btmp;
        }

    }

    /// <summary>
    /// 设定一个字符串类的变更通知触发，用于与显示控件绑定。
    /// </summary>
    class ViewTxt : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string txtStr;

        public string txtLongStr
        {
            get { return txtStr; }
            set
            {
                txtStr = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("txtLongStr"));
                }
            }
        }
    }


    /// <summary>
    /// 这里设计了一个类似WindowsForm里的DoEvents的类，用于批量处理文件时避免界面假死机。
    /// </summary>
    public class DispatcherHelper
    {
        /// <summary>
        /// Simulate Application.DoEvents function of <see cref=" System.Windows.Forms.Application"/> class.
        /// </summary>
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrames), frame);

            try
            {
                Dispatcher.PushFrame(frame);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static object ExitFrames(object frame)
        {
            ((DispatcherFrame)frame).Continue = false;
            return null;
        }
    }
}
