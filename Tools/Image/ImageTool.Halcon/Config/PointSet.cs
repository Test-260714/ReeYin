using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ImageTool.Halcon.Config
{
    [Serializable]
    public class PointSet
    {
        private double[] _rows;
        private double[] _columns;
        private int _length = 0;

        [XmlElement(ElementName = "Row")]
        public double[] Rows
        {
            get { return this._rows; }
            set { this._rows = value; }
        }

        [XmlElement(ElementName = "Column")]
        public double[] Columns
        {
            get { return this._columns; }
            set { this._columns = value; }
        }

        [XmlElement(ElementName = "Length")]
        public int Length
        {
            get { return this._length; }
            set { this._length = value; }
        }


        private string color = "yellow";
        [XmlElement(ElementName = "Color")]
        public string Color
        {
            get { return this.color; }
            set { this.color = value; }
        }

        public PointSet()
        {

        }

        public PointSet(double[] rows, double[] columns)
        {
            if(rows.Length != columns.Length)
            {
                int minLength = Math.Min(rows.Length, columns.Length);
                Rows = new double[minLength]; 
                Columns = new double[minLength];
                for (int i = 0; i < minLength; i++)
                {
                    Rows[i] = rows[i];
                    Columns[i] = columns[i];
                }

                Length = minLength;
            }
            else
            {
                Rows = rows;
                Columns = columns;
                Length = rows.Length;
            }
        }

    }
}
