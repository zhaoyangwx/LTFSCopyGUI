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
        Me.LinearToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.LogrithmToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AxTChart1 = New AxTeeChart.AxTChart()
        Me.ContextMenuStrip1.SuspendLayout()
        CType(Me.AxTChart1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'TextBox1
        '
        Me.TextBox1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBox1.Location = New System.Drawing.Point(12, 167)
        Me.TextBox1.MaxLength = 2147483647
        Me.TextBox1.Multiline = True
        Me.TextBox1.Name = "TextBox1"
        Me.TextBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.TextBox1.Size = New System.Drawing.Size(621, 281)
        Me.TextBox1.TabIndex = 0
        Me.TextBox1.WordWrap = False
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
        Me.ContextMenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.SToolStripMenuItem, Me.MinToolStripMenuItem, Me.MinToolStripMenuItem1, Me.MinToolStripMenuItem2, Me.HToolStripMenuItem, Me.LinearToolStripMenuItem, Me.LogrithmToolStripMenuItem})
        Me.ContextMenuStrip1.Name = "ContextMenuStrip1"
        Me.ContextMenuStrip1.Size = New System.Drawing.Size(145, 158)
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
        'LinearToolStripMenuItem
        '
        Me.LinearToolStripMenuItem.Name = "LinearToolStripMenuItem"
        Me.LinearToolStripMenuItem.Size = New System.Drawing.Size(144, 22)
        Me.LinearToolStripMenuItem.Text = "Linear"
        '
        'LogrithmToolStripMenuItem
        '
        Me.LogrithmToolStripMenuItem.Name = "LogrithmToolStripMenuItem"
        Me.LogrithmToolStripMenuItem.Size = New System.Drawing.Size(144, 22)
        Me.LogrithmToolStripMenuItem.Text = "Logarithmic"
        '
        'AxTChart1
        '
        Me.AxTChart1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.AxTChart1.Enabled = True
        Me.AxTChart1.Location = New System.Drawing.Point(10, 70)
        Me.AxTChart1.Name = "AxTChart1"
        Me.AxTChart1.OcxState = CType(resources.GetObject("AxTChart1.OcxState"), System.Windows.Forms.AxHost.State)
        Me.AxTChart1.Size = New System.Drawing.Size(623, 91)
        Me.AxTChart1.TabIndex = 7
        '
        'HashTaskWindow
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(645, 489)
        Me.ContextMenuStrip = Me.ContextMenuStrip1
        Me.Controls.Add(Me.ProgressBar2)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.CheckBox1)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.ProgressBar1)
        Me.Controls.Add(Me.TextBox1)
        Me.Controls.Add(Me.AxTChart1)
        Me.Name = "HashTaskWindow"
        Me.Text = "HashTaskWindow"
        Me.ContextMenuStrip1.ResumeLayout(False)
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
End Class
