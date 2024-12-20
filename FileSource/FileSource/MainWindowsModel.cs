using FileSource.Models;
using FileSource.ViewModels;
using FileSource.Views;
using Sinsegye.Ide.Utilities.Common;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileSource
{
    class MainWindowsModel : ViewModelBase
    {


        // ICommand 用于绑定到 View 中的按钮
        public ICommand ReNameCommand { get; }
        public ICommand DelectTagCommand { get; }


        public MainWindowsModel()
        {
            // 初始化命令
            ReNameCommand = new RelayCommand(OpenView);
            DelectTagCommand = new RelayCommand(DelectTag);
            var tab = new FileSourceData() { Name = "Test", TargetId = "192.168.110.153", View = new FileSource.Views.FileSource() };
            tab.View.DataContext = new FileSourceViewModel(tab.TargetId, Application.Current.Dispatcher);
            FileSourceDatas.Add(tab);
        }

        /// <summary>
        /// 打开弹窗的逻辑
        /// </summary>
        /// <param name="parameter"></param>
        private void OpenView(object parameter)
        {
            // 打开 ReName 窗体并获取返回的名称
            ReNameViewModel reNameViewModel = new ReNameViewModel();
            ReName reNameView = new ReName { DataContext = reNameViewModel };

            bool? result = reNameView.ShowDialog();  // 显示模态窗口

            if (result == true)
            {
                // 获取新的 Tab 名称
                var newTab = new FileSourceData { Name = reNameViewModel.NewTabName, TargetId =  reNameViewModel.TargetId , View = new FileSource.Views.FileSource() };
                // 添加新的项到 ObservableCollection
                newTab.View.DataContext = new FileSourceViewModel(newTab.TargetId, Application.Current.Dispatcher);
                FileSourceDatas.Add(newTab);
                // 设置当前选中的项为刚刚添加的项
                SelectedItemFileSourceDatas = newTab;
            }
        }

        /// <summary>
        ///删除标签
        /// </summary>
        /// <param name="parameter"></param>
        private void DelectTag(object tabItem)
        {


        }

        #region 数据

        public ObservableCollection<FileSourceData> fileSourceDatas = new ObservableCollection<FileSourceData>();
        public ObservableCollection<FileSourceData> FileSourceDatas
        {
            get { return fileSourceDatas; }
            set => this.SetProperty(ref this.fileSourceDatas, value);
        }

        public FileSourceData selectedItemFileSourceDatas = new FileSourceData();
        public FileSourceData SelectedItemFileSourceDatas
        {
            get { return selectedItemFileSourceDatas; }
            set => this.SetProperty(ref this.selectedItemFileSourceDatas, value);
        }



        #endregion


    }
}
