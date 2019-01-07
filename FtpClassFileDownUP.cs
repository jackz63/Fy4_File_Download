using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.IO;
using System.Text.RegularExpressions;                   //支持正则表达式。

namespace FtpClassFileDownUp
{
    public class FtpClassFileDownUP
    {
        //基本设置
        private string path;         //目标路径
        private string ftpip;        //ftp IP地址
        private string username;     //ftp用户名
        private string password;     //ftp密码

        public string strStatusView;
        public bool bCurProcStatus;

        /// <summary>
        /// 默认类构造函数，ftp类用基本默认参数实例化
        /// </summary>
        public FtpClassFileDownUP()
        {
            //基本设置
            path = @"ftp://" + "255.255.255.254" + "/";    //目标路径
            ftpip = "255.255.255.254";    //ftp IP地址
            username = "xxxx";   //ftp用户名
            password = "xxxx";   //ftp密码

            strStatusView = "";
            bCurProcStatus = true;
        }

        /// <summary>
        /// 构造函数，用用户提供参数实例化这个ftp类。
        /// </summary>
        /// <param name="theIP">ftp要访问的IP地址。</param>
        /// <param name="thePath">ftp要访问目标的路径名，形如：APath/Bpath/....</param>
        /// <param name="theUser">ftp访问节点的合法用户名。</param>
        /// <param name="thePassword">ftp访问节点的对应用户的密码。</param>
        public FtpClassFileDownUP(string theIP,string thePath,string theUser,string thePassword)
        {
            path = @"ftp://" + theIP + "/" + thePath;
            ftpip = theIP;
            username = theUser;
            password = thePassword;

            strStatusView = "";
            bCurProcStatus = true;
        }

        /// <summary>
        /// 获取ftp上面的文件和文件夹
        /// </summary>
        /// <returns>ftp访问当前路径下的文件名和目录名</returns>
        public string[] GetFileList()
        {
            string[] downloadFiles;
            StringBuilder result = new StringBuilder();
            FtpWebRequest request;
            try
            {
                request = (FtpWebRequest)FtpWebRequest.Create(new Uri(path));
                request.UseBinary = true;
                request.Credentials = new NetworkCredential(username, password);//设置用户名和密码
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.UseBinary = true;

                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());

                string line = reader.ReadLine();
                while (line != null)
                {
                    result.Append(line);
                    result.Append("\r\n");
                    line = reader.ReadLine();
                }
                // to remove the trailing "\r\n"
                result.Remove(result.ToString().LastIndexOf('\r'), 2);
                reader.Close();
                response.Close();
                bCurProcStatus = true;
                strStatusView = "获取文件文件夹成功！";
            }
            catch (Exception ex)
            {
                strStatusView = "获取文件文件夹失败！！！";
                bCurProcStatus = false;
                downloadFiles = null;
                return downloadFiles;
            }

            return result.ToString().Split('\n');
        }

        /// <summary>
        /// 获取文件大小
        /// </summary>
        /// <param name="file">ip服务器下的相对路径</param>
        /// <returns>文件大小</returns>
        public int GetFileSize(string file)
        {
            StringBuilder result = new StringBuilder();
            FtpWebRequest request;
            try
            {
                request = (FtpWebRequest)FtpWebRequest.Create(new Uri(path + file));
                request.UseBinary = true;
                request.Credentials = new NetworkCredential(username, password);//设置用户名和密码
                request.Method = WebRequestMethods.Ftp.GetFileSize;

                int dataLength = (int)request.GetResponse().ContentLength;
                strStatusView = "获取文件大小成功！\r\n";
                bCurProcStatus = true;

                return dataLength;
            }
            catch (Exception ex)
            {
                bCurProcStatus = false;
                strStatusView = "获取文件大小出错：" + ex.Message + "\r\n";
                return -1;
            }
        }

