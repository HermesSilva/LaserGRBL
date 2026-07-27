using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LaserGRBL.UserControls.NumericInput
{
    public partial class NumericUpDown : ColoredBorderControl, ISupportInitialize
    {

        public event EventHandler ValueChanged;

        //gives access to the protected text validation of the standard control, so a typed value
        //can be committed on ENTER instead of waiting for the focus to be lost
        internal class InnerNumericUpDown : System.Windows.Forms.NumericUpDown
        {
            public void CommitEditText()
            { ValidateEditText(); }
        }

        public NumericUpDown()
        {
            InitializeComponent();
            mNumericUpDown.ValueChanged += MNumericUpDown_ValueChanged;
            mNumericUpDown.KeyDown += MNumericUpDown_KeyDown;
            mNumericUpDown.Enter += MNumericUpDown_Enter;
            Click += (s, e) => mNumericUpDown.Focus();
        }

        private void MNumericUpDown_Enter(object sender, EventArgs e)
        {
            mNumericUpDown.Select(0, mNumericUpDown.Text.Length); //typing replaces the current value
        }

        private void MNumericUpDown_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitEditText();
                e.Handled = e.SuppressKeyPress = true; //no windows beep
            }
            else if (e.KeyCode == Keys.Escape)
            {
                mNumericUpDown.Text = mNumericUpDown.Value.ToString(); //discard what has been typed
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        /// <summary>Apply the typed text without waiting for the focus to be lost (out of range values are clamped)</summary>
        public void CommitEditText()
        {
            mNumericUpDown.CommitEditText();
            mNumericUpDown.Select(0, mNumericUpDown.Text.Length);
        }

        public int DecimalPlaces
        {
            get => mNumericUpDown.DecimalPlaces;
            set => mNumericUpDown.DecimalPlaces = value;
        }

        public decimal Increment
        {
            get => mNumericUpDown.Increment;
            set => mNumericUpDown.Increment = value;
        }

        private void MNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            ValueChanged?.Invoke(sender, e);
        }

        public void BeginInit()
        {
            mNumericUpDown.BeginInit();
        }

        public void EndInit()
        {
            mNumericUpDown.EndInit();
        }

        public decimal Value
        {
            get => mNumericUpDown.Value;
            set => mNumericUpDown.Value = value;
        }

        public decimal Minimum
        {
            get => mNumericUpDown.Minimum;
            set => mNumericUpDown.Minimum = value;
        }

        public decimal Maximum
        {
            get => mNumericUpDown.Maximum;
            set => mNumericUpDown.Maximum = value;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            mNumericUpDown.BackColor = ColorScheme.LogBackColor;
            BackColor = ColorScheme.LogBackColor;
            mNumericUpDown.ForeColor = ColorScheme.FormForeColor;
            BorderColor = ColorScheme.ControlsBorder;
            base.OnPaint(e);
        }
        

    }
}
