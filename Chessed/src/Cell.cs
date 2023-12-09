using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chessed
{
    internal class Cell
    {
        public int rowNumber;
        public int colNumber;
        public bool occupied;
        public bool legalNextMove;

        public Cell(int rowNumber, int colNumber)
        {
            this.rowNumber = rowNumber;
            this.colNumber = colNumber;
        }
    }
}