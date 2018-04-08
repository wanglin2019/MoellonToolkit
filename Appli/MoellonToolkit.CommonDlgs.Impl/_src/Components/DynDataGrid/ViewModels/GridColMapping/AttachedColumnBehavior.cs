﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MoellonToolkit.CommonDlgs.Impl.Components
{
    /// <summary>
    /// To manage dynamics columns of the grid.
    /// http://www.codeproject.com/Tips/676530/How-to-Add-Columns-to-a-DataGri
    /// 
    /// columns must inherit from: IGridColumnVM.
    /// </summary>
    public class AttachedColumnBehavior
    {
        public static readonly DependencyProperty HeaderTemplateProperty =
                DependencyProperty.RegisterAttached("HeaderTemplate",
                typeof(DataTemplate),
                typeof(AttachedColumnBehavior),
                new UIPropertyMetadata(null, OnHeaderTemplatePropertyChanged));

        public static readonly DependencyProperty AttachedColumnsProperty =
                DependencyProperty.RegisterAttached("AttachedColumns",
                typeof(IEnumerable),
                typeof(AttachedColumnBehavior),
                new UIPropertyMetadata(null, OnAttachedColumnsPropertyChanged));

        public static readonly DependencyProperty MappedCellsProperty =
                DependencyProperty.RegisterAttached("MappedCells",
                typeof(GridMappingCellsBase),
                typeof(AttachedColumnBehavior),
                new UIPropertyMetadata(null, OnMappedCellsPropertyChanged));


        public static readonly DependencyProperty AttachedCellTemplateProperty =
                DependencyProperty.RegisterAttached("AttachedCellTemplate",
                typeof(DataTemplate),
                typeof(AttachedColumnBehavior),
                new UIPropertyMetadata(null, OnCellTemplatePropertyChanged));

        public static readonly DependencyProperty AttachedCellEditingTemplateProperty =
            DependencyProperty.RegisterAttached("AttachedCellEditingTemplate",
            typeof(DataTemplate),
            typeof(DataGrid),
            new UIPropertyMetadata(null, OnCellEditingTemplatePropertyChanged));

        private static void OnAttachedColumnsPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = dependencyObject as DataGrid;
            if (dataGrid == null) 
                return;

            // load/refresh
            dataGrid.Loaded += (sender, args) => RefreshColumns(dataGrid, GetAttachedColumns(dataGrid));

            var columns = e.NewValue as INotifyCollectionChanged;
            if (columns == null)
                return;

            dataGrid.DataContextChanged += DataGrid_DataContextChanged;

            columns.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Remove)
                    RemoveColumns(dataGrid, args.OldItems);
                else if (args.Action == NotifyCollectionChangedAction.Add)
                    // adding a new col to existing ones
                    AddColumns(dataGrid, args.NewItems);
            };

            var items = dataGrid.ItemsSource as INotifyCollectionChanged;
            if (items != null)
                items.CollectionChanged += (sender, args) =>
                {
                    if (args.Action == NotifyCollectionChangedAction.Remove)
                        RemoveMappingByRow(dataGrid, args.NewItems);
                };

        }

        /// <summary>
        /// The content of the dataGrid has changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DataGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;

            // get the new value
            var dynDataGridVM = e.NewValue as DynDataGridVM;

            // get the new list of columns, from he ViewModel
            var columns = dynDataGridVM.CollColumnGrid; 

            // to refresh the UI header
            RefreshColumns(dataGrid, columns);
        }

        /// <summary>
        /// Refresh all columns of the dataGird.
        /// </summary>
        /// <param name="dataGrid"></param>
        /// <param name="columns"></param>
        private static void RefreshColumns(DataGrid dataGrid, IEnumerable columns)
        {
            // pb! si refresh complet, besoin sur ajout col!
            dataGrid.Columns.Clear();

            AddColumns(dataGrid, columns);
        }

        /// <summary>
        /// Add all columns to the dataGird.
        /// First time or refresh.
        /// </summary>
        /// <param name="dataGrid"></param>
        /// <param name="columns"></param>
        private static void AddColumns(DataGrid dataGrid, IEnumerable columns)
        {
            IGridColumnVM columnGridVM;

            // TODO: get the list ordered by the columnGridVM.DisplayIndex data!!
            foreach (var column in columns)
            {
                columnGridVM = column as IGridColumnVM;
                if (columnGridVM == null)
                    return;

                GridBoundColumn customBoundColumn = new GridBoundColumn()
                {
                    Header = column,
                    HeaderTemplate = GetHeaderTemplate(dataGrid),
                    CellTemplate = GetAttachedCellTemplate(dataGrid),
                    CellEditingTemplate = GetAttachedCellEditingTemplate(dataGrid),
                    GridMappingCells = GetMappedCells(dataGrid),
                };

                // the column position in the grid UI
                if (columnGridVM.DisplayIndex != -1)
                    customBoundColumn.DisplayIndex = columnGridVM.DisplayIndex;

                // TOOD: DisplayIndex must be less than Columns.Count !!
                // (is set on the add, no need to set it!)
                dataGrid.Columns.Add(customBoundColumn);
            }
        }

        private static void RemoveColumns(DataGrid dataGrid, IEnumerable columns)
        {
            foreach (var column in columns)
            {
                DataGridColumn col = dataGrid.Columns.Where(x => x.Header == column).Single();
                //GetMappedValues(dataGrid).RemoveByColumn(column);
                GetMappedCells(dataGrid).RemoveByColumn(column);
                dataGrid.Columns.Remove(col);
            }
        }

        private static void RemoveMappingByRow(DataGrid dataGrid, IEnumerable rows)
        {
            if (rows == null)
                return;

            foreach (var row in rows)
            {
                //GetMappedValues(dataGrid).RemoveByRow(row);
                GetMappedCells(dataGrid).RemoveByRow(row);
            }
        }

        #region OnChange handlers
        private static void OnCellTemplatePropertyChanged
        (DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {

        }
        private static void OnHeaderTemplatePropertyChanged
        (DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {

        }

        private static void OnCellEditingTemplatePropertyChanged
        (DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {

        }
        private static void OnMappedCellsPropertyChanged
        (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        #endregion

        /// <summary>
        /// Get the existing columns of the datagrid.
        /// </summary>
        /// <param name="dataGrid"></param>
        /// <returns></returns>
        public static IEnumerable GetAttachedColumns(DependencyObject dataGrid)
        {
            return (IEnumerable)dataGrid.GetValue(AttachedColumnsProperty);
        }

        public static void SetAttachedColumns(DependencyObject dataGrid, IEnumerable value)
        {
            dataGrid.SetValue(AttachedColumnsProperty, value);
        }

        public static GridMappingCellsBase GetMappedCells(DependencyObject dataGrid)
        {
            return (GridMappingCellsBase)dataGrid.GetValue(MappedCellsProperty);
        }

        public static void SetMappedCells(DependencyObject dataGrid, GridMappingCellsBase value)
        {
            dataGrid.SetValue(MappedCellsProperty, value);
        }

        public static DataTemplate GetHeaderTemplate(DependencyObject dataGrid)
        {
            return (DataTemplate)dataGrid.GetValue(HeaderTemplateProperty);
        }

        public static void SetHeaderTemplate(DependencyObject dataGrid, DataTemplate value)
        {
            dataGrid.SetValue(HeaderTemplateProperty, value);
        }

        public static DataTemplate GetAttachedCellTemplate(DependencyObject dataGrid)
        {
            return (DataTemplate)dataGrid.GetValue(AttachedCellTemplateProperty);
        }

        public static void SetAttachedCellTemplate(DependencyObject dataGrid, DataTemplate value)
        {
            dataGrid.SetValue(AttachedCellTemplateProperty, value);
        }

        public static DataTemplate GetAttachedCellEditingTemplate(DependencyObject dataGrid)
        {
            return (DataTemplate)dataGrid.GetValue(AttachedCellEditingTemplateProperty);
        }

        public static void SetAttachedCellEditingTemplate(DependencyObject dataGrid, DataTemplate value)
        {
            dataGrid.SetValue(AttachedCellEditingTemplateProperty, value);
        }
    }
}
