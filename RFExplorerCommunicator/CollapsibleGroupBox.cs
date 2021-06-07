//============================================================================
//RF Explorer for Windows - A Handheld Spectrum Analyzer for everyone!
//Copyright (C) 2010-20 RF Explorer Technologies SL, www.rf-explorer.com
//
//This application is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 3.0 of the License, or (at your option) any later version.
//
//This software is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//General Public License for more details.
//
//You should have received a copy of the GNU General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//=============================================================================

using System;
using System.Drawing;
using System.Windows.Forms;

namespace RFExplorerCommunicator
{
    public class CollapsibleGroupbox : IDisposable
    {
        #region members
        public GroupBox m_Groupbox; //reference to actual groupbox objects
        string m_sCollapsedCaption, m_sOriginalCaption;
        Button m_CollapseButton;
        public Button CollapseButton
        {
            get { return m_CollapseButton; }
        }
        Panel m_ClickPanel;
        EventHandler m_ClickHandler = null;
        Size m_Size, m_MinimumSize, m_MaximumSize, m_CollapsedSize;
        bool m_bCollapsed = false;
        Brush m_ButtonBrush = null;
        Pen m_ButtonPen = null;
        SolidBrush m_TextBrush = null;
        StringFormat drawFormat = new StringFormat();
        Color m_CollapsedTextColor = Color.Black;
        Color m_CollapseButtonColor = Color.DarkBlue;
        #endregion

        #region data fields
        /// <summary>
        /// change this to show a custom text when collapsed
        /// </summary>
        public string CollapsedCaption
        {
            get { return m_sCollapsedCaption; }
            set
            {
                m_sCollapsedCaption = value;
                if (m_bCollapsed)
                    m_Groupbox.Invalidate();
            }
        }

        /// <summary>
        /// Set to true to programmatically collapse the groupbox, false to uncollapse
        /// </summary>
        public bool Collapsed
        {
            get { return m_bCollapsed; }
            set
            {
                if (value != m_bCollapsed)
                {
                    m_bCollapsed = value;
                    UpdateCollapse();
                    OnCollapseButtonClickEvent(new EventArgs());
                }
            }
        }

        /// <summary>
        /// Color used for collapsed text, otherwise default is that of group box text
        /// </summary>
        public Color CollapsedTextColor
        {
            get { return m_CollapsedTextColor; }
            set
            {
                if (m_CollapsedTextColor != value)
                {
                    m_CollapsedTextColor = value;
                    m_TextBrush = new System.Drawing.SolidBrush(m_CollapsedTextColor);
                    if (m_bCollapsed)
                        m_Groupbox.Invalidate();
                }
            }
        }

        /// <summary>
        /// Color used for collapsible button (triangle) otherwise dark blue by default
        /// </summary>
        public Color CollapseButtonColor
        {
            get { return m_CollapseButtonColor; }
            set
            {
                if (m_CollapseButtonColor != value)
                {
                    m_CollapseButtonColor = value;
                    //now force color brush and pen to rebuild from this color
                    m_ButtonBrush = null;
                    m_ButtonPen = null;
                    m_CollapseButton.Invalidate();
                }
            }
        }
        #endregion

        #region methods
        public CollapsibleGroupbox(GroupBox objGroupbox)
        {
            m_Groupbox = objGroupbox;
            m_sCollapsedCaption = objGroupbox.Text;
            m_sOriginalCaption = objGroupbox.Text;
            m_Size = objGroupbox.Size;
            m_MinimumSize = objGroupbox.MinimumSize;
            m_MaximumSize = objGroupbox.MaximumSize;
            m_CollapsedSize = new Size(25, objGroupbox.Height);
            //MessageBox.Show(m_MinimumSize.ToString());

            m_ClickHandler = new EventHandler(this.m_CollapseButton_Click);

            m_ClickPanel = new Panel();
            //m_ClickPanel.BorderStyle = BorderStyle.FixedSingle;
            m_ClickPanel.BackColor = Color.Transparent;
            m_ClickPanel.Visible = true;
            m_ClickPanel.Dock = DockStyle.Fill;
            m_Groupbox.Controls.Add(m_ClickPanel);

            m_CollapseButton = new Button();
            m_CollapseButton.Size = new Size(12, 12);
            //m_CollapseButton.BackColor = Color.SkyBlue;
            m_CollapseButton.FlatStyle = FlatStyle.Flat;
            m_CollapseButton.FlatAppearance.BorderSize = 0;
            m_CollapseButton.Text = "";
            m_CollapseButton.Visible = true;
            m_Groupbox.Controls.Add(m_CollapseButton);
            m_CollapseButton.Click += m_ClickHandler;
            m_CollapseButton.Paint += new System.Windows.Forms.PaintEventHandler(this.m_CollapseButton_Paint);

            m_CollapsedTextColor = m_Groupbox.ForeColor;
            m_TextBrush = new System.Drawing.SolidBrush(m_CollapsedTextColor);

            UpdateCollapse();
        }