        /// <summary>
        /// 从ftp服务器上获得文件夹列表
        /// </summary>
        /// <returns>ftp访问当前路径下的文件目录名</returns>
        public List<string> GetDirctorys()
        {
            List<string> strs = new List<string>();
            try
            {
                string uri = path;
                FtpWebRequest reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
                // ftp用户名和密码
                reqFTP.Credentials = new NetworkCredential(username, password);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                WebResponse response = reqFTP.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());//中文文件名

                string line = reader.ReadLine();
                while (line != null)
                {
                    if (line.Contains("<DIR>"))
                    {
                        string msg = line.Substring(line.LastIndexOf("<DIR>") + 5).Trim();
                        strs.Add(msg);
                    }
                    line = reader.ReadLine();
                }
                reader.Close();
                response.Close();
                bCurProcStatus = true;
                strStatusView = "获取目录成功！\r\n";
                return strs;
            }
            catch (Exception ex)
            {
                bCurProcStatus = false;
                strStatusView = "获取目录出错：" + ex.Message + "\r\n";
            }
            return strs;
        }

        /// <summary>
        /// 从ftp服务器上获得文件列表
        /// </summary>
        /// <param name="receiveReg">文件名识别正则表达式</param>
        /// <returns>ftp访问当前路径下的文件名列表</returns>
        public List<string> GetFiles(Regex receiveReg)
        {
            List<string> strs = new List<string>();
            Regex regTmp = receiveReg;
            Match mtchTmp, mtchTmp0;
            Regex regTmp0 = new Regex(@"\d{14}");
            DateTime dtTmp;
            DateTime dtTheTime = DateTime.Now.AddHours(-1);         //这里只提取一小时以内的新文件。

            try
            {
                string uri = path;
                FtpWebRequest reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
                // ftp用户名和密码
                reqFTP.Credentials = new NetworkCredential(username, password);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                WebResponse response = reqFTP.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());       //中文文件名

                string msg = "";
                string line = reader.ReadLine();
                while (line != null)
                {
                    mtchTmp = regTmp.Match(line);
                    if (mtchTmp.Success)
                    {
                        msg = line.Trim();
                        mtchTmp0 = regTmp0.Match(msg);
                        dtTmp = DateTime.ParseExact(mtchTmp0.Value, "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
                        if (dtTmp > dtTheTime) strs.Insert(0, msg.Substring(msg.LastIndexOf(" ") + 1));
                    }
                    line = reader.ReadLine();
                }
                reader.Close();
                response.Close();
                bCurProcStatus = true;
                strStatusView = "获取文件成功！\r\n";
                return strs;
            }
            catch (Exception ex)
            {
                bCurProcStatus = false;
                strStatusView = "获取文件出错：" + ex.Message + "\r\n";
            }
            return strs;
        }

        /// <summary>
        /// ftp访问路径下读出某文件，转存到本地相应路径下。即：下载文件。
        /// </summary>
        /// <param name="localPath">下载文件存储的本地目录</param>
        /// <param name="fileName">下载文件名</param>
        public void Download(string localPath, List<string> filesLst)
        {
            FtpWebRequest reqFTP;
            string filePath;
            if (localPath == "")
                filePath = Environment.CurrentDirectory;
            else
                filePath = localPath;

            foreach (string fileName in filesLst)
            {
                try
                {
                    FileStream outputStream = new FileStream(filePath + "\\" + fileName, FileMode.Create);
                    reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(path + fileName));
                    reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                    reqFTP.UseBinary = true;
                    reqFTP.Credentials = new NetworkCredential(username, password);
                    reqFTP.UsePassive = false;
                    FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                    Stream ftpStream = response.GetResponseStream();
                    long cl = response.ContentLength;
                    int bufferSize = 2048;
                    int readCount;
                    byte[] buffer = new byte[bufferSize];
                    readCount = ftpStream.Read(buffer, 0, bufferSize);
                    while (readCount > 0)
                    {
                        outputStream.Write(buffer, 0, readCount);
                        readCount = ftpStream.Read(buffer, 0, bufferSize);
                    }
                    ftpStream.Close();
                    outputStream.Close();
                    response.Close();
                }
                catch (Exception ex)
                {
                    bCurProcStatus = false;
                    strStatusView = "下载文件失败！！！\r\n";
                    throw ex;
                }
            }

            bCurProcStatus = true;
            strStatusView = "下载文件成功！\r\n";
        }

        /// <summary>
        /// 文件上传
        /// </summary>
        /// <param name="filePath">原路径（绝对路径）包括文件名</param>
        /// <param name="objPath">目标文件夹：服务器下的相对路径 不填为根目录</param>
        public void FileUpLoad(string filePath, string objPath)
        {
            try
            {
                string url = path;
                if (objPath != "")
                    url += objPath + "/";
                try
                {

                    FtpWebRequest reqFTP = null;
                    //待上传的文件 （全路径）
                    try
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        using (FileStream fs = fileInfo.OpenRead())
                        {
                            long length = fs.Length;
                            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(url + fileInfo.Name));

                            //设置连接到FTP的帐号密码
                            reqFTP.Credentials = new NetworkCredential(username, password);
                            //设置请求完成后是否保持连接
                            reqFTP.KeepAlive = false;
                            //指定执行命令
                            reqFTP.Method = WebRequestMethods.Ftp.UploadFile;
                            //指定数据传输类型
                            reqFTP.UseBinary = true;

                            using (Stream stream = reqFTP.GetRequestStream())
                            {
                                //设置缓冲大小
                                int BufferLength = 5120;
                                byte[] b = new byte[BufferLength];
                                int i;
                                while ((i = fs.Read(b, 0, BufferLength)) > 0)
                                {
                                    stream.Write(b, 0, i);
                                }
                                bCurProcStatus = true;
                                strStatusView = "上传文件成功！\r\n";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        bCurProcStatus = false;
                        strStatusView = "上传文件失败！！！错误为：" + ex.Message + "\r\n";
                    }
                    finally
                    {

                    }
                }
                catch (Exception ex)
                {
                    bCurProcStatus = false;
                    strStatusView = "上传文件失败！！！错误为：" + ex.Message + "\r\n";
                }
                finally
                {

                }
            }
            catch (Exception ex)
            {
                bCurProcStatus = false;
                strStatusView = "上传文件失败！！！错误为：" + ex.Message + "\r\n";
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="fileName">服务器下的相对路径 包括文件名</param>
        public void DeleteFileName(string fileName)
        {
            try
            {
                FileInfo fileInf = new FileInfo(ftpip + "" + fileName);
                string uri = path + fileName;
                FtpWebRequest reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
                // 指定数据传输类型
                reqFTP.UseBinary = true;
                // ftp用户名和密码
                reqFTP.Credentials = new NetworkCredential(username, password);
                // 默认为true，连接不会被关闭
                // 在一个命令之后被执行
                reqFTP.KeepAlive = false;
                // 指定执行什么命令
                reqFTP.Method = WebRequestMethods.Ftp.DeleteFile;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                response.Close();
                bCurProcStatus = true;
                strStatusView = "删除文件成功！\r\n";
            }
            catch (Exception ex)
            {
                bCurProcStatus = false;
                strStatusView = "删除文件出错：" + ex.Message + "\r\n";
            }
        }

        /// <summary>
        /// 新建目录 上一级必须先存在
        /// </summary>
        /// <param name="dirName">服务器下的相对路径</param>
        public void MakeDir(string dirName)
        {
            try
            {
                string uri = path + dirName;
                FtpWebRequest reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
                // 指定数据传输类型
                reqFTP.UseBinary = true;
                // ftp用户名和密码
                reqFTP.Credentials = new NetworkCredential(username, password);
                reqFTP.Method = WebRequestMethods.Ftp.MakeDirectory;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                response.Close();
                bCurProcStatus = true;
                strStatusView = "创建目录成功！\r\n";
            }
            catch (Exception ex)
            {
                bCurProcStatus = false;
                strStatusView = "创建目录出错：" + ex.Message + "\r\n";
            }
        }

        /// <summary>
        /// 删除目录 上一级必须先存在
        /// </summary>
        /// <param name="dirName">服务器下的相对路径</param>
        public void DelDir(string dirName)
        {
            try
            {
                string uri = path + dirName;
                FtpWebRequest reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
                // ftp用户名和密码
                reqFTP.Credentials = new NetworkCredential(username, password);
                reqFTP.Method = WebRequestMethods.Ftp.RemoveDirectory;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                response.Close();
                bCurProcStatus = true;
                strStatusView = "删除目录成功！\r\n";
            }
            catch (Exception ex)
            {
                bCurProcStatus = false;
                strStatusView = "删除目录出错：" + ex.Message + "\r\n";
            }
        }

    }
}
