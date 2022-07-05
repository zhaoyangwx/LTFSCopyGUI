<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class HashTaskWindow
    Inherits System.Windows.Forms.Form

    'Form 重写 Dispose，以清理组件列表。
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Windows 窗体设计器所必需的
    Private components As System.ComponentModel.IContainer

    '注意: 以下过程是 Windows 窗体设计器所必需的
    '可以使用 Windows 窗体设计器修改它。  
    '不要使用代码编辑器修改它。
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(HashTaskWindow))
        Me.TextBox1 = New System.Windows.Forms.TextBox()
        Me.ProgressBar1 = New System.Windows.Forms.ProgressBar()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.CheckBox1 = New System.Windows.Forms.CheckBox()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.SaveFileDialog1 = New System.Windows.Forms.SaveFileDialog()
        Me.ProgressBar2 = New System.Windows.Forms.ProgressBar()
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.ContextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.SToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.MinToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.MinToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.MinToolStripMenuItem2 = New System.Windows.Forms.ToolStripMenuItem()
        Me.HToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.HToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.HToolStripMenuItem2 = New System.Windows.Forms.ToolStripMenuItem()
        Me.HToolStripMenuItem3 = New System.Windows.Forms.ToolStripMenuItem()
        Me.AllToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.LinearToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.LogrithmToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator2 = New System.Windows.Forms.ToolStripSeparator()
        Me.WordwrapToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer()
        Me.CheckBox2 = New System.Windows.Forms.CheckBox()
        Me.AxTChart1 = New AxTeeChart.AxTChart()
        Me.ContextMenuStrip1.SuspendLayout()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        CType(Me.AxTChart1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'TextBox1
        '
        Me.TextBox1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBox1.Location = New System.Drawing.Point(3, 3)
        Me.TextBox1.MaxLength = 30000
        Me.TextBox1.Multiline = True
        Me.TextBox1.Name = "TextBox1"
        Me.TextBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.TextBox1.Size = New System.Drawing.Size(615, 238)
        Me.TextBox1.TabIndex = 0
        '
        'ProgressBar1
        '
        Me.ProgressBar1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ProgressBar1.Location = New System.Drawing.Point(12, 12)
        Me.ProgressBar1.Name = "ProgressBar1"
        Me.ProgressBar1.Size = New System.Drawing.Size(621, 23)
        Me.ProgressBar1.TabIndex = 1
        '
        'Button1
        '
        Me.Button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom
        Me.Button1.Location = New System.Drawing.Point(244, 454)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(75, 23)
        Me.Button1.TabIndex = 2
        Me.Button1.Text = "Start"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Anchor = System.Windows.Forms.AnchorStyles.Bottom
        Me.Button2.Location = New System.Drawing.Point(325, 454)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(75, 23)
        Me.Button2.TabIndex = 3
        Me.Button2.Text = "Stop"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'CheckBox1
        '
        Me.CheckBox1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Location = New System.Drawing.Point(12, 458)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(60, 16)
        Me.CheckBox1.TabIndex = 4
        Me.CheckBox1.Text = "ReHash"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'Button3
        '
        Me.Button3.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button3.Location = New System.Drawing.Point(558, 451)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(75, 23)
        Me.Button3.TabIndex = 5
        Me.Button3.Text = "Save"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'SaveFileDialog1
        '
        Me.SaveFileDialog1.Filter = "schema|*.schema|xml|*.xml|所有文件|*.*"
        '
        'ProgressBar2
        '
        Me.ProgressBar2.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ProgressBar2.Location = New System.Drawing.Point(12, 41)
        Me.ProgressBar2.Name = "ProgressBar2"
        Me.ProgressBar2.Size = New System.Drawing.Size(621, 23)
        Me.ProgressBar2.TabIndex = 6
        '
        'Timer1
        '
        Me.Timer1.Enabled = True
        Me.Timer1.Interval = 1000
        '
        'ContextMenuStrip1
        '
        Me.ContextMenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.SToolStripMenuItem, Me.MinToolStripMenuItem, Me.MinToolStripMenuItem1, Me.MinToolStripMenuItem2, Me.HToolStripMenuItem, Me.HToolStripMenuItem1, Me.HToolStripMenuItem2, Me.HToolStripMenuItem3, Me.AllToolStripMenuItem, Me.ToolStripSeparator1, Me.LinearToolStripMenuItem, Me.LogrithmToolStripMenuItem, Me.ToolStripSeparator2, Me.WordwrapToolStripMenuItem})
        Me.ContextMenuStrip1.Name = "ContextMenuStrip1"
        Me.ContextMenuStrip1.Size = New System.Drawing.Size(145, 280)
        '
        'SToolStripMenuItem
        '
        Me.SToolStripMenuItem.Name = "SToolStripMenuItem"
        Me.SToolStripMenuItem.Size = New System.Drawing.Size(144, 22)
        Me.SToolStripMenuItem.Text = "60s"
        '
        'MinToolStripMenuItem
        '
        Me.MinToolStripMenuItem.Name = "MinToolStripMenuItem"
        Me.MinToolStripMenuItem.Size = New System.Drawing.Size(144, 22)
        Me.MinToolStripMenuItem.Text = "5min"
        '
        'MinToolStripMenuItem1
        '
        Me.MinToolStripMenuItem1.Name = "MinToolStripMenuItem1"
        Me.MinToolStripMenuItem1.Size = New System.Drawing.Size(144, 22)
        Me.MinToolStripMenuItem1.Text = "10min"
        '
        'MinToolStripMenuItem2
        '
        Me.MinToolStripMenuItem2.Name = "MinToolStripMenuItem2"
        Me.MinToolStripMenuItem2.Size = New System.Drawing.Size(144, 22)
        Me.MinToolStripMenuItem2.Text = "30min"
        '
        'HToolStripMenuItem
        '
        Me.HToolStripMenuItem.Name = "HToolStripMenuItem"
        Me.HToolStripMenuItem.Size = New System.Drawing.Size(144, 22)
        Me.HToolStripMenuItem.Text = "1h"
        '
        'HToolStripMenuItem1
        '
        Me.HToolStripMenuItem1.Name = "HToolStripMenuItem1"
        Me.HToolStripMenuItem1.Size = New System.Drawing.Size(144, 22)
        Me.HToolStripMenuItem1.Text = "3h"
        '
        'HToolStripMenuItem2
        '
        Me.HToolStripMenuItem2.Name = "HToolStripMenuItem2"
        Me.HToolStripMenuItem2.Size = New System.Drawing.Size(144, 22)
        Me.HToolStripMenuItem2.Text = "6h"
        '
        'HToolStripMenuItem3
        '
        Me.HToolStripMenuItem3.Name = "HToolStripMenuItem3"
        Me.HToolStripMenuItem3.Size = New System.Drawing.Size(144, 22)
        Me.HToolStripMenuItem3.Text = "12h"
        '
        'AllToolStripMenuItem
        '
        Me.AllToolStripMenuItem.Name = "AllToolStripMenuItem"
        Me.AllToolStripMenuItem.Size = New System.Drawing.Size(144, 22)
        Me.AllToolStripMenuItem.Text = "1d"
        '
        'ToolStripSeparator1
        '
        Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
        Me.ToolStripSeparator1.Size = New System.Drawing.Size(141, 6)
        '
        'LinearToolStripMenuItem
        '
        Me.LinearToolStripMenuItem.CheckOnClick = True
        Me.LinearToolStripMenuItem.Name = "LinearToolStripMenuItem"
        Me.LinearToolStripMenuItem.Size = New System.Drawing.Size(144, 22)
        Me.LinearToolStripMenuItem.Text = "Linear"
        '
        'LogrithmToolStripMenuItem
        '
        Me.LogrithmToolStripMenuItem.CheckOnClick = True
        Me.LogrithmToolStripMenuItem.Name = "LogrithmToolStripMenuItem"
        Me.LogrithmToolStripMenuItem.Size = New System.Drawing.Size(144, 22)
        Me.LogrithmToolStripMenuItem.Text = "Logarithmic"
        '
        'ToolStripSeparator2
        '
        Me.ToolStripSeparator2.Name = "ToolStripSeparator2"
        Me.ToolStripSeparator2.Size = New System.Drawing.Size(141, 6)
        '
        'WordwrapToolStripMenuItem
        '
        Me.WordwrapToolStripMenuItem.Checked = True
        Me.WordwrapToolStripMenuItem.CheckOnClick = True
        Me.WordwrapToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked
        Me.WordwrapToolStripMenuItem.Name = "WordwrapToolStripMenuItem"
        Me.WordwrapToolStripMenuItem.Size = New System.Drawing.Size(144, 22)
        Me.WordwrapToolStripMenuItem.Text = "Wordwrap"
        '
        'SplitContainer1
        '
        Me.SplitContainer1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.SplitContainer1.Location = New System.Drawing.Point(12, 70)
        Me.SplitContainer1.Name = "SplitContainer1"
        Me.SplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal
        '
        'SplitContainer1.Panel1
        '
        Me.SplitContainer1.Panel1.Controls.Add(Me.AxTChart1)
        '
        'SplitContainer1.Panel2
        '
        Me.SplitContainer1.Panel2.Controls.Add(Me.TextBox1)
        Me.SplitContainer1.Size = New System.Drawing.Size(621, 375)
        Me.SplitContainer1.SplitterDistance = 127
        Me.SplitContainer1.TabIndex = 10
        '
        'CheckBox2
        '
        Me.CheckBox2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.CheckBox2.AutoSize = True
        Me.CheckBox2.Checked = True
        Me.CheckBox2.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBox2.Location = New System.Drawing.Point(78, 458)
        Me.CheckBox2.Name = "CheckBox2"
        Me.CheckBox2.Size = New System.Drawing.Size(48, 16)
        Me.CheckBox2.TabIndex = 11
        Me.CheckBox2.Text = "Copy"
        Me.CheckBox2.UseVisualStyleBackColor = True
        '
        'AxTChart1
        '
        Me.AxTChart1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.AxTChart1.Enabled = True
        Me.AxTChart1.Location = New System.Drawing.Point(3, 3)
        Me.AxTChart1.Name = "AxTChart1"
        Me.AxTChart1.OcxState = CType(resources.GetObject("AxTChart1.OcxState"), System.Windows.Forms.AxHost.State)
        Me.AxTChart1.Size = New System.Drawing.Size(615, 121)
        Me.AxTChart1.TabIndex = 7
        '
        'HashTaskWindow
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(645, 489)
        Me.ContextMenuStrip = Me.ContextMenuStrip1
        Me.Controls.Add(Me.CheckBox2)
        Me.Controls.Add(Me.SplitContainer1)
        Me.Controls.Add(Me.ProgressBar2)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.CheckBox1)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.ProgressBar1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "HashTaskWindow"
        Me.Text = "HashTaskWindow"
        Me.ContextMenuStrip1.ResumeLayout(False)
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        Me.SplitContainer1.Panel2.PerformLayout()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer1.ResumeLayout(False)
        CType(Me.AxTChart1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents TextBox1 As TextBox
    Friend WithEvents ProgressBar1 As ProgressBar
    Friend WithEvents Button1 As Button
    Friend WithEvents Button2 As Button
    Friend WithEvents CheckBox1 As CheckBox
    Friend WithEvents Button3 As Button
    Friend WithEvents SaveFileDialog1 As SaveFileDialog
    Friend WithEvents ProgressBar2 As ProgressBar
    Friend WithEvents Timer1 As Timer
    Friend WithEvents AxTChart1 As AxTeeChart.AxTChart
    Friend WithEvents ContextMenuStrip1 As ContextMenuStrip
    Friend WithEvents SToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents MinToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents MinToolStripMenuItem1 As ToolStripMenuItem
    Friend WithEvents MinToolStripMenuItem2 As ToolStripMenuItem
    Friend WithEvents HToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LinearToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LogrithmToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SplitContainer1 As SplitContainer
    Friend WithEvents WordwrapToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator2 As ToolStripSeparator
    Friend WithEvents AllToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents HToolStripMenuItem1 As ToolStripMenuItem
    Friend WithEvents HToolStripMenuItem2 As ToolStripMenuItem
    Friend WithEvents HToolStripMenuItem3 As ToolStripMenuItem
    Friend WithEvents CheckBox2 As CheckBox
End Class
