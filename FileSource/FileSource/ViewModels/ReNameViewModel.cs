using Sinsegye.Ide.Utilities.Common;
using System.Windows.Input;
using System.Windows;

namespace FileSource.ViewModels
{
    class ReNameViewModel : ViewModelBase
    {
        public string NewTabName { get; set; }
        public string TargetId { get; set; }

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public ReNameViewModel()
        {
            ConfirmCommand = new RelayCommand(Confirm);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Confirm(object parameter)
        {
            // 可以在这里添加一些验证逻辑
            if (!string.IsNullOrEmpty(NewTabName))
            {
                // 确认命令，弹窗关闭时返回 true
                ((Window)parameter).DialogResult = true;
            }
        }
        private void Cancel(object parameter)
        {
            // 取消操作：关闭窗口并返回 false
            ((Window)parameter).DialogResult = false;
        }

    }
}
