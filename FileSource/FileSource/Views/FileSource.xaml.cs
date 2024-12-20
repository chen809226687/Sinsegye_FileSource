using FileSource.Models;
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

namespace FileSource.Views
{
    /// <summary>
    /// FileSource.xaml 的交互逻辑
    /// </summary>
    public partial class FileSource : UserControl
    {
        private Point _dragStartPoint;
        private DataGridRow _draggedRow;
        private int _draggedRowIndex;
        public FileSource()
        {
            InitializeComponent();
        }

        private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var visual = (Visual)e.OriginalSource;

            _dragStartPoint = e.GetPosition(dataGrid);

            // 确定点击的是哪一行
            _draggedRow = GetDataGridRowUnderMouse(dataGrid, e);
            if (_draggedRow != null)
            {
                _draggedRowIndex = dataGrid.Items.IndexOf(_draggedRow.Item);
            }
        }

        private void DataGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedRow == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            // 判断拖动的距离
            var dataGrid = sender as DataGrid;
            var currentPosition = e.GetPosition(dataGrid);

            if (Math.Abs(currentPosition.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            // 触发拖动操作
            DragDrop.DoDragDrop(_draggedRow, _draggedRow, DragDropEffects.Move);
        }

        private void DataGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedRow == null)
                return;

            var dataGrid = sender as DataGrid;
            var targetRow = GetDataGridRowUnderMouse(dataGrid, e);

            if (targetRow != null && targetRow != _draggedRow)
            {
                // 计算目标行的位置
                int targetIndex = dataGrid.Items.IndexOf(targetRow.Item);

                // 更新数据集合顺序
                var viewModel = (ImageData)this.DataContext;
               // viewModel.ReorderImages(_draggedRowIndex, targetIndex);
            }

            _draggedRow = null;
        }

        private DataGridRow GetDataGridRowUnderMouse(DataGrid dataGrid, MouseEventArgs e)
        {
            var element = e.OriginalSource as DependencyObject;
            while (element != null && !(element is DataGridRow))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            return element as DataGridRow;
        }
    }
}

