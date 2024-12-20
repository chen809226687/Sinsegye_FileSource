using FileSource.Models;
using FileSource.Service;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Sinsegye.Ide.Utilities.Common;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using static sinsegye.acpsharp.acp.native.ACPNativeMethods;

namespace FileSource.ViewModels
{
    public enum ErrorCode
    {
        None = 0,        // 没有错误

        SshErrorUploadFile = 100, // 无效输入
        SshErrorCreateDirectory = 101,
        Unauthorized = 102, // 未授权

        XMLDoesNotExist = 200,
        CreateXmlFail = 201,

        InternalError = 500 // 内部错误
    }

    class FileSourceViewModel : ViewModelBase
    {
        public ICommand ReadTargetCommand { get; }
        public ICommand AddFilesCommand { get; }
        public ICommand AddDirectoryCommand { get; }
        public ICommand CleanAllCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand SendDataCommand { get; }
        public ICommand DeleteImageDatasCommand { get; }
        public ICommand StartSendCommand { get; }
        public ICommand StopSendCommand { get; }
        public ICommand TriggerImageCommand { get; }

        private Guid guid;
        private SftpHelper sftpHelper;
        private AcpHelper acpHelper;

        public static string remoteDir;
        public static string xmlName = "ImageSources.xml";
        public static string localDir;
        public static string localFileSource = Path.Combine(Directory.GetCurrentDirectory(), "FileSource\\");
        private string _targetId;
        private Dispatcher _dispatcher;
        public FileSourceViewModel(string targetid, Dispatcher dispatcher)
        {
            _targetId = targetid;
            _dispatcher = dispatcher;
            var sshconfig = ReadJson(Path.Combine(Directory.GetCurrentDirectory(), "Configs", "SSHConfig.json"));
            sftpHelper = new SftpHelper(_targetId, sshconfig.port, sshconfig.username, sshconfig.password);
            acpHelper = new AcpHelper(targetid + ".1.1");
            guid = Guid.NewGuid();
            remoteDir = "/opt/SinsegyeRTE/project/vision/FileSource/" + guid;
            localDir = Path.Combine(Directory.GetCurrentDirectory(), "FileSource\\" + guid);
            ObjectId = guid.ToString();
            ImageDatas = new ObservableCollection<ImageData>();
            AddFilesCommand = new RelayCommand(AddFiles);
            AddDirectoryCommand = new RelayCommand(AddDirectory);
            CleanAllCommand = new RelayCommand(CleanAll);
            MoveUpCommand = new RelayCommand(MoveUp);
            MoveDownCommand = new RelayCommand(MoveDown);
            SendDataCommand = new RelayCommand(SendData);
            DeleteImageDatasCommand = new RelayCommand(DeleteImageDatas);
            ReadTargetCommand = new RelayCommand(ReadTarget);
            StartSendCommand = new RelayCommand(StartSend);
            StopSendCommand = new RelayCommand(StopSend);
            TriggerImageCommand = new RelayCommand(TriggerImage);
        }

        #region 交互逻辑

        /// <summary>
        /// 添加单个图片
        /// </summary>
        /// <param name="par"></param>
        public void AddFiles(object par)
        {
            // 打开文件选择对话框，启用多选
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Multiselect = true // 启用多选
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var filePath in openFileDialog.FileNames)
                {
                    // 获取文件名
                    string fileName = Path.GetFileName(filePath);

                    // 检查文件是否已经存在
                    if (ImageDatas.Any(img => img.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // 提示用户文件已存在
                        MessageBox.Show($"文件 {fileName} 已经存在！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue; // 跳过该文件
                    }

                    // 获取文件路径并转换为 ImageSource
                    ImageSource imageSource = LoadIcon(filePath);

                    // 将选择的图片添加到 DataGrid
                    ImageDatas.Add(new ImageData
                    {
                        Ischeck = true,
                        FileName = fileName,  // 图片文件的名称
                        Preview = imageSource,
                        SelectedFormat = ImageData.Formats[0],  // 假设你有一个图片格式列表
                        FilePath = filePath
                    });
                }
            }
            IsAllCheck = true;
        }

        /// <summary>
        /// 添加选择文件夹的图片
        /// </summary>
        /// <param name="par"></param>
        public void AddDirectory(object par)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            string tag = null;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folder = dlg.FileName;


