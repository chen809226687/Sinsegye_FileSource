using Sinsegye.Ide.Utilities.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FileSource.Models
{
    public class ImageData : ViewModelBase
    {
        private bool? ischeck;
        public bool? Ischeck
        {
            get { return ischeck; }
            set => this.SetProperty(ref this.ischeck, value);
        }

        public ImageSource preview;
        public ImageSource Preview
        {
            get { return preview; }
            set => this.SetProperty(ref this.preview, value);
        }

        private string fileName;
        public string FileName
        {
            get { return fileName; }
            set => this.SetProperty(ref this.fileName, value);
        }

        private string filePath;
        public string FilePath
        {
            get { return filePath; }
            set => this.SetProperty(ref this.filePath, value);
        }

        private string selectedFormat;
        public string SelectedFormat
        {
            get { return selectedFormat; }
            set => this.SetProperty(ref this.selectedFormat, value);
        }

        // 可选项
        public static List<string> Formats => new List<string> { "Auto", "RGB8", "RGB24" };



    }
}
