using Sinsegye.Ide.Utilities.Common;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace FileSource.Models
{
    public class FileSourceData : ViewModelBase
    {
        private string name;
        public string Name
        {
            get { return name; }
            set => this.SetProperty(ref this.name, value);
        }

        private string targetId;
        public string TargetId
        {
            get 
            {
                return targetId; 
            }
            set => this.SetProperty(ref this.targetId, value);
        }

        private UserControl view;
        public UserControl View
        {
            get { return view; }
            set => this.SetProperty(ref this.view, value);
        }
    }



}
