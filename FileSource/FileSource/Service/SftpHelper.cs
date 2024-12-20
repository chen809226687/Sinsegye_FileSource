using FileSource.Models;
using FileSource.ViewModels;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace FileSource.Service
{
    public class SftpHelper
    {

        private string host;
        private int port;
        private string username;
        private string password;
        SftpClient sftp;
        SshClient sshClient;


        public SftpHelper(string host, int port, string username, string password)
        {
            this.host = host;
            this.port = port;
            this.username = username;
            this.password = password;
        }

        // 创建目录
        public ErrorCode CreateDirectory(string remoteDir)
        {
            ErrorCode errorCode = ErrorCode.None;
            using (var sshClient = new SshClient(host, port, username, password))
            {
                try
                {
                    // 连接到服务器
                    sshClient.Connect();
                    Console.WriteLine("Connected to the server.");
                    // 创建目录的命令
                    string command = $"echo {password} | sudo -S mkdir -p {remoteDir}";
                    string chmodCommand = $"echo {password} | sudo -S chmod 777 {remoteDir}";

                    // 执行命令
                    // 执行创建目录命令
                    var createDirCommand = sshClient.CreateCommand(command);
                    var result = createDirCommand.Execute();

                    // 执行 chmod 命令
                    var chmodCommandExec = sshClient.CreateCommand(chmodCommand);
                    var chmodResult = chmodCommandExec.Execute();

                }
                catch (Exception ex)
                {
                    return ErrorCode.SshErrorUploadFile;
                }
                finally
                {
                    // 断开连接
                    sshClient.Disconnect();
                }

                return errorCode;
            }
        }

        private bool DirectoryExists(SftpClient sftp, string remotePath)
        {
            try
            {
                var list = sftp.ListDirectory(remotePath);
                return list.Any();  // 如果目录中有内容，则认为目录存在
            }
            catch (SftpPathNotFoundException)
            {
                return false;  // 目录不存在
            }
        }

        // 上传文件
        string localPath = string.Empty;
        public ErrorCode UploadFile(ObservableCollection<ImageData> ImageDatas, string remoteFilePath,out string error)
        {
            error = "";
            ErrorCode errorCode = ErrorCode.None;
            using (var sftp = new SftpClient(host, port, username, password))
            {
                try
                {
                    sftp.Connect();
                    Console.WriteLine("Connected to the server.");
                    // 获取远程文件列表
                    var remoteFiles = sftp.ListDirectory(remoteFilePath);

                    //更新xml
                    using (var fileStream = System.IO.File.OpenRead(FileSourceViewModel.localFileSource + FileSourceViewModel.xmlName))
                    {
                        sftp.UploadFile(fileStream, remoteFilePath + "/" + FileSourceViewModel.xmlName);
                    }

                    foreach (var data in ImageDatas)
                    {
                        // 在 UI 线程上安全地访问 UriSource
                        localPath = data.FilePath;
                        // 判断远程是否已有该文件，若没有则上传
                        if (!remoteFiles.Any(f => f.Name == data.FileName))
                        {
                            using (var fileStream = System.IO.File.OpenRead(localPath))
                            {
                                sftp.UploadFile(fileStream, remoteFilePath + "/" + data.FileName);
                                Console.WriteLine($"File uploaded to: {remoteFilePath}/{data.FileName}");
                            }
                        }
                    }

                    // 删除远程多余的文件（如果远程文件本地没有）
                    foreach (var remoteFile in remoteFiles)
                    {
                        if (remoteFile.Name != "." && remoteFile.Name != "..")
                        {
                            // 检查本地是否存在此文件，如果不存在则删除
                            if (!ImageDatas.Any(d => d.FileName == remoteFile.Name) && remoteFile.Name != FileSourceViewModel.xmlName)
                            {
                                sftp.DeleteFile(remoteFile.FullName);  // 删除远程多余的文件
                                Console.WriteLine($"Deleted extra file from remote: {remoteFile.Name}");
                            }
                        }
                    }
                    return errorCode;

                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return ErrorCode.SshErrorUploadFile;
                }
                finally
                {
                    sftp.Disconnect();
                    Console.WriteLine("Disconnected from the server.");
                }
            }
        }

        // 下载文件
        public void DownloadFile(Guid guid, string remoteFilePath, ObservableCollection<ImageData> ImageDatas)
        {
            using (var sftp = new SftpClient(host, port, username, password))
            {
                try
                {
                    sftp.Connect();
                    Console.WriteLine("Connected to the server.");
                    string localPath = Path.Combine(Directory.GetCurrentDirectory(), "FileSource");
                    // 如果本地目录不存在，创建它

                    Directory.CreateDirectory(localPath);

                    localPath += "\\" + guid;
                    // 如果本地目录不存在，创建它
                    // if (!Directory.Exists(localPath))
                    {
                        Directory.CreateDirectory(localPath);
                    }

                    if (File.Exists(Path.Combine(FileSourceViewModel.localDir, FileSourceViewModel.xmlName)))
                    {
                        File.Delete(Path.Combine(FileSourceViewModel.localDir, FileSourceViewModel.xmlName));
                    }

                    // 获取远程目录下的文件和文件夹
                    var files = sftp.ListDirectory(remoteFilePath);
                    // 获取本地目录下的文件列表
                    var localFiles = Directory.GetFiles(localPath);

                    // 删除本地多余的文件（本地存在而远程不存在的文件）
                    foreach (var localFile in localFiles)
                    {
                        string localFileName = Path.GetFileName(localFile);
                        bool fileExistsOnRemote = files.Any(remoteFile => remoteFile.Name == localFileName);

                        if (!fileExistsOnRemote)
                        {
                            File.Delete(localFile);
                            Console.WriteLine($"Deleted extra file: {localFileName}");
                        }
                    }

                    // 下载远程缺少的文件（远程存在而本地不存在的文件）
                    foreach (var file in files)
                    {
                        if (file.Name == "." || file.Name == "..")
                            continue; // 跳过当前目录和上级目录

                        string remoteFilePathOne = file.FullName;
                        string localFilePath = Path.Combine(localPath, file.Name);

                        // 如果本地没有该文件，则下载
                        if (!localFiles.Contains(localFilePath))
                        {
                            using (var fileStream = File.OpenWrite(localFilePath))
                            {
                                sftp.DownloadFile(remoteFilePathOne, fileStream);
                                Console.WriteLine($"File downloaded: {localFilePath}");
                            }
                        }
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    sftp.Disconnect();
                    Console.WriteLine("Disconnected from the server.");
                }
            }
        }
    }
}
