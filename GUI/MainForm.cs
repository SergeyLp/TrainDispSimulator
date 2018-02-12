using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GUI {
    public partial class mainForm : Form {
        public mainForm() {
            InitializeComponent();
        }

        private void OnPaint(object sender, PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            //e.Graphics.ResetTransform();
            //e.Graphics.TranslateTransform(10, 10);

            Pen pen = new Pen(Color.FromArgb(255, 0, 0, 0));
            e.Graphics.DrawLine(pen, 20, 10, 50, 10);
            e.Graphics.DrawLine(pen, 51, 10, 100, 10);
            e.Graphics.DrawLine(pen, 101, 10, 150, 10);
            e.Graphics.DrawLine(pen, 151, 10, 200, 10);
            e.Graphics.DrawLine(pen, 201, 10, 250, 10);

            System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Blue);
            g.FillRectangle(myBrush, new Rectangle(30, 10, 60, 30));

            string drawString = "3456Ф";
            Font drawFont = new Font("Seroe", 16);
            SolidBrush drawBrush = new SolidBrush(Color.Black);
            float x = 150.0F;
            float y = 50.0F;
            StringFormat drawFormat = new StringFormat();
            g.DrawString(drawString, drawFont, drawBrush, x, y, drawFormat);

            Rectangle rect = new Rectangle(0, 0, 50, 50);
            Pen pen2 = new Pen(Color.FromArgb(128, 200, 0, 200), 2);
            e.Graphics.DrawRectangle(pen2, rect);

            e.Graphics.ScaleTransform(1.75f, 0.5f);
            e.Graphics.DrawRectangle(pen, rect);

            //e.Graphics.DrawRectangle(pen2, rect);
        }

        private void mainForm_Load(object sender, EventArgs e) {

        }
    }
}