        /// <summary>
        /// Standard Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool m_bDisposed = false;
        /// <summary>
        /// Local dispose method
        /// </summary>
        /// <param name="bDisposing">if disposing is required</param>
        protected virtual void Dispose(bool bDisposing)
        {
            if (!m_bDisposed)
            {
                if (bDisposing)
                {
                    if (m_TextBrush != null)
                        m_TextBrush.Dispose();
                    if (m_ButtonPen != null)
                        m_ButtonPen.Dispose();
                    if (m_ButtonBrush != null)
                        m_ButtonBrush.Dispose();
                    if (drawFormat != null)
                        drawFormat.Dispose();
                    if (m_ClickPanel != null)
                        m_ClickPanel.Dispose();
                    if (m_CollapseButton != null)
                        m_CollapseButton.Dispose();
                }
                m_bDisposed = true;
            }
        }

        /// <summary>
        /// Optional
        /// This event fires when the button for collapse/uncollapse is clicked
        /// The collapsed property is already updated when this is fired
        /// </summary>
        public event EventHandler CollapseButtonClick;
        protected virtual void OnCollapseButtonClickEvent(EventArgs e)
        {
            if (CollapseButtonClick != null)
            {
                CollapseButtonClick(this, e);
            }
        }

        private void m_CollapseButton_Click(object sender, EventArgs e)
        {
            m_bCollapsed = !m_bCollapsed;
            UpdateCollapse();
            m_Groupbox.Focus();
            OnCollapseButtonClickEvent(new EventArgs());
        }

        private void m_CollapseButton_Paint(object sender, PaintEventArgs e)
        {
            if (m_ButtonBrush == null)
            {
                m_ButtonBrush = new SolidBrush(m_CollapseButtonColor);
                m_ButtonPen = new Pen(m_ButtonBrush);
            }

            int nWidth = m_CollapseButton.Width - 1;
            int nHeight = m_CollapseButton.Height;
            if (m_bCollapsed == false)
            {
                Point[] arrPoints ={
                                      new Point(nWidth, 1),
                                      new Point(1, nHeight / 2),
                                      new Point(nWidth, nHeight-1)
                                  };
                e.Graphics.FillPolygon(m_ButtonBrush, arrPoints);
            }
            else
            {
                Point[] arrPoints ={
                                      new Point(1, 1),
                                      new Point(nWidth, nHeight / 2),
                                      new Point(1, nHeight-1)
                                   };
                e.Graphics.FillPolygon(m_ButtonBrush, arrPoints);
            }
        }

        private void UpdateCollapse()
        {
            if (m_bCollapsed)
            {
                m_Groupbox.SuspendLayout();
                m_Groupbox.MinimumSize = m_CollapsedSize;
                m_Groupbox.MaximumSize = m_CollapsedSize;
                m_Groupbox.Text = "";
                m_ClickPanel.Click += m_ClickHandler;
                m_CollapseButton.Left = 6;
                m_CollapseButton.Top = 1;
                for (int nInd = 0; nInd < m_Groupbox.Controls.Count; nInd++)
                {
                    m_Groupbox.Controls[nInd].Visible = false;
                }
                m_Groupbox.Width = 25;
                m_ClickPanel.Visible = true;
                m_Groupbox.ResumeLayout(true);
            }
            else
            {
                m_Groupbox.SuspendLayout();
                m_Groupbox.MinimumSize = m_MinimumSize;
                m_Groupbox.MaximumSize = m_MaximumSize;
                m_Groupbox.Text = m_sOriginalCaption;
                m_Groupbox.Width = m_Size.Width;
                m_CollapseButton.Left = m_Size.Width - 2 * m_CollapseButton.Width;
                m_CollapseButton.Top = 1;
                m_ClickPanel.Click -= m_ClickHandler;
                for (int nInd = 0; nInd < m_Groupbox.Controls.Count; nInd++)
                {
                    bool bVisible = true;
                    Control objControl = m_Groupbox.Controls[nInd];
                    if (!objControl.Enabled)
                    {
                        //If control is not enabled, maybe we should not make it visible, depending on the tag value true/false
                        try
                        {
                            if (objControl.Tag != null)
                                if (((bool)objControl.Tag) == false)
                                    bVisible = false;
                        }
                        catch { }
                    }
                    if (bVisible)
                        objControl.Visible = true;
                }
                m_Groupbox.ResumeLayout(true);
            }
            m_CollapseButton.Visible = true;
        }

        public void Paint(Graphics objGraphics)
        {
            if (m_bCollapsed)
            {
                drawFormat.FormatFlags = StringFormatFlags.DirectionVertical;
                objGraphics.DrawString(m_sCollapsedCaption, m_Groupbox.Font, m_TextBrush, 3, m_CollapseButton.Bottom + 8, drawFormat);
            }
        }
        #endregion
    }
}
