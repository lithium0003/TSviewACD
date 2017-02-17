using System;
using System.Collections;
using System.Linq;
using System.Windows.Forms;

namespace TSviewACD
{
    /// <summary>
    /// This class is an implementation of the 'IComparer' interface.
    /// </summary>
    public class ListViewColumnSorter : IComparer
    {
        /// <summary>
        /// 比較する方法
        /// </summary>
        public enum ComparerMode
        {
            /// <summary>
            /// 文字列として比較
            /// </summary>
            String,
            /// <summary>
            /// 数値（Int32型）として比較
            /// </summary>
            Integer,
            /// <summary>
            /// 日時（DataTime型）として比較
            /// </summary>
            DateTime
        };

        /// <summary>
        /// Specifies the column to be sorted
        /// </summary>
        private int ColumnToSort;
        /// <summary>
        /// Specifies the order in which to sort (i.e. 'Ascending').
        /// </summary>
        private SortOrder OrderOfSort;
        /// <summary>
        /// Case insensitive comparer object
        /// </summary>
        private CaseInsensitiveComparer ObjectCompare;
        /// <summary>
        /// 並べ替えの方法
        /// </summary>
        private ComparerMode[] _columnModes;

        /// <summary>
        /// Class constructor.  Initializes various elements
        /// </summary>
        public ListViewColumnSorter()
        {
            // Initialize the column to '0'
            ColumnToSort = 0;

            // Initialize the sort order to 'none'
            OrderOfSort = SortOrder.None;

            // Initialize the CaseInsensitiveComparer object
            ObjectCompare = new CaseInsensitiveComparer();
        }

        /// <summary>
        /// This method is inherited from the IComparer interface.  It compares the two objects passed using a case insensitive comparison.
        /// </summary>
        /// <param name="x">First object to be compared</param>
        /// <param name="y">Second object to be compared</param>
        /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
        public int Compare(object x, object y)
        {
            int compareResult;
            ListViewItem listviewX, listviewY;

            // Cast the objects to be compared to ListViewItem objects
            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;

            // Compare the two items
            switch ((ColumnToSort < _columnModes?.Length) ? _columnModes[ColumnToSort] : ComparerMode.String)
            {
                case ComparerMode.String:
                    compareResult = ObjectCompare.Compare(listviewX.SubItems[ColumnToSort].Text, listviewY.SubItems[ColumnToSort].Text);
                    break;
                case ComparerMode.Integer:
                    {
                        long x_int, y_int;
                        if (!long.TryParse(string.Concat(listviewX.SubItems[ColumnToSort].Text.Where(p => p != ',')), out x_int)) x_int = 0;
                        if (!long.TryParse(string.Concat(listviewY.SubItems[ColumnToSort].Text.Where(p => p != ',')), out y_int)) y_int = 0;
                        compareResult = x_int.CompareTo(y_int);
                    }
                    break;
                case ComparerMode.DateTime:
                    {
                        DateTime x_int, y_int;
                        if (!DateTime.TryParse(listviewX.SubItems[ColumnToSort].Text, out x_int)) x_int = new DateTime();
                        if (!DateTime.TryParse(listviewY.SubItems[ColumnToSort].Text, out y_int)) y_int = new DateTime();
                        compareResult = x_int.CompareTo(y_int);
                    }
                    break;
                default:
                    compareResult = 0;
                    break;
            }

            // Calculate correct return value based on object comparison
            if (OrderOfSort == SortOrder.Ascending)
            {
                // Ascending sort is selected, return normal result of compare operation
                return compareResult;
            }
            else if (OrderOfSort == SortOrder.Descending)
            {
                // Descending sort is selected, return negative result of compare operation
                return (-compareResult);
            }
            else
            {
                // Return '0' to indicate they are equal
                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
        /// </summary>
        public int SortColumn
        {
            set
            {
                ColumnToSort = value;
            }
            get
            {
                return ColumnToSort;
            }
        }

        /// <summary>
        /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
        /// </summary>
        public SortOrder Order
        {
            set
            {
                OrderOfSort = value;
            }
            get
            {
                return OrderOfSort;
            }
        }

        /// <summary>
        /// 列ごとの並び替えの方法
        /// </summary>
        public ComparerMode[] ColumnModes
        {
            set
            {
                _columnModes = value;
            }
        }
    }
}