                // 获取文件夹中的所有图片文件
                var imageFiles = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                                           .Where(file => new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" }
                                           .Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                                           .ToList();

                foreach (var filePath in imageFiles)
                {
                    // 获取文件名
                    string fileName = Path.GetFileName(filePath);

                    // 检查文件是否已经存在
                    if (ImageDatas.Any(img => img.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        tag += fileName + ";";
                        continue; // 跳过该文件
                    }
                    // 获取文件路径并转换为 ImageSource
                    ImageSource imageSource = LoadIcon(filePath);

                    // 将选择的图片添加到 DataGrid
                    ImageDatas.Add(new ImageData
                    {
                        Ischeck = true,
                        FileName = Path.GetFileName(filePath),  // 图片文件的名称
                        Preview = imageSource,
                        SelectedFormat = ImageData.Formats[0],  // 假设你有一个图片格式列表
                        FilePath = filePath,
                    });
                }
                if (tag != null)
                {
                    // 提示用户文件已存在
                    MessageBox.Show($"文件 {tag} 已经存在！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            IsAllCheck = true;
        }

        /// <summary>
        /// 清空
        /// </summary>
        /// <param name="par"></param>
        public void CleanAll(object par)
        {
            ImageDatas.Clear();
        }

        /// <summary>
        /// 上移
        /// </summary>
        /// <param name="par"></param>
        private void MoveUp(object par)
        {
            if (SelectedImageData != null && ImageDatas.IndexOf(SelectedImageData) > 0)
            {
                var currentIndex = ImageDatas.IndexOf(SelectedImageData);
                if (currentIndex > 0)
                {
                    ImageDatas.Move(currentIndex, currentIndex - 1);  // ObservableCollection 提供的 Move 方法
                }
            }
        }

        /// <summary>
        /// 下移
        /// </summary>
        /// <param name="par"></param>
        private void MoveDown(object par)
        {
            if (SelectedImageData != null && ImageDatas.IndexOf(SelectedImageData) < ImageDatas.Count - 1)
            {
                var currentIndex = ImageDatas.IndexOf(SelectedImageData);
                if (currentIndex < ImageDatas.Count - 1)
                {
                    ImageDatas.Move(currentIndex, currentIndex + 1);  // ObservableCollection 提供的 Move 方法
                }
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="par"></param>
        private void SendData(object par)
        {
            string message;
            string error;
            IsSendData = false;
            Task.Run(() =>
            {
                var xmlResult = CreateXml();
                if (xmlResult != ErrorCode.None)
                {
                    // 任务完成后通知 UI 更新
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsSendData = true;
                        // 在 UI 线程中执行更新操作
                        MessageBox.Show("照片源更新失败!" + xmlResult);
                    });
                    return;
                }
                sftpHelper.CreateDirectory(remoteDir);
                var uploadFileResult = sftpHelper.UploadFile(ImageDatas, remoteDir, out error);
                if (uploadFileResult != ErrorCode.None)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsSendData = true;
                        // 在 UI 线程中执行更新操作
                        MessageBox.Show("照片源更新失败!" + uploadFileResult + "-" + error);
                    });
                    return;
                }

                error = "";
                var acpResult = acpHelper.UpdataConfig(remoteDir + "/" + xmlName, out error);
                if (acpResult != AcpErrorCode.ACP_ERR_SUCCESS)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsSendData = true;
                        // 在 UI 线程中执行更新操作
                        MessageBox.Show("照片源更新失败!通讯失败：" + acpResult + "_" + error);
                    });
                    return;
                }
                // 任务完成后通知 UI 更新
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsSendData = true;
                    // 在 UI 线程中执行更新操作
                    MessageBox.Show("照片源更新成功!");
                });
            });
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="par"></param>
        private void ReadTarget(object par)
        {
            IsReadData = false;
            ImageDatas.Clear();
            Task.Run(() =>
            {
                sftpHelper.DownloadFile(guid, remoteDir, ImageDatas);
                var xmlResult = ReadXml();
                // 任务完成后通知 UI 更新
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (xmlResult == ErrorCode.None)
                    {
                        MessageBox.Show("数据读取完成!");
                    }
                    else
                    {
                        MessageBox.Show("数据读取失败!" + xmlResult);
                    }
                    IsReadData = true;
                    // 在 UI 线程中执行更新操作

                });
            });

        }

        /// <summary>
        /// 删除照片
        /// </summary>
        /// <param name="parameter"></param>
        private void DeleteImageDatas(object parameter)
        {
            if (selectedImageData != null)
            {
                ImageDatas.Remove(selectedImageData);
            }
        }

        /// <summary>
        /// 加载图片资源
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static BitmapImage LoadIcon(string filePath)
        {
            // 使用 FileStream 读取文件，而不是直接加载文件
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // 创建 BitmapImage 实例
                BitmapImage bitmapImage = new BitmapImage();
                // 打开文件并以内存方式加载
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;  // 使用文件流作为数据源
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 加载后释放文件资源
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        #endregion

        #region ACP交互
        /// <summary>
        /// 开始发送
        /// </summary>
        /// <param name="parameter"></param>
        private void StartSend(object parameter)
        {
            IsStartSend = false;
            IsStopSend = true;
            IsTriggerImage = false;
            IsStackPanelButton = false;
            IsDatagrid = false;

            var AcpResult = acpHelper.OpenOnline(guid.ToString());
            if (AcpResult != AcpErrorCode.ACP_ERR_SUCCESS)
            {
                MessageBox.Show("ACP通讯失败!" + AcpResult);
            }
        }

        /// <summary>
        /// 停止发送
        /// </summary>
        /// <param name="parameter"></param>
        private void StopSend(object parameter)
        {
            IsStartSend = true;
            IsStopSend = false;
            IsTriggerImage = true;
            IsStackPanelButton = true;
            IsDatagrid = true;

            acpHelper.CloseOnline(guid.ToString());
        }

        /// <summary>
        /// 单个播放
        /// </summary>
        /// <param name="parameter"></param>
        private void TriggerImage(object parameter)
        {
            acpHelper.TriggerImage(guid.ToString());
        }

        #endregion

        #region xml操作
        ErrorCode CreateXml()
        {
            try
            {
                // 创建一个新的 XML 文档
                XmlDocument xmlDoc = new XmlDocument();

                // 创建 XML 声明
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", null, null);
                xmlDoc.AppendChild(xmlDeclaration);

                // 创建根节点 <Vision>
                XmlElement visionElement = xmlDoc.CreateElement("Vision");
                xmlDoc.AppendChild(visionElement);

                // 创建 <FileSource> 元素
                XmlElement fileSourceElement = xmlDoc.CreateElement("FileSource");
                fileSourceElement.SetAttribute("objectid", guid.ToString());
                fileSourceElement.SetAttribute("dirpath", remoteDir + "/");
                visionElement.AppendChild(fileSourceElement);

                // 创建 <Config> 元素
                XmlElement configElement = xmlDoc.CreateElement("Config");
                fileSourceElement.AppendChild(configElement);

                // 创建 <triggermode> 和 <cycletimems> 元素
                CreateXmlElement(xmlDoc, configElement, "triggermode", triggerMode.ToString().ToLower());
                CreateXmlElement(xmlDoc, configElement, "cycletimems", cycletime);

                // 创建 <Images> 元素
                XmlElement imagesElement = xmlDoc.CreateElement("Images");
                fileSourceElement.AppendChild(imagesElement);

                foreach (var var in ImageDatas)
                {
                    CreateImageElement(xmlDoc, imagesElement, var.FileName, var.Ischeck.ToString().ToLower(), var.SelectedFormat);
                }

                Directory.CreateDirectory(localFileSource);
                // 保存 XML 文件
                xmlDoc.Save(localFileSource + "\\" + xmlName);
                return ErrorCode.None;
            }
            catch (Exception ex)
            {
                return ErrorCode.CreateXmlFail;
            }

        }

        // 辅助方法：创建 <triggermode> 和 <cycletimems> 元素
        private void CreateXmlElement(XmlDocument xmlDoc, XmlElement parentElement, string elementName, string elementValue)
        {
            XmlElement element = xmlDoc.CreateElement(elementName);
            element.InnerText = elementValue;
            parentElement.AppendChild(element);
        }
        // 辅助方法：创建 <Image> 元素
        private void CreateImageElement(XmlDocument xmlDoc, XmlElement parentElement, string imageName, string active, string format)
        {
            XmlElement imageElement = xmlDoc.CreateElement("Image");
            imageElement.SetAttribute("Active", active);
            imageElement.SetAttribute("Format", format);
            imageElement.InnerText = imageName;
            parentElement.AppendChild(imageElement);
        }

        ErrorCode ReadXml()
        {
            ErrorCode errorCode = ErrorCode.None;
            string xmlFilePath = localDir + "\\" + xmlName;

            // 加载 XML 文件
            // string xmlFilePath = "FileSource.xml"; // 你的 XML 文件路径
            XmlDocument xmlDoc = new XmlDocument();

            if (!File.Exists(xmlFilePath))
            {
                return ErrorCode.XMLDoesNotExist;
            }

            xmlDoc.Load(xmlFilePath);  // 加载 XML 文件

            // 获取根节点 Vision
            XmlNode visionNode = xmlDoc.SelectSingleNode("Vision");
            Console.WriteLine("Root node: " + visionNode.Name);

            // 获取 FileSource 元素
            XmlNode fileSourceNode = visionNode.SelectSingleNode("FileSource");
            Console.WriteLine("FileSource node: " + fileSourceNode.Name);

            // 获取 FileSource 元素的属性
            string objectId = fileSourceNode.Attributes["objectid"].Value;
            string dirPath = fileSourceNode.Attributes["dirpath"].Value;
            Console.WriteLine($"objectid: {objectId}, dirpath: {dirPath}");

            // 获取 Config 元素
            XmlNode configNode = fileSourceNode.SelectSingleNode("Config");

            // 获取 triggermode 和 cycletimems 元素
            string triggerMode = configNode.SelectSingleNode("triggermode").InnerText;
            string cycleTime = configNode.SelectSingleNode("cycletimems").InnerText;
            TriggerMode = bool.Parse(triggerMode);
            Cycletime = cycleTime;

            // 获取 Images 元素
            XmlNode imagesNode = fileSourceNode.SelectSingleNode("Images");
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 遍历 Images 中的所有 Image 元素
                foreach (XmlNode imageNode in imagesNode.ChildNodes)
                {
                    // 获取每个 <Image> 元素的属性
                    string active = imageNode.Attributes["Active"]?.Value;
                    string format = imageNode.Attributes["Format"]?.Value;

                    // 获取 <Image> 元素的文本内容
                    string fileName = imageNode.InnerText;

                    ImageDatas.Add(new ImageData
                    {
                        Ischeck = bool.Parse(active),
                        FileName = fileName,
                        SelectedFormat = ImageData.Formats.Where(s => s == format).ToList().FirstOrDefault(),
                        Preview = LoadIcon(localDir + "\\" + fileName),
                        FilePath = localDir + "\\" + fileName,
                    });
                }
            });

            return errorCode;
        }


        #endregion

        #region Json操作
        public static SSHConfig ReadJson(string filePath)
        {
            // 读取 JSON 文件
            string json = File.ReadAllText(filePath);

            // 反序列化 JSON 数据到 Person 对象
            SSHConfig sSHConfig = JsonConvert.DeserializeObject<SSHConfig>(json);

            return sSHConfig;
        }

        #endregion

        #region 绑定数据

        private string cycletime = "500";
        public string Cycletime
        {
            get { return cycletime; }
            set => this.SetProperty(ref this.cycletime, value);
        }

        private string objectId;
        public string ObjectId
        {
            get { return objectId; }
            set => this.SetProperty(ref this.objectId, value);
        }

        private bool triggerMode;
        public bool TriggerMode
        {
            get { return triggerMode; }
            set => this.SetProperty(ref this.triggerMode, value);
        }

        private bool isDatagrid = true;
        public bool IsDatagrid
        {
            get { return isDatagrid; }
            set => this.SetProperty(ref this.isDatagrid, value);
        }

        private bool isStackPanelButton = true;
        public bool IsStackPanelButton
        {
            get { return isStackPanelButton; }
            set => this.SetProperty(ref this.isStackPanelButton, value);
        }

        private bool isSendData = true;
        public bool IsSendData
        {
            get { return isSendData; }
            set => this.SetProperty(ref this.isSendData, value);
        }

        private bool isReadData = true;
        public bool IsReadData
        {
            get { return isReadData; }
            set => this.SetProperty(ref this.isReadData, value);
        }

        private bool isStartSend = true;
        public bool IsStartSend
        {
            get { return isStartSend; }
            set => this.SetProperty(ref this.isStartSend, value);
        }

        private bool isStopSend = false;
        public bool IsStopSend
        {
            get { return isStopSend; }
            set => this.SetProperty(ref this.isStopSend, value);
        }

        private bool isTriggerImage = true;
        public bool IsTriggerImage
        {
            get { return isTriggerImage; }
            set => this.SetProperty(ref this.isTriggerImage, value);
        }

        public ObservableCollection<ImageData> imageDatas;
        public ObservableCollection<ImageData> ImageDatas
        {
            get { return imageDatas; }
            set => this.SetProperty(ref this.imageDatas, value);
        }

        //public ObservableCollection<ImageData> selectedImageDatas;
        //public ObservableCollection<ImageData> SelectedImageDatas
        //{
        //    get { return selectedImageDatas; }
        //    set => this.SetProperty(ref this.selectedImageDatas, value);
        //}

        public ImageData selectedImageData = new ImageData();
        public ImageData SelectedImageData
        {
            get { return selectedImageData; }
            set => this.SetProperty(ref this.selectedImageData, value);
        }

        // 用来控制全选和取消全选的属性
        private bool _isAllCheck;
        public bool IsAllCheck
        {
            get { return _isAllCheck; }
            set
            {
                this.SetProperty(ref this._isAllCheck, value);
                SetAllItemsCheckState(_isAllCheck);
            }
        }

        // 更新所有项的 Ischeck 状态
        private void SetAllItemsCheckState(bool isChecked)
        {
            foreach (var item in ImageDatas)
            {
                item.Ischeck = isChecked;
            }
        }
        #endregion

    }
}
