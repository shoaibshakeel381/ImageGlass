﻿using ImageGlass.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ImageGlass.Theme;
using System.Runtime.InteropServices;
using ImageGlass.Services.Configuration;

namespace ImageGlass
{
    public partial class frmColorPicker : Form
    {
        
        // default location offset on the parent form
        private static Point DefaultLocationOffset = new Point((int)(20 * DPIScaling.GetDPIScaleFactor()), (int)(80 * DPIScaling.GetDPIScaleFactor()));

        private Form _currentOwner = null;
        private ImageBox _imgBox = null;
        private BitmapBooster _bmpBooster = null;
        private Point _locationOffset = DefaultLocationOffset;
        private Point _cursorPos = new Point();


        public frmColorPicker()
        {
            InitializeComponent();
        }


        public void SetImageBox(ImageBox imgBox)
        {
            if (_imgBox != null)
            {
                _imgBox.MouseMove -= _imgBox_MouseMove;
                _imgBox.Click -= _imgBox_Click;
            }

            _imgBox = imgBox;

            _imgBox.MouseMove += _imgBox_MouseMove;
            _imgBox.Click += _imgBox_Click;
        }


        #region Borderless form moving
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();


        private void frmColorPicker_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        #endregion


        #region Create shadow for borderless form

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, // x-coordinate of upper-left corner
            int nTopRect, // y-coordinate of upper-left corner
            int nRightRect, // x-coordinate of lower-right corner
            int nBottomRect, // y-coordinate of lower-right corner
            int nWidthEllipse, // height of ellipse
            int nHeightEllipse // width of ellipse
        );

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);

        private bool m_aeroEnabled = false;              // variables for box shadow
        private const int CS_DROPSHADOW = 0x00020000;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_ACTIVATEAPP = 0x001C;

        public struct MARGINS                           // struct for box shadow
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        private bool CheckAeroEnabled()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                int enabled = 0;
                DwmIsCompositionEnabled(ref enabled);
                return (enabled == 1) ? true : false;
            }
            return false;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCPAINT: // box shadow
                    if (m_aeroEnabled)
                    {
                        var v = 2;
                        DwmSetWindowAttribute(this.Handle, 2, ref v, 4);

                        MARGINS margins = new MARGINS()
                        {
                            bottomHeight = 1,
                            leftWidth = 1,
                            rightWidth = 1,
                            topHeight = 1
                        };

                        DwmExtendFrameIntoClientArea(this.Handle, ref margins);
                    }
                    break;
                default:
                    break;
            }

            base.WndProc(ref m);
        }
        #endregion
        

        #region Properties to make a tool window

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams baseParams = base.CreateParams;
                baseParams.ExStyle |= 0x8000000 // WS_EX_NOACTIVATE
                    | 0x00000080;   // WS_EX_TOOLWINDOW


                #region Shadow for Borderless form
                m_aeroEnabled = CheckAeroEnabled();

                if (!m_aeroEnabled)
                    baseParams.ClassStyle |= CS_DROPSHADOW;
                #endregion


                return baseParams;
            }
        }

        #endregion


        #region Events to manage the form location
        
        private Point parentLocation = Point.Empty;
        private Point parentOffset = Point.Empty;
        private bool formOwnerMoving = false;



        private void _AttachEventsToParent(Form frmOwner)
        {
            if (frmOwner == null)
                return;

            parentLocation = this.Owner.Location;

            frmOwner.Move += Owner_Move;
            frmOwner.SizeChanged += Owner_Move;
            frmOwner.VisibleChanged += Owner_Move;
            frmOwner.Deactivate += FrmOwner_Deactivate;
            frmOwner.LocationChanged += FrmOwner_LocationChanged;
        }

        private void FrmOwner_LocationChanged(object sender, EventArgs e)
        {
            formOwnerMoving = false;
        }

        private void _DetachEventsFromParent(Form frmOwner)
        {
            if (frmOwner == null)
                return;

            frmOwner.Move -= Owner_Move;
            frmOwner.SizeChanged -= Owner_Move;
            frmOwner.VisibleChanged -= Owner_Move;
            frmOwner.Deactivate -= FrmOwner_Deactivate;
            frmOwner.LocationChanged -= FrmOwner_LocationChanged;
        }


        private void FrmOwner_Deactivate(object sender, EventArgs e)
        {
            this.TopMost = false;
        }

        private void Owner_Move(object sender, EventArgs e)
        {
            if (this.Owner == null) return;

            formOwnerMoving = true;

            var parentOffset = new Point(this.Owner.Left - parentLocation.X, this.Owner.Top - parentLocation.Y);

            _SetLocationBasedOnParent();
            parentLocation = this.Owner.Location;
        }

        private void frmColorPicker_Move(object sender, EventArgs e)
        {
            if (!formOwnerMoving)
            {
                _locationOffset = new Point(this.Left - this.Owner.Left, this.Top - this.Owner.Top);
                parentOffset = _locationOffset;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            if (Owner != _currentOwner)
            {
                _DetachEventsFromParent(_currentOwner);
                _currentOwner = Owner;
                _AttachEventsToParent(_currentOwner);
            }
            
            base.OnShown(e);
        }


        private void _SetLocationBasedOnParent()
        {
            if (Owner == null)
                return;

            if (Owner.WindowState == FormWindowState.Minimized || !Owner.Visible)
            {
                Visible = false;
                return;
            }

            // set location based on the main form
            Point ownerLocation = Owner.Location;
            ownerLocation.Offset(parentOffset);

            this.Location = ownerLocation;
        }

        #endregion


        #region Events to manage ImageBox

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_imgBox != null)
            {
                _imgBox.Cursor = Cursors.Default;
                _imgBox.MouseMove -= _imgBox_MouseMove;
                _imgBox.Click -= _imgBox_Click;
            }
        }

        private void _imgBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_imgBox.Image == null)
            {
                return;
            }
            _imgBox.Cursor = Cursors.Cross;
            _cursorPos = _imgBox.PointToImage(e.Location);

            //In case of opening a second image, 
            //there is a delay of loading image time which will cause error due to _imgBox is null.
            //Wrap try catch to skip this error
            try
            {
                if (_cursorPos.X >= 0 && _cursorPos.Y >= 0 && _cursorPos.X < _imgBox.Image.Width
                    && _cursorPos.Y < _imgBox.Image.Height)
                {
                    lblPixel.Text = string.Format("({0}, {1})", _cursorPos.X, _cursorPos.Y);
                }
            }
            catch { }
        }

        private void _imgBox_Click(object sender, EventArgs e)
        {
            if (_imgBox.Image == null)
            {
                return;
            }


            //In case of opening a second image, 
            //there is a delay of loading image time which will cause error due to _imgBox is null.
            //Wrap try catch to skip this error
            try
            {
                if (_cursorPos.X >= 0 && _cursorPos.Y >= 0 && _cursorPos.X < _imgBox.Image.Width && _cursorPos.Y < _imgBox.Image.Height)
                {
                    if (_bmpBooster != null)
                    {
                        _bmpBooster.Dispose();
                    }
                    _bmpBooster = new BitmapBooster(new Bitmap(_imgBox.Image));

                    Color color = _bmpBooster.Get(_cursorPos.X, _cursorPos.Y);
                    _DisplayColor(color);

                    _bmpBooster.Dispose();
                    _bmpBooster = null;
                }
            }
            catch { }
        }

        #endregion


        #region Display data

        private void _DisplayColor(Color color)
        {
            panelColor.BackColor = color;

            //RGBA color -----------------------------------------------
            if (GlobalSetting.IsColorPickerRGBA)
            {
                lblRGB.Text = "RGBA:";
                txtRGB.Text = string.Format("{0}, {1}, {2}, {3}", color.R, color.G, color.B, Math.Round(color.A / 255.0, 3));
            }
            else
            {
                lblRGB.Text = "RGB:";
                txtRGB.Text = string.Format("{0}, {1}, {2}", color.R, color.G, color.B);
            }

            //HEXA color -----------------------------------------------
            if (GlobalSetting.IsColorPickerHEXA)
            {
                lblHEX.Text = "HEXA:";
                txtHEX.Text = Theme.Theme.ConvertColorToHEX(color);
            }
            else
            {
                lblHEX.Text = "HEX:";
                txtHEX.Text = Theme.Theme.ConvertColorToHEX(color, true);
            }

            //CMYK color -----------------------------------------------
            var cmyk = Theme.Theme.ConvertColorToCMYK(color);
            txtCMYK.Text = string.Format("{0}%, {1}%, {2}%, {3}%", cmyk[0], cmyk[1], cmyk[2], cmyk[3]);

            //HSLA color -----------------------------------------------
            var hsla = Theme.Theme.ConvertColorToHSLA(color);
            if (GlobalSetting.IsColorPickerHSLA)
            {
                lblHSL.Text = "HSLA:";
                txtHSL.Text = string.Format("{0}, {1}%, {2}%, {3}", hsla[0], hsla[1], hsla[2], hsla[3]);
            }
            else
            {
                lblHSL.Text = "HSL:";
                txtHSL.Text = string.Format("{0}, {1}%, {2}%", hsla[0], hsla[1], hsla[2]);
            }

                
            

            lblPixel.ForeColor = Theme.Theme.InvertColor(color);
        }

        private void _ResetColor()
        {
            lblPixel.Text = string.Empty;
            txtRGB.Text = string.Empty;
            txtHEX.Text = string.Empty;
        }


        private void ColorTextbox_Click(object sender, EventArgs e)
        {
            var txt = (TextBox)sender;
            txt.SelectAll();

            //fixed: cannot copy the text if Owner form is not activated
            this.Owner.Activate();
            this.Activate();
        }

        


        #endregion


        #region Other Form Events
        private void frmColorPicker_KeyDown(object sender, KeyEventArgs e)
        {
            //lblPixel.Text = e.KeyCode.ToString();


            #region ESC or CTRL + SHIFT + K
            //ESC or CTRL + SHIFT + K --------------------------------------------------------
            if ((e.KeyCode == Keys.Escape && !e.Control && !e.Shift && !e.Alt) || //ESC 
                (e.KeyCode == Keys.K && e.Control && e.Shift && !e.Alt))//CTRL + SHIFT + K
            {
                LocalSetting.IsShowColorPickerOnStartup = false;
                this.Close();
            }
            #endregion
        }


        private void frmColorPicker_FormClosing(object sender, FormClosingEventArgs e)
        {
            LocalSetting.IsColorPickerToolOpening = false;
            LocalSetting.ForceUpdateActions |= MainFormForceUpdateAction.COLOR_PICKER_MENU;

            //Windows Bound-------------------------------------------------------------------
            GlobalSetting.SetConfig($"{Name}.WindowsBound", GlobalSetting.RectToString(Bounds));
        }


        /// <summary>
        /// Apply theme
        /// </summary>
        public void UpdateUI()
        {
            //apply current theme ------------------------------------------------------
            this.BackColor =
                txtRGB.BackColor =
                txtHEX.BackColor =
                txtCMYK.BackColor =
                txtHSL.BackColor =
                LocalSetting.Theme.BackgroundColor;

            lblPixel.ForeColor =
                lblRGB.ForeColor =
                lblHEX.ForeColor =
                lblCMYK.ForeColor =
                lblHSL.ForeColor =
                txtRGB.ForeColor =
                txtHEX.ForeColor =
                txtCMYK.ForeColor =
                txtHSL.ForeColor =
                Theme.Theme.InvertColor(LocalSetting.Theme.BackgroundColor);
        }

        private void frmColorPicker_Load(object sender, EventArgs e)
        {
            UpdateUI();

            //Windows Bound (Position + Size)-------------------------------------------
            Rectangle rc = GlobalSetting.StringToRect(GlobalSetting.GetConfig($"{Name}.WindowsBound", $"0,0,300,160"));

            if (rc.X == 0 && rc.Y == 0)
            {
                _locationOffset = DefaultLocationOffset;
                parentOffset = _locationOffset;

                _SetLocationBasedOnParent();
            }
            else
            {
                this.Location = rc.Location;
            }

            _ResetColor();
            

            lblRGB.Text = "RGB:";
            lblHEX.Text = "HEX:";
            lblHSL.Text = "HSL:";

            if (GlobalSetting.IsColorPickerRGBA)
            {
                lblRGB.Text = "RGBA:";
            }
            if (GlobalSetting.IsColorPickerHEXA)
            {
                lblHEX.Text = "HEXA:";
            }
            if (GlobalSetting.IsColorPickerHSLA)
            {
                lblHSL.Text = "HSLA:";
            }

        }




        #endregion


    }
}
